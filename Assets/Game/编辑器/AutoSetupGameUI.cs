#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;

/// <summary>
/// 自动检查当前场景是否缺少 UI 结构，缺少则自动创建。
/// 如果检测到重复对象（如多个 ColorPickPanel），会自动清理。
/// 创建后所有对象都在 Hierarchy 中，直接选中编辑即可。
/// </summary>
[InitializeOnLoad]
public static class AutoSetupGameUI
{
    static AutoSetupGameUI()
    {
        EditorApplication.delayCall += CheckAndSetup;
        EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += CheckAndSetup;
    }

    [MenuItem("Star Express/自动设置 Game UI")]
    private static void RunAutoSetup()
    {
        CheckAndSetup();
    }

    private static void CheckAndSetup()
    {
        if (Application.isPlaying) return;
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().rootCount == 0) return;

        bool changed = false;
        changed |= CleanupDuplicates();
        changed |= EnsureEventSystem();
        var canvas = EnsureGameCanvas(ref changed);
        if (canvas == null) return;
        changed |= EnsureUIManager(canvas.gameObject);
        changed |= EnsureGameHUD(canvas.transform);
        changed |= EnsureResourcePanel(canvas.transform);
        changed |= EnsureColorPickPanel(canvas.transform);
        changed |= EnsureShipPlacementFanPanelHeight(canvas.transform);
        changed |= EnsureCarriagePlacementPanel(canvas.transform);
        changed |= EnsureWeekRewardSelectionPopup(canvas.transform);
        changed |= EnsureGameOverPopup(canvas.transform);
        changed |= EnsureBackgroundCanvas();

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[Star Express] UI 结构已就绪。展开 GameCanvas 即可直接编辑各面板。");
        }
    }

    /// <summary>清理 GameCanvas 下重复的面板——只保留第一个，删除后续重复。</summary>
    private static bool CleanupDuplicates()
    {
        var canvasGo = GameObject.Find("GameCanvas");
        if (canvasGo == null) return false;

        bool cleaned = false;
        cleaned |= RemoveDuplicateChildren(canvasGo.transform, "GameHUD");
        cleaned |= RemoveDuplicateChildren(canvasGo.transform, "ResourcePanel");
        cleaned |= RemoveDuplicateChildren(canvasGo.transform, "ColorPickPanel");
        cleaned |= RemoveDuplicateChildren(canvasGo.transform, "CarriagePlacementPanel");
        cleaned |= RemoveDuplicateChildren(canvasGo.transform, "WeekRewardPopup");
        cleaned |= RemoveDuplicateChildren(canvasGo.transform, "WeekRewardSelectionPopup");
        cleaned |= RemoveLegacyWeekRewardPopup(canvasGo.transform);
        cleaned |= RemoveDuplicateChildren(canvasGo.transform, "GameOverPopup");
        cleaned |= RemoveDuplicateChildren(canvasGo.transform, "EventSystem");

        // 场景根级别的 EventSystem 只保留一个
        var allES = Object.FindObjectsOfType<EventSystem>();
        for (int i = 1; i < allES.Length; i++)
        {
            Undo.DestroyObjectImmediate(allES[i].gameObject);
            cleaned = true;
        }

        return cleaned;
    }

    private static bool RemoveLegacyWeekRewardPopup(Transform parent)
    {
        var old = parent.Find("WeekRewardPopup");
        if (old == null) return false;
        if (parent.Find("WeekRewardSelectionPopup") == null) return false;
        Undo.DestroyObjectImmediate(old.gameObject);
        return true;
    }

    private static bool RemoveDuplicateChildren(Transform parent, string childName)
    {
        bool found = false;
        bool removed = false;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i);
            if (child.name == childName)
            {
                if (found)
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                    removed = true;
                }
                found = true;
            }
        }
        return removed;
    }

    private static bool EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null) return false;
        var es = new GameObject("EventSystem");
        Undo.RegisterCreatedObjectUndo(es, "Auto Setup UI");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
        return true;
    }

    private static Canvas EnsureGameCanvas(ref bool changed)
    {
        var go = GameObject.Find("GameCanvas");
        if (go != null) return go.GetComponent<Canvas>();

        go = new GameObject("GameCanvas");
        Undo.RegisterCreatedObjectUndo(go, "Auto Setup UI");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        changed = true;
        return canvas;
    }

    private static bool EnsureUIManager(GameObject canvasGo)
    {
        if (Object.FindObjectOfType<UIManager>() != null) return false;
        Undo.AddComponent<UIManager>(canvasGo);
        return true;
    }

    private static bool EnsureGameHUD(Transform parent)
    {
        if (parent.Find("GameHUD") != null) return false;
        if (Object.FindObjectOfType<GameHUD>() != null) return false;

        var panel = MakeRect(parent, "GameHUD");
        var pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(0, 1);
        pr.anchorMax = new Vector2(1, 1);
        pr.pivot = new Vector2(0.5f, 1);
        pr.anchoredPosition = Vector2.zero;
        pr.sizeDelta = new Vector2(0, 48);
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.03f, 0.05f, 0.09f, 0.96f);
        var hudOutline = panel.AddComponent<Outline>();
        hudOutline.effectColor = new Color(0.15f, 0.18f, 0.25f, 0.5f);
        hudOutline.effectDistance = new Vector2(0, -1);

        const float pad = 20f, gap = 16f, w = 100f, h = 24f;
        float x = pad;
        var score = MakeText(panel.transform, "ScoreText", "得分: 0", new Vector2(x, 0), new Vector2(w, h));
        score.fontSize = 18;
        score.color = new Color(0.95f, 0.95f, 1f);
        x += w + gap;
        var ship = MakeText(panel.transform, "ShipCountText", "飞船: 0", new Vector2(x, 0), new Vector2(w, h));
        x += w + gap;
        var carriage = MakeText(panel.transform, "CarriageCountText", "客舱: 0", new Vector2(x, 0), new Vector2(w, h));
        x += w + gap;
        var starTunnel = MakeText(panel.transform, "StarTunnelCountText", "星隧: 0", new Vector2(x, 0), new Vector2(w, h));
        x += w + gap * 2;
        var weekCount = MakeText(panel.transform, "WeekCountdownText", "下周: 1:00", new Vector2(x, 0), new Vector2(110, h));

        var comp = panel.AddComponent<GameHUD>();
        var so = new SerializedObject(comp);
        so.FindProperty("scoreText").objectReferenceValue = score;
        so.FindProperty("shipCountText").objectReferenceValue = ship;
        so.FindProperty("carriageCountText").objectReferenceValue = carriage;
        so.FindProperty("starTunnelCountText").objectReferenceValue = starTunnel;
        so.FindProperty("weekCountdownText").objectReferenceValue = weekCount;
        so.ApplyModifiedProperties();
        return true;
    }

    private static Text MakeText(Transform parent, string name, string content, Vector2 pos, Vector2 size)
    {
        var go = MakeRect(parent, name);
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(0, 0.5f);
        r.pivot = new Vector2(0, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = size;
        var t = go.AddComponent<Text>();
        t.text = content;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 18;
        t.color = Color.white;
        return t;
    }

    private static bool EnsureResourcePanel(Transform parent)
    {
        var existing = parent.Find("ResourcePanel");
        if (existing != null)
        {
            if (!existing.gameObject.activeSelf) existing.gameObject.SetActive(true);
            return false;
        }
        if (Object.FindObjectOfType<ResourcePanel>() != null) return false;

        // 移除旧的直接子节点 ShipPlacementPanel（已迁移到 ResourcePanel 内）
        var legacy = parent.Find("ShipPlacementPanel");
        if (legacy != null)
        {
            Undo.DestroyObjectImmediate(legacy.gameObject);
        }

        const float blockHeight = 64f;
        const float spacing = 6f;

        var resourcePanel = MakeRect(parent, "ResourcePanel");
        var rpr = resourcePanel.GetComponent<RectTransform>();
        rpr.anchorMin = new Vector2(0, 0.5f);
        rpr.anchorMax = new Vector2(0, 0.5f);
        rpr.pivot = new Vector2(0, 0.5f);
        rpr.anchoredPosition = new Vector2(12, 0);
        rpr.sizeDelta = new Vector2(112, blockHeight * 3 + spacing * 2 + 24);
        var rpBg = resourcePanel.AddComponent<Image>();
        rpBg.color = new Color(0.04f, 0.06f, 0.1f, 0.92f);
        rpBg.raycastTarget = true;
        var rpOutline = resourcePanel.AddComponent<Outline>();
        rpOutline.effectColor = new Color(0.2f, 0.25f, 0.35f, 0.6f);
        rpOutline.effectDistance = new Vector2(1, -1);

        var title = MakeText(resourcePanel.transform, "Title", "资源栏", new Vector2(0, 10), new Vector2(100, 18));
        title.alignment = TextAnchor.MiddleCenter;
        title.fontSize = 13;
        title.fontStyle = FontStyle.Bold;
        title.color = new Color(0.75f, 0.82f, 0.95f);

        var shipPanel = MakeShipPlacementPanel(resourcePanel.transform, "飞船", "点击选线放置");
        var upgradePanel = MakeShipUpgradePanel(resourcePanel.transform, "客舱", "点击飞船升级容量");
        var starTunnelPanel = MakeStarTunnelPanel(resourcePanel.transform, "星隧", "周奖励获得");

        SetChildLayoutForVerticalStack(shipPanel.GetComponent<RectTransform>(), 0, blockHeight, spacing, -18);
        SetChildLayoutForVerticalStack(upgradePanel.GetComponent<RectTransform>(), 1, blockHeight, spacing, -18);
        SetChildLayoutForVerticalStack(starTunnelPanel.GetComponent<RectTransform>(), 2, blockHeight, spacing, -18);

        var resourceComp = resourcePanel.AddComponent<ResourcePanel>();
        var rso = new SerializedObject(resourceComp);
        rso.FindProperty("shipPlacementPanel").objectReferenceValue = shipPanel.GetComponent<ShipPlacementPanel>();
        rso.FindProperty("shipUpgradePanel").objectReferenceValue = upgradePanel.GetComponent<ShipUpgradePanel>();
        rso.FindProperty("starTunnelPanel").objectReferenceValue = starTunnelPanel.GetComponent<StarTunnelPanel>();
        rso.ApplyModifiedProperties();
        // ResourcePanel 始终显示
        return true;
    }

    private static GameObject MakeShipPlacementPanel(Transform parent, string label, string hint)
    {
        const float circleSize = 56f;
        const float spacing = 4f;
        const float countWidth = 32f;

        var panel = MakeRect(parent, "ShipPlacementPanel");
        var pr = panel.GetComponent<RectTransform>();
        pr.sizeDelta = new Vector2(circleSize + spacing + countWidth, 64);
        var le = panel.AddComponent<LayoutElement>();
        le.preferredWidth = circleSize + spacing + countWidth;
        le.preferredHeight = 64;

        var circle = MakeRect(panel.transform, "CircleButton");
        var cr = circle.GetComponent<RectTransform>();
        cr.anchorMin = cr.anchorMax = new Vector2(0, 0.5f);
        cr.pivot = new Vector2(0.5f, 0.5f);
        cr.anchoredPosition = new Vector2(circleSize * 0.5f, 0);
        cr.sizeDelta = new Vector2(circleSize, circleSize);
        var cImg = circle.AddComponent<Image>();
        cImg.color = new Color(0.35f, 0.52f, 0.78f);
        var cOutline = circle.AddComponent<Outline>();
        cOutline.effectColor = new Color(0.5f, 0.65f, 0.9f, 0.4f);
        cOutline.effectDistance = new Vector2(0, -1);
        AddButtonClickAnim(circle.AddComponent<Button>());

        var count = MakeRect(panel.transform, "CountText");
        var nr = count.GetComponent<RectTransform>();
        nr.anchorMin = nr.anchorMax = new Vector2(1, 0.5f);
        nr.pivot = new Vector2(0, 0.5f);
        nr.anchoredPosition = new Vector2(circleSize + spacing, 0);
        nr.sizeDelta = new Vector2(countWidth, circleSize * 0.7f);
        var txt = count.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 22;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.text = "0";

        var fan = MakeRect(panel.transform, "FanPanel");
        var fr = fan.GetComponent<RectTransform>();
        fr.anchorMin = fr.anchorMax = new Vector2(1, 0.5f);
        fr.pivot = new Vector2(0, 0.5f);
        fr.anchoredPosition = new Vector2(circleSize + spacing, 0);
        fr.sizeDelta = new Vector2(110, 220);
        var fImg = fan.AddComponent<Image>();
        fImg.color = new Color(0.08f, 0.1f, 0.14f, 0.97f);
        var fOutline = fan.AddComponent<Outline>();
        fOutline.effectColor = new Color(0.25f, 0.3f, 0.4f, 0.5f);
        fOutline.effectDistance = new Vector2(1, -1);
        fan.SetActive(false);

        var fanContent = MakeRect(fan.transform, "FanContent");
        var fcr = fanContent.GetComponent<RectTransform>();
        fcr.anchorMin = Vector2.zero;
        fcr.anchorMax = Vector2.one;
        fcr.offsetMin = new Vector2(6, 6);
        fcr.offsetMax = new Vector2(-6, -6);
        var vl = fanContent.AddComponent<VerticalLayoutGroup>();
        vl.spacing = 4;
        vl.childForceExpandHeight = false;
        vl.childControlHeight = true;
        vl.childAlignment = TextAnchor.UpperCenter;

        var comp = panel.AddComponent<ShipPlacementPanel>();
        var so = new SerializedObject(comp);
        so.FindProperty("panelRoot").objectReferenceValue = panel;
        so.FindProperty("circleButton").objectReferenceValue = circle.GetComponent<Button>();
        so.FindProperty("countText").objectReferenceValue = txt;
        so.FindProperty("fanPanelRoot").objectReferenceValue = fan;
        so.ApplyModifiedProperties();
        var lbl = MakeText(panel.transform, "Label", label, new Vector2(0, -circleSize * 0.4f), new Vector2(90, 14));
        lbl.alignment = TextAnchor.MiddleCenter;
        lbl.fontSize = 11;
        lbl.color = new Color(0.6f, 0.65f, 0.75f);
        var hintGo = MakeText(panel.transform, "Hint", hint, new Vector2(0, -circleSize * 0.65f), new Vector2(90, 12));
        hintGo.alignment = TextAnchor.MiddleCenter;
        hintGo.fontSize = 9;
        hintGo.color = new Color(0.45f, 0.5f, 0.58f);
        return panel;
    }

    private static GameObject MakeShipUpgradePanel(Transform parent, string label, string hint)
    {
        const float circleSize = 56f;
        const float spacing = 4f;
        const float countWidth = 32f;

        var panel = MakeRect(parent, "ShipUpgradePanel");
        var pr = panel.GetComponent<RectTransform>();
        pr.sizeDelta = new Vector2(circleSize + spacing + countWidth, 64);
        var le = panel.AddComponent<LayoutElement>();
        le.preferredWidth = circleSize + spacing + countWidth;
        le.preferredHeight = 64;

        var circle = MakeRect(panel.transform, "CircleButton");
        var cr = circle.GetComponent<RectTransform>();
        cr.anchorMin = cr.anchorMax = new Vector2(0, 0.5f);
        cr.pivot = new Vector2(0.5f, 0.5f);
        cr.anchoredPosition = new Vector2(circleSize * 0.5f, 0);
        cr.sizeDelta = new Vector2(circleSize, circleSize);
        var cImg = circle.AddComponent<Image>();
        cImg.color = new Color(0.42f, 0.62f, 0.38f);
        var cOutline = circle.AddComponent<Outline>();
        cOutline.effectColor = new Color(0.55f, 0.75f, 0.5f, 0.35f);
        cOutline.effectDistance = new Vector2(0, -1);
        AddButtonClickAnim(circle.AddComponent<Button>());

        var count = MakeRect(panel.transform, "CountText");
        var nr = count.GetComponent<RectTransform>();
        nr.anchorMin = nr.anchorMax = new Vector2(1, 0.5f);
        nr.pivot = new Vector2(0, 0.5f);
        nr.anchoredPosition = new Vector2(circleSize + spacing, 0);
        nr.sizeDelta = new Vector2(countWidth, circleSize * 0.7f);
        var txt = count.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 22;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.text = "0";

        var comp = panel.AddComponent<ShipUpgradePanel>();
        var so = new SerializedObject(comp);
        so.FindProperty("panelRoot").objectReferenceValue = panel;
        so.FindProperty("circleButton").objectReferenceValue = circle.GetComponent<Button>();
        so.FindProperty("countText").objectReferenceValue = txt;
        so.ApplyModifiedProperties();
        var lbl = MakeText(panel.transform, "Label", label, new Vector2(0, -circleSize * 0.4f), new Vector2(90, 14));
        lbl.alignment = TextAnchor.MiddleCenter;
        lbl.fontSize = 11;
        lbl.color = new Color(0.6f, 0.65f, 0.75f);
        var hintGo = MakeText(panel.transform, "Hint", hint, new Vector2(0, -circleSize * 0.65f), new Vector2(90, 12));
        hintGo.alignment = TextAnchor.MiddleCenter;
        hintGo.fontSize = 9;
        hintGo.color = new Color(0.45f, 0.5f, 0.58f);
        return panel;
    }

    private static GameObject MakeStarTunnelPanel(Transform parent, string label, string hint)
    {
        const float circleSize = 56f;
        const float spacing = 4f;
        const float countWidth = 32f;

        var panel = MakeRect(parent, "StarTunnelPanel");
        var pr = panel.GetComponent<RectTransform>();
        pr.sizeDelta = new Vector2(circleSize + spacing + countWidth, 64);
        var le = panel.AddComponent<LayoutElement>();
        le.preferredWidth = circleSize + spacing + countWidth;
        le.preferredHeight = 64;

        var area = MakeRect(panel.transform, "Area");
        var ar = area.GetComponent<RectTransform>();
        ar.anchorMin = ar.anchorMax = new Vector2(0, 0.5f);
        ar.pivot = new Vector2(0.5f, 0.5f);
        ar.anchoredPosition = new Vector2(circleSize * 0.5f, 0);
        ar.sizeDelta = new Vector2(circleSize, circleSize);
        var aImg = area.AddComponent<Image>();
        aImg.color = new Color(0.55f, 0.45f, 0.72f);
        var aOutline = area.AddComponent<Outline>();
        aOutline.effectColor = new Color(0.7f, 0.6f, 0.85f, 0.3f);
        aOutline.effectDistance = new Vector2(0, -1);

        var count = MakeRect(panel.transform, "CountText");
        var nr = count.GetComponent<RectTransform>();
        nr.anchorMin = nr.anchorMax = new Vector2(1, 0.5f);
        nr.pivot = new Vector2(0, 0.5f);
        nr.anchoredPosition = new Vector2(circleSize + spacing, 0);
        nr.sizeDelta = new Vector2(countWidth, circleSize * 0.7f);
        var txt = count.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 22;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.text = "0";

        var comp = panel.AddComponent<StarTunnelPanel>();
        var so = new SerializedObject(comp);
        so.FindProperty("panelRoot").objectReferenceValue = panel;
        so.FindProperty("areaRect").objectReferenceValue = ar;
        so.FindProperty("countText").objectReferenceValue = txt;
        so.ApplyModifiedProperties();
        var lbl = MakeText(panel.transform, "Label", label, new Vector2(0, -circleSize * 0.4f), new Vector2(90, 14));
        lbl.alignment = TextAnchor.MiddleCenter;
        lbl.fontSize = 11;
        lbl.color = new Color(0.6f, 0.65f, 0.75f);
        var hintGo = MakeText(panel.transform, "Hint", hint, new Vector2(0, -circleSize * 0.65f), new Vector2(90, 12));
        hintGo.alignment = TextAnchor.MiddleCenter;
        hintGo.fontSize = 9;
        hintGo.color = new Color(0.45f, 0.5f, 0.58f);
        return panel;
    }

    private static bool EnsureColorPickPanel(Transform parent)
    {
        var existing = parent.Find("ColorPickPanel");
        if (existing != null)
        {
            EnsureColorPickPanelSixButtons(existing);
            return false;
        }
        if (Object.FindObjectOfType<ColorPickPanel>() != null) return false;

        var panel = MakeRect(parent, "ColorPickPanel");
        var pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(320, 180);
        pr.anchoredPosition = Vector2.zero;
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.1f, 0.14f, 0.97f);
        bg.raycastTarget = true;

        const float bw = 56f, gap = 12f;
        float row1Y = 25f, row2Y = -25f, cancelY = -70f;
        var btnR = MakeButton(panel.transform, "Button_红", "红", Color.red, new Vector2(-bw - gap, row1Y));
        var btnG = MakeButton(panel.transform, "Button_绿", "绿", Color.green, new Vector2(0, row1Y));
        var btnB = MakeButton(panel.transform, "Button_蓝", "蓝", Color.blue, new Vector2(bw + gap, row1Y));
        var btnY = MakeButton(panel.transform, "Button_黄", "黄", Color.yellow, new Vector2(-bw - gap, row2Y));
        var btnCy = MakeButton(panel.transform, "Button_青", "青", Color.cyan, new Vector2(0, row2Y));
        var btnM = MakeButton(panel.transform, "Button_品", "品", Color.magenta, new Vector2(bw + gap, row2Y));
        var btnC = MakeButton(panel.transform, "Button_取消", "取消", new Color(0.5f, 0.5f, 0.5f), new Vector2(0, cancelY));
        AddButtonClickAnim(btnR, btnG, btnB, btnY, btnCy, btnM, btnC);

        var comp = panel.AddComponent<ColorPickPanel>();
        var so = new SerializedObject(comp);
        so.FindProperty("panelRoot").objectReferenceValue = panel;
        so.FindProperty("buttonRed").objectReferenceValue = btnR;
        so.FindProperty("buttonGreen").objectReferenceValue = btnG;
        so.FindProperty("buttonBlue").objectReferenceValue = btnB;
        so.FindProperty("buttonYellow").objectReferenceValue = btnY;
        so.FindProperty("buttonCyan").objectReferenceValue = btnCy;
        so.FindProperty("buttonMagenta").objectReferenceValue = btnM;
        so.FindProperty("buttonCancel").objectReferenceValue = btnC;
        so.ApplyModifiedProperties();

        panel.AddComponent<PopupShowAnim>();
        panel.SetActive(false);
        return true;
    }

    private const string BackgroundSpritePath = "Assets/Game/美术/Photos/Blue Nebula/Blue_Nebula_01-512x512.png";

    private static bool EnsureBackgroundCanvas()
    {
        var cam = Object.FindObjectOfType<Camera>();
        if (cam == null || !cam.CompareTag("MainCamera"))
        {
            var cams = Object.FindObjectsOfType<Camera>();
            foreach (var c in cams) { if (c.CompareTag("MainCamera")) { cam = c; break; } }
            if (cam == null && cams.Length > 0) cam = cams[0];
        }
        if (cam == null) return false;

        RemoveLegacyBackgroundCanvas(cam);

        var existing = GameObject.Find("Background");
        if (existing != null)
        {
            var sr = existing.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                bool changed = false;
                if (sr.sprite == null)
                {
                    var sp = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundSpritePath);
                    if (sp != null) { sr.sprite = sp; changed = true; }
                }
                if (sr.sortingLayerID != SortingOrderConstants.ShipsLayerId) { sr.sortingLayerID = SortingOrderConstants.ShipsLayerId; changed = true; }
                if (sr.sortingOrder != SortingOrderConstants.Background) { sr.sortingOrder = SortingOrderConstants.Background; changed = true; }
                return changed;
            }
            return false;
        }

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundSpritePath);
        if (sprite == null) return false;

        var go = new GameObject("Background");
        Undo.RegisterCreatedObjectUndo(go, "Auto Setup UI");
        var sr2 = go.AddComponent<SpriteRenderer>();
        sr2.sprite = sprite;
        sr2.color = Color.white;
        sr2.sortingLayerID = SortingOrderConstants.ShipsLayerId;
        sr2.sortingOrder = SortingOrderConstants.Background;

        go.AddComponent<BackgroundCameraFollow>();
        go.transform.position = new Vector3(0, 0, 10f);

        var map = GameObject.Find("Map");
        if (map != null) go.transform.SetParent(map.transform, true);

        RenderSettings.skybox = null;
        return true;
    }

    private static void RemoveLegacyBackgroundCanvas(Camera cam)
    {
        var old = cam.transform.Find("BackgroundCanvas");
        if (old != null) { Undo.DestroyObjectImmediate(old.gameObject); }
    }

    private static void AddButtonClickAnim(params Button[] buttons)
    {
        foreach (var b in buttons)
            if (b != null && b.GetComponent<ButtonClickAnim>() == null)
                b.gameObject.AddComponent<ButtonClickAnim>();
    }

    private static bool EnsureShipPlacementFanPanelHeight(Transform parent)
    {
        var rp = parent.Find("ResourcePanel");
        if (rp == null) return false;
        var spp = rp.Find("ShipPlacementPanel");
        if (spp == null) return false;
        var fan = spp.Find("FanPanel");
        if (fan == null) return false;
        var fr = fan.GetComponent<RectTransform>();
        if (fr == null || fr.sizeDelta.y >= 200f) return false;
        fr.sizeDelta = new Vector2(110, 220);
        return true;
    }

    private static void EnsureColorPickPanelSixButtons(Transform panel)
    {
        if (panel.Find("Button_黄") != null)
        {
            RepositionColorPickButtons(panel);
            return;
        }
        const float bw = 56f, gap = 12f;
        var btnY = MakeButton(panel.transform, "Button_黄", "黄", Color.yellow, new Vector2(-bw - gap, -25));
        var btnCy = MakeButton(panel.transform, "Button_青", "青", Color.cyan, new Vector2(0, -25));
        var btnM = MakeButton(panel.transform, "Button_品", "品", Color.magenta, new Vector2(bw + gap, -25));
        RepositionColorPickButtons(panel);
        var comp = panel.GetComponent<ColorPickPanel>();
        if (comp != null)
        {
            var so = new SerializedObject(comp);
            so.FindProperty("buttonYellow").objectReferenceValue = btnY;
            so.FindProperty("buttonCyan").objectReferenceValue = btnCy;
            so.FindProperty("buttonMagenta").objectReferenceValue = btnM;
            so.ApplyModifiedProperties();
        }
    }

    /// <summary>统一重排选色面板按钮位置，保证 3 色/6 色布局整齐不重叠。</summary>
    private static void RepositionColorPickButtons(Transform panel)
    {
        const float bw = 56f, gap = 12f;
        float row1Y = 25f, row2Y = -25f, cancelY = -70f;
        var posMap = new System.Collections.Generic.Dictionary<string, Vector2>
        {
            { "Button_红", new Vector2(-bw - gap, row1Y) },
            { "Button_绿", new Vector2(0, row1Y) },
            { "Button_蓝", new Vector2(bw + gap, row1Y) },
            { "Button_黄", new Vector2(-bw - gap, row2Y) },
            { "Button_青", new Vector2(0, row2Y) },
            { "Button_品", new Vector2(bw + gap, row2Y) },
            { "Button_取消", new Vector2(0, cancelY) }
        };
        for (int i = 0; i < panel.childCount; i++)
        {
            var child = panel.GetChild(i);
            if (posMap.TryGetValue(child.name, out var pos))
            {
                var rt = child.GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition = pos;
            }
        }
    }

    private static bool EnsureCarriagePlacementPanel(Transform parent)
    {
        if (parent.Find("CarriagePlacementPanel") != null) return false;
        if (Object.FindObjectOfType<CarriagePlacementPanel>() != null) return false;

        var panel = MakeRect(parent, "CarriagePlacementPanel");
        var pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(0.5f, 0);
        pr.anchorMax = new Vector2(0.5f, 0);
        pr.pivot = new Vector2(0.5f, 0);
        pr.anchoredPosition = new Vector2(0, 80);
        pr.sizeDelta = new Vector2(280, 80);
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.11f, 0.15f, 0.95f);

        var hint = MakeText(panel.transform, "HintText", "点击地图上的飞船 → 升级容量 +2\n（按 Esc 取消）", new Vector2(0, 20), new Vector2(260, 44));
        hint.alignment = TextAnchor.MiddleCenter;
        hint.fontSize = 16;
        var btn = MakeRect(panel.transform, "CancelButton");
        btn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -15);
        btn.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 28);
        btn.AddComponent<Image>().color = new Color(0.5f, 0.3f, 0.3f);
        var b = btn.AddComponent<Button>();
        MakeText(btn.transform, "Text", "取消", Vector2.zero, new Vector2(60, 22)).alignment = TextAnchor.MiddleCenter;

        var comp = panel.AddComponent<CarriagePlacementPanel>();
        var so = new SerializedObject(comp);
        so.FindProperty("cancelButton").objectReferenceValue = b;
        so.ApplyModifiedProperties();
        AddButtonClickAnim(b);
        panel.AddComponent<PopupShowAnim>();
        panel.SetActive(false);
        return true;
    }

    private static bool EnsureWeekRewardSelectionPopup(Transform parent)
    {
        if (parent.Find("WeekRewardSelectionPopup") != null) return false;
        if (Object.FindObjectOfType<WeekRewardSelectionPopup>() != null) return false;

        var panel = MakeRect(parent, "WeekRewardSelectionPopup");
        var pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(420, 280);
        pr.anchoredPosition = Vector2.zero;
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.08f, 0.12f, 0.98f);
        bg.raycastTarget = true;

        var weekT = MakeText(panel.transform, "WeekText", "第 1 周", new Vector2(0, 80), new Vector2(380, 36));
        weekT.alignment = TextAnchor.MiddleCenter;
        weekT.fontSize = 26;
        var hintT = MakeText(panel.transform, "HintText", "选择 1 项奖励", new Vector2(0, 40), new Vector2(380, 24));
        hintT.alignment = TextAnchor.MiddleCenter;

        float optY = -20, optW = 140, optH = 50, gap = 20;
        var opt1 = MakeRect(panel.transform, "Option1");
        opt1.GetComponent<RectTransform>().anchoredPosition = new Vector2(-optW * 0.5f - gap * 0.5f, optY);
        opt1.GetComponent<RectTransform>().sizeDelta = new Vector2(optW, optH);
        opt1.AddComponent<Image>().color = new Color(0.25f, 0.3f, 0.38f);
        var o1 = opt1.AddComponent<Button>();
        var o1Label = MakeText(opt1.transform, "Label", "客舱", Vector2.zero, new Vector2(120, 40));
        o1Label.alignment = TextAnchor.MiddleCenter;

        var opt2 = MakeRect(panel.transform, "Option2");
        opt2.GetComponent<RectTransform>().anchoredPosition = new Vector2(optW * 0.5f + gap * 0.5f, optY);
        opt2.GetComponent<RectTransform>().sizeDelta = new Vector2(optW, optH);
        opt2.AddComponent<Image>().color = new Color(0.25f, 0.3f, 0.38f);
        var o2 = opt2.AddComponent<Button>();
        var o2Label = MakeText(opt2.transform, "Label", "星隧", Vector2.zero, new Vector2(120, 40));
        o2Label.alignment = TextAnchor.MiddleCenter;

        var comp = panel.AddComponent<WeekRewardSelectionPopup>();
        var so = new SerializedObject(comp);
        so.FindProperty("weekText").objectReferenceValue = weekT;
        so.FindProperty("hintText").objectReferenceValue = hintT;
        so.FindProperty("option1Button").objectReferenceValue = o1;
        so.FindProperty("option1Label").objectReferenceValue = o1Label;
        so.FindProperty("option2Button").objectReferenceValue = o2;
        so.FindProperty("option2Label").objectReferenceValue = o2Label;
        so.ApplyModifiedProperties();
        AddButtonClickAnim(o1, o2);
        panel.AddComponent<PopupShowAnim>();
        panel.SetActive(false);
        return true;
    }

    private static bool EnsureGameOverPopup(Transform parent)
    {
        if (parent.Find("GameOverPopup") != null) return false;
        if (Object.FindObjectOfType<GameOverPopup>() != null) return false;

        var panel = MakeRect(parent, "GameOverPopup");
        var pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(380, 240);
        pr.anchoredPosition = Vector2.zero;
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.06f, 0.08f, 0.98f);
        bg.raycastTarget = true;

        var title = MakeText(panel.transform, "TitleText", "游戏结束", new Vector2(0, 70), new Vector2(320, 36));
        title.alignment = TextAnchor.MiddleCenter;
        title.fontSize = 24;
        var scoreT = MakeText(panel.transform, "ScoreText", "得分: 0", new Vector2(0, 30), new Vector2(320, 28));
        scoreT.alignment = TextAnchor.MiddleCenter;
        var reasonT = MakeText(panel.transform, "ReasonText", "站点拥挤超阈值", new Vector2(0, -10), new Vector2(320, 40));
        reasonT.alignment = TextAnchor.MiddleCenter;

        var retryBtn = MakeRect(panel.transform, "RetryButton");
        retryBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-70, -70);
        retryBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 36);
        retryBtn.AddComponent<Image>().color = new Color(0.3f, 0.6f, 0.3f);
        var retry = retryBtn.AddComponent<Button>();
        MakeText(retryBtn.transform, "Text", "重试", Vector2.zero, new Vector2(80, 28)).alignment = TextAnchor.MiddleCenter;

        var backBtn = MakeRect(panel.transform, "BackButton");
        backBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(70, -70);
        backBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 36);
        backBtn.AddComponent<Image>().color = new Color(0.5f, 0.3f, 0.3f);
        var back = backBtn.AddComponent<Button>();
        MakeText(backBtn.transform, "Text", "返回", Vector2.zero, new Vector2(80, 28)).alignment = TextAnchor.MiddleCenter;

        var comp = panel.AddComponent<GameOverPopup>();
        var so = new SerializedObject(comp);
        so.FindProperty("scoreText").objectReferenceValue = scoreT;
        so.FindProperty("reasonText").objectReferenceValue = reasonT;
        so.FindProperty("retryButton").objectReferenceValue = retry;
        so.FindProperty("backButton").objectReferenceValue = back;
        so.ApplyModifiedProperties();
        AddButtonClickAnim(retry, back);
        panel.AddComponent<PopupShowAnim>();
        panel.SetActive(false);
        return true;
    }

    private static Button MakeButton(Transform parent, string name, string label, Color color, Vector2 pos)
    {
        var go = MakeRect(parent, name);
        var r = go.GetComponent<RectTransform>();
        r.sizeDelta = new Vector2(64, 36);
        r.anchoredPosition = pos;
        var img = go.AddComponent<Image>();
        img.color = color;
        var btn = go.AddComponent<Button>();

        var tGo = MakeRect(go.transform, "Text");
        var tr = tGo.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = tr.offsetMax = Vector2.zero;
        var t = tGo.AddComponent<Text>();
        t.text = label;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 18;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        return btn;
    }

    /// <summary>设置子块在父块内从上到下垂直排列，锚点顶部中心。offsetY 为顶部留白。</summary>
    private static void SetChildLayoutForVerticalStack(RectTransform child, int index, float blockHeight, float spacing = 8f, float offsetY = 0f)
    {
        if (child == null) return;
        child.anchorMin = new Vector2(0.5f, 1f);
        child.anchorMax = new Vector2(0.5f, 1f);
        child.pivot = new Vector2(0.5f, 1f);
        float y = offsetY - index * (blockHeight + spacing);
        child.anchoredPosition = new Vector2(0, y);
    }

    private static GameObject MakeRect(Transform parent, string name)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Auto Setup UI");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }
}
#endif

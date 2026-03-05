using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using Game.Scripts.UI;

/// <summary>
/// Editor script to create gameplay UI elements in the scene for easy editing.
/// Menu: Tools > Gameplay UI > Setup Game UI
/// </summary>
public static class GameplayUISetup
{
    public static void SetupGameUI()
    {
        EnsureGameCanvas();
        EnsurePauseButton();
        EnsureTimeSpeedPanel();
        EnsurePauseMenu();
        EnsureGameOverPopup();
        EnsureWeekRewardSelectionPopup();
        EnsureColorPickPanel();
        Debug.Log("[GameplayUISetup] Game UI setup complete!");
    }

    public static void EnsurePauseButton()
    {
        var canvas = EnsureGameCanvas();
        if (canvas == null) return;
        if (EnsurePauseButtonUnder(canvas.transform))
        {
            Selection.activeGameObject = GameObject.Find("PauseButton");
            Debug.Log("[GameplayUISetup] Created PauseButton");
            return;
        }
        var existing = GameObject.Find("PauseButton");
        if (existing != null) Selection.activeGameObject = existing;
    }

    /// <summary>Creates PauseButton under parent if missing. Returns true if created.</summary>
    public static bool EnsurePauseButtonUnder(Transform parent)
    {
        if (parent.Find("PauseButton") != null) return false;

        const float size = 64f;
        const float margin = 8f;
        var btn = CreateButton(parent, "PauseButton", "⏸", Vector2.zero, new Vector2(size, size), new Color(0.35f, 0.4f, 0.5f));
        Undo.RegisterCreatedObjectUndo(btn.gameObject, "Create Pause Button");
        var r = btn.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(1, 1);
        r.anchorMax = new Vector2(1, 1);
        r.pivot = new Vector2(1, 1);
        r.anchoredPosition = new Vector2(-margin, -margin);
        r.sizeDelta = new Vector2(size, size);
        btn.GetComponentInChildren<Text>().fontSize = 28;

        btn.gameObject.AddComponent<GameplayButtonHoverSound>();
        btn.gameObject.AddComponent<ButtonClickAnim>();

        btn.onClick.AddListener(() =>
        {
            GameplayAudio.Instance?.PlayGeneralClick();
            var gm = GameManager.Instance;
            if (gm == null || gm.IsGameOver || gm.IsPausedForWeekReward) return;
            var menu = Object.FindObjectOfType<PauseMenu>(true);
            if (menu != null)
            {
                if (gm.IsPausedByUser)
                {
                    gm.SetUserPaused(false);
                    menu.Hide();
                }
                else
                {
                    gm.SetUserPaused(true);
                    menu.Show();
                    menu.transform.SetAsLastSibling();
                }
            }
        });

        return true;
    }

    public static void AddMissingContinueButton()
    {
        if (AddContinueButtonIfMissing())
        {
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            var existing = Object.FindObjectOfType<PauseMenu>(true);
            if (existing != null && existing.continueButton != null)
                Selection.activeGameObject = existing.continueButton.gameObject;
            Debug.Log("[GameplayUISetup] Added ContinueButton to PauseMenu. Save the scene to persist.");
        }
        else
        {
            var existing = Object.FindObjectOfType<PauseMenu>(true);
            if (existing != null && existing.continueButton != null)
                Selection.activeGameObject = existing.continueButton.gameObject;
            Debug.Log("[GameplayUISetup] PauseMenu already has ContinueButton or no PauseMenu found.");
        }
    }

    /// <summary>Adds ContinueButton to PauseMenu if missing. Returns true if changes were made.</summary>
    public static bool AddContinueButtonIfMissing()
    {
        var existing = Object.FindObjectOfType<PauseMenu>(true);
        if (existing == null) return false;
        if (existing.continueButton != null) return false;
        if (existing.backToMenuButton == null) return false;
        AddContinueButtonToPauseMenuInEditor(existing);
        return true;
    }

    public static void EnsurePauseMenu()
    {
        var canvas = EnsureGameCanvas();
        if (canvas == null) return;

        var existing = Object.FindObjectOfType<PauseMenu>(true);
        if (existing != null)
        {
            if (existing.backToMenuButton == null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }
            else if (existing.continueButton == null)
            {
                AddContinueButtonToPauseMenuInEditor(existing);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                Selection.activeGameObject = existing.gameObject;
                Debug.Log("[GameplayUISetup] Added ContinueButton to existing PauseMenu. Save the scene to persist.");
                return;
            }
            else
            {
                Selection.activeGameObject = existing.gameObject;
                Debug.Log("[GameplayUISetup] PauseMenu already exists");
                return;
            }
        }

        var panel = new GameObject("PauseMenu");
        panel.transform.SetParent(canvas.transform, false);

        var pr = panel.AddComponent<RectTransform>();
        pr.anchorMin = Vector2.zero;
        pr.anchorMax = Vector2.one;
        pr.offsetMin = pr.offsetMax = Vector2.zero;

        var overlay = new GameObject("Overlay");
        overlay.transform.SetParent(panel.transform, false);
        var overlayRt = overlay.AddComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = overlayRt.offsetMax = Vector2.zero;
        var overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = new Color(0, 0, 0, 0.6f);
        overlayImg.raycastTarget = true;
        var overlayBtn = overlay.AddComponent<Button>();
        overlayBtn.transition = Selectable.Transition.None;
        overlayBtn.targetGraphic = overlayImg;

        var box = new GameObject("Box");
        box.transform.SetParent(panel.transform, false);
        var boxRt = box.AddComponent<RectTransform>();
        boxRt.anchorMin = boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.sizeDelta = new Vector2(360, 320);
        boxRt.anchoredPosition = Vector2.zero;

        var bg = box.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.1f, 0.14f, 0.95f);
        bg.raycastTarget = true;

        var title = CreateText(box.transform, "TitleText", "暂停", new Vector2(0, 120), new Vector2(280, 32), 24);

        float volY1 = 50, volY2 = 0, volW = 200, volH = 24;
        var musicLabel = CreateText(box.transform, "MusicLabel", "背景音乐", new Vector2(-volW * 0.5f - 50, volY1), new Vector2(80, 24), 16);
        musicLabel.alignment = TextAnchor.MiddleLeft;
        var musicSlider = CreateVolumeSlider(box.transform, "MusicSlider", new Vector2(0, volY1), new Vector2(volW, volH), "MusicVolume", 0.6f);

        var sfxLabel = CreateText(box.transform, "SFXLabel", "音效", new Vector2(-volW * 0.5f - 50, volY2), new Vector2(80, 24), 16);
        sfxLabel.alignment = TextAnchor.MiddleLeft;
        var sfxSlider = CreateVolumeSlider(box.transform, "SFXSlider", new Vector2(0, volY2), new Vector2(volW, volH), "SFXVolume", 0.7f);

        float btnY = -70, btnW = 120, btnH = 48, btnGap = 20;
        var continueBtn = CreateMenuStyleButton(box.transform, "ContinueButton", "继续", new Vector2(-btnW * 0.5f - btnGap * 0.5f, btnY), new Vector2(btnW, btnH));
        continueBtn.gameObject.AddComponent<GameplayButtonHoverSound>();

        var backBtn = CreateMenuStyleButton(box.transform, "BackToMenuButton", "返回主菜单", new Vector2(btnW * 0.5f + btnGap * 0.5f, btnY), new Vector2(btnW, btnH));
        backBtn.gameObject.AddComponent<GameplayButtonHoverSound>();

        var comp = panel.AddComponent<PauseMenu>();
        comp.musicSlider = musicSlider;
        comp.sfxSlider = sfxSlider;
        comp.continueButton = continueBtn;
        comp.backToMenuButton = backBtn;

        overlay.AddComponent<GameplayButtonHoverSound>();
        overlayBtn.onClick.AddListener(() =>
        {
            GameplayAudio.Instance?.PlayGeneralClick();
            var gm = GameManager.Instance;
            if (gm != null && gm.IsPausedByUser)
            {
                gm.SetUserPaused(false);
                comp.Hide();
            }
        });

        panel.AddComponent<PopupShowAnim>();
        panel.SetActive(false);

        Selection.activeGameObject = panel;
        Debug.Log("[GameplayUISetup] Created PauseMenu");
    }

    private static void AddContinueButtonToPauseMenuInEditor(PauseMenu pauseMenu)
    {
        var backBtn = pauseMenu.backToMenuButton;
        if (backBtn == null) return;

        Transform parent = backBtn.transform.parent;
        if (parent == null) return;

        var backRt = backBtn.GetComponent<RectTransform>();
        float btnW = backRt != null ? backRt.sizeDelta.x : 160f;
        float btnH = backRt != null ? backRt.sizeDelta.y : 48f;
        float btnGap = 20f;
        Vector2 backPos = backRt != null ? backRt.anchoredPosition : Vector2.zero;
        Vector2 continuePos = new Vector2(backPos.x - btnW - btnGap, backPos.y);

        var continueBtn = CreateMenuStyleButton(parent, "ContinueButton", "继续", continuePos, new Vector2(btnW, btnH));
        Undo.RegisterCreatedObjectUndo(continueBtn.gameObject, "Add Continue Button");
        var menuBtn = continueBtn.GetComponent<MenuButton>();
        if (menuBtn == null) menuBtn = continueBtn.gameObject.AddComponent<MenuButton>();
        var textComp = continueBtn.transform.Find("Text")?.GetComponent<Text>();
        if (textComp != null) menuBtn.buttonText = textComp;
        if (continueBtn.GetComponent<GameplayButtonHoverSound>() == null)
            continueBtn.gameObject.AddComponent<GameplayButtonHoverSound>();

        Undo.RecordObject(pauseMenu, "Assign Continue Button");
        pauseMenu.continueButton = continueBtn;
    }

    public static void EnsureGameOverPopup()
    {
        var canvas = EnsureGameCanvas();
        if (canvas == null) return;

        var existing = Object.FindObjectOfType<GameOverPopup>(true);
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            Debug.Log("[GameplayUISetup] GameOverPopup already exists");
            return;
        }

        var panel = new GameObject("GameOverPopup");
        panel.transform.SetParent(canvas.transform, false);

        var pr = panel.AddComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(380, 240);
        pr.anchoredPosition = Vector2.zero;

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.06f, 0.08f, 0.98f);
        bg.raycastTarget = true;

        var title = CreateText(panel.transform, "TitleText", "游戏结束", new Vector2(0, 70), new Vector2(320, 36), 24);
        var scoreT = CreateText(panel.transform, "ScoreText", "得分: 0", new Vector2(0, 30), new Vector2(320, 28), 18);
        var reasonT = CreateText(panel.transform, "ReasonText", "站点拥挤超阈值", new Vector2(0, -10), new Vector2(320, 40), 16);

        var retryBtn = CreateButton(panel.transform, "RetryButton", "重试", new Vector2(-70, -70), new Vector2(100, 36), new Color(0.3f, 0.6f, 0.3f));
        var backBtn = CreateButton(panel.transform, "BackButton", "返回", new Vector2(70, -70), new Vector2(100, 36), new Color(0.5f, 0.3f, 0.3f));

        var comp = panel.AddComponent<GameOverPopup>();
        comp.scoreText = scoreT;
        comp.reasonText = reasonT;
        comp.retryButton = retryBtn;
        comp.backButton = backBtn;

        AddButtonClickAnim(retryBtn, backBtn);
        retryBtn.gameObject.AddComponent<GameplayButtonHoverSound>();
        backBtn.gameObject.AddComponent<GameplayButtonHoverSound>();
        panel.AddComponent<PopupShowAnim>();
        panel.SetActive(false);

        Selection.activeGameObject = panel;
        Debug.Log("[GameplayUISetup] Created GameOverPopup");
    }

    public static void EnsureWeekRewardSelectionPopup()
    {
        var canvas = EnsureGameCanvas();
        if (canvas == null) return;

        var existing = Object.FindObjectOfType<WeekRewardSelectionPopup>(true);
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            Debug.Log("[GameplayUISetup] WeekRewardSelectionPopup already exists");
            return;
        }

        var panel = new GameObject("WeekRewardSelectionPopup");
        panel.transform.SetParent(canvas.transform, false);

        var pr = panel.AddComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.anchoredPosition = Vector2.zero;

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.08f, 0.12f, 0.98f);
        bg.raycastTarget = true;

        var layout = new Vector2(480, 400);
        var btnSize = new Vector2(180, 200);
        var gap = 30f;
        pr.sizeDelta = layout;

        var weekT = CreateText(panel.transform, "WeekText", "第 1 周", new Vector2(0, layout.y * 0.35f), new Vector2(layout.x - 40, 36), 26);
        var hintT = CreateText(panel.transform, "HintText", "选择 1 项奖励", new Vector2(0, layout.y * 0.22f), new Vector2(layout.x - 40, 24), 18);

        float optY = -20;
        var (opt1, icon1) = CreateRewardOptionCard(panel.transform, "Option1", new Vector2(-btnSize.x * 0.5f - gap * 0.5f, optY), btnSize);
        var (opt2, icon2) = CreateRewardOptionCard(panel.transform, "Option2", new Vector2(btnSize.x * 0.5f + gap * 0.5f, optY), btnSize);

        var comp = panel.AddComponent<WeekRewardSelectionPopup>();
        comp.weekText = weekT;
        comp.hintText = hintT;
        comp.option1Button = opt1;
        comp.option1Icon = icon1;
        comp.option2Button = opt2;
        comp.option2Icon = icon2;

        AddButtonClickAnim(opt1, opt2);
        opt1.gameObject.AddComponent<GameplayButtonHoverSound>();
        opt2.gameObject.AddComponent<GameplayButtonHoverSound>();
        panel.AddComponent<PopupShowAnim>();
        panel.SetActive(false);

        Selection.activeGameObject = panel;
        Debug.Log("[GameplayUISetup] Created WeekRewardSelectionPopup");
    }

    public static void EnsureColorPickPanel()
    {
        var canvas = EnsureGameCanvas();
        if (canvas == null) return;

        var existing = Object.FindObjectOfType<ColorPickPanel>(true);
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            Debug.Log("[GameplayUISetup] ColorPickPanel already exists");
            return;
        }

        var panel = new GameObject("ColorPickPanel");
        panel.transform.SetParent(canvas.transform, false);

        var pr = panel.AddComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(360, 160);
        pr.anchoredPosition = Vector2.zero;

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.1f, 0.14f, 0.95f);
        bg.raycastTarget = true;

        var comp = panel.AddComponent<ColorPickPanel>();

        var colors = new[] { ("Button_红", "红"), ("Button_绿", "绿"), ("Button_蓝", "蓝"), ("Button_黄", "黄"), ("Button_青", "青"), ("Button_品", "品") };
        var buttons = new Button[6];
        float startX = -125f;
        float spacing = 50f;
        for (int i = 0; i < 6; i++)
        {
            var btn = CreateColorButton(panel.transform, colors[i].Item1, colors[i].Item2, new Vector2(startX + i * spacing, 20), new Vector2(40, 40));
            buttons[i] = btn;
        }

        var cancelBtn = CreateButton(panel.transform, "Button_取消", "取消", new Vector2(0, -55), new Vector2(80, 32), new Color(0.3f, 0.35f, 0.4f));
        comp.buttonRed = buttons[0];
        comp.buttonGreen = buttons[1];
        comp.buttonBlue = buttons[2];
        comp.buttonYellow = buttons[3];
        comp.buttonCyan = buttons[4];
        comp.buttonMagenta = buttons[5];
        comp.buttonCancel = cancelBtn;

        panel.AddComponent<PopupShowAnim>();
        panel.SetActive(false);

        Selection.activeGameObject = panel;
        Debug.Log("[GameplayUISetup] Created ColorPickPanel");
    }

    private static GameObject EnsureGameCanvas()
    {
        var canvas = GameObject.Find("GameCanvas");
        if (canvas != null) return canvas;

        canvas = new GameObject("GameCanvas");
        var c = canvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvas.AddComponent<GraphicRaycaster>();

        Selection.activeGameObject = canvas;
        Debug.Log("[GameplayUISetup] Created GameCanvas");
        return canvas;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = size;
        go.AddComponent<Image>().color = color;
        var btn = go.AddComponent<Button>();
        var labelGo = new GameObject("Text");
        labelGo.transform.SetParent(go.transform, false);
        var lr = labelGo.AddComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = lr.offsetMax = Vector2.zero;
        var txt = labelGo.AddComponent<Text>();
        txt.text = label;
        txt.font = GameUIFonts.Default;
        txt.fontSize = 18;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        return btn;
    }

    private static Button CreateColorButton(Transform parent, string name, string label, Vector2 pos, Vector2 size)
    {
        var btn = CreateButton(parent, name, "", pos, size, Color.white);
        btn.GetComponent<Image>().color = GetColorForName(name);
        return btn;
    }

    private static Color GetColorForName(string name)
    {
        return name switch
        {
            "Button_红" => Color.red,
            "Button_绿" => Color.green,
            "Button_蓝" => Color.blue,
            "Button_黄" => Color.yellow,
            "Button_青" => Color.cyan,
            "Button_品" => Color.magenta,
            _ => Color.white
        };
    }

    private static Button CreateMenuStyleButton(Transform parent, string name, string label, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0f);
        var btn = go.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        var labelGo = new GameObject("Text");
        labelGo.transform.SetParent(go.transform, false);
        var lr = labelGo.AddComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = new Vector2(20, 0);
        lr.offsetMax = Vector2.zero;
        var txt = labelGo.AddComponent<Text>();
        txt.text = label;
        txt.font = GameUIFonts.Default;
        txt.fontSize = 24;
        txt.alignment = TextAnchor.MiddleLeft;
        txt.color = Color.white;
        var shadow = labelGo.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.5f);
        shadow.effectDistance = new Vector2(1, -1);
        var menuBtn = go.AddComponent<MenuButton>();
        menuBtn.buttonText = txt;
        menuBtn.highlightColor = new Color(1f, 0.6f, 0.2f, 1f);
        go.AddComponent<ButtonClickAnim>();
        return btn;
    }

    private static Slider CreateVolumeSlider(Transform parent, string name, Vector2 pos, Vector2 size, string prefKey, float defaultValue)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = size;

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.22f, 0.28f);

        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(go.transform, false);
        var faRect = fillArea.AddComponent<RectTransform>();
        faRect.anchorMin = new Vector2(0, 0.25f);
        faRect.anchorMax = new Vector2(1, 0.75f);
        faRect.offsetMin = new Vector2(4, 2);
        faRect.offsetMax = new Vector2(-4, -2);

        var fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.35f, 0.5f, 0.4f);

        var handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(go.transform, false);
        var haRect = handleArea.AddComponent<RectTransform>();
        haRect.anchorMin = Vector2.zero;
        haRect.anchorMax = Vector2.one;
        haRect.offsetMin = new Vector2(10, 0);
        haRect.offsetMax = new Vector2(-10, 0);

        var handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        var hRect = handle.AddComponent<RectTransform>();
        hRect.sizeDelta = new Vector2(20, 0);
        var hImg = handle.AddComponent<Image>();
        hImg.color = new Color(0.6f, 0.7f, 0.65f);

        var slider = go.AddComponent<Slider>();
        slider.fillRect = fillRect;
        slider.handleRect = hRect;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = PlayerPrefs.GetFloat(prefKey, defaultValue);
        return slider;
    }

    private static Text CreateText(Transform parent, string name, string content, Vector2 pos, Vector2 size, int fontSize = 18)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = size;
        var t = go.AddComponent<Text>();
        t.text = content;
        t.font = GameUIFonts.Default;
        t.fontSize = fontSize;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        return t;
    }

    private static (Button button, Image iconImage) CreateRewardOptionCard(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = size;

        var bgImg = go.AddComponent<Image>();
        bgImg.color = new Color(0.18f, 0.2f, 0.28f, 0.98f);
        bgImg.raycastTarget = true;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = bgImg;

        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(go.transform, false);
        var iconRt = iconGo.AddComponent<RectTransform>();
        iconRt.anchorMin = Vector2.zero;
        iconRt.anchorMax = Vector2.one;
        iconRt.offsetMin = Vector2.zero;
        iconRt.offsetMax = Vector2.zero;
        var iconImg = iconGo.AddComponent<Image>();
        iconImg.color = Color.white;
        iconImg.raycastTarget = false;

        return (btn, iconImg);
    }

    public static void EnsureTimeSpeedPanel()
    {
        var canvas = EnsureGameCanvas();
        if (canvas == null) return;
        if (EnsureTimeSpeedPanelUnder(canvas.transform))
        {
            Selection.activeGameObject = GameObject.Find("TimeSpeedPanel");
            Debug.Log("[GameplayUISetup] Created TimeSpeedPanel");
            return;
        }
        var existing = GameObject.Find("TimeSpeedPanel");
        if (existing != null) Selection.activeGameObject = existing;
    }

    public static bool EnsureTimeSpeedPanelUnder(Transform parent)
    {
        if (parent.Find("TimeSpeedPanel") != null) return false;

        const float buttonSize = 48f;
        const float spacing = 6f;
        const float margin = 8f;
        const float pauseButtonSize = 64f;
        float panelHeight = buttonSize * 4 + spacing * 3;
        float startY = -margin - pauseButtonSize - spacing - panelHeight;

        var panel = new GameObject("TimeSpeedPanel");
        panel.transform.SetParent(parent, false);
        panel.transform.SetAsLastSibling();
        Undo.RegisterCreatedObjectUndo(panel, "Create TimeSpeedPanel");

        var pr = panel.AddComponent<RectTransform>();
        pr.anchorMin = new Vector2(1, 1);
        pr.anchorMax = new Vector2(1, 1);
        pr.pivot = new Vector2(1, 1);
        pr.anchoredPosition = new Vector2(-margin, startY);
        pr.sizeDelta = new Vector2(buttonSize, panelHeight);

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.06f, 0.1f, 0.85f);
        bg.raycastTarget = true;

        var btn0x = CreateSpeedButton(panel.transform, "Speed0x", "⏸", new Vector2(0, -buttonSize * 3 - spacing * 3), new Vector2(buttonSize, buttonSize));
        var btn1x = CreateSpeedButton(panel.transform, "Speed1x", "1x", new Vector2(0, -buttonSize * 2 - spacing * 2), new Vector2(buttonSize, buttonSize));
        var btn1_5x = CreateSpeedButton(panel.transform, "Speed1_5x", "1.5x", new Vector2(0, -buttonSize - spacing), new Vector2(buttonSize, buttonSize));
        var btn2x = CreateSpeedButton(panel.transform, "Speed2x", "2x", new Vector2(0, 0), new Vector2(buttonSize, buttonSize));

        var comp = panel.AddComponent<TimeSpeedPanel>();
        comp.speed0xButton = btn0x;
        comp.speed1xButton = btn1x;
        comp.speed1_5xButton = btn1_5x;
        comp.speed2xButton = btn2x;
        comp.BindEvents();

        AddButtonClickAnim(btn0x, btn1x, btn1_5x, btn2x);
        btn0x.gameObject.AddComponent<GameplayButtonHoverSound>();
        btn1x.gameObject.AddComponent<GameplayButtonHoverSound>();
        btn1_5x.gameObject.AddComponent<GameplayButtonHoverSound>();
        btn2x.gameObject.AddComponent<GameplayButtonHoverSound>();

        return true;
    }

    private static Button CreateSpeedButton(Transform parent, string name, string label, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(go, "Create Speed Button");

        var r = go.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(0.5f, 1);
        r.anchorMax = new Vector2(0.5f, 1);
        r.pivot = new Vector2(0.5f, 1);
        r.anchoredPosition = pos;
        r.sizeDelta = size;

        var img = go.AddComponent<Image>();
        img.color = new Color(0.35f, 0.4f, 0.5f, 0.9f);
        var btn = go.AddComponent<Button>();

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = textRt.offsetMax = Vector2.zero;
        var txt = textGo.AddComponent<Text>();
        txt.text = label;
        txt.font = GameUIFonts.Default;
        txt.fontSize = 16;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;

        return btn;
    }

    private static void AddButtonClickAnim(params Button[] buttons)
    {
        foreach (var b in buttons)
            if (b != null && b.GetComponent<ButtonClickAnim>() == null)
                b.gameObject.AddComponent<ButtonClickAnim>();
    }
}

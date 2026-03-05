using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 运行时仅绑定事件、控制显隐，不创建 UI。所有 UI 由 Star Express/自动设置 Game UI 在场景中创建，可在编辑器中直接调整。
/// </summary>
public class GameUIRuntimeBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        var go = new GameObject("GameUIRuntimeBootstrap");
        DontDestroyOnLoad(go);
        var b = go.AddComponent<GameUIRuntimeBootstrap>();
        SceneManager.sceneLoaded += (_, __) => b.EnsureUI();
        b.EnsureUI();
    }

    private void EnsureUI()
    {
        if (IsLevelScene())
        {
            CleanupDontDestroyOnLoadGameCanvas();
            EnsureGameCanvasVisibleInLevel();
            BindPauseButton();
            HideNonLevelPopups();
        }
        else
        {
            EnsureGameCanvas();
            HidePauseButtonInNonLevelScene();
            HideAllGameplayPopups();
        }
    }

    /// <summary>从 LevelSelect 进入关卡时，重新显示被隐藏的 GameCanvas，并确保 TimeSpeedPanel 存在。</summary>
    private static void EnsureGameCanvasVisibleInLevel()
    {
        var canvases = Object.FindObjectsOfType<Canvas>(true);
        bool foundGameCanvas = false;
        foreach (var c in canvases)
        {
            if (c != null && c.gameObject.name == "GameCanvas")
            {
                foundGameCanvas = true;
                c.gameObject.SetActive(true);
                
                if (c.GetComponent<GraphicRaycaster>() == null)
                {
                    c.gameObject.AddComponent<GraphicRaycaster>();
                }
                if (c.GetComponent<UIManager>() == null)
                {
                    c.gameObject.AddComponent<UIManager>();
                }

                var panel = c.transform.Find("TimeSpeedPanel");
                if (panel == null)
                {
                    CreateTimeSpeedPanel(c.transform);
                }
                else
                {
                    var comp = panel.GetComponent<TimeSpeedPanel>();
                    if (comp != null)
                    {
                        UIManager.Register(comp);
                    }
                    panel.gameObject.SetActive(false);
                }
                EnsureTimeSpeedToggleButtonExists(c.transform);
                EnsureWeekRewardSelectionPopupExists(c.transform);
                EnsureGameOverPopupExists(c.transform);
                EnsureCarriagePlacementPanelExists(c.transform);
            }
        }
        if (!foundGameCanvas)
        {
            Debug.LogWarning($"[GameUIRuntimeBootstrap] EnsureGameCanvasVisibleInLevel: GameCanvas not found");
        }
    }

    private static void EnsureWeekRewardSelectionPopupExists(Transform canvas)
    {
        if (canvas == null) return;

        var existingPopup = canvas.Find("WeekRewardSelectionPopup");
        if (existingPopup != null)
        {
            var comp = existingPopup.GetComponent<WeekRewardSelectionPopup>();
            if (comp != null)
            {
                UIManager.Register(comp);
            }
            existingPopup.gameObject.SetActive(false);
            return;
        }

        CreateWeekRewardSelectionPopup(canvas);
    }

    private static void CreateWeekRewardSelectionPopup(Transform parent)
    {
        var popup = new GameObject("WeekRewardSelectionPopup");
        popup.transform.SetParent(parent, false);

        var r = popup.AddComponent<RectTransform>();
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;

        var bg = popup.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0.05f, 0.07f, 0.12f, 0.92f);

        var content = new GameObject("Content");
        content.transform.SetParent(popup.transform, false);
        var cr = content.AddComponent<RectTransform>();
        cr.anchorMin = new Vector2(0.5f, 0.5f);
        cr.anchorMax = new Vector2(0.5f, 0.5f);
        cr.pivot = new Vector2(0.5f, 0.5f);
        cr.anchoredPosition = Vector2.zero;
        cr.sizeDelta = new Vector2(400, 280);

        var yearText = CreateText(content.transform, "YearText", "第 1 年", new Vector2(0, 100), new Vector2(200, 36), 24);
        yearText.alignment = TextAnchor.MiddleCenter;
        yearText.fontStyle = FontStyle.Bold;

        var hintText = CreateText(content.transform, "HintText", "选择 1 项奖励", new Vector2(0, 60), new Vector2(200, 24), 18);
        hintText.alignment = TextAnchor.MiddleCenter;

        var (opt1, icon1) = CreateRewardOption(content.transform, "Option1", new Vector2(-110, -20), new Vector2(160, 140));
        var (opt2, icon2) = CreateRewardOption(content.transform, "Option2", new Vector2(110, -20), new Vector2(160, 140));

        var comp = popup.AddComponent<WeekRewardSelectionPopup>();
        comp.yearText = yearText;
        comp.hintText = hintText;
        comp.option1Button = opt1;
        comp.option1Icon = icon1;
        comp.option2Button = opt2;
        comp.option2Icon = icon2;

        UIManager.Register(comp);
        popup.SetActive(false);
    }

    private static void EnsureGameOverPopupExists(Transform canvas)
    {
        if (canvas == null) return;

        var existingPopup = canvas.Find("GameOverPopup");
        if (existingPopup != null)
        {
            var comp = existingPopup.GetComponent<GameOverPopup>();
            if (comp != null)
            {
                UIManager.Register(comp);
            }
            existingPopup.gameObject.SetActive(false);
            return;
        }

        CreateGameOverPopup(canvas);
    }

    private static void CreateGameOverPopup(Transform parent)
    {
        var popup = new GameObject("GameOverPopup");
        popup.transform.SetParent(parent, false);

        var r = popup.AddComponent<RectTransform>();
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;

        var bg = popup.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0.05f, 0.07f, 0.12f, 0.92f);

        var content = new GameObject("Content");
        content.transform.SetParent(popup.transform, false);
        var cr = content.AddComponent<RectTransform>();
        cr.anchorMin = new Vector2(0.5f, 0.5f);
        cr.anchorMax = new Vector2(0.5f, 0.5f);
        cr.pivot = new Vector2(0.5f, 0.5f);
        cr.anchoredPosition = Vector2.zero;
        cr.sizeDelta = new Vector2(400, 300);

        var titleText = CreateText(content.transform, "TitleText", "调度失败", new Vector2(0, 100), new Vector2(300, 40), 28);
        titleText.fontStyle = FontStyle.Bold;

        var scoreText = CreateText(content.transform, "ScoreText", "得分: 0", new Vector2(0, 50), new Vector2(200, 30), 20);

        var reasonText = CreateText(content.transform, "ReasonText", "站点拥挤超阈值", new Vector2(0, 10), new Vector2(300, 24), 16);

        var retryBtn = CreateButton(content.transform, "RetryButton", "重试", new Vector2(-80, -60), new Vector2(120, 40));
        var backBtn = CreateButton(content.transform, "BackButton", "返回", new Vector2(80, -60), new Vector2(120, 40));

        var comp = popup.AddComponent<GameOverPopup>();
        comp.titleText = titleText;
        comp.scoreText = scoreText;
        comp.reasonText = reasonText;
        comp.retryButton = retryBtn;
        comp.backButton = backBtn;

        UIManager.Register(comp);
        popup.SetActive(false);
    }

    private static void EnsureCarriagePlacementPanelExists(Transform canvas)
    {
        if (canvas == null) return;

        var existingPanel = canvas.Find("CarriagePlacementPanel");
        if (existingPanel != null)
        {
            var comp = existingPanel.GetComponent<CarriagePlacementPanel>();
            if (comp != null)
            {
                UIManager.Register(comp);
            }
            existingPanel.gameObject.SetActive(false);
            return;
        }

        CreateCarriagePlacementPanel(canvas);
    }

    private static void CreateCarriagePlacementPanel(Transform parent)
    {
        var panel = new GameObject("CarriagePlacementPanel");
        panel.transform.SetParent(parent, false);

        var r = panel.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(0.5f, 0);
        r.anchorMax = new Vector2(0.5f, 0);
        r.pivot = new Vector2(0.5f, 0);
        r.anchoredPosition = new Vector2(0, 80);
        r.sizeDelta = new Vector2(400, 60);

        var bg = panel.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0.08f, 0.1f, 0.15f, 0.9f);

        var hintTxt = CreateText(panel.transform, "HintText", "点击地图上的飞船 → 升级容量 +2\n（按 Esc 取消）", Vector2.zero, new Vector2(380, 50), 16);
        hintTxt.alignment = TextAnchor.MiddleCenter;

        var cancelBtn = CreateButton(panel.transform, "CancelButton", "取消", new Vector2(160, 0), new Vector2(60, 30));

        var comp = panel.AddComponent<CarriagePlacementPanel>();
        comp.cancelButton = cancelBtn;
        comp.hintText = hintTxt;

        UIManager.Register(comp);
        panel.SetActive(false);
    }

    private static Button CreateButton(Transform parent, string name, string text, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(0.5f, 0.5f);
        r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = size;

        var img = go.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0.2f, 0.25f, 0.35f, 1f);
        var btn = go.AddComponent<UnityEngine.UI.Button>();

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var tr = textGo.AddComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;
        var t = textGo.AddComponent<Text>();
        t.text = text;
        t.font = GameUIFonts.Default;
        t.fontSize = 18;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;

        return btn;
    }

    private static Text CreateText(Transform parent, string name, string text, Vector2 pos, Vector2 size, int fontSize)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(0.5f, 0.5f);
        r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = size;
        var t = go.AddComponent<Text>();
        t.text = text;
        t.fontSize = fontSize;
        t.font = GameUIFonts.Default;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        return t;
    }

    private static (Button, Image) CreateRewardOption(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(0.5f, 0.5f);
        r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = size;

        var img = go.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0.15f, 0.18f, 0.25f, 1f);
        var btn = go.AddComponent<UnityEngine.UI.Button>();
        btn.transition = Selectable.Transition.ColorTint;

        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(go.transform, false);
        var iconR = iconGo.AddComponent<RectTransform>();
        iconR.anchorMin = Vector2.zero;
        iconR.anchorMax = Vector2.one;
        iconR.offsetMin = new Vector2(10, 10);
        iconR.offsetMax = new Vector2(-10, -10);
        var icon = iconGo.AddComponent<UnityEngine.UI.Image>();
        icon.preserveAspect = true;

        return (btn, icon);
    }

    /// <summary>进入关卡后销毁 DontDestroyOnLoad 中的冗余 GameCanvas，避免重复的隐藏弹窗占用资源。</summary>
    private static void CleanupDontDestroyOnLoadGameCanvas()
    {
        var activeScene = SceneManager.GetActiveScene();
        var canvases = Object.FindObjectsOfType<Canvas>(true);
        foreach (var c in canvases)
        {
            if (c == null || c.gameObject.name != "GameCanvas") continue;
            if (c.gameObject.scene != activeScene)
            {
                var uiManager = c.GetComponent<UIManager>();
                if (uiManager != null && UIManager.Instance == uiManager)
                {
                    UIManager.ClearInstance();
                }
                Object.Destroy(c.gameObject);
            }
        }
    }

    private static void HideNonLevelPopups()
    {
        var gameOver = Object.FindObjectOfType<GameOverPopup>(true);
        if (gameOver != null) gameOver.Hide();
        var weekReward = Object.FindObjectOfType<WeekRewardSelectionPopup>(true);
        if (weekReward != null) weekReward.Hide();
        var pauseMenu = Object.FindObjectOfType<PauseMenu>(true);
        if (pauseMenu != null) pauseMenu.Hide();
        var story = Object.FindObjectOfType<StoryPanel>(true);
        if (story != null) story.Hide();
        var tutorialStep = Object.FindObjectOfType<TutorialStepPanel>(true);
        if (tutorialStep != null) tutorialStep.Hide();
        var guide = Object.FindObjectOfType<OperationGuidePanel>(true);
        if (guide != null) guide.Hide();
    }

    private static void HideAllGameplayPopups()
    {
        var gameOver = Object.FindObjectOfType<GameOverPopup>(true);
        if (gameOver != null) gameOver.Hide();
        var weekReward = Object.FindObjectOfType<WeekRewardSelectionPopup>(true);
        if (weekReward != null) weekReward.Hide();
        var pauseMenu = Object.FindObjectOfType<PauseMenu>(true);
        if (pauseMenu != null) pauseMenu.Hide();
        HideTimeSpeedPanel();
        var story = Object.FindObjectOfType<StoryPanel>(true);
        if (story != null) story.Hide();
        var tutorialStep = Object.FindObjectOfType<TutorialStepPanel>(true);
        if (tutorialStep != null) tutorialStep.Hide();
        var guide = Object.FindObjectOfType<OperationGuidePanel>(true);
        if (guide != null) guide.Hide();
    }

    private static void HidePauseButtonInNonLevelScene()
    {
        var btn = GameObject.Find("PauseButton");
        if (btn != null) btn.SetActive(false);
        HideTimeSpeedPanel();
        HideTimeSpeedToggleButton();
    }

    private static void HideTimeSpeedPanel()
    {
        var panel = GameObject.Find("TimeSpeedPanel");
        if (panel != null) panel.SetActive(false);
    }

    private static void HideTimeSpeedToggleButton()
    {
        var btn = GameObject.Find("TimeSpeedToggleButton");
        if (btn != null) btn.SetActive(false);
    }

    private static bool IsLevelScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        string[] nonLevelScenes = { "StartMenu", "LevelSelect" };
        foreach (var name in nonLevelScenes)
        {
            if (sceneName == name) return false;
        }
        return true;
    }

    private static void EnsureGameCanvas()
    {
        var existingCanvas = GameObject.Find("GameCanvas");
        if (existingCanvas != null)
        {
            if (existingCanvas.GetComponent<UIManager>() == null)
            {
                existingCanvas.AddComponent<UIManager>();
            }
            HideTimeSpeedPanel();
            HideTimeSpeedToggleButton();
            return;
        }

        var canvas = new GameObject("GameCanvas");
        canvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvas.AddComponent<GraphicRaycaster>();
        canvas.AddComponent<UIManager>();
        Object.DontDestroyOnLoad(canvas);
    }

    private static void CreateTimeSpeedPanel(Transform parent)
    {
        const float buttonSize = 48f;
        const float spacing = 6f;
        const float margin = 8f;
        const float pauseButtonSize = 64f;
        float panelHeight = buttonSize * 4 + spacing * 3;
        float startY = -margin - pauseButtonSize - spacing - panelHeight;

        var panel = new GameObject("TimeSpeedPanel");
        panel.transform.SetParent(parent, false);

        var pr = panel.AddComponent<RectTransform>();
        pr.anchorMin = new Vector2(1, 1);
        pr.anchorMax = new Vector2(1, 1);
        pr.pivot = new Vector2(1, 1);
        pr.anchoredPosition = new Vector2(-margin, startY);
        pr.sizeDelta = new Vector2(buttonSize, panelHeight);

        var bg = panel.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0.04f, 0.06f, 0.1f, 0.85f);

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

        UIManager.Register(comp);

        panel.SetActive(false);
        EnsureTimeSpeedToggleButtonExists(parent);
    }

    private static void EnsureTimeSpeedToggleButtonExists(Transform canvas)
    {
        if (canvas == null) return;

        var existingBtn = canvas.Find("TimeSpeedToggleButton");
        if (existingBtn != null)
        {
            var toggle = existingBtn.GetComponent<TimeSpeedPanelToggle>();
            if (toggle == null)
            {
                toggle = existingBtn.gameObject.AddComponent<TimeSpeedPanelToggle>();
            }
            var btn = existingBtn.GetComponent<Button>();
            if (btn != null)
            {
                toggle.BindButton(btn);
            }
            return;
        }

        var go = new GameObject("TimeSpeedToggleButton");
        go.transform.SetParent(canvas, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(1, 1);
        r.anchorMax = new Vector2(1, 1);
        r.pivot = new Vector2(0.5f, 0.5f);
        const float size = 48f, margin = 8f, pauseButtonSize = 64f, gap = 8f;
        r.anchoredPosition = new Vector2(-margin - pauseButtonSize - gap - size * 0.5f, -margin - pauseButtonSize * 0.5f);
        r.sizeDelta = new Vector2(size, size);
        var img = go.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0.35f, 0.4f, 0.5f);
        var btn2 = go.AddComponent<UnityEngine.UI.Button>();
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var tr = textGo.AddComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = tr.offsetMax = Vector2.zero;
        var txt = textGo.AddComponent<UnityEngine.UI.Text>();
        txt.text = "⏱";
        txt.fontSize = 22;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        var toggle2 = go.AddComponent<TimeSpeedPanelToggle>();
        toggle2.BindButton(btn2);
    }

    private static Button CreateSpeedButton(Transform parent, string name, string label, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var r = go.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(0.5f, 1);
        r.anchorMax = new Vector2(0.5f, 1);
        r.pivot = new Vector2(0.5f, 1);
        r.anchoredPosition = pos;
        r.sizeDelta = size;

        var img = go.AddComponent<Image>();
        img.color = new Color(0.7f, 0.75f, 0.85f, 1f);
        var btn = go.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;

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

    /// <summary>仅绑定暂停键点击事件，不创建。场景内对象由 Star Express/自动设置 Game UI 创建，可在编辑器中直接调整。</summary>
    private static void BindPauseButton()
    {
        var existing = FindPauseButtonInActiveScene() ?? GameObject.Find("PauseButton");
        if (existing == null)
        {
            Debug.LogWarning("[GameUIRuntimeBootstrap] 未找到 PauseButton，请运行 Star Express/自动设置 Game UI 在场景中创建");
            return;
        }
        existing.SetActive(true);
        var btn = existing.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnPauseButtonClicked);
        }
    }

    private static PauseMenu FindPauseMenuInActiveScene()
    {
        var activeScene = SceneManager.GetActiveScene();
        var all = Object.FindObjectsOfType<PauseMenu>(true);
        foreach (var pm in all)
        {
            if (pm != null && pm.gameObject.scene == activeScene)
                return pm;
        }
        return all != null && all.Length > 0 ? all[0] : null;
    }

    /// <summary>优先使用当前关卡场景内的 PauseButton，保证从不同入口进入时行为一致。</summary>
    private static GameObject FindPauseButtonInActiveScene()
    {
        var activeScene = SceneManager.GetActiveScene();
        foreach (var root in activeScene.GetRootGameObjects())
        {
            var btns = root.GetComponentsInChildren<Button>(true);
            foreach (var b in btns)
            {
                if (b != null && b.gameObject.name == "PauseButton" && b.gameObject.scene == activeScene)
                    return b.gameObject;
            }
        }
        return null;
    }

    /// <summary>暂停键点击逻辑：始终使用当前场景内的 PauseMenu，保证从 LevelSelect/StartMenu 进入时与直接进入 SolarSystem 行为一致。</summary>
    private static void OnPauseButtonClicked()
    {
        GameplayAudio.Instance?.PlayGeneralClick();
        var gm = GameManager.Instance;
        if (gm == null || gm.IsGameOver || gm.IsPausedForWeekReward) return;
        var menu = FindPauseMenuInActiveScene();
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
    }

}

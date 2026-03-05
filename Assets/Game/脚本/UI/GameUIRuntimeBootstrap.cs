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
            EnsureGameCanvasVisibleInLevel();
            BindPauseButton();
            HideNonLevelPopups();
            CleanupDontDestroyOnLoadGameCanvas();
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
        foreach (var c in canvases)
        {
            if (c != null && c.gameObject.name == "GameCanvas")
                c.gameObject.SetActive(true);
        }

        EnsureTimeSpeedPanelInScene();
    }

    /// <summary>确保场景中的 GameCanvas 下有 TimeSpeedPanel 并显示。</summary>
    private static void EnsureTimeSpeedPanelInScene()
    {
        var activeScene = SceneManager.GetActiveScene();
        foreach (var root in activeScene.GetRootGameObjects())
        {
            if (root.name == "GameCanvas")
            {
                var panel = root.transform.Find("TimeSpeedPanel");
                if (panel == null)
                {
                    CreateTimeSpeedPanel(root.transform);
                }
                else
                {
                    panel.gameObject.SetActive(true);
                }
                return;
            }
        }
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
                Object.Destroy(c.gameObject);
                Debug.Log("[GameUIRuntimeBootstrap] 已清理 DontDestroyOnLoad 中的冗余 GameCanvas");
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
    }

    private static void HideTimeSpeedPanel()
    {
        var panel = GameObject.Find("TimeSpeedPanel");
        if (panel != null) panel.SetActive(false);
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
        if (GameObject.Find("GameCanvas") != null) return;

        var canvas = new GameObject("GameCanvas");
        canvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvas.AddComponent<GraphicRaycaster>();
        Object.DontDestroyOnLoad(canvas);
        Debug.Log("[GameUIRuntimeBootstrap] Created GameCanvas");

        CreateTimeSpeedPanel(canvas.transform);
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

        Debug.Log("[GameUIRuntimeBootstrap] Created TimeSpeedPanel");
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

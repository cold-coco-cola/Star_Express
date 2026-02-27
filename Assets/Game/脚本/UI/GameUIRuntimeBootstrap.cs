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

    /// <summary>从 LevelSelect 进入关卡时，重新显示被隐藏的 GameCanvas。</summary>
    private static void EnsureGameCanvasVisibleInLevel()
    {
        var canvases = Object.FindObjectsOfType<Canvas>(true);
        foreach (var c in canvases)
        {
            if (c != null && c.gameObject.name == "GameCanvas")
                c.gameObject.SetActive(true);
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
    }

    private static void HideAllGameplayPopups()
    {
        var gameOver = Object.FindObjectOfType<GameOverPopup>(true);
        if (gameOver != null) gameOver.Hide();
        var weekReward = Object.FindObjectOfType<WeekRewardSelectionPopup>(true);
        if (weekReward != null) weekReward.Hide();
        var pauseMenu = Object.FindObjectOfType<PauseMenu>(true);
        if (pauseMenu != null) pauseMenu.Hide();
    }

    private static void HidePauseButtonInNonLevelScene()
    {
        var btn = GameObject.Find("PauseButton");
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
        if (GameObject.Find("GameCanvas") != null) return;

        var canvas = new GameObject("GameCanvas");
        canvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvas.AddComponent<GraphicRaycaster>();
        Object.DontDestroyOnLoad(canvas);
        Debug.Log("[GameUIRuntimeBootstrap] Created GameCanvas");
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

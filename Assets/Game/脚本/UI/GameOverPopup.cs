using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 失败/结算界面。显示得分、失败原因，提供重试与返回按钮。
/// PRD §6.4、§7.2。
/// </summary>
public class GameOverPopup : BasePanel
{
    [Header("绑定")]
    public Text scoreText;
    public Text reasonText;
    public Button retryButton;
    public Button backButton;

    private void Awake()
    {
        var gm = GameManager.Instance;
        if (gm != null)
            gm.OnGameOver += OnGameOver;
    }

    private void Start()
    {
        if (retryButton != null) retryButton.onClick.AddListener(OnRetry);
        if (backButton != null) backButton.onClick.AddListener(OnBack);
    }

    protected override void OnDestroy()
    {
        var gm = GameManager.Instance;
        if (gm != null)
            gm.OnGameOver -= OnGameOver;
        base.OnDestroy();
    }

    private void OnGameOver(StationBehaviour failedStation)
    {
        RefreshScore();
        if (reasonText != null)
            reasonText.text = failedStation != null ? $"站点「{failedStation.displayName}」拥挤超阈值" : "游戏结束";
        Show();
    }

    /// <summary>由 GameplayUIController 直接调用，确保得分正确显示。</summary>
    public void ShowWithScore(int score, StationBehaviour failedStation)
    {
        if (scoreText != null) scoreText.text = "得分: " + score;
        if (reasonText != null)
            reasonText.text = failedStation != null ? $"站点「{failedStation.displayName}」拥挤超阈值" : "游戏结束";
        Show();
    }

    private void RefreshScore()
    {
        if (scoreText != null)
        {
            var gm = GameManager.Instance;
            scoreText.text = "得分: " + (gm != null ? gm.Score : 0);
        }
    }

    private void OnRetry()
    {
        var gm = GameManager.Instance;
        if (gm != null) Destroy(gm.gameObject);
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }

    private void OnBack()
    {
        // 返回主菜单；若无主菜单场景则重载当前场景
        var buildCount = SceneManager.sceneCountInBuildSettings;
        if (buildCount > 1)
            SceneManager.LoadScene(0);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}

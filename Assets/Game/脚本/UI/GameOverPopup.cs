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

    private void Start()
    {
        if (retryButton != null)
        {
            if (retryButton.GetComponent<GameplayButtonHoverSound>() == null)
                retryButton.gameObject.AddComponent<GameplayButtonHoverSound>();
            retryButton.onClick.AddListener(() => { GameplayAudio.Instance?.PlayGeneralClick(); OnRetry(); });
        }
        if (backButton != null)
        {
            if (backButton.GetComponent<GameplayButtonHoverSound>() == null)
                backButton.gameObject.AddComponent<GameplayButtonHoverSound>();
            backButton.onClick.AddListener(() => { GameplayAudio.Instance?.PlayGeneralClick(); OnBack(); });
        }
    }

    /// <summary>由 GameplayUIController 在相机聚焦动画完成后调用，确保得分与失败原因正确显示。</summary>
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
        var gm = GameManager.Instance;
        if (gm != null) Destroy(gm.gameObject);
        Hide();
        var buildCount = SceneManager.sceneCountInBuildSettings;
        if (buildCount > 1)
            SceneManager.LoadScene(0);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}

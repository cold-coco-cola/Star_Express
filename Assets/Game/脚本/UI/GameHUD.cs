using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD 常驻：分数、资源栏（飞船/客舱/星隧）、周倒计时。
/// PRD §6.4、UI 文档 §3.2。
/// </summary>
public class GameHUD : BasePanel
{
    [Header("绑定（留空自动查找）")]
    public Text scoreText;
    public Text shipCountText;
    public Text carriageCountText;
    public Text starTunnelCountText;
    public Text weekCountdownText;

    private void Start()
    {
        if (scoreText == null) scoreText = FindChildText(transform, "ScoreText");
        if (shipCountText == null) shipCountText = FindChildText(transform, "ShipCountText");
        if (carriageCountText == null) carriageCountText = FindChildText(transform, "CarriageCountText");
        if (starTunnelCountText == null) starTunnelCountText = FindChildText(transform, "StarTunnelCountText");
        if (weekCountdownText == null) weekCountdownText = FindChildText(transform, "WeekCountdownText");
        if (scoreText == null) scoreText = FindChildTextInCanvas("ScoreText");

        transform.SetAsLastSibling();
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnScoreChanged += OnScoreChanged;
            gm.OnWeekReward += OnWeekReward;
        }
        RefreshAll();
    }

    protected override void OnDestroy()
    {
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnScoreChanged -= OnScoreChanged;
            gm.OnWeekReward -= OnWeekReward;
        }
        base.OnDestroy();
    }

    private float _resourceRefreshTimer;

    private void Update()
    {
        RefreshScore();
        RefreshWeekCountdown();
        _resourceRefreshTimer += Time.deltaTime;
        if (_resourceRefreshTimer >= 0.5f) { _resourceRefreshTimer = 0; RefreshResources(); }
    }

    private void OnScoreChanged(int total) => RefreshScore();
    private void OnWeekReward(int week, ResourceType type) => RefreshResources();

    private void RefreshAll()
    {
        RefreshScore();
        RefreshResources();
        RefreshWeekCountdown();
    }

    private void RefreshScore()
    {
        if (scoreText == null) return;
        var gm = GameManager.Instance;
        scoreText.text = "得分: " + (gm != null ? gm.Score : 0);
    }

    private void RefreshResources()
    {
        var lm = GameManager.Instance != null ? GameManager.Instance.GetLineManagerComponent() : null;
        if (lm == null) return;
        if (shipCountText != null) shipCountText.text = "飞船: " + lm.ShipStock;
        if (carriageCountText != null) carriageCountText.text = "客舱: " + lm.CarriageStock;
        if (starTunnelCountText != null) starTunnelCountText.text = "星隧: " + lm.StarTunnelStock;
    }

    private void RefreshWeekCountdown()
    {
        if (weekCountdownText == null) return;
        var gm = GameManager.Instance;
        if (gm == null || gm.IsGameOver) { weekCountdownText.text = ""; return; }
        if (gm.IsPausedForWeekReward) { weekCountdownText.text = "选择奖励中..."; return; }
        float remaining = gm.WeekTimerRemaining;
        int m = (int)(remaining / 60);
        int s = (int)(remaining % 60);
        weekCountdownText.text = $"下周: {m}:{s:D2}";
    }

    private static Text FindChildText(Transform root, string name)
    {
        if (root == null) return null;
        var t = root.Find(name);
        if (t != null) return t.GetComponent<Text>();
        for (int i = 0; i < root.childCount; i++)
        {
            var c = FindChildText(root.GetChild(i), name);
            if (c != null) return c;
        }
        return null;
    }

    private static Text FindChildTextInCanvas(string name)
    {
        var canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
        if (canvas == null) return null;
        return FindChildText(canvas.transform, name);
    }
}

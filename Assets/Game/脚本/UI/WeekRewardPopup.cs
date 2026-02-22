using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 本周奖励弹窗。每经过 1 周弹出，显示获得的资源。
/// PRD §6.4、UI 文档。
/// </summary>
public class WeekRewardPopup : BasePanel
{
    [Header("绑定")]
    public Text weekText;
    public Text rewardText;
    public Button confirmButton;

    private void Start()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirm);
        var gm = GameManager.Instance;
        if (gm != null)
            gm.OnWeekReward += OnWeekReward;
    }

    protected override void OnDestroy()
    {
        var gm = GameManager.Instance;
        if (gm != null)
            gm.OnWeekReward -= OnWeekReward;
        base.OnDestroy();
    }

    private void OnWeekReward(int week, ResourceType type)
    {
        if (weekText != null) weekText.text = $"第 {week} 周";
        if (rewardText != null)
        {
            string r = type == ResourceType.Carriage ? "客舱 +1" : type == ResourceType.StarTunnel ? "星隧 +1" : "飞船 +1";
            rewardText.text = $"获得：飞船 +1，{r}";
        }
        Show();
    }

    private void OnConfirm()
    {
        Hide();
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 每周到时暂停游戏，弹出资源选择界面。
/// 从客舱、星隧、新线路中随机抽取 2 项作为选项，玩家从中选择 1 项作为奖励。
/// </summary>
public class WeekRewardSelectionPopup : BasePanel
{
    public enum RewardOption { Carriage, StarTunnel, NewLine }

    [Header("绑定")]
    public Text weekText;
    public Text hintText;
    public Button option1Button;
    public Text option1Label;
    public Text option1Desc;
    public Image option1Icon;
    public Button option2Button;
    public Text option2Label;
    public Text option2Desc;
    public Image option2Icon;

    private RewardOption[] _options = new RewardOption[2];

    /// <summary>选择完成时触发，参数为选中的 1 个资源类型。</summary>
    public event Action<RewardOption> OnSelectionComplete;

    private void Start()
    {
        if (option1Button != null)
        {
            if (option1Button.GetComponent<GameplayButtonHoverSound>() == null)
                option1Button.gameObject.AddComponent<GameplayButtonHoverSound>();
            option1Button.onClick.AddListener(() => { GameplayAudio.Instance?.PlayGeneralClick(); SelectAndClose(0); });
        }
        if (option2Button != null)
        {
            if (option2Button.GetComponent<GameplayButtonHoverSound>() == null)
                option2Button.gameObject.AddComponent<GameplayButtonHoverSound>();
            option2Button.onClick.AddListener(() => { GameplayAudio.Instance?.PlayGeneralClick(); SelectAndClose(1); });
        }
        OnSelectionComplete += OnSelectionCompleteHandler;
        var gm = GameManager.Instance;
        if (gm != null) gm.OnWeekRewardSelectionRequired += ShowForWeek;
    }

    protected override void OnDestroy()
    {
        OnSelectionComplete -= OnSelectionCompleteHandler;
        var gm = GameManager.Instance;
        if (gm != null) gm.OnWeekRewardSelectionRequired -= ShowForWeek;
        base.OnDestroy();
    }

    private void OnSelectionCompleteHandler(RewardOption chosen)
    {
        var gm = GameManager.Instance;
        if (gm != null) gm.ApplyWeekRewardSelection(chosen);
    }

    /// <summary>显示弹窗，传入周数。从三种资源中随机抽 2 项，玩家二选一。</summary>
    public void ShowForWeek(int week)
    {
        _options = PickTwoRandomOptions();
        if (weekText != null) weekText.text = $"第 {week} 周";
        if (hintText != null) hintText.text = "选择 1 项奖励";
        SetOptionLabel(0);
        SetOptionLabel(1);
        if (transform.parent != null) transform.SetAsLastSibling();
        Show();
    }

    private RewardOption[] PickTwoRandomOptions()
    {
        var pool = new List<RewardOption> { RewardOption.Carriage, RewardOption.StarTunnel };
        var lm = GameManager.Instance != null ? GameManager.Instance.GetLineManagerComponent() : null;
        if (lm != null && lm.MaxLineCount < 6)
            pool.Add(RewardOption.NewLine);
        var result = new RewardOption[2];
        for (int i = 0; i < 2; i++)
        {
            int idx = UnityEngine.Random.Range(0, pool.Count);
            result[i] = pool[idx];
            pool.RemoveAt(idx);
        }
        return result;
    }

    private void SelectAndClose(int index)
    {
        if (index < 0 || index >= _options.Length) return;
        OnSelectionComplete?.Invoke(_options[index]);
        Hide();
    }

    /// <summary>根据选项索引设置名称、描述、图标。图标通过 VisualConfig.rewardIcons[(int)RewardOption] 获取。</summary>
    private void SetOptionLabel(int index)
    {
        if (index >= _options.Length) return;
        var opt = _options[index];
        string name = opt == RewardOption.Carriage ? "客舱" : opt == RewardOption.StarTunnel ? "星隧" : "新线路";
        string desc = opt == RewardOption.Carriage ? "飞船容量 +2" : opt == RewardOption.StarTunnel ? "快速传送通道" : "解锁新线路";

        var label = index == 0 ? option1Label : option2Label;
        var descText = index == 0 ? option1Desc : option2Desc;
        var iconImg = index == 0 ? option1Icon : option2Icon;

        if (label != null) label.text = name;
        if (descText != null) descText.text = desc;
        if (iconImg != null)
        {
            var vc = GameManager.Instance != null ? GameManager.Instance.visualConfig : null;
            if (vc != null && vc.rewardIcons != null && (int)opt < vc.rewardIcons.Length && vc.rewardIcons[(int)opt] != null)
                iconImg.sprite = vc.rewardIcons[(int)opt];
            else
                iconImg.sprite = null;
        }
    }
}

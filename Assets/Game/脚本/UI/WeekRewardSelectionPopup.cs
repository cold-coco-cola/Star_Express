using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 每年到时暂停游戏，弹出资源选择界面。
/// 从客舱、星隧、新线路中随机抽取 2 项作为选项，玩家从中选择 1 项作为奖励。
/// </summary>
public class WeekRewardSelectionPopup : BasePanel
{
    public enum RewardOption { Carriage, StarTunnel, NewLine }

    [Header("绑定")]
    public Text yearText;
    public Text hintText;
    public Button option1Button;
    public Image option1Icon;
    public Button option2Button;
    public Image option2Icon;

    private RewardOption[] _options = new RewardOption[2];

    /// <summary>选择完成时触发，参数为选中的 1 个资源类型。</summary>
    public event Action<RewardOption> OnSelectionComplete;

    private void Start()
    {
        ResolveIconRefsIfMissing();
        RemoveDescAndNameText(option1Button);
        RemoveDescAndNameText(option2Button);
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

    /// <summary>若场景引用丢失，从 Option 下 Find("Icon") 恢复 option1Icon/option2Icon。</summary>
    private void ResolveIconRefsIfMissing()
    {
        if (option1Icon == null && option1Button != null)
        {
            var icon = option1Button.transform.Find("Icon");
            if (icon != null) option1Icon = icon.GetComponent<Image>();
        }
        if (option2Icon == null && option2Button != null)
        {
            var icon = option2Button.transform.Find("Icon");
            if (icon != null) option2Icon = icon.GetComponent<Image>();
        }
    }

    private static void RemoveDescAndNameText(Button optionButton)
    {
        if (optionButton == null) return;
        var t = optionButton.transform;
        for (int i = t.childCount - 1; i >= 0; i--)
        {
            var child = t.GetChild(i);
            if (child.name == "DescText" || child.name == "NameText")
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                else
#endif
                    UnityEngine.Object.Destroy(child.gameObject);
            }
        }
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

    /// <summary>显示弹窗，传入年数。从三种资源中随机抽 2 项，玩家二选一。</summary>
    public void ShowForWeek(int year)
    {
        _options = PickTwoRandomOptions();
        if (yearText != null) yearText.text = $"第 {year} 年";
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

    /// <summary>根据选项索引设置图标，VisualConfig.rewardIcons 顺序为 Carriage/StarTunnel/NewLine，对应 rocket/Star_Tunnel/track。</summary>
    private void SetOptionLabel(int index)
    {
        if (index >= _options.Length) return;
        var opt = _options[index];
        var iconImg = index == 0 ? option1Icon : option2Icon;

        if (iconImg == null) return;

        var rt = iconImg.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        iconImg.raycastTarget = false;
        iconImg.color = Color.white;
        iconImg.type = Image.Type.Simple;
        iconImg.preserveAspect = false;
        iconImg.enabled = true;
        if (iconImg.gameObject != null)
            iconImg.gameObject.SetActive(true);
        iconImg.transform.SetAsFirstSibling();

        Sprite sprite = GetRewardSprite(opt);
        iconImg.sprite = sprite;
        if (sprite == null)
            iconImg.color = new Color(0.3f, 0.35f, 0.45f, 0.98f);
    }

    /// <summary>优先从 VisualConfig.rewardIcons 取图，为空时在编辑器用 AssetDatabase 从 Obtaining_Props 加载，或从 Resources 加载。</summary>
    private static Sprite GetRewardSprite(RewardOption opt)
    {
        var gm = GameManager.Instance;
        var vc = gm != null ? gm.visualConfig : null;
        if (vc != null && vc.rewardIcons != null && (int)opt < vc.rewardIcons.Length && vc.rewardIcons[(int)opt] != null)
            return vc.rewardIcons[(int)opt];

#if UNITY_EDITOR
        if (vc == null)
            vc = AssetDatabase.LoadAssetAtPath<VisualConfig>("Assets/Game/配置/VisualConfig.asset");
        if (vc != null && vc.rewardIcons != null && (int)opt < vc.rewardIcons.Length && vc.rewardIcons[(int)opt] != null)
            return vc.rewardIcons[(int)opt];
        string name = opt == RewardOption.Carriage ? "rocket" : opt == RewardOption.StarTunnel ? "Star_Tunnel" : "track";
            string path = "Assets/Game/美术/Photos/Obtaining_Props/" + name + ".jpg";
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        if (assets != null)
            foreach (var a in assets)
                if (a is Sprite s) return s;
#endif
        string resName = opt == RewardOption.Carriage ? "rocket" : opt == RewardOption.StarTunnel ? "Star_Tunnel" : "track";
        var fromRes = Resources.Load<Sprite>("Obtaining_Props/" + resName);
        if (fromRes != null) return fromRes;
        return null;
    }
}

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 星隧面板。仿照 ShipPlacementPanel 结构：色块 + CountText，仅展示数量，无点击/选择。
/// </summary>
public class StarTunnelPanel : BasePanel
{
    [Header("绑定")]
    public RectTransform areaRect;
    public Text countText;

    private float _nextRefresh;

    private void Start()
    {
        _nextRefresh = 0f;
        RefreshCount();
    }

    private void Update()
    {
        if (Time.time >= _nextRefresh)
        {
            _nextRefresh = Time.time + 0.5f;
            RefreshCount();
        }
    }

    private void RefreshCount()
    {
        if (countText == null) return;
        var lm = GetLineManager();
        countText.text = lm != null ? lm.StarTunnelStock.ToString() : "0";
    }

    private LineManager GetLineManager()
    {
        var gm = GameManager.Instance;
        if (gm != null) return gm.GetLineManagerComponent();
        return FindObjectOfType<GameManager>()?.GetLineManagerComponent();
    }
}

using UnityEngine;

/// <summary>
/// 资源面板容器。统一管理 ShipPlacementPanel、ShipUpgradePanel、StarTunnelPanel 三个子面板。
/// 当任一子面板弹出选择/操作框时，隐藏其他子面板的 CountText，使画面更和谐。
/// </summary>
public class ResourcePanel : BasePanel
{
    [Header("子面板")]
    public ShipPlacementPanel shipPlacementPanel;
    public ShipUpgradePanel shipUpgradePanel;
    public StarTunnelPanel starTunnelPanel;

    private bool _lastExpanded;

    private void Update()
    {
        bool expanded = IsAnyChildExpanded();
        if (expanded != _lastExpanded)
        {
            _lastExpanded = expanded;
            SetOtherCountTextsVisible(!expanded);
        }
    }

    private bool IsAnyChildExpanded()
    {
        if (shipPlacementPanel != null && shipPlacementPanel.IsFanPanelOpen())
            return true;
        if (UIManager.IsShowing<CarriagePlacementPanel>())
            return true;
        if (UIManager.IsShowing<StarTunnelHintPopup>())
            return true;
        return false;
    }

    private void SetOtherCountTextsVisible(bool visible)
    {
        SetCountTextVisible(shipPlacementPanel, visible);
        SetCountTextVisible(shipUpgradePanel, visible);
        SetCountTextVisible(starTunnelPanel, visible);
    }

    private static void SetCountTextVisible(MonoBehaviour panel, bool visible)
    {
        if (panel == null) return;
        var tr = panel.transform.Find("CountText");
        if (tr != null) tr.gameObject.SetActive(visible);
    }
}

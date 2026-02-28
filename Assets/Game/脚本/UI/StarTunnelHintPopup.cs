using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 星隧说明弹窗。结构与 CarriagePlacementPanel 相同，点击 StarTunnelPanel 时显示。
/// </summary>
public class StarTunnelHintPopup : BasePanel
{
    [Header("绑定")]
    public Button closeButton;
    public Text hintText;

    private void Start()
    {
        if (closeButton != null)
        {
            if (closeButton.GetComponent<GameplayButtonHoverSound>() == null)
                closeButton.gameObject.AddComponent<GameplayButtonHoverSound>();
            closeButton.onClick.AddListener(() => { GameplayAudio.Instance?.PlayGeneralClick(); OnClose(); });
        }
        if (hintText != null)
            hintText.text = "建设线路穿过陨石带时，会自动直接消耗星燧资源哦~";
    }

    private void OnClose()
    {
        Hide();
    }
}

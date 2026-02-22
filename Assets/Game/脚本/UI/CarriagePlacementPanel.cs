using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 放置客舱模式提示面板。显示操作说明与取消按钮。
/// </summary>
public class CarriagePlacementPanel : BasePanel
{
    [Header("绑定")]
    public Button cancelButton;
    public Text hintText;

    private void Start()
    {
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancel);
        if (hintText == null)
        {
            var ht = transform.Find("HintText");
            if (ht != null) hintText = ht.GetComponent<Text>();
            if (hintText == null) hintText = GetComponentInChildren<Text>();
        }
        if (hintText != null)
            hintText.text = "点击地图上的飞船 → 升级容量 +2\n（按 Esc 取消）";
    }

    private void OnCancel()
    {
        if (GameplayUIController.Instance != null)
            GameplayUIController.Instance.TryTransition(GameplayUIState.Idle);
        Hide();
    }
}

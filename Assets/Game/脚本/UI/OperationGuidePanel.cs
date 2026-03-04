using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 常驻可打开的操作指南面板，不暂停游戏。
/// </summary>
public class OperationGuidePanel : BasePanel
{
    [SerializeField] private Button backgroundCloseButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Text titleText;
    [SerializeField] private Text contentText;

    [TextArea(8, 30)]
    [SerializeField] private string guideContent =
        "操作指南\n\n" +
        "【建设线路】\n" +
        "点击站点开始，再点击另一个站点完成连线\n\n" +
        "【编辑线路】\n" +
        "右键点击线路：\n" +
        "  · 左键点击 -> 修改线路\n" +
        "  · 双击 -> 删除该段\n\n" +
        "【线路面板】\n" +
        "右侧面板同样支持右键操作\n\n" +
        "【放置飞船】\n" +
        "点击已有线路放置飞船\n\n" +
        "【升级飞船】\n" +
        "点击飞船后选择升级\n\n" +
        "【周奖励】\n" +
        "每周结束可选择奖励：客舱/星燧/新线路";

    protected override void OnInit()
    {
        if (titleText == null || contentText == null)
        {
            var texts = GetComponentsInChildren<Text>(true);
            foreach (var t in texts)
            {
                if (t == null) continue;
                if (titleText == null && t.name.Contains("Title")) titleText = t;
                else if (contentText == null && t.name.Contains("Content")) contentText = t;
            }
            if (titleText == null && texts.Length > 0) titleText = texts[0];
            if (contentText == null && texts.Length > 1) contentText = texts[1];
        }

        if (backgroundCloseButton == null || closeButton == null)
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                if (b == null) continue;
                if (closeButton == null && b.name.Contains("关闭")) closeButton = b;
                else if (backgroundCloseButton == null && b.name.Contains("Background")) backgroundCloseButton = b;
            }
            if (closeButton == null && buttons.Length > 0) closeButton = buttons[0];
            if (backgroundCloseButton == null && buttons.Length > 1) backgroundCloseButton = buttons[1];
        }

        SetupButton(backgroundCloseButton, HideGuide);
        SetupButton(closeButton, HideGuide);

        if (titleText != null)
        {
            titleText.font = GameUIFonts.Default;
            titleText.text = "操作指南";
        }
        if (contentText != null)
        {
            contentText.font = GameUIFonts.Default;
            contentText.text = guideContent;
        }

        if (GetComponent<PopupShowAnim>() == null)
            gameObject.AddComponent<PopupShowAnim>();
    }

    public void ShowGuide()
    {
        Show();
        transform.SetAsLastSibling();
    }

    private void HideGuide()
    {
        GameplayAudio.Instance?.PlayGeneralClick();
        Hide();
    }

    private static void SetupButton(Button btn, UnityEngine.Events.UnityAction onClick)
    {
        if (btn == null) return;
        btn.onClick.RemoveListener(onClick);
        btn.onClick.AddListener(onClick);
        if (btn.GetComponent<GameplayButtonHoverSound>() == null)
            btn.gameObject.AddComponent<GameplayButtonHoverSound>();
        if (btn.GetComponent<ButtonClickAnim>() == null)
            btn.gameObject.AddComponent<ButtonClickAnim>();
    }
}

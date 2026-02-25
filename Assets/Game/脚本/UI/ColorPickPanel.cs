using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 选色面板。仅显示已解锁的线路颜色（初始 3 条：红/绿/蓝）。
/// Show(stationA, stationB, onSelected, onCancel) 打开面板。
/// </summary>
public class ColorPickPanel : BasePanel
{
    [Header("面板引用（在 Inspector 中拖拽）")]
    public Button buttonRed;
    public Button buttonGreen;
    public Button buttonBlue;
    public Button buttonYellow;
    public Button buttonCyan;
    public Button buttonMagenta;
    public Button buttonCancel;

    private StationBehaviour _stationA;
    private StationBehaviour _stationB;
    private Action<LineColor> _onSelected;
    private Action _onCancel;

    public StationBehaviour LastStationB => _stationB;

    protected override void OnInit()
    {
        TryAutoBindButtons();
        BindButtonEvents();
    }

    /// <summary>打开选色面板并设置回调。仅显示已解锁的线路颜色。</summary>
    public void Show(StationBehaviour stationA, StationBehaviour stationB,
                     Action<LineColor> onSelected, Action onCancel)
    {
        _stationA = stationA;
        _stationB = stationB;
        _onSelected = onSelected;
        _onCancel = onCancel;
        RefreshUnlockedVisibility();
        base.Show();
        if (panelRoot != null)
            panelRoot.transform.SetAsLastSibling();
    }

    /// <summary>根据 MaxLineCount 仅显示已解锁的颜色按钮，并调整面板尺寸。</summary>
    private void RefreshUnlockedVisibility()
    {
        int maxCount = 3;
        var lm = GameManager.Instance != null ? GameManager.Instance.GetLineManagerComponent() : null;
        if (lm != null) maxCount = lm.MaxLineCount;

        var buttons = GetColorButtonsInOrder();
        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] != null)
                buttons[i].gameObject.SetActive(i < maxCount);
        }

        var rect = panelRoot != null ? panelRoot.GetComponent<RectTransform>() : null;
        if (rect != null)
        {
            rect.sizeDelta = maxCount <= 3 ? new Vector2(360, 160) : new Vector2(420, 220);
        }

        if (buttonCancel != null)
        {
            var cr = buttonCancel.GetComponent<RectTransform>();
            if (cr != null)
                cr.anchoredPosition = new Vector2(0, maxCount <= 3 ? -55 : -90);
        }
    }

    private List<Button> GetColorButtonsInOrder()
    {
        return new List<Button> { buttonRed, buttonGreen, buttonBlue, buttonYellow, buttonCyan, buttonMagenta };
    }

    private void OnColor(LineColor color)
    {
        Hide();
        _onSelected?.Invoke(color);
        _onSelected = null;
        _onCancel = null;
    }

    private void OnCancelClick()
    {
        Hide();
        _onCancel?.Invoke();
        _onSelected = null;
        _onCancel = null;
    }

    private void TryAutoBindButtons()
    {
        if (panelRoot == null) return;
        var t = panelRoot.transform;
        if (buttonRed == null) buttonRed = t.Find("Button_红")?.GetComponent<Button>();
        if (buttonGreen == null) buttonGreen = t.Find("Button_绿")?.GetComponent<Button>();
        if (buttonBlue == null) buttonBlue = t.Find("Button_蓝")?.GetComponent<Button>();
        if (buttonYellow == null) buttonYellow = t.Find("Button_黄")?.GetComponent<Button>();
        if (buttonCyan == null) buttonCyan = t.Find("Button_青")?.GetComponent<Button>();
        if (buttonMagenta == null) buttonMagenta = t.Find("Button_品")?.GetComponent<Button>();
        if (buttonCancel == null) buttonCancel = t.Find("Button_取消")?.GetComponent<Button>();
    }

    private void BindButtonEvents()
    {
        var audio = GameplayAudio.Instance;
        if (buttonRed != null) { buttonRed.onClick.RemoveAllListeners(); buttonRed.onClick.AddListener(() => { audio?.PlayClick(); OnColor(LineColor.Red); }); AddHoverSound(buttonRed); }
        if (buttonGreen != null) { buttonGreen.onClick.RemoveAllListeners(); buttonGreen.onClick.AddListener(() => { audio?.PlayClick(); OnColor(LineColor.Green); }); AddHoverSound(buttonGreen); }
        if (buttonBlue != null) { buttonBlue.onClick.RemoveAllListeners(); buttonBlue.onClick.AddListener(() => { audio?.PlayClick(); OnColor(LineColor.Blue); }); AddHoverSound(buttonBlue); }
        if (buttonYellow != null) { buttonYellow.onClick.RemoveAllListeners(); buttonYellow.onClick.AddListener(() => { audio?.PlayClick(); OnColor(LineColor.Yellow); }); AddHoverSound(buttonYellow); }
        if (buttonCyan != null) { buttonCyan.onClick.RemoveAllListeners(); buttonCyan.onClick.AddListener(() => { audio?.PlayClick(); OnColor(LineColor.Cyan); }); AddHoverSound(buttonCyan); }
        if (buttonMagenta != null) { buttonMagenta.onClick.RemoveAllListeners(); buttonMagenta.onClick.AddListener(() => { audio?.PlayClick(); OnColor(LineColor.Magenta); }); AddHoverSound(buttonMagenta); }
        if (buttonCancel != null) { buttonCancel.onClick.RemoveAllListeners(); buttonCancel.onClick.AddListener(() => { audio?.PlayClick(); OnCancelClick(); }); AddHoverSound(buttonCancel); }
    }

    private void AddHoverSound(UnityEngine.UI.Button btn)
    {
        if (btn == null || btn.GetComponent<GameplayButtonHoverSound>() != null) return;
        btn.gameObject.AddComponent<GameplayButtonHoverSound>();
    }
}

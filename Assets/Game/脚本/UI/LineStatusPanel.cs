using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 右侧线路状态面板。显示 6 条线路的圆形指示器，支持右键删除、悬停效果。
/// </summary>
public class LineStatusPanel : BasePanel
{
    [Header("圆形指示器（按 LineColor 顺序：红绿蓝黄青品）")]
    [SerializeField] private Image[] _circleImages = new Image[6];

    [Header("尺寸")]
    [SerializeField] private float _smallSize = 28f;
    [SerializeField] private float _largeSize = 40f;
    [SerializeField] private float _animDuration = 0.12f;

    [Header("脉冲")]
    [SerializeField] private float _pulseScale = 1.15f;
    [SerializeField] private float _pulseSpeed = 8f;

    [Header("悬停")]
    [SerializeField] private float _hoverScale = 1.08f;

    [Header("取消遮罩（留空则仅支持 ESC 取消）")]
    [SerializeField] private Button _cancelOverlay;

    private static readonly LineColor[] _colorOrder = { LineColor.Red, LineColor.Green, LineColor.Blue, LineColor.Yellow, LineColor.Cyan, LineColor.Magenta };
    private static readonly Color _lockedColor = new Color(0.4f, 0.4f, 0.45f, 0.6f);

    private LineColor? _pendingDeleteColor;
    private float _refreshTimer;
    private readonly Dictionary<LineColor, CircleState> _states = new Dictionary<LineColor, CircleState>();
    private readonly Dictionary<LineColor, float> _scaleProgress = new Dictionary<LineColor, float>();
    private readonly Dictionary<LineColor, bool> _hovered = new Dictionary<LineColor, bool>();

    private enum CircleState { Locked, Unlocked, InUse }

    protected override void OnInit()
    {
        TryAutoBindCircles();
        if (_cancelOverlay != null)
        {
            _cancelOverlay.onClick.RemoveAllListeners();
            _cancelOverlay.onClick.AddListener(CancelPendingDelete);
        }
        for (int i = 0; i < _colorOrder.Length; i++)
        {
            var c = _colorOrder[i];
            _states[c] = CircleState.Locked;
            _scaleProgress[c] = 1f;
            _hovered[c] = false;
        }
    }

    public override void Show()
    {
        base.Show();
        RefreshAll();
    }

    private void TryAutoBindCircles()
    {
        if (panelRoot == null) return;
        var t = panelRoot.transform;
        for (int i = 0; i < 6; i++)
        {
            if (_circleImages[i] != null) continue;
            var child = t.Find("Circle_" + i) ?? t.Find("Circle_" + _colorOrder[i]);
            if (child != null)
                _circleImages[i] = child.GetComponent<Image>();
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        _refreshTimer += Time.deltaTime;
        if (_refreshTimer >= 0.15f)
        {
            _refreshTimer = 0f;
            RefreshAll();
        }

        if (_pendingDeleteColor.HasValue && Input.GetKeyDown(KeyCode.Escape))
            CancelPendingDelete();

        UpdateScaleAnimations();
        UpdatePulseForPendingDelete();
    }

    private void RefreshAll()
    {
        var lm = GetLineManager();
        int unlockedCount = lm != null ? lm.MaxLineCount : 6;

        for (int i = 0; i < _colorOrder.Length; i++)
        {
            var color = _colorOrder[i];
            bool isUnlocked = i < unlockedCount;
            bool isUsed = IsLineInUse(lm, color);

            var state = !isUnlocked ? CircleState.Locked : (isUsed ? CircleState.InUse : CircleState.Unlocked);
            _states[color] = state;

            var img = GetCircleImage(i);
            if (img == null) continue;

            img.raycastTarget = isUnlocked;

            if (_pendingDeleteColor == color) continue;
            bool isHovered = _hovered.ContainsKey(color) && _hovered[color] && state == CircleState.InUse;
            img.color = GetColorForState(color, state, isHovered);
        }
    }

    private bool IsLineInUse(LineManager lm, LineColor color)
    {
        if (lm == null || lm.Lines == null) return false;
        foreach (var line in lm.Lines)
        {
            if (line.color == color && line.stationSequence != null && line.stationSequence.Count >= 2)
                return true;
        }
        return false;
    }

    private Color GetColorForState(LineColor color, CircleState state, bool highlight = false)
    {
        if (state == CircleState.Locked) return _lockedColor;
        var c = GetLineColor(color);
        if (highlight)
        {
            return new Color(
                Mathf.Min(1f, c.r * 1.5f),
                Mathf.Min(1f, c.g * 1.5f),
                Mathf.Min(1f, c.b * 1.5f),
                1f
            );
        }
        return c;
    }

    private Color GetLineColor(LineColor lineColor)
    {
        var vc = GameManager.Instance != null ? GameManager.Instance.visualConfig : null;
        if (vc != null && vc.lineColors != null && vc.lineColors.Length > 0)
        {
            int idx = (int)lineColor;
            if (idx >= 0 && idx < vc.lineColors.Length)
            {
                var c = vc.lineColors[idx];
                if (c.r < 0.95f || c.g < 0.95f || c.b < 0.95f) return c;
            }
        }
        return lineColor switch
        {
            LineColor.Red => Color.red,
            LineColor.Green => Color.green,
            LineColor.Blue => Color.blue,
            LineColor.Yellow => Color.yellow,
            LineColor.Cyan => Color.cyan,
            LineColor.Magenta => Color.magenta,
            _ => Color.white
        };
    }

    private void UpdateScaleAnimations()
    {
        for (int i = 0; i < _colorOrder.Length; i++)
        {
            var color = _colorOrder[i];
            var img = GetCircleImage(i);
            if (img == null) continue;

            var state = _states.ContainsKey(color) ? _states[color] : CircleState.Locked;
            float targetProgress = state == CircleState.InUse ? 1f : 0f;

            float current = _scaleProgress[color];
            float step = Time.unscaledDeltaTime / Mathf.Max(0.01f, _animDuration);
            current = Mathf.MoveTowards(current, targetProgress, step);
            _scaleProgress[color] = current;

            float size = Mathf.Lerp(_smallSize, _largeSize, current);
            bool isHovered = _hovered.ContainsKey(color) && _hovered[color] && state == CircleState.InUse;
            if (isHovered)
                size *= _hoverScale;

            if (_pendingDeleteColor == color) continue;
            var rt = img.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = new Vector2(size, size);

            img.color = GetColorForState(color, state, isHovered);
        }
    }

    private void CancelPendingDelete()
    {
        _pendingDeleteColor = null;
        if (_cancelOverlay != null) _cancelOverlay.gameObject.SetActive(false);
        RefreshAll();
    }

    private void UpdatePulseForPendingDelete()
    {
        if (!_pendingDeleteColor.HasValue) return;

        for (int i = 0; i < _colorOrder.Length; i++)
        {
            if (_colorOrder[i] != _pendingDeleteColor.Value) continue;
            var img = GetCircleImage(i);
            if (img == null) break;

            float pulseScale = 1f + (_pulseScale - 1f) * Mathf.Sin(Time.time * _pulseSpeed);
            float baseSize = _largeSize;
            var rt = img.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = new Vector2(baseSize * pulseScale, baseSize * pulseScale);

            img.color = GetColorForState(_pendingDeleteColor.Value, CircleState.InUse, true);
            break;
        }
    }

    /// <summary>由 LineStatusCircleItem 调用：悬停进入。</summary>
    public void OnCirclePointerEnter(LineColor color)
    {
        if (_states.TryGetValue(color, out var state) && state == CircleState.InUse)
        {
            _hovered[color] = true;
            GameplayAudio.Instance?.PlayHover();
        }
    }

    /// <summary>由 LineStatusCircleItem 调用：悬停离开。</summary>
    public void OnCirclePointerExit(LineColor color)
    {
        _hovered[color] = false;
    }

    /// <summary>由 LineStatusCircleItem 调用：右键点击。</summary>
    public void OnCircleRightClick(LineColor color)
    {
        if (_states.TryGetValue(color, out var state) && state != CircleState.InUse) return;

        GameplayAudio.Instance?.PlayGeneralClick();

        if (_pendingDeleteColor == color)
        {
            var lm = GetLineManager();
            if (lm != null && lm.TryRemoveLineByColor(color))
            {
                _pendingDeleteColor = null;
                RefreshAll();
            }
        }
        else
        {
            _pendingDeleteColor = color;
            if (_cancelOverlay != null) _cancelOverlay.gameObject.SetActive(true);
        }
    }

    private Image GetCircleImage(int index)
    {
        if (index < 0 || index >= _circleImages.Length) return null;
        return _circleImages[index];
    }

    private LineManager GetLineManager()
    {
        var gm = GameManager.Instance;
        if (gm != null) return gm.GetLineManagerComponent();
        return Object.FindObjectOfType<GameManager>()?.GetLineManagerComponent();
    }
}

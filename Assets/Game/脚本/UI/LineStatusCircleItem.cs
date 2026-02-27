using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 线路状态面板中的单个圆形指示器。处理悬停与右键点击，转发给 LineStatusPanel。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class LineStatusCircleItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    [SerializeField] private LineColor _lineColor = LineColor.Red;

    private LineStatusPanel _panel;

    private void Awake()
    {
        if (_panel == null)
            _panel = GetComponentInParent<LineStatusPanel>();
    }

    public void SetLineColor(LineColor color)
    {
        _lineColor = color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _panel?.OnCirclePointerEnter(_lineColor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _panel?.OnCirclePointerExit(_lineColor);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
            _panel?.OnCircleRightClick(_lineColor);
    }
}

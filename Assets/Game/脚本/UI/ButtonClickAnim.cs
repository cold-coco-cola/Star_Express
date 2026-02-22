using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 按钮点击缩放动画，使点击反馈更丝滑。
/// 挂到 Button 上即可生效。
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonClickAnim : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private float _pressScale = 0.92f;
    [SerializeField] private float _animDuration = 0.08f;

    private RectTransform _rect;
    private Vector3 _normalScale;
    private float _t;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        if (_rect != null) _normalScale = _rect.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_rect == null) return;
        _t = 0f;
        StopAllCoroutines();
        StartCoroutine(AnimateTo(_pressScale));
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_rect == null) return;
        _t = 0f;
        StopAllCoroutines();
        StartCoroutine(AnimateTo(1f));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnPointerUp(eventData);
    }

    private System.Collections.IEnumerator AnimateTo(float targetScale)
    {
        float start = _normalScale.x > 0.001f ? _rect.localScale.x / _normalScale.x : 1f;
        float duration = _animDuration;
        while (_t < 1f)
        {
            _t += Time.unscaledDeltaTime / duration;
            float ease = 1f - (1f - _t) * (1f - _t);
            float s = Mathf.Lerp(start, targetScale, ease);
            _rect.localScale = _normalScale * s;
            yield return null;
        }
        _rect.localScale = _normalScale * targetScale;
    }

}

using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// 色块按钮悬停/按下视效。用于 FanContent 的选线色块，提供进入时缩放反馈（解决“只有离开才有视觉”的问题）。
/// 悬停放大、按下缩小、松手回弹。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ColorBlockHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private float _hoverScale = 1.08f;
    [SerializeField] private float _pressScale = 0.92f;
    [SerializeField] private float _duration = 0.1f;

    private RectTransform _rect;
    private Vector3 _normalScale;
    private Coroutine _coroutine;
    private bool _isHovered;
    private bool _isPressed;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        if (_rect != null) _normalScale = _rect.localScale;
    }

    private void OnDisable()
    {
        if (_rect != null && _normalScale.x > 0.001f)
            _rect.localScale = _normalScale;
        if (_coroutine != null) StopCoroutine(_coroutine);
        _coroutine = null;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        UpdateScale();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        _isPressed = false;
        UpdateScale();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isPressed = true;
        UpdateScale();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isPressed = false;
        UpdateScale();
    }

    private void UpdateScale()
    {
        float target = _isPressed ? _pressScale : (_isHovered ? _hoverScale : 1f);
        AnimateTo(target);
    }

    private void AnimateTo(float targetScale)
    {
        if (_rect == null) return;
        if (_coroutine != null) StopCoroutine(_coroutine);
        _coroutine = StartCoroutine(AnimateCoroutine(targetScale));
    }

    private IEnumerator AnimateCoroutine(float targetScale)
    {
        float start = _normalScale.x > 0.001f ? _rect.localScale.x / _normalScale.x : 1f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / _duration;
            float ease = 1f - (1f - t) * (1f - t);
            float s = Mathf.Lerp(start, targetScale, ease);
            _rect.localScale = _normalScale * s;
            yield return null;
        }
        _rect.localScale = _normalScale * targetScale;
        _coroutine = null;
    }
}

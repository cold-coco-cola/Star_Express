using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 按钮点击缩放动画，使点击反馈更丝滑 q 弹。
/// 挂到 Button 上即可生效。面板关闭时自动回弹，避免色块卡在缩小状态。
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonClickAnim : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private float _pressScale = 0.88f;
    [SerializeField] private float _animDuration = 0.12f;
    [SerializeField] private float _releaseOvershoot = 1.04f;

    private RectTransform _rect;
    private Vector3 _normalScale;
    private float _t;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        if (_rect != null) _normalScale = _rect.localScale;
    }

    private void OnDisable()
    {
        if (_rect != null && _normalScale.x > 0.001f)
            _rect.localScale = _normalScale;
        StopAllCoroutines();
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
        bool release = targetScale >= 1f;
        float overshoot = release ? _releaseOvershoot : 1f;

        while (_t < 1f)
        {
            _t += Time.unscaledDeltaTime / duration;
            float ease = 1f - (1f - _t) * (1f - _t);
            float s;
            if (release && _t < 0.7f)
            {
                float u = _t / 0.7f;
                float curve = 1f - (1f - u) * (1f - u);
                s = Mathf.Lerp(start, overshoot, curve);
            }
            else if (release)
            {
                float u = (_t - 0.7f) / 0.3f;
                s = Mathf.Lerp(overshoot, targetScale, 1f - (1f - u) * (1f - u));
            }
            else
            {
                s = Mathf.Lerp(start, targetScale, ease);
            }
            _rect.localScale = _normalScale * s;
            yield return null;
        }
        _rect.localScale = _normalScale * targetScale;
    }
}

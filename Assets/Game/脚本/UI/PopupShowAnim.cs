using UnityEngine;
using System.Collections;

/// <summary>
/// 弹窗出现时的缩放动画，使出现更丝滑。
/// 挂到弹窗根节点上，Enable 时自动播放。
/// </summary>
public class PopupShowAnim : MonoBehaviour
{
        [SerializeField] private float _startScale = 0.92f;
        [SerializeField] private float _duration = 0.28f;

    private RectTransform _rect;
    private Vector3 _targetScale;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        if (_rect != null) _targetScale = _rect.localScale;
    }

    private void OnEnable()
    {
        if (_rect == null) return;
        _rect.localScale = _targetScale * _startScale;
        StopAllCoroutines();
        StartCoroutine(AnimateIn());
    }

        private IEnumerator AnimateIn()
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / _duration;
                float ease = t * t * (3f - 2f * t);
                _rect.localScale = _targetScale * Mathf.Lerp(_startScale, 1f, ease);
                yield return null;
            }
            _rect.localScale = _targetScale;
        }
}

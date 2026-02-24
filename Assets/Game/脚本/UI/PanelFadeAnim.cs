using UnityEngine;
using System.Collections;

namespace Game.Scripts.UI
{
    /// <summary>
    /// 弹窗淡入淡出，挂到弹窗根节点（如 OptionsPanel）。
    /// </summary>
    public class PanelFadeAnim : MonoBehaviour
    {
        [SerializeField] private float _fadeInDuration = 0.5f;
        [SerializeField] private float _fadeOutDuration = 0.25f;

        private CanvasGroup _cg;

        private void Awake()
        {
            _cg = GetComponent<CanvasGroup>();
            if (_cg == null)
                _cg = gameObject.AddComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            _cg.alpha = 0f;
            StopAllCoroutines();
            StartCoroutine(FadeIn());
        }

        public void HideWithFade(System.Action onComplete = null)
        {
            StopAllCoroutines();
            StartCoroutine(FadeOutAndHide(onComplete));
        }

        private IEnumerator FadeIn()
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / _fadeInDuration;
                float ease = t * t * (3f - 2f * t);
                _cg.alpha = Mathf.Lerp(0f, 1f, ease);
                yield return null;
            }
            _cg.alpha = 1f;
        }

        private IEnumerator FadeOutAndHide(System.Action onComplete)
        {
            float t = 0f;
            float startAlpha = _cg.alpha;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / _fadeOutDuration;
                float ease = 1f - (1f - t) * (1f - t) * (1f - t);
                _cg.alpha = Mathf.Lerp(startAlpha, 0f, ease);
                yield return null;
            }
            gameObject.SetActive(false);
            _cg.alpha = 1f;
            onComplete?.Invoke();
        }
    }
}

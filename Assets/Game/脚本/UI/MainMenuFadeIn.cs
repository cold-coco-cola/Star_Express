using UnityEngine;
using System.Collections;

namespace Game.Scripts.UI
{
    /// <summary>
    /// 主菜单整体淡入，挂到 MainMenuManager 或菜单根节点。
    /// </summary>
    public class MainMenuFadeIn : MonoBehaviour
    {
        [SerializeField] private float _duration = 1.4f;
        [SerializeField] private float _startAlpha = 0f;

        private CanvasGroup _cg;

        private void Awake()
        {
            _cg = GetComponent<CanvasGroup>();
            if (_cg == null)
                _cg = gameObject.AddComponent<CanvasGroup>();
            _cg.alpha = _startAlpha;
        }

        private void Start()
        {
            StartCoroutine(FadeIn());
        }

        private IEnumerator FadeIn()
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / _duration;
                float ease = SmoothStep(t);
                _cg.alpha = Mathf.Lerp(_startAlpha, 1f, ease);
                yield return null;
            }
            _cg.alpha = 1f;
        }

        private static float SmoothStep(float t) => t * t * (3f - 2f * t);
    }
}

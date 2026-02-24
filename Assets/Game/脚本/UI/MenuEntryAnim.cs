using UnityEngine;
using System.Collections;

namespace Game.Scripts.UI
{
    /// <summary>
    /// 菜单按钮入场动画：逐个淡入出现，带延迟。
    /// </summary>
    public class MenuEntryAnim : MonoBehaviour
    {
        [SerializeField] private float _delayBetween = 0.18f;
        [SerializeField] private float _animDuration = 0.9f;
        [SerializeField] private float _startAlpha = 0f;

        private void OnEnable()
        {
            SetAllChildrenAlpha(_startAlpha);
            StartCoroutine(AnimateChildren());
        }

        private void SetAllChildrenAlpha(float alpha)
        {
            foreach (Transform t in transform)
            {
                if (!t.gameObject.activeSelf) continue;
                var cg = t.GetComponent<CanvasGroup>();
                if (cg == null) cg = t.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = alpha;
            }
        }

        private IEnumerator AnimateChildren()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();

            var children = new System.Collections.Generic.List<Transform>();
            foreach (Transform t in transform)
                if (t.gameObject.activeSelf)
                    children.Add(t);

            for (int i = 0; i < children.Count; i++)
            {
                StartCoroutine(AnimateOne(children[i]));
                yield return new WaitForSecondsRealtime(_delayBetween);
            }
        }

        private IEnumerator AnimateOne(Transform target)
        {
            var canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = target.gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = _startAlpha;

            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / _animDuration;
                float ease = t * t * (3f - 2f * t);
                canvasGroup.alpha = Mathf.Lerp(_startAlpha, 1f, ease);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }
    }
}

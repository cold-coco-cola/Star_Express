using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace Game.Scripts.UI
{
    /// <summary>
    /// 菜单按钮：初始透明，悬停/点击时灰色背景，过渡丝滑。
    /// </summary>
    public class MenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        public Text buttonText;
        public Image highlightIcon;

        [Header("背景")]
        [Tooltip("悬停/点击时的背景色")]
        public Color hoverBgColor = new Color(0.4f, 0.4f, 0.45f, 0.35f);
        public Color normalBgColor = new Color(1f, 1f, 1f, 0f);

        [Header("背景图片")]
        [Tooltip("悬停时的背景图片（优先级高于背景色）")]
        public Sprite hoverSprite;

        [Header("文字")]
        public Color highlightColor = new Color(1f, 0.6f, 0.2f, 1f);
        public float scaleMultiplier = 1.05f;
        public float transitionDuration = 0.2f;

        private Color _originalTextColor;
        private Vector3 _originalScale;
        private Image _bgImage;
        private Sprite _originalSprite;
        private Coroutine _transitionCoroutine;
        private bool _isHovered;
        private bool _isPressed;
        private bool _hasClickAnim;

        private void Start()
        {
            if (buttonText == null || !IsDirectChild(buttonText.transform))
                buttonText = GetComponentInChildren<Text>();
            if (buttonText != null) _originalTextColor = buttonText.color;
            _originalScale = transform.localScale;
            _bgImage = GetComponent<Image>();
            _hasClickAnim = GetComponent<ButtonClickAnim>() != null;
            if (_bgImage != null)
            {
                _originalSprite = _bgImage.sprite;
                _bgImage.color = normalBgColor;
            }
            if (highlightIcon != null) highlightIcon.enabled = false;
        }

        private bool IsDirectChild(Transform t)
        {
            return t.parent == transform;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            var audio = GetComponentInParent<MenuAudio>();
            if (audio != null) audio.PlayHover();
            UpdateVisual();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            if (!_isPressed)
                UpdateVisual();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isPressed = true;
            UpdateVisual();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPressed = false;
            UpdateVisual();
        }

        public void OnSelect(BaseEventData eventData) { _isHovered = true; UpdateVisual(); }
        public void OnDeselect(BaseEventData eventData) { _isHovered = false; if (!_isPressed) UpdateVisual(); }

        private void UpdateVisual()
        {
            bool show = _isHovered || _isPressed;
            if (_transitionCoroutine != null)
                StopCoroutine(_transitionCoroutine);
            _transitionCoroutine = StartCoroutine(TransitionTo(show));
        }

        private IEnumerator TransitionTo(bool highlight)
        {
            Color startBg = _bgImage != null ? _bgImage.color : normalBgColor;
            Color targetBg = highlight ? hoverBgColor : normalBgColor;
            Color startColor = buttonText != null ? buttonText.color : _originalTextColor;
            Color targetColor = highlight ? highlightColor : _originalTextColor;
            Sprite startSprite = _bgImage != null ? _bgImage.sprite : _originalSprite;
            Sprite targetSprite = highlight && hoverSprite != null ? hoverSprite : _originalSprite;

            if (_bgImage != null && hoverSprite != null)
                _bgImage.sprite = targetSprite;

            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / transitionDuration;
                float ease = EaseOutCubic(t);
                if (_bgImage != null)
                {
                    _bgImage.color = Color.Lerp(startBg, targetBg, ease);
                }
                if (!_hasClickAnim)
                {
                    Vector3 startScale = transform.localScale;
                    Vector3 targetScale = highlight ? _originalScale * scaleMultiplier : _originalScale;
                    transform.localScale = Vector3.Lerp(startScale, targetScale, ease);
                }
                if (buttonText != null)
                    buttonText.color = Color.Lerp(startColor, targetColor, ease);
                if (highlightIcon != null)
                    highlightIcon.enabled = highlight;
                yield return null;
            }
            if (_bgImage != null)
            {
                _bgImage.color = targetBg;
                _bgImage.sprite = targetSprite;
            }
            if (!_hasClickAnim)
                transform.localScale = _originalScale * (highlight ? scaleMultiplier : 1f);
            if (buttonText != null) buttonText.color = targetColor;
            if (highlightIcon != null) highlightIcon.enabled = highlight;
            _transitionCoroutine = null;
        }

        private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

        private void OnDisable()
        {
            if (_transitionCoroutine != null)
                StopCoroutine(_transitionCoroutine);
            if (!_hasClickAnim)
                transform.localScale = _originalScale;
            if (buttonText != null) buttonText.color = _originalTextColor;
            if (_bgImage != null)
            {
                _bgImage.color = normalBgColor;
                _bgImage.sprite = _originalSprite;
            }
        }
    }
}

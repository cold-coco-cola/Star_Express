using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Scripts.UI
{
    /// <summary>
    /// 为按钮添加悬停音效，无视觉变化。挂到 Button 上，需父级有 MenuAudio。
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ButtonHoverSound : MonoBehaviour, IPointerEnterHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {
            var audio = GetComponentInParent<MenuAudio>();
            if (audio != null) audio.PlayHover();
        }
    }
}

using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 关卡内按钮悬停音效。挂到 Button 上，调用 GameplayAudio.PlayHover。
/// 含短冷却避免快速划过多个按钮时音效重叠。
/// </summary>
[RequireComponent(typeof(UnityEngine.UI.Button))]
public class GameplayButtonHoverSound : MonoBehaviour, IPointerEnterHandler
{
    [Tooltip("Same sound minimum interval (seconds), avoid overlap when quickly moving across buttons")]
    [SerializeField] private float _cooldown = 0.03f;

    private static float _lastPlayTime;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (GameplayAudio.Instance == null) return;
        if (Time.unscaledTime - _lastPlayTime < _cooldown) return;
        _lastPlayTime = Time.unscaledTime;
        GameplayAudio.Instance.PlayHover();
    }
}

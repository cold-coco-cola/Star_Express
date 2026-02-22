using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 使 Canvas 背景随摄像机移动和缩放而变化，保持填满视野。
/// 挂到作为 Main Camera 子物体的 Canvas 上；Canvas 需为 World Space 模式。
/// </summary>
public class BackgroundCanvasController : MonoBehaviour
{
    [Tooltip("缩放系数，越大背景越大以覆盖视野")]
    public float scaleFactor = 1.05f;

    private Camera _cam;
    private RectTransform _rect;

    private void Awake()
    {
        _cam = Camera.main;
        _rect = GetComponent<RectTransform>();
        if (_rect == null) _rect = GetComponentInChildren<RectTransform>();
    }

    private void LateUpdate()
    {
        if (_cam == null) _cam = Camera.main;
        if (_cam == null || _rect == null) return;

        float ortho = _cam.orthographicSize;
        float aspect = _cam.aspect;
        float viewHeight = ortho * 2f;
        float viewWidth = viewHeight * aspect;
        float w = viewWidth * scaleFactor;
        float h = viewHeight * scaleFactor;
        _rect.sizeDelta = new Vector2(w, h);
    }
}

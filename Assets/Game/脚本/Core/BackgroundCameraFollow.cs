using UnityEngine;

/// <summary>
/// 使背景随摄像机移动和缩放而变化，保持填满视野。使用平滑插值使过渡自然。
/// </summary>
public class BackgroundCameraFollow : MonoBehaviour
{
    [Tooltip("缩放系数，越大背景越大以覆盖视野")]
    public float scaleFactor = 1.2f;
    [Tooltip("位置平滑速度，越大跟随越快")]
    public float positionSmoothSpeed = 8f;
    [Tooltip("缩放平滑速度，越大过渡越快")]
    public float scaleSmoothSpeed = 6f;

    private Camera _cam;
    private SpriteRenderer _sr;
    private float _baseScale = 1f;
    private Vector3 _targetPosition;
    private float _targetScale = 1f;

    private void Awake()
    {
        _cam = Camera.main;
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null && _sr.sprite != null)
        {
            var bounds = _sr.sprite.bounds;
            float maxExtent = Mathf.Max(bounds.size.x, bounds.size.y);
            _baseScale = maxExtent > 0.001f ? 1f / maxExtent : 1f;
        }
        _targetPosition = transform.position;
        _targetScale = transform.localScale.x;
    }

    private void LateUpdate()
    {
        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        float ortho = _cam.orthographicSize;
        float aspect = _cam.aspect;
        float viewHeight = ortho * 2f;
        float viewWidth = viewHeight * aspect;
        float viewMax = Mathf.Max(viewWidth, viewHeight) * scaleFactor;
        _targetScale = viewMax * _baseScale;
        _targetPosition = new Vector3(_cam.transform.position.x, _cam.transform.position.y, transform.position.z);

        float dt = Time.deltaTime;
        transform.position = Vector3.Lerp(transform.position, _targetPosition, 1f - Mathf.Exp(-positionSmoothSpeed * dt));
        float s = Mathf.Lerp(transform.localScale.x, _targetScale, 1f - Mathf.Exp(-scaleSmoothSpeed * dt));
        transform.localScale = new Vector3(s, s, 1f);
    }
}

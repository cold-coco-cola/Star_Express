using UnityEngine;

/// <summary>
/// 使背景随摄像机移动和缩放而变化，保持填满视野。
/// 支持基础漂移（缓慢移动营造空灵感）及开局略放大。
/// </summary>
public class BackgroundCameraFollow : MonoBehaviour
{
    [Tooltip("缩放系数，越大背景越大以覆盖视野")]
    public float scaleFactor = 1.2f;
    [Tooltip("开局缩放倍率，略大于 1 时开局更放大，随时间过渡到 1")]
    public float initialScaleFactor = 1.15f;
    [Tooltip("开局放大过渡时长（秒），0 则无过渡")]
    public float initialScaleDuration = 30f;
    [Tooltip("位置平滑速度，越大跟随越快")]
    public float positionSmoothSpeed = 8f;
    [Tooltip("缩放平滑速度，越大过渡越快")]
    public float scaleSmoothSpeed = 6f;

    [Header("基础漂移（空灵效果）")]
    [Tooltip("漂移幅度（世界单位），0 则无漂移")]
    public float driftAmount = 0.15f;
    [Tooltip("漂移周期（秒），越大移动越慢")]
    public float driftPeriodX = 22f;
    [Tooltip("Y 方向漂移周期，与 X 略不同可形成椭圆轨迹")]
    public float driftPeriodY = 30f;
    [Tooltip("基础缩放幅度（倍率），0 则无缩放变化")]
    public float driftScaleAmount = 0.03f;
    [Tooltip("缩放周期（秒）")]
    public float driftScalePeriod = 25f;

    private Camera _cam;
    private SpriteRenderer _sr;
    private float _baseScale = 1f;
    private Vector3 _targetPosition;
    private float _targetScale = 1f;
    private float _gameTime;

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

        _gameTime += Time.deltaTime;

        float ortho = _cam.orthographicSize;
        float aspect = _cam.aspect;
        float viewHeight = ortho * 2f;
        float viewWidth = viewHeight * aspect;
        float scaleMult = scaleFactor;
        if (initialScaleDuration > 0 && initialScaleFactor > 1f)
        {
            float t = Mathf.Clamp01(_gameTime / initialScaleDuration);
            scaleMult *= Mathf.Lerp(initialScaleFactor, 1f, t);
        }
        float viewMax = Mathf.Max(viewWidth, viewHeight) * scaleMult;
        _targetScale = viewMax * _baseScale;
        if (driftScaleAmount > 0 && driftScalePeriod > 0)
        {
            float scaleDrift = 1f + Mathf.Sin(_gameTime * (2f * Mathf.PI / driftScalePeriod)) * driftScaleAmount;
            _targetScale *= scaleDrift;
        }

        Vector3 camPos = _cam.transform.position;
        Vector3 drift = Vector3.zero;
        if (driftAmount > 0 && driftPeriodX > 0 && driftPeriodY > 0)
        {
            float px = Mathf.Sin(_gameTime * (2f * Mathf.PI / driftPeriodX)) * driftAmount;
            float py = Mathf.Cos(_gameTime * (2f * Mathf.PI / driftPeriodY)) * driftAmount;
            drift = new Vector3(px, py, 0f);
        }
        _targetPosition = new Vector3(camPos.x, camPos.y, transform.position.z) + drift;

        float dt = Time.deltaTime;
        transform.position = Vector3.Lerp(transform.position, _targetPosition, 1f - Mathf.Exp(-positionSmoothSpeed * dt));
        float s = Mathf.Lerp(transform.localScale.x, _targetScale, 1f - Mathf.Exp(-scaleSmoothSpeed * dt));
        transform.localScale = new Vector3(s, s, 1f);
    }
}

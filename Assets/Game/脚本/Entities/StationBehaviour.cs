using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 站点行为（PRD §4.1）：
/// - 持有站点数据（id, shapeType, isUnlocked）
/// - 独立计时器生成乘客（PRD §3.3）
/// - 维护排队乘客列表，参与拥挤判定
/// </summary>
public class StationBehaviour : MonoBehaviour
{
    [Header("由 LevelLoader 注入")]
    public string id;
    public ShapeType stationType;
    public string displayName;

    [Header("状态")]
    public bool isUnlocked;

    [Header("排队与容量")]
    public List<Passenger> waitingPassengers = new List<Passenger>();
    public int queueCapacity = 8;
    public int crowdingThreshold = 6;

    [Header("乘客生成")]
    [SerializeField] private float _spawnTimer;
    private int _nextPassengerId;
    private bool _spawnTimerInitialized;

    private SpriteRenderer _visualRenderer;
    private bool _highlighted;
    private static readonly Color HighlightColor = new Color(1f, 1f, 0.4f, 1f);

    [Header("生成动画")]
    [SerializeField] private float _spawnAnimDuration = 2.8f;
    [Tooltip("最大 overshoot 倍率：先放大再缩回，更明显")]
    [SerializeField] private float _spawnOvershootMax = 1.65f;
    private float _spawnAnimProgress = 1f;

    [Header("点击弹起")]
    [SerializeField] private float _clickPopDuration = 1.2f;
    [SerializeField] private float _clickPopScale = 1.25f;
    [SerializeField] private float _clickPopShakeAmount = 4f;
    private float _clickPopProgress = 1f;

    [Header("过载视觉")]
    [SerializeField] private float _overloadShakeAmount = 8f;
    [SerializeField] private float _overloadShakeAmountVertical = 0.12f;
    private float _overloadShakePhase;

    public bool IsCrowded => waitingPassengers.Count >= crowdingThreshold;

    private SpriteRenderer _overloadBarFill;
    private GameObject _overloadBarRoot;

    private void Awake()
    {
        var visual = transform.Find("Visual");
        _visualRenderer = visual != null ? visual.GetComponent<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>();
    }

    /// <summary>播放从小变大的生成动画，含黑→原色过渡。</summary>
    public void PlaySpawnAnimation()
    {
        _spawnAnimProgress = 0f;
        if (_visualRenderer != null) _visualRenderer.color = Color.black;
    }

    private void Update()
    {
        if (_spawnAnimProgress < 1f)
        {
            _spawnAnimProgress += Time.deltaTime / Mathf.Max(0.01f, _spawnAnimDuration);
            if (_spawnAnimProgress > 1f) _spawnAnimProgress = 1f;
            ApplySpawnAnimScale();
            if (_visualRenderer != null)
                _visualRenderer.color = Color.Lerp(Color.black, Color.white, _spawnAnimProgress);
        }
        else if (_visualRenderer != null && _spawnAnimProgress >= 1f)
        {
            if (!_highlighted) _visualRenderer.color = isUnlocked ? Color.white : new Color(1f, 1f, 1f, 0.5f);
        }
        if (_clickPopProgress < 1f)
        {
            _clickPopProgress += Time.deltaTime / Mathf.Max(0.01f, _clickPopDuration);
            if (_clickPopProgress > 1f) _clickPopProgress = 1f;
        }

        UpdateOverloadVisual();

        if (!isUnlocked) return;
        if (GameManager.Instance == null) return;

        float interval = GetSpawnInterval();
        if (!_spawnTimerInitialized)
        {
            _spawnTimer = Random.Range(0f, interval);
            _spawnTimerInitialized = true;
        }
        _spawnTimer += Time.deltaTime;

        var balance = GameManager.Instance.gameBalance;
        int count = balance != null ? balance.passengerSpawnCountPerStation : 1;
        float subInterval = count > 1 ? interval / count : interval;

        while (_spawnTimer >= subInterval)
        {
            _spawnTimer -= subInterval;
            SpawnPassenger();
        }
    }

    /// <summary>获取当前生成间隔（考虑第一关 overrides 的分段间隔）。</summary>
    private float GetSpawnInterval()
    {
        var gm = GameManager.Instance;
        if (gm == null) return 10f;

        var balance = gm.gameBalance;
        float defaultInterval = balance != null ? balance.passengerSpawnInterval : 10f;

        var config = gm.levelConfig;
        if (config != null && config.overrides != null)
        {
            var ov = config.overrides;
            if (gm.CurrentWeek >= ov.passengerSpawnIntervalAfterWeeks && ov.passengerSpawnIntervalLate > 0)
                return ov.passengerSpawnIntervalLate;
            if (ov.passengerSpawnInterval > 0)
                return ov.passengerSpawnInterval;
        }

        return defaultInterval;
    }

    /// <summary>
    /// 生成一名乘客（PRD §3.3）。
    /// 目标形状：在已解锁站点的形状中均匀随机；站点不生成与自己同类的乘客。
    /// </summary>
    private void SpawnPassenger()
    {
        var targetShape = PickTargetShape();
        if (targetShape == null) return;

        var go = new GameObject($"Passenger_{id}_{_nextPassengerId++}");
        go.transform.SetParent(transform, false);
        var p = go.AddComponent<Passenger>();
        p.id = go.name;
        p.targetShape = targetShape.Value;
        p.state = Passenger.PassengerState.Waiting;
        p.currentStation = this;
        p.targetStationId = PickTargetStationId(targetShape.Value);

        waitingPassengers.Add(p);
        p.UpdateQueuePosition(waitingPassengers.Count - 1);
        p.ApplyVisual();
        p.PlaySpawnAnimation();
    }

    /// <summary>从已解锁站点的形状中均匀随机选取目标形状；站点不生成与自己同类的乘客。</summary>
    private ShapeType? PickTargetShape()
    {
        if (GameManager.Instance == null) return null;
        var stations = GameManager.Instance.GetAllStations();
        if (stations == null || stations.Count == 0) return null;

        var shapes = new List<ShapeType>();
        foreach (var kv in stations)
        {
            if (!kv.Value.isUnlocked) continue;
            var shape = kv.Value.stationType;
            if (shape == stationType) continue; // 不生成与自己同类的乘客
            if (!shapes.Contains(shape))
                shapes.Add(shape);
        }

        if (shapes.Count == 0) return null;
        return shapes[Random.Range(0, shapes.Count)];
    }

    /// <summary>若多站同形状，返回 null，让乘客在第一个到达的同形状站下车；单站时返回其 id 以便精确匹配。</summary>
    private string PickTargetStationId(ShapeType shape)
    {
        if (GameManager.Instance == null) return null;
        var stations = GameManager.Instance.GetAllStations();
        var candidates = new List<string>();
        foreach (var kv in stations)
        {
            if (kv.Value.isUnlocked && kv.Value.stationType == shape && kv.Value != this)
                candidates.Add(kv.Key);
        }
        if (candidates.Count == 0)
        {
            foreach (var kv in stations)
            {
                if (kv.Value.isUnlocked && kv.Value.stationType == shape)
                    candidates.Add(kv.Key);
            }
        }
        if (candidates.Count == 0) return null;
        if (candidates.Count == 1) return candidates[0];
        return null; // 多站同形状：不指定具体站，在第一个到达的同形状站下车
    }

    /// <summary>刷新排队乘客的站台位置，并确保缩放正确（多线经停时避免异常放大）。</summary>
    public void RefreshPassengerPositions()
    {
        for (int i = 0; i < waitingPassengers.Count; i++)
        {
            var p = waitingPassengers[i];
            if (p == null) continue;
            p.UpdateQueuePosition(i);
            if (p.state == Passenger.PassengerState.Waiting)
                p.transform.localScale = Vector3.one;
        }
    }

    private void ApplySpawnAnimScale()
    {
        if (_visualRenderer == null) return;
        var visual = _visualRenderer.transform;
        float t = _spawnAnimProgress;
        float baseScale = LevelLoader.GetStationVisualScale(_visualRenderer.sprite);
        float ortho = Camera.main != null ? Camera.main.orthographicSize : 10f;
        float overshoot = Mathf.Lerp(1.2f, _spawnOvershootMax, Mathf.Clamp01(ortho / 15f));
        float scale = baseScale * EaseElasticSpawn(t, overshoot) * GetClickPopScale();
        visual.localScale = Vector3.one * Mathf.Max(0.01f, scale);
    }

    /// <summary>点击弹起缩放：1→放大(前25%)→缩回(后75%)，缩回放慢更舒缓。</summary>
    private float GetClickPopScale()
    {
        if (_clickPopProgress >= 1f) return 1f;
        float t = _clickPopProgress;
        if (t < 0.22f)
        {
            float u = t / 0.22f;
            return Mathf.Lerp(1f, _clickPopScale, 1f - (1f - u) * (1f - u));
        }
        float u2 = (t - 0.22f) / 0.78f;
        float decay = Mathf.Exp(-2.2f * u2);
        float bounce = Mathf.Cos(2.5f * Mathf.PI * u2);
        return 1f + (_clickPopScale - 1f) * decay * bounce;
    }

    private float GetClickPopShake()
    {
        if (_clickPopProgress >= 1f) return 0f;
        float t = _clickPopProgress;
        float decay = 1f - t * t;
        return Mathf.Sin(t * 12f * Mathf.PI) * decay * _clickPopShakeAmount;
    }

    /// <summary>弹性缓动：0→快速到 overshoot→回弹→轻微 overshoot→1，先放大再缩回更明显。</summary>
    private static float EaseElasticSpawn(float t, float overshoot)
    {
        if (t < 0.18f)
        {
            float u = t / 0.18f;
            float easeOut = 1f - (1f - u) * (1f - u) * (1f - u);
            return easeOut * overshoot;
        }
        float u2 = (t - 0.18f) / 0.82f;
        float decay = Mathf.Exp(-2.5f * u2);
        float bounce = Mathf.Cos(3.5f * Mathf.PI * u2);
        float elastic = 1f + (overshoot - 1f) * decay * bounce;
        if (t > 0.88f)
            return Mathf.Lerp(elastic, 1f, (t - 0.88f) / 0.12f);
        return elastic;
    }

    private void UpdateOverloadVisual()
    {
        int deathThreshold = 8;
        float deathDuration = 20f;
        var balance = GameManager.Instance != null ? GameManager.Instance.gameBalance : null;
        if (balance != null) { deathThreshold = balance.deathThreshold; deathDuration = balance.deathDurationSeconds; }

        bool overload = waitingPassengers.Count >= deathThreshold;
        float timer = GameManager.Instance != null ? GameManager.Instance.GetDeathCrowdTimer(this) : 0f;
        float fill = deathDuration > 0 ? Mathf.Clamp01(timer / deathDuration) : 0f;

        if (_visualRenderer != null && _spawnAnimProgress >= 1f)
        {
            var visual = _visualRenderer.transform;
            float baseScale = LevelLoader.GetStationVisualScale(_visualRenderer.sprite);
            if (overload)
            {
                float freq = 4f + fill * 22f;
                _overloadShakePhase += Time.deltaTime * freq;
                float shakeAmount = _overloadShakeAmount * (0.7f + fill * 0.5f);
                float popScale = GetClickPopScale();
                float pulseScale = 1f + 0.22f * (0.5f + 0.5f * Mathf.Sin(_overloadShakePhase));
                if (stationType == ShapeType.Circle)
                {
                    float vy = Mathf.Sin(_overloadShakePhase) * _overloadShakeAmountVertical * (0.8f + fill * 0.6f);
                    visual.localRotation = Quaternion.identity;
                    visual.localPosition = new Vector3(0, vy, visual.localPosition.z);
                    visual.localScale = Vector3.one * (baseScale * pulseScale * popScale);
                }
                else
                {
                    float shake = Mathf.Sin(_overloadShakePhase) * shakeAmount;
                    visual.localRotation = Quaternion.Euler(0, 0, shake);
                    visual.localPosition = new Vector3(0, 0, visual.localPosition.z);
                    visual.localScale = Vector3.one * (baseScale * pulseScale * popScale);
                }
                _visualRenderer.color = Color.Lerp(Color.white, new Color(0.75f, 0.35f, 0.35f), fill);
            }
            else
            {
                _overloadShakePhase = 0f;
                if (!_highlighted)
                    _visualRenderer.color = isUnlocked ? Color.white : new Color(1f, 1f, 1f, 0.5f);
                float shake = GetClickPopShake();
                visual.localRotation = Quaternion.Euler(0, 0, shake);
                visual.localPosition = new Vector3(0, 0, visual.localPosition.z);
                visual.localScale = Vector3.one * (baseScale * GetClickPopScale());
            }
        }

        if (overload || fill > 0.01f)
        {
            EnsureOverloadBar();
            if (_overloadBarRoot != null)
            {
                _overloadBarRoot.SetActive(true);
                if (_overloadBarFill != null)
                {
                    float r = 0.5f + fill * 0.45f;
                    float g = 0.35f - fill * 0.25f;
                    float b = 0.35f - fill * 0.25f;
                    _overloadBarFill.color = new Color(r, g, b, 0.4f + fill * 0.35f);
                    float barHeight = overload ? Mathf.Max(0.15f, 0.5f * fill) : 0.5f * fill;
                    _overloadBarFill.transform.localScale = new Vector3(0.08f, barHeight, 1f);
                }
            }
        }
        else if (_overloadBarRoot != null)
            _overloadBarRoot.SetActive(false);
    }

    private void EnsureOverloadBar()
    {
        if (_overloadBarRoot != null) return;
        _overloadBarRoot = new GameObject("OverloadBar");
        _overloadBarRoot.transform.SetParent(transform, false);
        _overloadBarRoot.transform.localPosition = new Vector3(0.6f, 0, -0.05f);

        var bg = new GameObject("BarBg");
        bg.transform.SetParent(_overloadBarRoot.transform, false);
        bg.transform.localPosition = Vector3.zero;
        bg.transform.localScale = new Vector3(0.08f, 0.5f, 1f);
        var bgSr = bg.AddComponent<SpriteRenderer>();
        bgSr.sprite = CreatePixelSprite();
        bgSr.color = new Color(0.3f, 0.3f, 0.35f, 0.5f);
        bgSr.sortingLayerID = SortingOrderConstants.ShipsLayerId;
        bgSr.sortingOrder = SortingOrderConstants.OverloadBarBg;

        var fillGo = new GameObject("BarFill");
        fillGo.transform.SetParent(_overloadBarRoot.transform, false);
        fillGo.transform.localPosition = new Vector3(0, -0.25f, -0.01f);
        _overloadBarFill = fillGo.AddComponent<SpriteRenderer>();
        _overloadBarFill.sprite = CreatePixelSprite();
        _overloadBarFill.color = new Color(0.8f, 0.2f, 0.2f, 0.6f);
        _overloadBarFill.sortingLayerID = SortingOrderConstants.ShipsLayerId;
        _overloadBarFill.sortingOrder = SortingOrderConstants.OverloadBarFill;
    }

    private static Sprite CreatePixelSprite()
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0f));
    }

    /// <summary>根据 isUnlocked 更新 Visual。</summary>
    public void RefreshVisual()
    {
        if (_visualRenderer == null)
            _visualRenderer = GetComponentInChildren<SpriteRenderer>();
        if (_visualRenderer == null) return;
        if (_highlighted) { _visualRenderer.color = HighlightColor; return; }

        _visualRenderer.color = isUnlocked ? Color.white : new Color(1f, 1f, 1f, 0.5f);
    }

    /// <summary>连线输入用：点击站点时的弹起反馈。</summary>
    public void PlayClickPop()
    {
        _clickPopProgress = 0f;
    }

    /// <summary>连线输入用：高亮选中站点。</summary>
    public void SetHighlight(bool on)
    {
        _highlighted = on;
        if (_visualRenderer == null)
            _visualRenderer = GetComponentInChildren<SpriteRenderer>(true);
        if (_visualRenderer == null) return;
        if (on)
            _visualRenderer.color = HighlightColor;
        else
            RefreshVisual();
    }
}

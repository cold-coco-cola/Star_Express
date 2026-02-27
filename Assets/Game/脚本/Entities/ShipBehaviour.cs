using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 飞船行为（PRD §4.4 + §5.1）：
/// - 沿 line 往返移动，到站停靠 dockDuration 后继续；端点调头
/// - 停靠时执行：卸客·目的地 → 卸客·换乘 → 载客（PRD §5.1）
/// - 视觉：放置的飞船与线路颜色一致
/// </summary>
public class ShipBehaviour : MonoBehaviour
{
    public enum ShipState { Moving, Docked }

    [Header("由放置逻辑注入")]
    public string id;
    public Line line;

    [Header("载客")]
    public int capacity = 4;
    public List<Passenger> passengers = new List<Passenger>();

    [Header("运行时状态")]
    public int currentSegmentIndex;
    public int direction = 1;
    public float progressOnSegment;
    public ShipState state = ShipState.Moving;
    public float dockRemainingTime;

    private float _speed;
    private float _dockDuration;
    private SpriteRenderer _cachedPrimaryRenderer;
    private bool _dockedAtSegmentEnd;
    private int _arrivalDirection;
    private bool _dockProcessed;
    private bool _initialDockDone;

    [Header("悬停高亮")]
    private bool _highlighted;
    private static readonly Color HighlightColor = new Color(1f, 1f, 0.85f, 0.4f);
    [SerializeField] private float _hoverScale = 1.5f;
    private float _hoverScaleProgress = 1f;
    private float _hoverScaleDuration = 0.15f;

    [Header("升级动画")]
    private float _upgradeAnimProgress = 1f;
    [SerializeField] private float _upgradeAnimDuration = 0.3f;
    [SerializeField] private float _upgradePopScale = 1.2f;
    [SerializeField] private float _upgradeShakeAmount = 8f;

    private static Sprite _placeholderShipSprite;
    private static Material _shipMaterial;
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private const float ShipZ = 0f;
    private static int GetShipSortingLayerId() => SortingOrderConstants.ShipsLayerId;

    private void Start()
    {
        var balance = GameManager.Instance != null ? GameManager.Instance.gameBalance : null;
        _speed = balance != null ? balance.shipSpeedUnitsPerSecond : 1.5f;
        _dockDuration = balance != null ? balance.dockDurationSeconds : 1f;
        if (capacity <= 0)
        {
            capacity = balance != null ? balance.shipCapacity : 4;
        }
        EnsureClickCollider();
        ApplyVisual();
    }

    /// <summary>确保有 Collider2D，供放置客舱时点击检测。</summary>
    private void EnsureClickCollider()
    {
        if (GetComponent<Collider2D>() != null) return;
        var c = gameObject.AddComponent<CircleCollider2D>();
        c.radius = 0.6f;
        c.isTrigger = true;
    }

    private void OnMouseEnter()
    {
        if (!IsInPlacementMode()) return;
        _highlighted = true;
        _hoverScaleProgress = 0f;
        ApplyHighlight();
    }

    private void OnMouseExit()
    {
        if (!_highlighted) return;
        _highlighted = false;
        _hoverScaleProgress = 0f;
        ApplyHighlight();
        ResetScale();
    }

    private void ResetScale()
    {
        float scaleByCapacity = 1f + (capacity - 4) * 0.08f;
        transform.localScale = new Vector3(1f * scaleByCapacity, 0.5f * scaleByCapacity, 1f);
    }

    private bool IsInPlacementMode()
    {
        return GameplayUIController.Instance != null
            && GameplayUIController.Instance.CurrentState == GameplayUIState.PlacingCarriage;
    }

    private void ApplyHighlight()
    {
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr == null) return;
        sr.color = _highlighted ? HighlightColor : GetShipDisplayColor();
    }

    private void ApplyHoverScale()
    {
        float scaleByCapacity = 1f + (capacity - 4) * 0.08f;
        float hoverScale = _highlighted 
            ? Mathf.Lerp(1f, _hoverScale, _hoverScaleProgress) 
            : Mathf.Lerp(_hoverScale, 1f, _hoverScaleProgress);
        transform.localScale = new Vector3(1f * scaleByCapacity * hoverScale, 0.5f * scaleByCapacity * hoverScale, 1f);
    }

    /// <summary>升级成功时播放放大回弹+摇晃动画。</summary>
    public void PlayUpgradeAnimation()
    {
        _upgradeAnimProgress = 0f;
    }

    private Color GetShipDisplayColor()
    {
        if (line == null) return Color.white;
        if (line.displayColor != default(Color))
            return new Color(line.displayColor.r, line.displayColor.g, line.displayColor.b, 1f);
        Color lineColor = ResolveLineColor(line.color);
        return new Color(lineColor.r, lineColor.g, lineColor.b, 1f);
    }

    #region 视觉 (Visual)

    public void ApplyVisual()
    {
        Sprite shipSprite = ResolveShipSprite();
        EnsureAtLeastOneRenderer(shipSprite);

        Color shipColor = Color.white;
        if (line != null)
        {
            if (line.displayColor != default(Color))
                shipColor = new Color(line.displayColor.r, line.displayColor.g, line.displayColor.b, 1f);
            else
            {
                Color lineColor = ResolveLineColor(line.color);
                shipColor = new Color(lineColor.r, lineColor.g, lineColor.b, 1f);
                line.displayColor = shipColor;
            }
        }

        Material runtimeMat = GetOrCreateShipMaterial();
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr.GetComponentInParent<Passenger>() != null) continue;
            ApplyShipVisualToRenderer(sr, shipColor, shipSprite, runtimeMat);
        }

        float scaleByCapacity = 1f + (capacity - 4) * 0.08f;
        if (_upgradeAnimProgress >= 1f && !_highlighted)
        {
            transform.localScale = new Vector3(1f * scaleByCapacity, 0.5f * scaleByCapacity, 1f);
        }

        EnsureCarriageIndicators();
        RefreshPassengerPositionsOnShip();
        if (_highlighted) ApplyHighlight();
    }

    /// <summary>客舱升级视觉：在飞船后方显示小圆点，每个客舱+2 显示 1 个。</summary>
    private void EnsureCarriageIndicators()
    {
        int extraCarriages = Mathf.Max(0, (capacity - 4) / 2);
        var container = transform.Find("CarriageIndicators");
        if (container == null)
        {
            container = new GameObject("CarriageIndicators").transform;
            container.SetParent(transform, false);
            container.localPosition = Vector3.zero;
            container.localRotation = Quaternion.identity;
            container.localScale = Vector3.one;
        }
        int existing = container.childCount;
        for (int i = existing; i < extraCarriages; i++)
        {
            var dot = new GameObject("Carriage_" + i);
            dot.transform.SetParent(container, false);
            dot.transform.localPosition = new Vector3(-0.5f - i * 0.25f, -0.15f, -0.02f);
            dot.transform.localScale = Vector3.one * 0.2f;
            var sr = dot.AddComponent<SpriteRenderer>();
            sr.sprite = GetOrCreateCarriageDotSprite();
            sr.color = new Color(1f, 0.9f, 0.6f, 0.9f);
            sr.sortingLayerID = GetShipSortingLayerId();
            sr.sortingOrder = SortingOrderConstants.ShipCarriageIndicator;
        }
        for (int i = container.childCount - 1; i >= extraCarriages; i--)
        {
            if (Application.isPlaying)
                Destroy(container.GetChild(i).gameObject);
            else
                DestroyImmediate(container.GetChild(i).gameObject);
        }
    }

    /// <summary>船上乘客相对站点乘客等比缩小，不拉伸。抵消飞船 Y 压缩，停靠时向船尾方向偏移。</summary>
    public void RefreshPassengerPositionsOnShip()
    {
        var shipScale = transform.localScale;
        float dockOffsetX = (state == ShipState.Docked) ? -0.1f : 0f;

        for (int i = 0; i < passengers.Count; i++)
        {
            var p = passengers[i];
            if (p == null) continue;
            if (p.currentShip != this) continue;
            if (p.transform.parent != transform)
                p.transform.SetParent(transform, false);
            p.gameObject.SetActive(true);
            float row = i / 4;
            float col = i % 4;
            float x = (col - 1.5f) * 0.25f + 0.05f;
            float y = - row * 0.45f - 0.35f;
            p.transform.localPosition = new Vector3(x, y, -0.05f);
            p.transform.localScale = new Vector3(1f / shipScale.x, 1f / shipScale.y, 1f / shipScale.z);
            var iconSr = p.GetComponentInChildren<SpriteRenderer>();
            if (iconSr != null)
            {
                iconSr.sortingLayerID = SortingOrderConstants.ShipsLayerId;
                iconSr.sortingOrder = SortingOrderConstants.Passenger;
                iconSr.enabled = true;
                float iconScale = Passenger.GetShipPassengerIconScale(iconSr.sprite, p.targetShape);
                iconSr.transform.localScale = Vector3.one * iconScale;
                if (iconSr.sprite == null)
                    iconSr.sprite = Passenger.GetPlaceholderShapeSpriteForShip();
                iconSr.color = new Color(0.85f, 0.85f, 0.95f, 0.5f);
            }
        }
    }

    private static Color ResolveLineColor(LineColor lineColor)
    {
        var vc = GameManager.Instance != null ? GameManager.Instance.visualConfig : null;
        if (vc != null && vc.lineColors != null && vc.lineColors.Length > 0)
        {
            int idx = (int)lineColor;
            if (idx >= 0 && idx < vc.lineColors.Length)
            {
                Color c = vc.lineColors[idx];
                if (c.r < 0.9f || c.g < 0.9f || c.b < 0.9f) return c;
            }
        }
        return lineColor switch
        {
            LineColor.Red => Color.red,
            LineColor.Green => Color.green,
            LineColor.Blue => Color.blue,
            LineColor.Yellow => Color.yellow,
            LineColor.Cyan => Color.cyan,
            LineColor.Magenta => Color.magenta,
            _ => Color.white
        };
    }

    private static Sprite ResolveShipSprite()
    {
        var vc = GameManager.Instance != null ? GameManager.Instance.visualConfig : null;
        if (vc != null && vc.shipSprite != null) return vc.shipSprite;
        return GetOrCreateShipPlaceholderSprite();
    }

    private void EnsureAtLeastOneRenderer(Sprite sprite)
    {
        if (GetComponentInChildren<SpriteRenderer>(true) != null) return;
        var sr = EnsureVisualRenderer();
        if (sprite != null && sr != null) sr.sprite = sprite;
    }

    private SpriteRenderer EnsureVisualRenderer()
    {
        Transform visual = transform.Find("Visual");
        if (visual != null)
        {
            var sr = visual.GetComponent<SpriteRenderer>();
            if (sr != null) return sr;
            sr = visual.gameObject.AddComponent<SpriteRenderer>();
            sr.sortingLayerID = GetShipSortingLayerId();
            sr.sortingOrder = SortingOrderConstants.Ship;
            return sr;
        }
        var go = new GameObject("Visual");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        go.layer = gameObject.layer;
        var newSr = go.AddComponent<SpriteRenderer>();
        newSr.sortingLayerID = GetShipSortingLayerId();
        newSr.sortingOrder = SortingOrderConstants.Ship;
        return newSr;
    }

    private static Material GetOrCreateShipMaterial()
    {
        if (_shipMaterial != null) return _shipMaterial;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("UI/Default");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        if (shader != null)
            _shipMaterial = new Material(shader);
        return _shipMaterial;
    }

    private static void ApplyShipVisualToRenderer(SpriteRenderer sr, Color color, Sprite fallbackSprite, Material runtimeMaterial)
    {
        if (sr == null) return;
        if (runtimeMaterial != null)
            sr.material = new Material(runtimeMaterial);
        if (fallbackSprite != null && sr.sprite == null)
            sr.sprite = fallbackSprite;
        Color c = new Color(color.r, color.g, color.b, 1f);
        sr.color = c;
        sr.SetPropertyBlock(null);
        if (sr.material != null)
        {
            Color white = Color.white;
            if (sr.material.HasProperty(ColorId)) sr.material.SetColor(ColorId, white);
            if (sr.material.HasProperty(BaseColorId)) sr.material.SetColor(BaseColorId, white);
        }
        sr.sortingLayerID = GetShipSortingLayerId();
        sr.sortingOrder = SortingOrderConstants.Ship;
        sr.enabled = true;
    }

    private static Sprite GetOrCreateShipPlaceholderSprite()
    {
        if (_placeholderShipSprite != null) return _placeholderShipSprite;
        var tex = new Texture2D(64, 64);
        var fill = new Color32(255, 255, 255, 255);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float dx = x / 63f - 0.5f;
                float dy = y / 63f - 0.5f;
                tex.SetPixel(x, y, (dx * dx + dy * dy <= 0.25f) ? fill : new Color32(0, 0, 0, 0));
            }
        tex.Apply();
        _placeholderShipSprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        return _placeholderShipSprite;
    }

    private static Sprite _carriageDotSprite;
    private static Sprite GetOrCreateCarriageDotSprite()
    {
        if (_carriageDotSprite != null) return _carriageDotSprite;
        var tex = new Texture2D(16, 16);
        var fill = new Color32(255, 255, 255, 255);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                float dx = x / 15f - 0.5f;
                float dy = y / 15f - 0.5f;
                tex.SetPixel(x, y, (dx * dx + dy * dy <= 0.3f) ? fill : new Color32(0, 0, 0, 0));
            }
        tex.Apply();
        _carriageDotSprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
        return _carriageDotSprite;
    }

    #endregion

    #region 移动与停靠 (Movement & Docking)

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
        if (line == null || line.stationSequence == null || line.stationSequence.Count < 2) return;

        _cachedPrimaryRenderer = _cachedPrimaryRenderer != null ? _cachedPrimaryRenderer : GetComponentInChildren<SpriteRenderer>(true);
        if (_cachedPrimaryRenderer != null && _cachedPrimaryRenderer.sprite == null)
            ApplyVisual();

        var seq = line.stationSequence;
        int maxSeg = seq.Count - 2;
        if (maxSeg < 0) return;
        currentSegmentIndex = Mathf.Clamp(currentSegmentIndex, 0, maxSeg);

        if (state == ShipState.Moving && currentSegmentIndex == 0 && progressOnSegment <= 0.0001f && direction == 1 && !_initialDockDone)
        {
            _initialDockDone = true;
            EnterDocked(false);
        }

        if (state == ShipState.Moving)
            UpdateMoving();
        else
            UpdateDocked();

        if (!IsInPlacementMode() && _highlighted)
        {
            _highlighted = false;
            _hoverScaleProgress = 0f;
            ApplyHighlight();
            ResetScale();
        }

        if (_hoverScaleProgress < 1f && _highlighted)
        {
            _hoverScaleProgress += Time.deltaTime / _hoverScaleDuration;
            if (_hoverScaleProgress > 1f) _hoverScaleProgress = 1f;
        }

        if (_upgradeAnimProgress >= 1f && _highlighted)
        {
            ApplyHoverScale();
        }

        if (_upgradeAnimProgress < 1f)
        {
            _upgradeAnimProgress += Time.deltaTime / _upgradeAnimDuration;
            if (_upgradeAnimProgress > 1f) _upgradeAnimProgress = 1f;
            ApplyUpgradeAnim();
        }
    }

    private void ApplyUpgradeAnim()
    {
        float t = _upgradeAnimProgress;
        float scale = 1f;
        if (t < 0.3f)
            scale = Mathf.Lerp(1f, _upgradePopScale, t / 0.3f);
        else if (t < 0.6f)
            scale = Mathf.Lerp(_upgradePopScale, 0.95f, (t - 0.3f) / 0.3f);
        else
            scale = Mathf.Lerp(0.95f, 1f, (t - 0.6f) / 0.4f);

        float shake = Mathf.Sin(t * Mathf.PI * 8) * _upgradeShakeAmount * (1f - t);

        float scaleByCapacity = 1f + (capacity - 4) * 0.08f;
        var baseScale = new Vector3(1f, 0.5f, 1f) * scaleByCapacity;
        transform.localScale = baseScale * scale;

        float baseAngle = transform.eulerAngles.z;
        transform.eulerAngles = new Vector3(0f, 0f, baseAngle + shake);
    }

    private void LateUpdate()
    {
        if (passengers.Count == 0) return;
        for (int i = 0; i < passengers.Count; i++)
        {
            var p = passengers[i];
            if (p == null) continue;
            var iconSr = p.GetComponentInChildren<SpriteRenderer>();
            if (iconSr != null && (iconSr.sortingOrder != SortingOrderConstants.Passenger || iconSr.sortingLayerID != SortingOrderConstants.ShipsLayerId))
            {
                iconSr.sortingLayerID = SortingOrderConstants.ShipsLayerId;
                iconSr.sortingOrder = SortingOrderConstants.Passenger;
            }
        }
    }

    private void UpdateMoving()
    {
        var seq = line.stationSequence;
        int count = seq.Count;
        int maxSeg = count - 2;
        if (maxSeg < 0) return;

        currentSegmentIndex = Mathf.Clamp(currentSegmentIndex, 0, maxSeg);
        StationBehaviour stationA = seq[currentSegmentIndex];
        StationBehaviour stationB = currentSegmentIndex + 1 < count ? seq[currentSegmentIndex + 1] : null;
        if (stationA == null || stationB == null)
        {
            TryAdvanceToValidSegment();
            return;
        }

        var lineManager = GameManager.Instance != null ? GameManager.Instance.GetLineManager() : null;
        bool useOffsetPath = lineManager != null;

        Vector3 posA = useOffsetPath ? lineManager.GetPositionOnLine(line, currentSegmentIndex, 0f) : stationA.transform.position;
        Vector3 posB = useOffsetPath ? lineManager.GetPositionOnLine(line, currentSegmentIndex, 1f) : stationB.transform.position;
        posA.z = ShipZ;
        posB.z = ShipZ;
        float segmentLength = Vector2.Distance(posA, posB);
        if (segmentLength < 0.0001f) segmentLength = 0.0001f;
        progressOnSegment += direction * _speed * Time.deltaTime / segmentLength;

        if (progressOnSegment >= 1f)
        {
            transform.position = new Vector3(posB.x, posB.y, ShipZ);
            EnterDocked(true);
            return;
        }
        if (progressOnSegment <= 0f)
        {
            transform.position = new Vector3(posA.x, posA.y, ShipZ);
            EnterDocked(false);
            return;
        }

        Vector3 pos = useOffsetPath
            ? lineManager.GetPositionOnLine(line, currentSegmentIndex, progressOnSegment)
            : Vector3.Lerp(stationA.transform.position, stationB.transform.position, progressOnSegment);
        pos.z = ShipZ;
        transform.position = pos;
        if (useOffsetPath)
            UpdateFacingFromTangent(lineManager.GetTangentOnLine(line, currentSegmentIndex, progressOnSegment));
        else
            UpdateFacing(posA, posB);
    }

    private void TryAdvanceToValidSegment()
    {
        var seq = line.stationSequence;
        int count = seq.Count;
        int maxSeg = count - 2;
        if (maxSeg < 0) return;
        for (int s = 0; s <= maxSeg; s++)
        {
            if (seq[s] != null && seq[s + 1] != null)
            {
                currentSegmentIndex = s;
                progressOnSegment = direction >= 0 ? 0f : 1f;
                var p = seq[s].transform.position;
                transform.position = new Vector3(p.x, p.y, ShipZ);
                return;
            }
        }
    }

    private void UpdateFacing(Vector3 from, Vector3 to)
    {
        Vector2 dir = (to - from) * direction;
        if (dir.sqrMagnitude < 0.0001f) return;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.eulerAngles = new Vector3(0f, 0f, angle);
    }

    private void UpdateFacingFromTangent(Vector3 tangent)
    {
        Vector2 dir = new Vector2(tangent.x, tangent.y) * direction;
        if (dir.sqrMagnitude < 0.0001f) return;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.eulerAngles = new Vector3(0f, 0f, angle);
    }

    private void EnterDocked(bool atSegmentEnd)
    {
        state = ShipState.Docked;
        dockRemainingTime = _dockDuration;
        _dockedAtSegmentEnd = atSegmentEnd;
        _arrivalDirection = direction;
        _dockProcessed = false;
    }

    private void UpdateDocked()
    {
        var seq = line.stationSequence;
        int stationIdx = currentSegmentIndex + (_dockedAtSegmentEnd ? 1 : 0);
        if (seq == null || stationIdx < 0 || stationIdx >= seq.Count || seq[stationIdx] == null)
        {
            state = ShipState.Moving;
            TryAdvanceToValidSegment();
            return;
        }

        if (!_dockProcessed)
        {
            _dockProcessed = true;
            ProcessDocking();
        }

        dockRemainingTime -= Time.deltaTime;
        if (dockRemainingTime > 0f) return;

        LeaveDock();
    }

    private void LeaveDock()
    {
        var seq = line.stationSequence;
        int count = seq.Count;
        int stationIdx = currentSegmentIndex + (_dockedAtSegmentEnd ? 1 : 0);
        bool atFirst = stationIdx == 0;
        bool atLast = stationIdx == count - 1;

        if (atFirst)
        {
            direction = 1;
            currentSegmentIndex = 0;
            progressOnSegment = 0f;
        }
        else if (atLast)
        {
            direction = -1;
            currentSegmentIndex = count - 2;
            progressOnSegment = 1f;
        }
        else
        {
            direction = _arrivalDirection;
            if (direction == 1)
            {
                currentSegmentIndex++;
                progressOnSegment = 0f;
            }
            else
            {
                currentSegmentIndex--;
                progressOnSegment = 1f;
            }
        }

        currentSegmentIndex = Mathf.Clamp(currentSegmentIndex, 0, count - 2);
        progressOnSegment = Mathf.Clamp01(progressOnSegment);
        state = ShipState.Moving;

        if (count >= 2 && currentSegmentIndex >= 0 && currentSegmentIndex + 1 < count)
        {
            var lm = GameManager.Instance != null ? GameManager.Instance.GetLineManager() : null;
            if (lm != null)
            {
                float t = direction >= 0 ? 0f : 1f;
                UpdateFacingFromTangent(lm.GetTangentOnLine(line, currentSegmentIndex, t));
            }
            else
            {
                var a = seq[currentSegmentIndex].transform.position;
                var b = seq[currentSegmentIndex + 1].transform.position;
                UpdateFacing(a, b);
            }
        }
    }

    #endregion

    #region 载客逻辑 (PRD §5.1)

    /// <summary>
    /// 停靠时一次性执行（PRD §5.1）：使用 PassengerTransportLogic 统一计算并执行卸客·载客。
    /// </summary>
    private void ProcessDocking()
    {
        var seq = line.stationSequence;
        int stationIdx = currentSegmentIndex + (_dockedAtSegmentEnd ? 1 : 0);
        if (stationIdx < 0 || stationIdx >= seq.Count) return;
        StationBehaviour station = seq[stationIdx];
        if (station == null || !station.isUnlocked) return;

        var lm = GameManager.Instance != null ? GameManager.Instance.GetLineManagerComponent() : null;
        var allLines = lm != null ? lm.Lines : null;

        // 载客/换乘需用「离站后的前进方向」：端点的 _arrivalDirection 指向来向，离站时会掉头，需修正
        int direction = _arrivalDirection;
        if (stationIdx == 0) direction = 1;
        else if (stationIdx == seq.Count - 1) direction = -1;
        if (direction == 0) direction = 1; // 防御：不应出现 0
        var result = PassengerTransportLogic.ComputeDockingActions(station, stationIdx, this, direction, allLines);

        // 1. 卸客·目的地
        foreach (var p in result.ToUnloadDestination)
        {
            if (p == null) continue;
            passengers.Remove(p);
            p.Arrive();
            if (GameManager.Instance != null)
                GameManager.Instance.AddScore(1);
            Destroy(p.gameObject);
        }

        // 2. 卸客·换乘
        foreach (var p in result.ToTransfer)
        {
            if (p == null) continue;
            passengers.Remove(p);
            p.TransferToStation(station);
        }

        // 3. 载客（先到先上）。须先 Add 再 BoardShip，否则 BoardShip 内 RefreshPassengerPositionsOnShip 时新乘客尚未在列表中，不会被设置船上 scale，导致换乘后上另一艘船的乘客显示异常放大。
        foreach (var p in result.ToLoad)
        {
            if (p == null) continue;
            if (passengers.Count >= capacity) break;
            if (!station.waitingPassengers.Remove(p)) continue;
            passengers.Add(p);
            p.BoardShip(this);
        }

        RefreshPassengerPositionsOnShip();
        station.RefreshPassengerPositions();
    }

    #endregion
}

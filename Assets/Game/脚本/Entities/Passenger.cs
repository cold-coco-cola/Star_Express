using UnityEngine;

/// <summary>
/// 乘客实体（PRD §4.2）。
/// 状态机：Waiting（站台排队）→ OnShip（在船上）→ Arrived（到达目标站，加分后移除）。
/// OnShip ↔ Waiting 可反复（换乘）。
/// </summary>
public class Passenger : MonoBehaviour
{
    public enum PassengerState { Waiting, OnShip, Arrived }

    [Header("由生成逻辑注入")]
    public string id;
    public ShapeType targetShape;
    public string targetStationId;

    [Header("运行时状态")]
    public PassengerState state = PassengerState.Waiting;
    public StationBehaviour currentStation;
    public ShipBehaviour currentShip;

    private SpriteRenderer _iconRenderer;
    private float _spawnAnimProgress = 1f;

    /// <summary>播放生成动画（快速缩放出现）。</summary>
    public void PlaySpawnAnimation()
    {
        _spawnAnimProgress = 0f;
    }

    private void Update()
    {
        if (state == PassengerState.OnShip) return;
        if (_spawnAnimProgress < 1f)
        {
            _spawnAnimProgress += Time.deltaTime / 0.2f;
            if (_spawnAnimProgress > 1f) _spawnAnimProgress = 1f;
            float t = _spawnAnimProgress;
            float ease = 1f - (1f - t) * (1f - t);
            transform.localScale = Vector3.one * Mathf.Max(0.01f, ease);
        }
        else
        {
            transform.localScale = Vector3.one;
        }
    }

    /// <summary>LateUpdate 中再次修正缩放，确保站台乘客不受其他逻辑影响而异常放大（多线经停、飞船到达时）。</summary>
    private void LateUpdate()
    {
        if (state != PassengerState.Waiting) return;
        if (_spawnAnimProgress < 1f) return;
        if (transform.parent == null) return;
        if (transform.parent.GetComponent<StationBehaviour>() == null) return;
        transform.localScale = Vector3.one;
    }

    /// <summary>设置乘客头顶目标形状图标。</summary>
    public void ApplyVisual()
    {
        if (_iconRenderer == null)
        {
            var iconGo = new GameObject("TargetIcon");
            iconGo.transform.SetParent(transform, false);
            iconGo.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            iconGo.transform.localScale = Vector3.one * 0.25f;
            _iconRenderer = iconGo.AddComponent<SpriteRenderer>();
            _iconRenderer.sortingLayerID = SortingOrderConstants.ShipsLayerId;
            _iconRenderer.sortingOrder = state == PassengerState.Waiting ? SortingOrderConstants.StationPassenger : SortingOrderConstants.Passenger;
        }
        else if (state == PassengerState.Waiting)
        {
            _iconRenderer.transform.localScale = Vector3.one * 0.25f;
        }

        var vc = GameManager.Instance != null ? GameManager.Instance.visualConfig : null;
        if (vc != null && vc.shapeSprites != null)
        {
            int idx = (int)targetShape;
            if (idx >= 0 && idx < vc.shapeSprites.Length && vc.shapeSprites[idx] != null)
                _iconRenderer.sprite = vc.shapeSprites[idx];
        }
        if (_iconRenderer.sprite == null)
            _iconRenderer.sprite = GetPlaceholderShapeSprite();
        _iconRenderer.color = new Color(0.55f, 0.55f, 0.6f, 1f);
        _iconRenderer.sortingOrder = state == PassengerState.Waiting ? SortingOrderConstants.StationPassenger : SortingOrderConstants.Passenger;
    }

    /// <summary>乘客上船。调用方需先从站台 waitingPassengers 移除。</summary>
    public void BoardShip(ShipBehaviour ship)
    {
        state = PassengerState.OnShip;
        currentShip = ship;
        currentStation = null;
        ApplyVisual();
        if (_iconRenderer == null) _iconRenderer = GetComponentInChildren<SpriteRenderer>();
        transform.SetParent(ship.transform, false);
        gameObject.SetActive(true);
        if (_iconRenderer != null)
        {
            _iconRenderer.sortingLayerID = SortingOrderConstants.ShipsLayerId;
            _iconRenderer.sortingOrder = SortingOrderConstants.Passenger;
        }
        if (ship != null) ship.RefreshPassengerPositionsOnShip();
    }

    /// <summary>乘客到达目标站（卸客·目的地），得分。</summary>
    public void Arrive()
    {
        state = PassengerState.Arrived;
        currentShip = null;
        currentStation = null;
    }

    /// <summary>换乘下船，加入新站排队。ApplyVisual 会恢复 icon 的 scale=0.25（船上为 1），避免换乘乘客显示异常放大。</summary>
    public void TransferToStation(StationBehaviour station)
    {
        state = PassengerState.Waiting;
        currentShip = null;
        currentStation = station;
        station.waitingPassengers.Add(this);
        transform.SetParent(station.transform, false);
        transform.localScale = Vector3.one;
        gameObject.SetActive(true);
        UpdateQueuePosition(station.waitingPassengers.Count - 1);
        ApplyVisual();
    }

    private static Sprite _placeholderShapeSprite;
    public static Sprite GetPlaceholderShapeSpriteForShip() => GetPlaceholderShapeSprite();
    private static Sprite GetPlaceholderShapeSprite()
    {
        if (_placeholderShapeSprite != null) return _placeholderShapeSprite;
        var tex = new Texture2D(32, 32);
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float dx = x / 31f - 0.5f;
                float dy = y / 31f - 0.5f;
                tex.SetPixel(x, y, (dx * dx + dy * dy <= 0.25f) ? Color.white : new Color(0, 0, 0, 0));
            }
        tex.Apply();
        _placeholderShapeSprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
        return _placeholderShapeSprite;
    }

    /// <summary>根据排队序号设置乘客在站台的位置偏移。</summary>
    public void UpdateQueuePosition(int queueIndex)
    {
        float offsetX = (queueIndex % 4) * 0.27f - 0.3f;
        float offsetY = 0.5f + (queueIndex / 4) * 0.28f;
        transform.localPosition = new Vector3(offsetX, offsetY, -0.1f);
    }
}

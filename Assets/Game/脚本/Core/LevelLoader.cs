using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 读取 LevelConfig，在 Stations 节点下实例化 Station Prefab，注入 position、stationType、id、displayName、isUnlocked，
/// 并按 VisualConfig.shapeSprites 设置 Sprite（若有），最后调用 RefreshVisual()。可选填充 id→StationBehaviour 供 UnlockController 使用。
/// </summary>
public static class LevelLoader
{
    private static Sprite _placeholderSprite;

    /// <summary>站点 Visual 统一世界尺寸（直径约该值），保证 7 颗星视觉大小一致。</summary>
    public const float StationVisualWorldSize = 0.7f;
    /// <summary>站点间距缩放，使站点更分散。</summary>
    public const float StationPositionScale = 1.8f;

    /// <summary>
    /// 加载关卡：在 stationsParent 下按 levelConfig 实例化开局站点（前 startStationCount 个），并注入数据与视觉。
    /// 若 stationsById 非 null，会注册每个站的 id→StationBehaviour。
    /// </summary>
    public static void Load(LevelConfig levelConfig, Transform stationsParent, GameObject stationPrefab, VisualConfig visualConfig, Dictionary<string, StationBehaviour> stationsById = null)
    {
        if (levelConfig == null || stationsParent == null || stationPrefab == null)
        {
            Debug.LogWarning("[LevelLoader] Load 跳过: levelConfig=" + (levelConfig != null) + " stationsParent=" + (stationsParent != null) + " stationPrefab=" + (stationPrefab != null));
            return;
        }

        if (stationsById != null) stationsById.Clear();
        int toLoad = Mathf.Min(levelConfig.startStationCount > 0 ? levelConfig.startStationCount : 3, levelConfig.stations.Count);
        if (toLoad <= 0) toLoad = Mathf.Min(3, levelConfig.stations.Count);
        int count = 0;
        for (int i = 0; i < toLoad && i < levelConfig.stations.Count; i++)
        {
            var config = levelConfig.stations[i];
            GameObject go = UnityEngine.Object.Instantiate(stationPrefab, stationsParent);
            go.name = "Station_" + config.id;
            go.transform.localPosition = new Vector3(config.position.x * StationPositionScale, config.position.y * StationPositionScale, 0f);
            go.transform.localScale = Vector3.one;
            go.layer = 0;
            go.SetActive(true);

            var sr = go.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                if (visualConfig != null && visualConfig.shapeSprites != null && visualConfig.shapeSprites.Length > 0)
                {
                    int index = (int)config.shapeType;
                    if (index >= 0 && index < visualConfig.shapeSprites.Length && visualConfig.shapeSprites[index] != null)
                        sr.sprite = visualConfig.shapeSprites[index];
                }
                if (sr.sprite == null)
                    sr.sprite = GetOrCreatePlaceholderSprite();
                sr.sortingLayerID = SortingOrderConstants.ShipsLayerId;
                sr.sortingOrder = SortingOrderConstants.Station;
                var visual = sr.transform;
                visual.localScale = Vector3.one * GetStationVisualScale(sr.sprite);
            }

            var station = go.GetComponent<StationBehaviour>();
            if (station != null)
            {
                station.id = config.id;
                station.stationType = config.shapeType;
                station.displayName = config.displayName;
                station.isUnlocked = true;
                station.RefreshVisual();
                if (stationsById != null) stationsById[config.id] = station;
            }

            count++;
        }
        Debug.Log("[LevelLoader] 已生成 " + count + " 个开局站点。");
    }

    /// <summary>
    /// 动态生成单个站点（无尽模式）。position 为世界坐标（已乘 StationPositionScale）。
    /// 若 stationsById 非 null 会注册；playSpawnAnimation 为 true 时播放从小变大动画。
    /// </summary>
    public static StationBehaviour SpawnStation(LevelConfig levelConfig, Transform stationsParent, GameObject stationPrefab, VisualConfig visualConfig, Vector3 position, ShapeType shapeType, string displayName, string id, Dictionary<string, StationBehaviour> stationsById, bool playSpawnAnimation = true)
    {
        if (levelConfig == null || stationsParent == null || stationPrefab == null) return null;

        GameObject go = Object.Instantiate(stationPrefab, stationsParent);
        go.name = "Station_" + id;
        go.transform.localPosition = position;
        go.transform.localScale = Vector3.one;
        go.layer = 0;
        go.SetActive(true);

        var sr = go.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            if (visualConfig != null && visualConfig.shapeSprites != null && visualConfig.shapeSprites.Length > 0)
            {
                int index = (int)shapeType;
                if (index >= 0 && index < visualConfig.shapeSprites.Length && visualConfig.shapeSprites[index] != null)
                    sr.sprite = visualConfig.shapeSprites[index];
            }
            if (sr.sprite == null)
                sr.sprite = GetOrCreatePlaceholderSprite();
            sr.sortingLayerID = SortingOrderConstants.ShipsLayerId;
            sr.sortingOrder = SortingOrderConstants.Station;
            var visual = sr.transform;
            float baseScale = GetStationVisualScale(sr.sprite);
            visual.localScale = Vector3.one * (playSpawnAnimation ? 0.01f : baseScale);
        }

        var station = go.GetComponent<StationBehaviour>();
        if (station != null)
        {
            station.id = id;
            station.stationType = shapeType;
            station.displayName = displayName;
            station.isUnlocked = true;
            if (playSpawnAnimation)
                station.PlaySpawnAnimation();
            station.RefreshVisual();
            if (stationsById != null) stationsById[id] = station;
        }
        return station;
    }

    /// <summary>根据 Sprite 尺寸计算统一显示缩放，保证各形状站点视觉大小一致。</summary>
    public static float GetStationVisualScale(Sprite sprite)
    {
        if (sprite == null) return StationVisualWorldSize;
        var size = sprite.bounds.size;
        float maxExtent = Mathf.Max(size.x, size.y);
        return maxExtent > 0.001f ? StationVisualWorldSize / maxExtent : StationVisualWorldSize;
    }

    private static Sprite GetOrCreatePlaceholderSprite()
    {
        if (_placeholderSprite != null) return _placeholderSprite;
        var tex = new Texture2D(64, 64);
        var fill = new Color32(255, 255, 255, 255);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float dx = x / 63f - 0.5f; float dy = y / 63f - 0.5f;
                tex.SetPixel(x, y, (dx * dx + dy * dy <= 0.25f) ? fill : new Color32(0, 0, 0, 0));
            }
        tex.Apply();
        _placeholderSprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        return _placeholderSprite;
    }
}

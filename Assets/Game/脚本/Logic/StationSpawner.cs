using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 无尽模式：每周在 20s～45s 内分散生成新站点。
/// 每周生成 1（30%）或 2（70%）个站点；尽量远离已有线路；多个站点错开时间生成。
/// </summary>
public class StationSpawner : MonoBehaviour
{
    private const float MinDistanceFromLineFactor = 0.6f;
    private const float SpawnTimeMin = 20f;
    private const float SpawnTimeMax = 45f;

    private float _weekTimer;
    private readonly List<float> _scheduledSpawnTimes = new List<float>();
    private int _lastWeek = -1;

    private void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.IsGameOver) return;
        var config = gm.levelConfig;
        if (config == null || config.stations == null || config.stations.Count == 0) return;

        int week = gm.CurrentWeek;
        if (week != _lastWeek)
        {
            _lastWeek = week;
            _scheduledSpawnTimes.Clear();
            int toSpawn = Random.value < 0.7f ? 2 : 1;
            const float minGap = 8f;
            if (toSpawn == 1)
            {
                _scheduledSpawnTimes.Add(SpawnTimeMin + Random.Range(0f, SpawnTimeMax - SpawnTimeMin));
            }
            else
            {
                float t1 = SpawnTimeMin + Random.Range(0f, SpawnTimeMax - SpawnTimeMin - minGap);
                float t2 = t1 + minGap + Random.Range(0f, SpawnTimeMax - t1 - minGap);
                _scheduledSpawnTimes.Add(t1);
                _scheduledSpawnTimes.Add(t2);
            }
        }

        float weekElapsed = gm.WeekDurationSeconds - gm.WeekTimerRemaining;
        for (int i = _scheduledSpawnTimes.Count - 1; i >= 0; i--)
        {
            if (weekElapsed >= _scheduledSpawnTimes[i])
            {
                if (TrySpawnStation())
                    _scheduledSpawnTimes.RemoveAt(i);
            }
        }
    }

    private bool TrySpawnStation()
    {
        var gm = GameManager.Instance;
        if (gm == null) return false;
        var stations = gm.GetAllStations();
        if (stations == null || stations.Count == 0) return false;

        var config = gm.levelConfig;
        if (config == null || config.stations == null || config.stations.Count == 0) return false;

        StationConfig template = PickSpawnTemplate(config);
        if (template == null) return false;

        Vector3? pos = FindSpawnPosition(stations, config, gm);
        if (pos == null) return false;

        string id = "station_" + Time.frameCount + "_" + Random.Range(0, 9999);
        var parent = gm.stationsParent;
        if (parent == null) return false;

        var station = LevelLoader.SpawnStation(config, parent, gm.stationPrefab, gm.visualConfig, pos.Value, template.shapeType, template.displayName, id, stations, true);
        if (station != null) gm.NotifyStationSpawned(station);
        Debug.Log($"[StationSpawner] 生成新站 {id} ({template.displayName}) 于 {pos.Value}");
        return true;
    }

    private static StationConfig PickSpawnTemplate(LevelConfig config)
    {
        var pool = new List<StationConfig>();
        if (config.spawnTemplateIds != null && config.spawnTemplateIds.Count > 0)
        {
            foreach (var id in config.spawnTemplateIds)
            {
                var c = config.stations.Find(s => s.id == id);
                if (c != null) pool.Add(c);
            }
        }
        if (pool.Count == 0)
            pool.AddRange(config.stations);
        if (pool.Count == 0) return null;
        return pool[Random.Range(0, pool.Count)];
    }

    private Vector3? FindSpawnPosition(Dictionary<string, StationBehaviour> stations, LevelConfig config, GameManager gm)
    {
        float minD = config.spawnDistanceMin * LevelLoader.StationPositionScale;
        float maxD = config.spawnDistanceMax * LevelLoader.StationPositionScale;
        float minFromLine = minD * MinDistanceFromLineFactor;

        var list = new List<StationBehaviour>();
        foreach (var kv in stations)
            if (kv.Value != null && kv.Value.isUnlocked)
                list.Add(kv.Value);
        if (list.Count == 0) return null;

        var lm = gm != null ? gm.GetLineManagerComponent() : null;
        var allLines = lm != null ? lm.Lines : null;

        // 以所有站点的质心为基准，保证整体分布均衡
        Vector2 centroid = ComputeCentroid(list);

        // 8 个扇区（每扇区 45°），优先在站点较少的扇区生成，避免单方向延伸
        float preferredAngle = GetPreferredSpawnAngle(centroid, list);

        for (int attempt = 0; attempt < 50; attempt++)
        {
            // 在偏好角度附近 ±90° 内随机，兼顾整体性与随机性
            float angleJitter = Random.Range(-90f, 90f) * Mathf.Deg2Rad;
            float angle = (preferredAngle * Mathf.Deg2Rad) + angleJitter;
            float dist = Random.Range(minD, maxD);
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
            Vector3 candidate = new Vector3(centroid.x + offset.x, centroid.y + offset.y, 0f);

            bool ok = true;
            foreach (var s in list)
            {
                float d = Vector2.Distance(s.transform.position, candidate);
                if (d < minD * 0.5f) { ok = false; break; }
            }
            if (!ok) continue;

            if (allLines != null && allLines.Count > 0)
            {
                float distToLine = GetMinDistanceToLines(candidate, allLines);
                if (distToLine < minFromLine) continue;
            }
            return candidate;
        }

        // 兜底：在质心附近、偏好角度方向生成
        float fallbackAngle = preferredAngle * Mathf.Deg2Rad;
        Vector2 fallbackOffset = new Vector2(Mathf.Cos(fallbackAngle), Mathf.Sin(fallbackAngle)) * (minD + maxD) * 0.5f;
        return new Vector3(centroid.x + fallbackOffset.x, centroid.y + fallbackOffset.y, 0f);
    }

    /// <summary>计算所有站点的质心。</summary>
    private static Vector2 ComputeCentroid(List<StationBehaviour> list)
    {
        Vector2 sum = Vector2.zero;
        foreach (var s in list)
            sum += (Vector2)s.transform.position;
        return sum / list.Count;
    }

    /// <summary>根据各扇区站点数量，加权随机返回优先生成角度（度），偏少但不绝对。</summary>
    private static float GetPreferredSpawnAngle(Vector2 centroid, List<StationBehaviour> list)
    {
        const int sectorCount = 8;
        var counts = new int[sectorCount];
        int maxCount = 0;
        foreach (var s in list)
        {
            Vector2 delta = (Vector2)s.transform.position - centroid;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;
            int sector = Mathf.Clamp((int)(angle / (360f / sectorCount)), 0, sectorCount - 1);
            counts[sector]++;
            if (counts[sector] > maxCount) maxCount = counts[sector];
        }

        // 加权随机：站点少的扇区权重更高，但不绝对
        float totalWeight = 0f;
        for (int i = 0; i < sectorCount; i++)
        {
            float w = maxCount - counts[i] + 1f;
            totalWeight += w;
        }
        float r = Random.Range(0f, totalWeight);
        int chosen = 0;
        for (int i = 0; i < sectorCount; i++)
        {
            float w = maxCount - counts[i] + 1f;
            r -= w;
            if (r <= 0) { chosen = i; break; }
        }

        float sectorSpan = 360f / sectorCount;
        return chosen * sectorSpan + sectorSpan * 0.5f;
    }

    private static float GetMinDistanceToLines(Vector3 point, IReadOnlyList<Line> lines)
    {
        float min = float.MaxValue;
        foreach (var line in lines)
        {
            if (line == null || line.stationSequence == null) continue;
            var seq = line.stationSequence;
            for (int i = 0; i < seq.Count - 1; i++)
            {
                var a = seq[i];
                var b = seq[i + 1];
                if (a == null || b == null) continue;
                float d = DistanceToSegment(point, a.transform.position, b.transform.position);
                if (d < min) min = d;
            }
        }
        return min < float.MaxValue ? min : 9999f;
    }

    private static float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector2 ap = new Vector2(p.x - a.x, p.y - a.y);
        Vector2 ab = new Vector2(b.x - a.x, b.y - a.y);
        float lenSq = ab.sqrMagnitude;
        if (lenSq < 0.0001f) return ap.magnitude;
        float t = Mathf.Clamp01(Vector2.Dot(ap, ab) / lenSq);
        Vector2 closest = new Vector2(a.x, a.y) + ab * t;
        return Vector2.Distance(new Vector2(p.x, p.y), closest);
    }
}

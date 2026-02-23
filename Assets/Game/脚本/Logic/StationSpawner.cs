using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 无尽模式：每周在 20s～45s 内分散生成新站点。
/// 每周生成 1（30%）或 2（70%）个站点；尽量远离已有线路；多个站点错开时间生成。
/// 站点间距不足时拒绝生成；距线路过近时可吸附为线上站。
/// </summary>
public class StationSpawner : MonoBehaviour
{
    /// <summary>距线路小于此倍数×minD 时拒绝独立生成；若开启吸附则尝试吸附为线上站。</summary>
    private const float MinDistanceFromLineFactor = 0.6f;
    /// <summary>吸附时，新站与线段两端的最小距离（倍数×minD），避免与端点重叠。</summary>
    private const float SnapMinDistanceFromEndpointFactor = 0.4f;
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

        var result = FindSpawnPosition(stations, config, gm);
        if (result == null) return false;

        string id = "station_" + Time.frameCount + "_" + Random.Range(0, 9999);
        var parent = gm.stationsParent;
        if (parent == null) return false;

        var station = LevelLoader.SpawnStation(config, parent, gm.stationPrefab, gm.visualConfig, result.Value.position, template.shapeType, template.displayName, id, stations, true);
        if (station == null) return false;

        if (result.Value.lineToInsertInto != null)
        {
            var lm = gm.GetLineManagerComponent();
            if (lm != null)
                lm.InsertStationIntoSegment(result.Value.lineToInsertInto, result.Value.segmentIndex, station, result.Value.insertProgress);
        }

        gm.NotifyStationSpawned(station);
        Debug.Log(result.Value.lineToInsertInto != null
            ? $"[StationSpawner] 生成线上站 {id} ({template.displayName}) 于 {result.Value.position}"
            : $"[StationSpawner] 生成新站 {id} ({template.displayName}) 于 {result.Value.position}");
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

    private struct SpawnResult
    {
        public Vector3 position;
        public Line lineToInsertInto;
        public int segmentIndex;
        public float insertProgress;
    }

    private SpawnResult? FindSpawnPosition(Dictionary<string, StationBehaviour> stations, LevelConfig config, GameManager gm)
    {
        float minD = config.spawnDistanceMin * LevelLoader.StationPositionScale;
        float maxD = config.spawnDistanceMax * LevelLoader.StationPositionScale;
        float minFromLine = minD * MinDistanceFromLineFactor;
        float snapThreshold = config.spawnSnapToLineThreshold > 0
            ? config.spawnSnapToLineThreshold * LevelLoader.StationPositionScale
            : minFromLine;
        float minFromEndpoint = minD * SnapMinDistanceFromEndpointFactor;

        var list = new List<StationBehaviour>();
        foreach (var kv in stations)
            if (kv.Value != null && kv.Value.isUnlocked)
                list.Add(kv.Value);
        if (list.Count == 0) return null;

        var lm = gm != null ? gm.GetLineManagerComponent() : null;
        var allLines = lm != null ? lm.Lines : null;

        Vector2 centroid = ComputeCentroid(list);
        float preferredAngle = GetPreferredSpawnAngle(centroid, list);

        for (int attempt = 0; attempt < 50; attempt++)
        {
            float angleJitter = Random.Range(-90f, 90f) * Mathf.Deg2Rad;
            float angle = (preferredAngle * Mathf.Deg2Rad) + angleJitter;
            float dist = Random.Range(minD, maxD);
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
            Vector3 candidate = new Vector3(centroid.x + offset.x, centroid.y + offset.y, 0f);

            foreach (var s in list)
            {
                if (Vector2.Distance(s.transform.position, candidate) < minD) goto nextAttempt;
            }

            if (allLines != null && allLines.Count > 0)
            {
                var snap = TryFindSnapPosition(config, candidate, allLines, list, minFromLine, snapThreshold, minFromEndpoint);
                if (snap.HasValue)
                    return snap.Value;
                float distToLine = GetMinDistanceToLines(candidate, allLines);
                if (distToLine < minFromLine) continue;
            }
            return new SpawnResult { position = candidate };
        nextAttempt:;
        }

        return null;
    }

    /// <summary>当候选点距线路过近时，尝试吸附到线段上成为线上站。</summary>
    private SpawnResult? TryFindSnapPosition(LevelConfig config, Vector3 candidate, IReadOnlyList<Line> lines, List<StationBehaviour> allStations, float minFromLine, float snapThreshold, float minFromEndpoint)
    {
        if (config == null || config.spawnSnapToLineThreshold <= 0) return null;
        float distToLine = GetMinDistanceToLinesWithSegment(candidate, lines, out Line bestLine, out int bestSeg, out Vector3 closestPoint, out float insertProgress);
        if (distToLine > snapThreshold) return null;
        if (bestLine == null) return null;

        var seq = bestLine.stationSequence;
        if (bestSeg < 0 || bestSeg + 1 >= seq.Count) return null;
        var a = seq[bestSeg].transform.position;
        var b = seq[bestSeg + 1].transform.position;
        float distA = Vector2.Distance(closestPoint, a);
        float distB = Vector2.Distance(closestPoint, b);
        if (distA < minFromEndpoint || distB < minFromEndpoint) return null;

        foreach (var s in allStations)
        {
            if (Vector2.Distance(s.transform.position, closestPoint) < minFromEndpoint) return null;
        }
        return new SpawnResult { position = closestPoint, lineToInsertInto = bestLine, segmentIndex = bestSeg, insertProgress = insertProgress };
    }

    private static float GetMinDistanceToLinesWithSegment(Vector3 point, IReadOnlyList<Line> lines, out Line bestLine, out int bestSeg, out Vector3 closestPoint, out float insertProgress)
    {
        bestLine = null;
        bestSeg = -1;
        closestPoint = point;
        insertProgress = 0f;
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
                float d = DistanceToSegment(point, a.transform.position, b.transform.position, out Vector3 closest, out float t);
                if (d < min) { min = d; bestLine = line; bestSeg = i; closestPoint = closest; insertProgress = t; }
            }
        }
        return min < float.MaxValue ? min : 9999f;
    }

    private static float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b, out Vector3 closest, out float t)
    {
        Vector2 ap = new Vector2(p.x - a.x, p.y - a.y);
        Vector2 ab = new Vector2(b.x - a.x, b.y - a.y);
        float lenSq = ab.sqrMagnitude;
        if (lenSq < 0.0001f) { closest = a; t = 0f; return ap.magnitude; }
        t = Mathf.Clamp01(Vector2.Dot(ap, ab) / lenSq);
        Vector2 closest2 = new Vector2(a.x, a.y) + ab * t;
        closest = new Vector3(closest2.x, closest2.y, 0f);
        return Vector2.Distance(new Vector2(p.x, p.y), closest2);
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

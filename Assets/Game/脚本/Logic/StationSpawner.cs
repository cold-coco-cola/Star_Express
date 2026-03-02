using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 无尽模式：每周在 1/5～4/5 时段内分散生成新站点。
/// 每周生成 2（40%）或 3（60%）个站点；倾向向外扩张；站点间距不足时拒绝生成。
/// </summary>
public class StationSpawner : MonoBehaviour
{
    private int GetSpawnCount(int week)
    {
        if (week == 0) return 2;
        if (week == 1) return 2;
        if (week <= 3) return 2;
        if (week <= 6) return Random.value < 0.4f ? 2 : 3;
        return Random.value < 0.3f ? 3 : 4; // 第7周起倾向3~4个，扩张更快
    }

    private const float SpawnWindowStart = 0.2f;
    private const float SpawnWindowEnd = 0.8f;
    private const float MinSpawnGapSeconds = 10f;

    /// <summary>距线路小于此倍数×minD 时拒绝独立生成；若开启吸附则尝试吸附为线上站。</summary>
    private const float MinDistanceFromLineFactor = 0.6f;
    /// <summary>吸附时，新站与线段两端的最小距离（倍数×minD），避免与端点重叠。</summary>
    private const float SnapMinDistanceFromEndpointFactor = 0.4f;

    private const int StrictAttempts = 50;
    private const int RelaxedAttempts = 30;
    private const float RelaxedMinDFactor = 0.55f;
    private const float RelaxedMaxDFactor = 1.35f;

    private float _weekTimer;
    private readonly List<float> _scheduledSpawnTimes = new List<float>();
    private int _lastWeek = -1;
    private int _spawnedThisWeek = 0;

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
            _spawnedThisWeek = 0;
            float weekDur = gm.WeekDurationSeconds;
            float spawnMin = weekDur * SpawnWindowStart;
            float spawnMax = weekDur * SpawnWindowEnd;
            int toSpawn = GetSpawnCount(week);
            float span = spawnMax - spawnMin;
            float totalGap = MinSpawnGapSeconds * (toSpawn - 1);
            float segSize = Mathf.Max(0.1f, (span - totalGap) / toSpawn);
            for (int k = 0; k < toSpawn; k++)
            {
                float segStart = spawnMin + k * (segSize + MinSpawnGapSeconds);
                _scheduledSpawnTimes.Add(segStart + Random.Range(0f, segSize));
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

        // 根据周数和概率选择形状，覆盖模板中的 shapeType
        int week = gm.CurrentWeek;
        ShapeType shapeToUse = PickShapeType(week, _spawnedThisWeek);

        var result = FindSpawnPosition(stations, config, gm);
        if (result == null) return false;

        string id = "station_" + Time.frameCount + "_" + Random.Range(0, 9999);
        var parent = gm.stationsParent;
        if (parent == null) return false;

        var station = LevelLoader.SpawnStation(config, parent, gm.stationPrefab, gm.visualConfig, result.Value.position, shapeToUse, template.displayName, id, stations, true);
        if (station == null) return false;

        if (result.Value.lineToInsertInto != null)
        {
            var lm = gm.GetLineManagerComponent();
            if (lm != null)
                lm.InsertStationIntoSegment(result.Value.lineToInsertInto, result.Value.segmentIndex, station, result.Value.insertProgress);
        }

        gm.NotifyStationSpawned(station);
        _spawnedThisWeek++;
        Debug.Log(result.Value.lineToInsertInto != null
            ? $"[StationSpawner] 生成线上站 {id} ({template.displayName}, {shapeToUse}) 于 {result.Value.position}"
            : $"[StationSpawner] 生成新站 {id} ({template.displayName}, {shapeToUse}) 于 {result.Value.position}");
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

    /// <summary>根据当前周数返回已解锁的形状列表。</summary>
    private List<ShapeType> GetUnlockedShapes(int week)
    {
        var shapes = new List<ShapeType> { ShapeType.Circle, ShapeType.Triangle, ShapeType.Square, ShapeType.Star };
        var balance = GameManager.Instance != null ? GameManager.Instance.gameBalance : null;
        int hex = balance != null ? balance.hexagonUnlockWeek : 3;
        int sec = balance != null ? balance.sectorUnlockWeek : 4;
        int crs = balance != null ? balance.crossUnlockWeek : 5;
        int cap = balance != null ? balance.capsuleUnlockWeek : 7;
        if (week >= hex) shapes.Add(ShapeType.Hexagon);
        if (week >= sec) shapes.Add(ShapeType.Sector);
        if (week >= crs) shapes.Add(ShapeType.Cross);
        if (week >= cap) shapes.Add(ShapeType.Capsule);
        return shapes;
    }

    /// <summary>根据当前周数返回高级形状的生成概率（0-1）。</summary>
    private float GetAdvancedShapeProbability(int week)
    {
        if (week <= 1) return 0f;
        if (week == 2) return 0.05f;
        if (week <= 4) return 0.10f;
        if (week <= 6) return 0.20f;
        if (week <= 10) return 0.35f;
        return 0.50f;
    }

    /// <summary>根据周数和概率选择站点形状。解锁当周第一个生成的站点必为新解锁的形状。</summary>
    private ShapeType PickShapeType(int week, int spawnedThisWeek)
    {
        var unlockedShapes = GetUnlockedShapes(week);
        var basicShapes = unlockedShapes.Where(s => (int)s <= 3).ToList();
        var advancedShapes = unlockedShapes.Where(s => (int)s >= 4).ToList();

        if (spawnedThisWeek == 0)
        {
            var newlyUnlocked = GetNewlyUnlockedShape(week);
            if (newlyUnlocked.HasValue)
                return newlyUnlocked.Value;
        }

        float advancedProb = GetAdvancedShapeProbability(week);

        if (advancedShapes.Count == 0 || Random.value >= advancedProb)
            return basicShapes[Random.Range(0, basicShapes.Count)];

        return advancedShapes[Random.Range(0, advancedShapes.Count)];
    }

    private ShapeType? GetNewlyUnlockedShape(int week)
    {
        var balance = GameManager.Instance != null ? GameManager.Instance.gameBalance : null;
        int hexWeek = balance != null ? balance.hexagonUnlockWeek : 3;
        int secWeek = balance != null ? balance.sectorUnlockWeek : 4;
        int crsWeek = balance != null ? balance.crossUnlockWeek : 5;
        int capWeek = balance != null ? balance.capsuleUnlockWeek : 7;

        if (week == capWeek) return ShapeType.Capsule;
        if (week == crsWeek) return ShapeType.Cross;
        if (week == secWeek) return ShapeType.Sector;
        if (week == hexWeek) return ShapeType.Hexagon;
        return null;
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

        var list = new List<StationBehaviour>();
        foreach (var kv in stations)
            if (kv.Value != null && kv.Value.isUnlocked)
                list.Add(kv.Value);
        if (list.Count == 0) return null;

        float lateScale = GetLateGameDistanceScale(list.Count);
        minD *= lateScale;
        maxD *= lateScale;

        float minFromLine = minD * MinDistanceFromLineFactor;
        float snapThreshold = config.spawnSnapToLineThreshold > 0
            ? config.spawnSnapToLineThreshold * LevelLoader.StationPositionScale
            : minFromLine;
        float minFromEndpoint = minD * SnapMinDistanceFromEndpointFactor;

        var lm = gm != null ? gm.GetLineManagerComponent() : null;
        var allLines = lm != null ? lm.Lines : null;

        var beltCenters = GetAsteroidBeltCenters();

        Vector2 centroid = ComputeCentroid(list);
        int week = gm != null ? gm.CurrentWeek : 0;
        float preferredAngle = GetPreferredSpawnAngle(centroid, list, beltCenters, week);
        float avgDistFromCentroid = GetAverageDistanceFromCentroid(centroid, list);

        for (int attempt = 0; attempt < StrictAttempts; attempt++)
        {
            if (TryFindValidPosition(centroid, preferredAngle, minD, maxD, minFromLine, minFromEndpoint, avgDistFromCentroid, list.Count, config, allLines, list, beltCenters, out SpawnResult result))
                return result;
        }

        float relaxedMinD = minD * RelaxedMinDFactor;
        float relaxedMinFromEndpoint = minFromEndpoint * 0.5f;
        for (int attempt = 0; attempt < RelaxedAttempts; attempt++)
        {
            if (TryFindValidPosition(centroid, preferredAngle, relaxedMinD, maxD * RelaxedMaxDFactor, relaxedMinD * MinDistanceFromLineFactor, relaxedMinFromEndpoint, avgDistFromCentroid, list.Count, config, allLines, list, beltCenters, out SpawnResult result))
                return result;
        }

        if (allLines != null && allLines.Count > 0 && config != null && config.spawnSnapToLineThreshold > 0)
        {
            var snap = TryFindAnyValidSnap(allLines, list, relaxedMinFromEndpoint);
            if (snap.HasValue && !IsPointInsideAnyAsteroidBelt(new Vector2(snap.Value.position.x, snap.Value.position.y), StationBeltAvoidMargin))
                return snap.Value;
        }

        return null;
    }

    /// <summary>获取场景中所有陨石带（优先从 Map/AsteroidBelts 下收集，否则全场景查找）。</summary>
    private static AsteroidBeltBehaviour[] GetAllAsteroidBelts()
    {
        var list = new List<AsteroidBeltBehaviour>();
        var map = GameObject.Find("Map");
        var beltsRoot = map != null ? map.transform.Find("AsteroidBelts") : null;
        if (beltsRoot == null) beltsRoot = GameObject.Find("AsteroidBelts")?.transform;
        if (beltsRoot != null)
        {
            foreach (Transform child in beltsRoot)
            {
                var b = child.GetComponent<AsteroidBeltBehaviour>();
                if (b != null) list.Add(b);
            }
        }
        if (list.Count == 0)
            list.AddRange(Object.FindObjectsOfType<AsteroidBeltBehaviour>());
        return list.ToArray();
    }

    private static List<Vector2> GetAsteroidBeltCenters()
    {
        var belts = GetAllAsteroidBelts();
        if (belts == null || belts.Length == 0) return null;
        var centers = new List<Vector2>();
        foreach (var b in belts)
        {
            if (b == null) continue;
            var c = b.GetBeltCenter();
            if (c.HasValue) centers.Add(c.Value);
        }
        return centers.Count > 0 ? centers : null;
    }

    /// <summary>站点视觉半径约 0.7，用于扩展陨石带避让检测范围，避免站点与陨石带视觉重叠。</summary>
    private const float StationBeltAvoidMargin = 0.7f;

    private static bool IsPointInsideAnyAsteroidBelt(Vector2 p, float margin = 0f)
    {
        var belts = GetAllAsteroidBelts();
        if (belts == null) return false;
        foreach (var b in belts)
        {
            if (b != null && b.IsPointInsideBelt(p, margin)) return true;
        }
        return false;
    }

    private bool TryFindValidPosition(Vector2 centroid, float preferredAngle, float minD, float maxD, float minFromLine, float minFromEndpoint, float avgDistFromCentroid, int stationCount, LevelConfig config, IReadOnlyList<Line> allLines, List<StationBehaviour> list, List<Vector2> beltCenters, out SpawnResult result)
    {
        result = default;
        float snapThreshold = config != null && config.spawnSnapToLineThreshold > 0
            ? config.spawnSnapToLineThreshold * LevelLoader.StationPositionScale
            : minFromLine;

        float angleJitter = Random.Range(-90f, 90f) * Mathf.Deg2Rad;
        float angle = (preferredAngle * Mathf.Deg2Rad) + angleJitter;
        // 整体扩张趋势：新站倾向在「当前平均半径」之外；扩张速度加快
        float frontier = Mathf.Clamp(avgDistFromCentroid, minD, maxD * 0.95f);
        float dist;
        if (stationCount >= 18)
        {
            dist = frontier + Random.Range(0.65f, 1.15f) * Mathf.Max(0.1f, maxD - frontier);
        }
        else if (stationCount >= 12)
        {
            dist = frontier + Random.Range(0.5f, 1.1f) * Mathf.Max(0.1f, maxD - frontier);
        }
        else
        {
            dist = frontier + Random.Range(0.2f, 1f) * Mathf.Max(0.1f, maxD - frontier);
        }
        dist = Mathf.Clamp(dist, minD, maxD);
        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
        Vector3 candidate = new Vector3(centroid.x + offset.x, centroid.y + offset.y, 0f);

        if (IsPointInsideAnyAsteroidBelt(new Vector2(candidate.x, candidate.y), StationBeltAvoidMargin))
            return false;

        foreach (var s in list)
        {
            if (Vector2.Distance(s.transform.position, candidate) < minD) return false;
        }

        if (allLines != null && allLines.Count > 0)
        {
            var snap = TryFindSnapPosition(config, candidate, allLines, list, minFromLine, snapThreshold, minFromEndpoint);
            if (snap.HasValue)
            {
                if (IsPointInsideAnyAsteroidBelt(new Vector2(snap.Value.position.x, snap.Value.position.y), StationBeltAvoidMargin))
                    return false;
                result = snap.Value;
                return true;
            }
            float distToLine = GetMinDistanceToLines(candidate, allLines);
            if (distToLine < minFromLine) return false;
        }
        result = new SpawnResult { position = candidate };
        return true;
    }

    /// <summary>遍历所有线段，寻找任意可吸附的线上位置（兜底用）。</summary>
    private SpawnResult? TryFindAnyValidSnap(IReadOnlyList<Line> lines, List<StationBehaviour> allStations, float minFromEndpoint)
    {
        var candidates = new List<(Line line, int seg, Vector3 pos, float t)>();
        foreach (var line in lines)
        {
            if (line == null || line.stationSequence == null) continue;
            var seq = line.stationSequence;
            for (int i = 0; i < seq.Count - 1; i++)
            {
                var a = seq[i];
                var b = seq[i + 1];
                if (a == null || b == null) continue;
                for (int k = 1; k <= 4; k++)
                {
                    float t = k / 5f;
                    Vector3 p = Vector3.Lerp(a.transform.position, b.transform.position, t);
                    float dA = Vector2.Distance(p, a.transform.position);
                    float dB = Vector2.Distance(p, b.transform.position);
                    if (dA < minFromEndpoint || dB < minFromEndpoint) continue;
                    bool ok = true;
                    foreach (var s in allStations)
                    {
                        if (Vector2.Distance(s.transform.position, p) < minFromEndpoint) { ok = false; break; }
                    }
                    if (ok) candidates.Add((line, i, p, t));
                }
            }
        }
        if (candidates.Count == 0) return null;
        var c = candidates[Random.Range(0, candidates.Count)];
        return new SpawnResult { position = c.pos, lineToInsertInto = c.line, segmentIndex = c.seg, insertProgress = c.t };
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

    /// <summary>后期站点增多时，增大最小距离限制，避免过密。</summary>
    private static float GetLateGameDistanceScale(int stationCount)
    {
        if (stationCount < 12) return 1f;
        if (stationCount < 24) return 1f + (stationCount - 12) * 0.02f;
        if (stationCount < 36) return 1.24f + (stationCount - 24) * 0.025f;
        return Mathf.Min(1.8f, 1.54f + (stationCount - 36) * 0.015f);
    }

    /// <summary>计算所有站点的质心。</summary>
    private static Vector2 ComputeCentroid(List<StationBehaviour> list)
    {
        Vector2 sum = Vector2.zero;
        foreach (var s in list)
            sum += (Vector2)s.transform.position;
        return sum / list.Count;
    }

    /// <summary>站点到质心的平均距离，用于扩张趋势：新站倾向在此半径之外。</summary>
    private static float GetAverageDistanceFromCentroid(Vector2 centroid, List<StationBehaviour> list)
    {
        if (list == null || list.Count == 0) return 0f;
        float sum = 0f;
        foreach (var s in list)
            sum += Vector2.Distance((Vector2)s.transform.position, centroid);
        return sum / list.Count;
    }

    /// <summary>根据各扇区站点数量返回优先生成角度（度）。优先向上、左右上方扩张，避免向下。</summary>
    private static float GetPreferredSpawnAngle(Vector2 centroid, List<StationBehaviour> list, List<Vector2> beltCenters = null, int week = 0)
    {
        if (week == 0)
        {
            return 90f;
        }

        const int sectorCount = 8;
        float sectorSpan = 360f / sectorCount;
        var counts = new int[sectorCount];
        int maxCount = 0;
        foreach (var s in list)
        {
            Vector2 delta = (Vector2)s.transform.position - centroid;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;
            int sector = Mathf.Clamp((int)(angle / sectorSpan), 0, sectorCount - 1);
            counts[sector]++;
            if (counts[sector] > maxCount) maxCount = counts[sector];
        }

        // 方向权重：上=2.5，左右上=2.0，左右=1.5，下=0.5
        float[] directionWeights = new float[] { 1.8f, 2.0f, 2.0f, 1.5f, 1.5f, 0.6f, 0.6f, 1.8f };

        var weights = new float[sectorCount];
        for (int i = 0; i < sectorCount; i++)
        {
            float w = (maxCount - counts[i] + 1f) * directionWeights[i];
            weights[i] = Mathf.Max(0.01f, w);
        }

        float totalWeight = 0f;
        for (int i = 0; i < sectorCount; i++) totalWeight += weights[i];
        float r = Random.Range(0f, totalWeight);
        int chosen = 0;
        for (int i = 0; i < sectorCount; i++)
        {
            r -= weights[i];
            if (r <= 0) { chosen = i; break; }
        }

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

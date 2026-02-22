using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 无尽模式：每周中段随机时间，在已有站点周围一定距离随机生成新站点。
/// 每周生成 1（30%）或 2（70%）个站点；尽量远离已有线路。
/// </summary>
public class StationSpawner : MonoBehaviour
{
    private const float MinDistanceFromLineFactor = 0.6f;

    private float _weekTimer;
    private float _spawnTriggerTime = -1f;
    private int _spawnedThisWeek;
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
            _spawnTriggerTime = -1f;
            _spawnedThisWeek = 0;
        }

        float duration = gm.WeekDurationSeconds;
        _weekTimer = duration - gm.WeekTimerRemaining;

        if (_spawnTriggerTime < 0)
        {
            float min = config.spawnMidWeekTimeMin * duration;
            float max = config.spawnMidWeekTimeMax * duration;
            _spawnTriggerTime = Random.Range(min, max);
        }

        int toSpawn = _spawnedThisWeek == 0 ? (Random.value < 0.7f ? 2 : 1) : 0;
        if (toSpawn > 0 && _weekTimer >= _spawnTriggerTime)
        {
            for (int i = 0; i < toSpawn; i++)
            {
                if (TrySpawnStation()) _spawnedThisWeek++;
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

        LevelLoader.SpawnStation(config, parent, gm.stationPrefab, gm.visualConfig, pos.Value, template.shapeType, template.displayName, id, stations, true);
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

        var baseStation = list[Random.Range(0, list.Count)];
        Vector2 basePos = baseStation.transform.position;

        for (int attempt = 0; attempt < 50; attempt++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist = Random.Range(minD, maxD);
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
            Vector3 candidate = new Vector3(basePos.x + offset.x, basePos.y + offset.y, 0f);

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
        return new Vector3(basePos.x + maxD, basePos.y, 0f);
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

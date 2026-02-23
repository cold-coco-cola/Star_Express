using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StationConfig
{
    public string id;
    public string displayName;
    public ShapeType shapeType;
    public Vector2 position;
    public string unlockPhase; // "fixed" / "random_pool"
    public int unlockAfterWeeks;
}

[Serializable]
public class LevelConfigOverrides
{
    public float passengerSpawnInterval = 14f;
    public int passengerSpawnIntervalAfterWeeks = 0;
    public float passengerSpawnIntervalLate = 12f;
}

[CreateAssetMenu(fileName = "LevelConfig", menuName = "Star Express/Level Config", order = 1)]
public class LevelConfig : ScriptableObject
{
    public string levelId;
    public string displayName;
    public List<StationConfig> stations = new List<StationConfig>();
    public List<string> randomPoolStations = new List<string>();
    public int randomUnlockPerWeekMin = 1;
    public int randomUnlockPerWeekMax = 2;
    public LevelConfigOverrides overrides;

    [Header("无尽模式：开局与随机生成")]
    [Tooltip("开局加载的站点数量（取 stations 前 N 个）")]
    public int startStationCount = 3;
    [Tooltip("新站生成：距最近站点的最小距离（世界单位），保证站点与乘客显示不重合")]
    public float spawnDistanceMin = 2.2f;
    [Tooltip("新站生成：距最近站点的最大距离（世界单位）")]
    public float spawnDistanceMax = 3.5f;
    [Tooltip("距线路过近时吸附为线上站：距离阈值（世界单位），0 表示不吸附")]
    public float spawnSnapToLineThreshold = 0.8f;
    [Tooltip("每周中段生成时间：周进度百分比下限，如 0.4 表示 40%")]
    [Range(0.2f, 0.8f)] public float spawnMidWeekTimeMin = 0.4f;
    [Tooltip("每周中段生成时间：周进度百分比上限，如 0.7 表示 70%")]
    [Range(0.3f, 0.9f)] public float spawnMidWeekTimeMax = 0.7f;
    [Tooltip("生成模板：从这些 station id 中随机取形状与名称；空则从 stations 全表随机")]
    public List<string> spawnTemplateIds = new List<string>();
}

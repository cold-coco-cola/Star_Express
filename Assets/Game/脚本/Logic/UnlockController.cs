using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 每周末执行解锁：阶段一按 unlockAfterWeeks 解锁固定站；阶段二从 randomPoolStations 随机 1～2 站解锁。
/// </summary>
public static class UnlockController
{
    /// <summary>
    /// 进入新周时调用。阶段一：fixed 且 currentWeek >= unlockAfterWeeks 的站解锁；阶段二：周 3 起每周末从随机池解锁 1～2 站。
    /// </summary>
    public static void OnWeekAdvanced(int currentWeek, LevelConfig levelConfig, Dictionary<string, StationBehaviour> stationsById)
    {
        if (levelConfig == null || stationsById == null) return;

        // 阶段一：固定节奏（周 0～2）
        foreach (var config in levelConfig.stations)
        {
            if (config.unlockPhase != "fixed") continue;
            if (currentWeek < config.unlockAfterWeeks) continue;
            if (!stationsById.TryGetValue(config.id, out var station)) continue;
            if (station.isUnlocked) continue;

            station.isUnlocked = true;
            station.RefreshVisual();
        }

        // 阶段二：随机池（周 3 起）
        if (currentWeek < 3) return;
        if (levelConfig.randomPoolStations == null || levelConfig.randomPoolStations.Count == 0) return;

        var lockedPool = new List<string>();
        foreach (var id in levelConfig.randomPoolStations)
        {
            if (!stationsById.TryGetValue(id, out var st)) continue;
            if (!st.isUnlocked) lockedPool.Add(id);
        }
        if (lockedPool.Count == 0) return;

        int toUnlock = Mathf.Min(Random.Range(levelConfig.randomUnlockPerWeekMin, levelConfig.randomUnlockPerWeekMax + 1), lockedPool.Count);
        for (int i = 0; i < toUnlock && lockedPool.Count > 0; i++)
        {
            int idx = Random.Range(0, lockedPool.Count);
            string id = lockedPool[idx];
            lockedPool.RemoveAt(idx);
            if (stationsById.TryGetValue(id, out var station))
            {
                station.isUnlocked = true;
                station.RefreshVisual();
            }
        }
    }
}

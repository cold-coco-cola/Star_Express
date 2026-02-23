using System.Collections.Generic;

/// <summary>
/// 路径可达性工具（PRD §5.2）。
/// BFS 判定：从起点站出发，经过「同线相邻站 + 同站换乘」是否可到达某形状的站点。
/// 首版全站可换乘，不依赖 Hub。
/// </summary>
public static class RouteHelper
{
    /// <summary>
    /// 判断从 startStation 出发，通过当前所有航线，能否到达形状为 targetShape 的任意站点。
    /// </summary>
    public static bool CanReach(StationBehaviour startStation, ShapeType targetShape, IReadOnlyList<Line> allLines)
    {
        if (startStation == null || allLines == null || allLines.Count == 0) return false;
        if (startStation.stationType == targetShape) return true;

        var visited = new HashSet<StationBehaviour> { startStation };
        var queue = new Queue<StationBehaviour>();
        queue.Enqueue(startStation);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var neighbors = GetNeighbors(current, allLines);
            foreach (var neighbor in neighbors)
            {
                if (visited.Contains(neighbor)) continue;
                visited.Add(neighbor);
                if (neighbor.isUnlocked && neighbor.stationType == targetShape)
                    return true;
                queue.Enqueue(neighbor);
            }
        }

        return false;
    }

    /// <summary>
    /// 判断从 startStation 出发，乘客是否可以通过当前船所在的线路 shipLine 直达目标形状。
    /// </summary>
    public static bool CanReachOnLine(StationBehaviour startStation, ShapeType targetShape, Line shipLine)
    {
        if (shipLine == null || shipLine.stationSequence == null) return false;
        foreach (var station in shipLine.stationSequence)
        {
            if (station != null && station != startStation && station.isUnlocked && station.stationType == targetShape)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 获取目标站在线路上的索引。targetStationId 非空时精确匹配；否则返回第一个匹配形状的索引。返回 -1 表示不在线上。
    /// </summary>
    public static int GetTargetStationIndexOnLine(Line line, ShapeType targetShape, string targetStationId)
    {
        if (line == null || line.stationSequence == null) return -1;
        var seq = line.stationSequence;
        if (!string.IsNullOrEmpty(targetStationId))
        {
            for (int i = 0; i < seq.Count; i++)
            {
                if (seq[i] != null && seq[i].id == targetStationId) return i;
            }
            return -1;
        }
        for (int i = 0; i < seq.Count; i++)
        {
            if (seq[i] != null && seq[i].isUnlocked && seq[i].stationType == targetShape) return i;
        }
        return -1;
    }

    /// <summary>
    /// 获取目标站在线路上的所有索引（多站同形状时）。targetStationId 非空时只返回精确匹配；否则返回所有匹配形状的索引。
    /// </summary>
    public static List<int> GetAllTargetStationIndicesOnLine(Line line, ShapeType targetShape, string targetStationId)
    {
        var result = new List<int>();
        if (line == null || line.stationSequence == null) return result;
        var seq = line.stationSequence;
        if (!string.IsNullOrEmpty(targetStationId))
        {
            for (int i = 0; i < seq.Count; i++)
            {
                if (seq[i] != null && seq[i].id == targetStationId) { result.Add(i); return result; }
            }
            return result;
        }
        for (int i = 0; i < seq.Count; i++)
        {
            if (seq[i] != null && seq[i].isUnlocked && seq[i].stationType == targetShape)
                result.Add(i);
        }
        return result;
    }

    /// <summary>
    /// 从 station 出发，存在直达目标的线路（同线可达、无需换乘）。用于「直达优先」：有直达时不考虑换乘。
    /// </summary>
    public static bool HasDirectRouteFromStation(StationBehaviour station, ShapeType targetShape, string targetStationId,
        IReadOnlyList<Line> allLines)
    {
        if (station == null || allLines == null) return false;
        foreach (var line in allLines)
        {
            if (line == null || line.stationSequence == null) continue;
            if (!line.stationSequence.Contains(station)) continue;
            if (GetTargetStationIndexOnLine(line, targetShape, targetStationId) >= 0)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 从 station 出发，返回所有直达目标的线路（同线可达、无需换乘）。
    /// </summary>
    public static List<Line> GetDirectRouteLinesFromStation(StationBehaviour station, ShapeType targetShape, string targetStationId,
        IReadOnlyList<Line> allLines)
    {
        var result = new List<Line>();
        if (station == null || allLines == null) return result;
        foreach (var line in allLines)
        {
            if (line == null || line.stationSequence == null) continue;
            if (!line.stationSequence.Contains(station)) continue;
            if (GetTargetStationIndexOnLine(line, targetShape, targetStationId) >= 0)
                result.Add(line);
        }
        return result;
    }

    /// <summary>
    /// 目标站是否在船当前前进方向上。direction 1=向高索引，-1=向低索引。
    /// </summary>
    public static bool IsTargetAheadOnLine(int currentStationIdx, int targetStationIdx, int direction)
    {
        if (targetStationIdx < 0) return false;
        if (direction > 0) return targetStationIdx > currentStationIdx;
        if (direction < 0) return targetStationIdx < currentStationIdx;
        return false;
    }

    /// <summary>
    /// 获取当前线上、从该站换乘可到达目标的换乘站索引列表。用于载客时判断是否有可达的换乘站在前进方向。
    /// </summary>
    public static List<int> GetTransferStationIndicesOnLine(Line line, ShapeType targetShape, string targetStationId,
        IReadOnlyList<Line> allLines)
    {
        var result = new List<int>();
        if (line == null || line.stationSequence == null || allLines == null || allLines.Count < 2) return result;

        var seq = line.stationSequence;
        for (int i = 0; i < seq.Count; i++)
        {
            var st = seq[i];
            if (st == null) continue;
            if (st.stationType == targetShape && (string.IsNullOrEmpty(targetStationId) || st.id == targetStationId))
            {
                result.Add(i);
                continue;
            }
            if (!IsTransferStation(st, line, allLines)) continue;
            if (CanReach(st, targetShape, allLines) || CanReachTargetShapeAny(st, targetShape, allLines))
                result.Add(i);
        }
        return result;
    }

    private static bool IsTransferStation(StationBehaviour station, Line currentLine, IReadOnlyList<Line> allLines)
    {
        if (station == null || allLines == null) return false;
        int count = 0;
        bool hasOther = false;
        foreach (var l in allLines)
        {
            if (l == null || l.stationSequence == null) continue;
            if (!l.stationSequence.Contains(station)) continue;
            count++;
            if (l != currentLine) hasOther = true;
        }
        return count >= 2 && hasOther;
    }

    /// <summary>
    /// 判断在 transferStation 下船后，通过其他线路能否到达目标形状。
    /// 用于换乘决策：当前船无法直达，但在此站换乘其他线路可达。
    /// 若目标站未解锁也允许换乘，乘客可先下车等待。
    /// </summary>
    public static bool CanReachViaTransfer(StationBehaviour transferStation, ShapeType targetShape,
        Line currentShipLine, IReadOnlyList<Line> allLines)
    {
        if (transferStation == null || allLines == null) return false;
        int linesAtStation = 0;
        bool hasOtherLine = false;
        foreach (var line in allLines)
        {
            if (line == null || line.stationSequence == null) continue;
            if (line.stationSequence.Contains(transferStation))
            {
                linesAtStation++;
                if (line != currentShipLine) hasOtherLine = true;
            }
        }
        if (linesAtStation < 2 || !hasOtherLine) return false;

        return CanReach(transferStation, targetShape, allLines)
            || CanReachTargetShapeAny(transferStation, targetShape, allLines);
    }

    /// <summary>从 startStation 出发，是否存在任意（含未解锁）目标形状站点。用于换乘时允许先下车等待。</summary>
    public static bool CanReachTargetShapeAny(StationBehaviour startStation, ShapeType targetShape,
        IReadOnlyList<Line> allLines)
    {
        if (startStation == null || allLines == null || allLines.Count == 0) return false;
        if (startStation.stationType == targetShape) return true;

        var visited = new HashSet<StationBehaviour> { startStation };
        var queue = new Queue<StationBehaviour>();
        queue.Enqueue(startStation);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var neighbors = GetNeighbors(current, allLines);
            foreach (var neighbor in neighbors)
            {
                if (visited.Contains(neighbor)) continue;
                visited.Add(neighbor);
                if (neighbor.stationType == targetShape)
                    return true;
                queue.Enqueue(neighbor);
            }
        }
        return false;
    }

    private static List<StationBehaviour> GetNeighbors(StationBehaviour station, IReadOnlyList<Line> allLines)
    {
        var neighbors = new List<StationBehaviour>();
        foreach (var line in allLines)
        {
            if (line == null || line.stationSequence == null) continue;
            var seq = line.stationSequence;
            for (int i = 0; i < seq.Count; i++)
            {
                if (seq[i] != station) continue;
                if (i > 0 && seq[i - 1] != null && !neighbors.Contains(seq[i - 1]))
                    neighbors.Add(seq[i - 1]);
                if (i < seq.Count - 1 && seq[i + 1] != null && !neighbors.Contains(seq[i + 1]))
                    neighbors.Add(seq[i + 1]);
            }
        }
        return neighbors;
    }
}

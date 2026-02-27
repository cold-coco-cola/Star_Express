using System.Collections.Generic;

/// <summary>
/// 乘客运输逻辑（PRD §5.1）：统一处理停靠时的卸客与载客决策。
/// 核心原则：直达优先——存在不需换乘的解法时，不考虑换乘。
/// 决策树：
/// 1. 卸客·目的地：当前站即目标 → 下车得分
/// 2. 卸客·换乘：本线目标在反方向 → 下车等反向船；目标不在本线且本站可换乘 → 下车换乘
/// 3. 载客：有直达线时仅载本线直达；无直达时才载需换乘的乘客
/// </summary>
public static class PassengerTransportLogic
{
    /// <summary>停靠处理结果，供 ShipBehaviour 执行。</summary>
    public struct DockingResult
    {
        public List<Passenger> ToUnloadDestination;
        public List<Passenger> ToTransfer;
        public List<Passenger> ToLoad;
    }

    /// <summary>
    /// 计算本次停靠应执行的所有乘客操作。
    /// </summary>
    /// <param name="station">当前停靠站</param>
    /// <param name="stationIdx">当前站在线路 sequence 中的索引</param>
    /// <param name="ship">当前飞船</param>
    /// <param name="direction">船当前前进方向（1=向高索引，-1=向低索引）</param>
    /// <param name="allLines">所有航线（可为 null，此时不执行换乘/载客）</param>
    public static DockingResult ComputeDockingActions(StationBehaviour station, int stationIdx, ShipBehaviour ship,
        int direction, IReadOnlyList<Line> allLines)
    {
        var result = new DockingResult
        {
            ToUnloadDestination = new List<Passenger>(),
            ToTransfer = new List<Passenger>(),
            ToLoad = new List<Passenger>()
        };

        if (station == null || ship == null) return result;

        var line = ship.line;
        if (line == null || line.stationSequence == null) return result;

        // 1. 卸客·目的地：精确匹配 targetStationId，避免同形状多站时卸错站
        foreach (var p in ship.passengers)
        {
            if (p == null) continue;
            if (IsDestinationStation(station, p))
                result.ToUnloadDestination.Add(p);
        }

        // 2. 卸客·换乘（排除已卸目的地的乘客）：目标在反方向时应下车，避免反向运输；目标不在当前线时在换乘站下车
        var toTransferSet = new HashSet<Passenger>(result.ToUnloadDestination);
        if (allLines != null)
        {
            foreach (var p in ship.passengers)
            {
                if (p == null || toTransferSet.Contains(p)) continue;
                if (ShouldTransfer(p, station, stationIdx, line, direction, allLines))
                    result.ToTransfer.Add(p);
            }
        }

        // 3. 载客：仅载目标在前进方向的乘客，避免反向运输
        if (allLines != null && allLines.Count > 0 && ship.passengers.Count < ship.capacity)
        {
            var waiting = station.waitingPassengers;
            int onboardAfterUnload = ship.passengers.Count - result.ToUnloadDestination.Count - result.ToTransfer.Count;
            for (int i = 0; i < waiting.Count && result.ToLoad.Count + onboardAfterUnload < ship.capacity; i++)
            {
                var p = waiting[i];
                if (p == null) continue;
                if (ShouldLoad(p, station, stationIdx, line, direction, allLines))
                    result.ToLoad.Add(p);
            }
        }

        return result;
    }

    /// <summary>当前站是否为乘客的目的地（优先 targetStationId 精确匹配）。</summary>
    private static bool IsDestinationStation(StationBehaviour station, Passenger p)
    {
        if (!string.IsNullOrEmpty(p.targetStationId))
            return station.id == p.targetStationId;
        return station.stationType == p.targetShape;
    }

    /// <summary>
    /// 乘客是否应在此站换乘下车。
    /// 规则（优先级）：① 本线前进方向有目的地 → 不下车直达；② 本线目标在反方向 → 下车等反向船；③ 目标不在本线且本站可换乘 → 下车换乘。
    /// </summary>
    public static bool ShouldTransfer(Passenger passenger, StationBehaviour station, int stationIdx, Line currentLine,
        int direction, IReadOnlyList<Line> allLines)
    {
        if (passenger == null || station == null || currentLine == null) return false;

        var targetIndices = RouteHelper.GetAllTargetStationIndicesOnLine(currentLine, passenger.targetShape, passenger.targetStationId);

        // 目标在当前线上：若前进方向上有任一目标站则不下车（直达优先）；否则目标在反方向，下车等反向船
        if (targetIndices.Count > 0)
        {
            foreach (int idx in targetIndices)
            {
                if (RouteHelper.IsTargetAheadOnLine(currentLine, stationIdx, idx, direction))
                    return false; // 有目标在前进方向，继续直达
            }
            return true; // 所有目标都在反方向，下车换乘
        }

        // 目标不在当前线，需在换乘站下车
        if (allLines == null || allLines.Count < 2) return false;
        if (!IsTransferStation(station, currentLine, allLines))
            return false;
        return CanReachTarget(station, passenger.targetShape, allLines);
    }

    /// <summary>
    /// 是否应载该乘客。直达优先：有直达线路时只载本线直达；无直达时才考虑换乘。
    /// </summary>
    public static bool ShouldLoad(Passenger passenger, StationBehaviour station, int stationIdx, Line line,
        int direction, IReadOnlyList<Line> allLines)
    {
        if (passenger == null || station == null || line == null || allLines == null || allLines.Count == 0) return false;
        if (!CanReachTarget(station, passenger.targetShape, allLines)) return false;

        var seq = line.stationSequence;
        if (seq == null || seq.Count == 0) return false;

        var directLines = RouteHelper.GetDirectRouteLinesFromStation(station, passenger.targetShape, passenger.targetStationId, allLines);

        // 存在直达线路：仅当本线是直达线且目标在前进方向时载客
        if (directLines.Count > 0)
        {
            if (!directLines.Contains(line)) return false;
            var targetIndices = RouteHelper.GetAllTargetStationIndicesOnLine(line, passenger.targetShape, passenger.targetStationId);
            foreach (int idx in targetIndices)
            {
                if (RouteHelper.IsTargetAheadOnLine(line, stationIdx, idx, direction))
                    return true;
            }
            return false;
        }

        // 无直达：需换乘，仅当本线前进方向有可换乘到目标的换乘站时载客
        int step = (direction > 0) ? 1 : -1;
        bool isLoop = line.IsLoop();
        int count = seq.Count;

        if (isLoop && count >= 3)
        {
            for (int k = 1; k < count; k++)
            {
                int i = ((stationIdx + step * k) % count + count) % count;
                var s = seq[i];
                if (s == null) continue;
                if (IsDestinationStation(s, passenger)) return true;
                if (allLines.Count >= 2 && IsTransferStation(s, line, allLines) &&
                    (RouteHelper.CanReach(s, passenger.targetShape, allLines) || RouteHelper.CanReachTargetShapeAny(s, passenger.targetShape, allLines)))
                    return true;
            }
        }
        else
        {
            for (int i = stationIdx + step; i >= 0 && i < seq.Count; i += step)
            {
                var s = seq[i];
                if (s == null) continue;
                if (IsDestinationStation(s, passenger)) return true;
                if (allLines.Count >= 2 && IsTransferStation(s, line, allLines) &&
                    (RouteHelper.CanReach(s, passenger.targetShape, allLines) || RouteHelper.CanReachTargetShapeAny(s, passenger.targetShape, allLines)))
                    return true;
            }
        }
        return false;
    }

    /// <summary>从某站出发，能否到达目标形状（含未解锁站，允许先下车等待）。</summary>
    public static bool CanReachTarget(StationBehaviour startStation, ShapeType targetShape,
        IReadOnlyList<Line> allLines)
    {
        if (startStation == null || allLines == null || allLines.Count == 0) return false;
        if (startStation.stationType == targetShape) return true;

        return RouteHelper.CanReach(startStation, targetShape, allLines)
            || RouteHelper.CanReachTargetShapeAny(startStation, targetShape, allLines);
    }

    /// <summary>目标形状是否在当前线路上（排除当前站）。</summary>
    public static bool IsTargetOnLine(StationBehaviour currentStation, ShapeType targetShape, Line line)
    {
        return RouteHelper.CanReachOnLine(currentStation, targetShape, line);
    }

    /// <summary>本站是否为换乘站（至少两条线路经停）。</summary>
    public static bool IsTransferStation(StationBehaviour station, Line currentLine, IReadOnlyList<Line> allLines)
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
}

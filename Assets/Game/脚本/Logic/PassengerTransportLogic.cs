using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 乘客运输逻辑（PRD §5.1）：统一处理停靠时的卸客与载客决策。
/// 决策树：
/// 1. 卸客·目的地：当前站即目标形状 → 下车得分
/// 2. 卸客·换乘：目标不在当前线 且 本站为换乘站 且 从本站可到达目标 → 下车等待
/// 3. 载客：目标可达（含换乘）→ 上车
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
    /// <param name="ship">当前飞船</param>
    /// <param name="allLines">所有航线（可为 null，此时不执行换乘/载客）</param>
    public static DockingResult ComputeDockingActions(StationBehaviour station, ShipBehaviour ship,
        IReadOnlyList<Line> allLines)
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

        // 1. 卸客·目的地
        foreach (var p in ship.passengers)
        {
            if (p == null) continue;
            if (station.stationType == p.targetShape)
                result.ToUnloadDestination.Add(p);
        }

        // 2. 卸客·换乘（排除已卸目的地的乘客）
        var toTransferSet = new HashSet<Passenger>(result.ToUnloadDestination);
        if (allLines != null && allLines.Count >= 2)
        {
            foreach (var p in ship.passengers)
            {
                if (p == null || toTransferSet.Contains(p)) continue;
                if (ShouldTransfer(p, station, line, allLines))
                    result.ToTransfer.Add(p);
            }
        }

        // 3. 载客
        if (allLines != null && allLines.Count > 0 && ship.passengers.Count < ship.capacity)
        {
            var waiting = station.waitingPassengers;
            for (int i = 0; i < waiting.Count && result.ToLoad.Count + ship.passengers.Count - result.ToUnloadDestination.Count - result.ToTransfer.Count < ship.capacity; i++)
            {
                var p = waiting[i];
                if (p == null) continue;
                if (CanReachTarget(station, p.targetShape, allLines))
                    result.ToLoad.Add(p);
            }
        }

        return result;
    }

    /// <summary>乘客是否应在此站换乘下车。</summary>
    public static bool ShouldTransfer(Passenger passenger, StationBehaviour station, Line currentLine,
        IReadOnlyList<Line> allLines)
    {
        if (passenger == null || station == null || currentLine == null || allLines == null) return false;

        // 目标已在当前线路上，无需换乘
        if (IsTargetOnLine(station, passenger.targetShape, currentLine))
            return false;

        // 本站必须有至少两条线路才可换乘
        if (!IsTransferStation(station, currentLine, allLines))
            return false;

        // 从本站出发（经任意线路）能到达目标
        return CanReachTarget(station, passenger.targetShape, allLines);
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

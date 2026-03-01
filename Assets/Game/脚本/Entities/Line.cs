using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 航线纯数据：color、stationSequence、ships；无 GameObject，由 LineManager 维护并驱动视觉。
/// </summary>
public class Line
{
    public string id;
    public LineColor color;
    /// <summary>与线路视觉一致的颜色，由 LineManager 在 CreateOrUpdateLineRenderer 时写入，飞船直接使用。</summary>
    public Color displayColor;
    public List<StationBehaviour> stationSequence = new List<StationBehaviour>();
    public List<ShipBehaviour> ships = new List<ShipBehaviour>();

    /// <summary>每个线段消耗的星燧数量，索引与 stationSequence 对应（N个站点有N-1个线段）。</summary>
    public List<int> segmentStarTunnelCosts = new List<int>();

    public Line(string lineId, LineColor lineColor)
    {
        id = lineId;
        color = lineColor;
    }

    /// <summary>Whether the segment at the given index is an end segment (one end is a line endpoint).</summary>
    public bool IsEndSegment(int segmentIndex)
    {
        if (stationSequence == null || stationSequence.Count < 2) return false;
        if (segmentIndex < 0 || segmentIndex >= stationSequence.Count - 1) return false;
        if (IsLoop()) return true;
        return segmentIndex == 0 || segmentIndex == stationSequence.Count - 2;
    }

    /// <summary>Gets the endpoint station of an end segment (degree-1 station). Returns null for middle segments.</summary>
    public StationBehaviour GetEndStationOfSegment(int segmentIndex)
    {
        if (!IsEndSegment(segmentIndex)) return null;
        if (IsLoop())
        {
            return stationSequence[segmentIndex];
        }
        if (segmentIndex == 0) return stationSequence[0];
        return stationSequence[stationSequence.Count - 1];
    }

    /// <summary>Checks whether the station is already in this line.</summary>
    public bool ContainsStation(StationBehaviour station)
    {
        if (station == null || stationSequence == null) return false;
        return stationSequence.Contains(station);
    }

    /// <summary>Checks if station is in the line, excluding one occurrence (e.g. the other endpoint for loop detection).</summary>
    public bool ContainsStationExcluding(StationBehaviour station, StationBehaviour exclude)
    {
        if (station == null || stationSequence == null) return false;
        foreach (var s in stationSequence)
        {
            if (s == station && s != exclude) return true;
        }
        return false;
    }

    /// <summary>Checks if adding this station can form a loop (it is already an endpoint).</summary>
    public bool CanFormLoop(StationBehaviour station)
    {
        if (stationSequence == null || stationSequence.Count < 2) return false;
        return stationSequence[0] == station || stationSequence[stationSequence.Count - 1] == station;
    }

    /// <summary>Checks if the line is already a loop (first and last are the same station).</summary>
    public bool IsLoop()
    {
        if (stationSequence == null || stationSequence.Count < 3) return false;
        return stationSequence[0] == stationSequence[stationSequence.Count - 1];
    }
}

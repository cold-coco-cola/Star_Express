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
        return segmentIndex == 0 || segmentIndex == stationSequence.Count - 2;
    }

    /// <summary>Gets the endpoint station of an end segment (degree-1 station). Returns null for middle segments.</summary>
    public StationBehaviour GetEndStationOfSegment(int segmentIndex)
    {
        if (!IsEndSegment(segmentIndex)) return null;
        if (segmentIndex == 0) return stationSequence[0];
        return stationSequence[stationSequence.Count - 1];
    }

    /// <summary>Checks whether the station is already in this line.</summary>
    public bool ContainsStation(StationBehaviour station)
    {
        if (station == null || stationSequence == null) return false;
        return stationSequence.Contains(station);
    }
}

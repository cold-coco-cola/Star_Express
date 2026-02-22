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
}

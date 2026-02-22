using UnityEngine;

/// <summary>
/// 建线/延伸接口，供输入层调用。实现类为 LineManager。
/// </summary>
public interface ILineManager
{
    bool TryCreateOrExtendLine(StationBehaviour stationA, StationBehaviour stationB, LineColor color);
    /// <summary>获取线上某段某进度的世界坐标（圆滑曲线），供飞船贴线移动。</summary>
    Vector3 GetPositionOnLine(Line line, int segmentIndex, float progressOnSegment);
    /// <summary>获取线上某段某进度处的切向（单位向量），供飞船朝向。</summary>
    Vector3 GetTangentOnLine(Line line, int segmentIndex, float progressOnSegment);
}

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
    /// <summary>Tries to remove an end segment. Returns true if successful.</summary>
    bool TryRemoveEndSegment(Line line, int segmentIndex);
    /// <summary>Gets the segment under the given world position. Returns (line, segmentIndex) or null. If endSegmentsOnly=true, only checks end segments (first or last).</summary>
    (Line line, int segmentIndex)? GetSegmentUnderMouse(Vector2 worldPosition, float hitRadius, bool endSegmentsOnly = false);
    /// <summary>Inserts a station into the middle of the given segment.</summary>
    bool InsertStationIntoLine(Line line, int segmentIndex, StationBehaviour newStation);
    /// <summary>Sets segment highlight. pulse=true for editing state (continuous scaling), pulse=false for hover (static).</summary>
    void SetSegmentHighlight(Line line, int segmentIndex, bool highlighted, bool pulse = false);
}

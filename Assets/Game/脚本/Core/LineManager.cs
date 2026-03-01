using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 维护 Line 列表；TryCreateOrExtendLine 实现新建/延伸；同色仅一条线，延伸仅限端点；每条 Line 对应一个 LineRenderer 视觉。
/// </summary>
public class LineManager : MonoBehaviour, ILineManager
{
    public Transform linesRoot;
    public VisualConfig visualConfig;
    [Header("飞船放置（阶段 5）")]
    [Tooltip("留空则编辑器下按路径加载")]
    public GameObject shipPrefab;
    [Tooltip("留空则自动查找 Map/Ships 或 Ships")]
    public Transform shipsRoot;

    private readonly List<Line> _lines = new List<Line>();
    private int _nextLineId;
    private int _nextShipId;
    [SerializeField] private int _maxLineCount = 3;
    private static Material _cachedLineMaterial;

    [Header("资源存量（PRD §4.6）")]
    [Tooltip("飞船：每周 +1，初始与线路数一致（3 艘）")]
    [SerializeField] private int _shipStock = 3;
    [Tooltip("客舱：可升级飞船容量 +2")]
    [SerializeField] private int _carriageStock = 0;
    [Tooltip("星隧：首关不使用")]
    [SerializeField] private int _starTunnelStock = 2;

    /// <summary>当前所有航线，供放置飞船 UI 使用。</summary>
    public IReadOnlyList<Line> Lines => _lines;
    /// <summary>线路数量上限（初始 3，周奖励新线路可增至 6）。</summary>
    public int MaxLineCount => _maxLineCount;
    public int ShipStock => _shipStock;
    public int CarriageStock => _carriageStock;
    public int StarTunnelStock => _starTunnelStock;

    private void Awake()
    {
        if (linesRoot == null)
            linesRoot = GameObject.Find("Map")?.transform?.Find("Lines") ?? GameObject.Find("Lines")?.transform;
        if (shipsRoot == null)
            shipsRoot = GameObject.Find("Map")?.transform?.Find("Ships") ?? GameObject.Find("Ships")?.transform;
        // 若 shipsRoot 为持久化对象（如预制体资产内引用），不能作为 Instantiate 的 parent，改为在场景中创建/使用根节点
        if (shipsRoot != null && !shipsRoot.gameObject.scene.IsValid())
            shipsRoot = null;
        if (shipsRoot == null)
        {
            var go = new GameObject("Ships");
            go.transform.SetParent(transform, false);
            shipsRoot = go.transform;
        }
#if UNITY_EDITOR
        if (shipPrefab == null)
            shipPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/预制体/Ship.prefab");
#endif
    }

    /// <summary>获取线段AB穿越所有陨石带的总次数。</summary>
    public int GetTotalCrossCount(Vector2 a, Vector2 b)
    {
        var belts = FindObjectsOfType<AsteroidBeltBehaviour>();
        if (belts == null || belts.Length == 0) return 0;
        int total = 0;
        foreach (var belt in belts)
        {
            if (belt != null)
                total += belt.GetCrossCount(a, b);
        }
        return total;
    }

    private static void NotifyStarTunnelInsufficient()
    {
        Debug.LogWarning("星燧不足，无法穿越陨石带");
    }

    /// <summary>
    /// 尝试在 A、B 间建线或延伸已有线（颜色 color）。同色仅一条线；延伸仅限该线首/末站。返回是否成功。
    /// </summary>
    public bool TryCreateOrExtendLine(StationBehaviour stationA, StationBehaviour stationB, LineColor color)
    {
        if (stationA == null || stationB == null) return false;
        if (stationA == stationB) return false;
        if (!stationA.isUnlocked || !stationB.isUnlocked) return false;

        Line existingOfColor = null;
        foreach (var line in _lines)
        {
            if (line.color == color)
            {
                existingOfColor = line;
                break;
            }
        }

        if (existingOfColor == null)
        {
            if (_lines.Count >= _maxLineCount) return false;
            if (_shipStock <= 0) return false;

            int crossCount = GetTotalCrossCount(stationA.transform.position, stationB.transform.position);
            if (_starTunnelStock < crossCount)
            {
                NotifyStarTunnelInsufficient();
                return false;
            }
            _starTunnelStock -= crossCount;

            var line = new Line("Line_" + (_nextLineId++), color);
            line.stationSequence.Add(stationA);
            line.stationSequence.Add(stationB);
            line.segmentStarTunnelCosts.Add(crossCount);
            _lines.Add(line);
            RefreshAllLinesSharingSegmentsWith(line);
            SpawnShip(line, true);
            return true;
        }

        var seq = existingOfColor.stationSequence;
        if (seq.Count < 2) return false;

        if (existingOfColor.IsLoop()) return false;

        int firstIdx = 0;
        int lastIdx = seq.Count - 1;
        bool aIsFirst = seq[firstIdx] == stationA;
        bool aIsLast = seq[lastIdx] == stationA;
        bool bIsFirst = seq[firstIdx] == stationB;
        bool bIsLast = seq[lastIdx] == stationB;

        if (aIsFirst && bIsLast || aIsLast && bIsFirst)
        {
            if (HasAdjacent(seq, stationA, stationB)) return false;
        }

        StationBehaviour targetStation = null;
        StationBehaviour excludeEndpoint = null;
        bool wouldFormLoop = false;
        if (aIsFirst) { targetStation = stationB; excludeEndpoint = seq[lastIdx]; wouldFormLoop = (seq[lastIdx] == stationB); }
        else if (aIsLast) { targetStation = stationB; excludeEndpoint = seq[firstIdx]; wouldFormLoop = (seq[firstIdx] == stationB); }
        else if (bIsFirst) { targetStation = stationA; excludeEndpoint = seq[lastIdx]; wouldFormLoop = (seq[lastIdx] == stationA); }
        else if (bIsLast) { targetStation = stationA; excludeEndpoint = seq[firstIdx]; wouldFormLoop = (seq[firstIdx] == stationA); }

        if (targetStation != null && existingOfColor.ContainsStationExcluding(targetStation, excludeEndpoint) && !wouldFormLoop)
            return false;

        if (wouldFormLoop)
        {
            StationBehaviour endStation = seq[lastIdx];
            StationBehaviour startStation = seq[firstIdx];
            int crossCount = GetTotalCrossCount(endStation.transform.position, startStation.transform.position);
            if (_starTunnelStock < crossCount)
            {
                NotifyStarTunnelInsufficient();
                return false;
            }
            _starTunnelStock -= crossCount;

            if (aIsFirst) { seq.Insert(0, seq[lastIdx]); }
            else if (aIsLast) { seq.Add(seq[firstIdx]); }
            else if (bIsFirst) { seq.Insert(0, seq[lastIdx]); }
            else if (bIsLast) { seq.Add(seq[firstIdx]); }
            existingOfColor.segmentStarTunnelCosts.Add(crossCount);
            RefreshAllLinesSharingSegmentsWith(existingOfColor);
            return true;
        }

        if (aIsFirst)
        {
            int crossCount = GetTotalCrossCount(stationB.transform.position, seq[firstIdx].transform.position);
            if (_starTunnelStock < crossCount) { NotifyStarTunnelInsufficient(); return false; }
            _starTunnelStock -= crossCount;
            seq.Insert(0, stationB);
            existingOfColor.segmentStarTunnelCosts.Insert(0, crossCount);
            RefreshAllLinesSharingSegmentsWith(existingOfColor);
            return true;
        }
        if (bIsFirst)
        {
            int crossCount = GetTotalCrossCount(stationA.transform.position, seq[firstIdx].transform.position);
            if (_starTunnelStock < crossCount) { NotifyStarTunnelInsufficient(); return false; }
            _starTunnelStock -= crossCount;
            seq.Insert(0, stationA);
            existingOfColor.segmentStarTunnelCosts.Insert(0, crossCount);
            RefreshAllLinesSharingSegmentsWith(existingOfColor);
            return true;
        }
        if (aIsLast)
        {
            int crossCount = GetTotalCrossCount(seq[lastIdx].transform.position, stationB.transform.position);
            if (_starTunnelStock < crossCount) { NotifyStarTunnelInsufficient(); return false; }
            _starTunnelStock -= crossCount;
            seq.Add(stationB);
            existingOfColor.segmentStarTunnelCosts.Add(crossCount);
            RefreshAllLinesSharingSegmentsWith(existingOfColor);
            return true;
        }
        if (bIsLast)
        {
            int crossCount = GetTotalCrossCount(seq[lastIdx].transform.position, stationA.transform.position);
            if (_starTunnelStock < crossCount) { NotifyStarTunnelInsufficient(); return false; }
            _starTunnelStock -= crossCount;
            seq.Add(stationA);
            existingOfColor.segmentStarTunnelCosts.Add(crossCount);
            RefreshAllLinesSharingSegmentsWith(existingOfColor);
            return true;
        }

        return false;
    }

    /// <summary>将新站插入到指定线段的指定进度处，成为线上站。会更新该线上飞船的 segment 与 progress。</summary>
    public void InsertStationIntoSegment(Line line, int segmentIndex, StationBehaviour newStation, float insertProgress)
    {
        if (line == null || newStation == null) return;
        var seq = line.stationSequence;
        if (seq == null || segmentIndex < 0 || segmentIndex + 1 >= seq.Count) return;
        insertProgress = Mathf.Clamp01(insertProgress);

        seq.Insert(segmentIndex + 1, newStation);

        if (line.segmentStarTunnelCosts != null && segmentIndex < line.segmentStarTunnelCosts.Count)
        {
            int cost = line.segmentStarTunnelCosts[segmentIndex];
            int first = cost / 2;
            int second = cost - first;
            line.segmentStarTunnelCosts[segmentIndex] = first;
            line.segmentStarTunnelCosts.Insert(segmentIndex + 1, second);
        }

        foreach (var ship in line.ships)
        {
            if (ship == null) continue;
            if (ship.currentSegmentIndex < segmentIndex) continue;
            if (ship.currentSegmentIndex > segmentIndex)
            {
                ship.currentSegmentIndex++;
                continue;
            }
            float t = ship.progressOnSegment;
            if (t <= insertProgress)
            {
                ship.progressOnSegment = insertProgress > 0.0001f ? t / insertProgress : 0f;
            }
            else
            {
                ship.currentSegmentIndex = segmentIndex + 1;
                float span = 1f - insertProgress;
                ship.progressOnSegment = span > 0.0001f ? (t - insertProgress) / span : 1f;
            }
        }

        RefreshAllLinesSharingSegmentsWith(line);
    }

    /// <summary>Inserts a station into the middle of the given segment (simplified: insert at 0.5 progress).</summary>
    public bool InsertStationIntoLine(Line line, int segmentIndex, StationBehaviour newStation)
    {
        if (line == null || newStation == null) return false;
        if (line.ContainsStation(newStation)) return false;
        if (!newStation.isUnlocked) return false;
        var seq = line.stationSequence;
        if (seq == null || segmentIndex < 0 || segmentIndex + 1 >= seq.Count) return false;

        InsertStationIntoSegment(line, segmentIndex, newStation, 0.5f);
        return true;
    }

    /// <summary>Gets the segment under the given world position. Returns (line, segmentIndex) or null. If endSegmentsOnly=true, only checks end segments (first or last).</summary>
    public (Line line, int segmentIndex)? GetSegmentUnderMouse(Vector2 worldPosition, float hitRadius, bool endSegmentsOnly = false)
    {
        var world2D = worldPosition;
        Line bestLine = null;
        int bestSegment = -1;
        float bestDist = float.MaxValue;

        foreach (var line in _lines)
        {
            var pts = GetRawVertexPositions(line);
            if (pts == null || pts.Count < 2) continue;

            int lastSegmentIdx = pts.Count - 2;

            for (int i = 0; i < pts.Count - 1; i++)
            {
                if (endSegmentsOnly && i != 0 && i != lastSegmentIdx) continue;

                float d = GetMinDistanceToSegment(pts, i, world2D);
                if (d < bestDist && d <= hitRadius)
                {
                    bestDist = d;
                    bestLine = line;
                    bestSegment = i;
                }
            }
        }

        if (bestLine != null && bestSegment >= 0)
            return (bestLine, bestSegment);
        return null;
    }

    private float GetMinDistanceToSegment(List<Vector3> pts, int segmentIndex, Vector2 worldPos)
    {
        if (pts == null || segmentIndex < 0 || segmentIndex + 1 >= pts.Count) return float.MaxValue;
        Vector3 p0 = segmentIndex > 0 ? pts[segmentIndex - 1] : pts[segmentIndex];
        Vector3 p1 = pts[segmentIndex];
        Vector3 p2 = pts[segmentIndex + 1];
        Vector3 p3 = segmentIndex + 2 < pts.Count ? pts[segmentIndex + 2] : pts[segmentIndex + 1];

        float minDist = float.MaxValue;
        const int samples = 16;
        for (int k = 0; k <= samples; k++)
        {
            float t = k / (float)samples;
            Vector3 pt = CatmullRom(p0, p1, p2, p3, t);
            float d = Vector2.Distance(new Vector2(pt.x, pt.y), worldPos);
            if (d < minDist) minDist = d;
        }
        return minDist;
    }

    /// <summary>Tries to remove an end segment. Returns true if successful.</summary>
    public bool TryRemoveEndSegment(Line line, int segmentIndex)
    {
        if (line == null || !line.IsEndSegment(segmentIndex)) return false;

        var seq = line.stationSequence;
        if (seq == null || seq.Count < 2) return false;

        bool wasLoop = line.IsLoop();

        if (wasLoop)
        {
            return TryRemoveLoopSegment(line, segmentIndex);
        }

        var endStation = line.GetEndStationOfSegment(segmentIndex);
        if (endStation == null) return false;

        StationBehaviour keepStation = null;
        int newSegmentIndex = 0;
        if (segmentIndex == 0)
        {
            keepStation = seq.Count > 1 ? seq[1] : null;
            newSegmentIndex = 0;
        }
        else
        {
            keepStation = seq.Count > 1 ? seq[seq.Count - 2] : null;
            newSegmentIndex = Mathf.Max(0, seq.Count - 3);
        }

        int costToReturn = 0;
        if (line.segmentStarTunnelCosts != null && segmentIndex >= 0 && segmentIndex < line.segmentStarTunnelCosts.Count)
        {
            costToReturn = line.segmentStarTunnelCosts[segmentIndex];
            _starTunnelStock += costToReturn;
        }

        for (int i = line.ships.Count - 1; i >= 0; i--)
        {
            var ship = line.ships[i];
            if (ship == null) continue;

            if (ship.currentSegmentIndex == segmentIndex)
            {
                if (keepStation != null && seq.Count >= 2)
                {
                    ship.transform.position = new Vector3(
                        keepStation.transform.position.x,
                        keepStation.transform.position.y,
                        0f
                    );
                    ship.currentSegmentIndex = newSegmentIndex;
                    ship.progressOnSegment = 0f;
                    ship.direction = 1;
                    ship.state = ShipBehaviour.ShipState.Moving;
                }
            }
            else if (ship.currentSegmentIndex > segmentIndex)
            {
                ship.currentSegmentIndex--;
                if (ship.currentSegmentIndex < 0) ship.currentSegmentIndex = 0;
            }
        }

        if (line.segmentStarTunnelCosts != null && line.segmentStarTunnelCosts.Count > 0 && segmentIndex < line.segmentStarTunnelCosts.Count)
            line.segmentStarTunnelCosts.RemoveAt(segmentIndex);

        if (segmentIndex == 0)
            seq.RemoveAt(0);
        else
            seq.RemoveAt(seq.Count - 1);

        if (seq.Count < 2)
        {
            RemoveLine(line);
        }
        else
        {
            RefreshAllLinesSharingSegmentsWith(line);
        }

        return true;
    }

    /// <summary>Removes a segment from a loop line. Rearranges the sequence so the break point becomes the new endpoints.</summary>
    private bool TryRemoveLoopSegment(Line line, int segmentIndex)
    {
        var seq = line.stationSequence;
        if (seq == null || seq.Count < 3) return false;
        if (segmentIndex < 0 || segmentIndex >= seq.Count - 1) return false;

        int costToReturn = 0;
        if (line.segmentStarTunnelCosts != null && segmentIndex < line.segmentStarTunnelCosts.Count)
        {
            costToReturn = line.segmentStarTunnelCosts[segmentIndex];
            _starTunnelStock += costToReturn;
            line.segmentStarTunnelCosts.RemoveAt(segmentIndex);
        }

        seq.RemoveAt(seq.Count - 1);

        var newSeq = new List<StationBehaviour>();
        for (int i = segmentIndex; i >= 0; i--)
        {
            newSeq.Add(seq[i]);
        }
        for (int i = seq.Count - 1; i > segmentIndex; i--)
        {
            newSeq.Add(seq[i]);
        }

        seq.Clear();
        seq.AddRange(newSeq);

        StationBehaviour keepStation = seq.Count > 0 ? seq[0] : null;

        for (int i = line.ships.Count - 1; i >= 0; i--)
        {
            var ship = line.ships[i];
            if (ship == null) continue;

            if (keepStation != null && seq.Count >= 2)
            {
                ship.transform.position = new Vector3(
                    keepStation.transform.position.x,
                    keepStation.transform.position.y,
                    0f
                );
                ship.currentSegmentIndex = 0;
                ship.progressOnSegment = 0f;
                ship.direction = 1;
                ship.state = ShipBehaviour.ShipState.Moving;
            }
        }

        if (seq.Count < 2)
        {
            RemoveLine(line);
        }
        else
        {
            RefreshAllLinesSharingSegmentsWith(line);
        }

        return true;
    }

    /// <summary>移除指定颜色的整条线路。成功返回 true。</summary>
    public bool TryRemoveLineByColor(LineColor color)
    {
        Line toRemove = null;
        foreach (var line in _lines)
        {
            if (line.color == color)
            {
                toRemove = line;
                break;
            }
        }
        if (toRemove == null) return false;
        RemoveLine(toRemove);
        return true;
    }

    /// <summary>Removes the entire line (when only one station remains). Destroys all ships and releases the visual.</summary>
    private void RemoveLine(Line line)
    {
        if (line == null) return;

        int shipCount = line.ships.Count;
        if (shipCount > 0)
            _shipStock += shipCount;

        if (line.segmentStarTunnelCosts != null && line.segmentStarTunnelCosts.Count > 0)
        {
            int totalCost = line.segmentStarTunnelCosts.Sum();
            _starTunnelStock += totalCost;
        }

        foreach (var ship in line.ships)
        {
            if (ship != null && ship.gameObject != null)
                Destroy(ship.gameObject);
        }
        line.ships.Clear();
        line.stationSequence.Clear();
        if (line.segmentStarTunnelCosts != null)
            line.segmentStarTunnelCosts.Clear();
        _lines.Remove(line);

        if (linesRoot == null)
            linesRoot = GameObject.Find("Map")?.transform?.Find("Lines") ?? GameObject.Find("Lines")?.transform;
        if (linesRoot != null)
        {
            string childName = "Line_视觉_" + line.id;
            Transform child = linesRoot.Find(childName);
            if (child != null)
                Destroy(child.gameObject);
        }
    }

    /// <summary>Sets segment highlight. pulse=true for editing state (continuous scaling), pulse=false for hover (static).</summary>
    public void SetSegmentHighlight(Line line, int segmentIndex, bool highlighted, bool pulse = false)
    {
        if (line == null) return;

        if (linesRoot == null)
            linesRoot = GameObject.Find("Map")?.transform?.Find("Lines") ?? GameObject.Find("Lines")?.transform;
        if (linesRoot == null) linesRoot = transform;

        string baseName = "Line_视觉_" + line.id;
        Transform lineTransform = linesRoot.Find(baseName);
        if (lineTransform == null) return;

        string highlightName = "SegmentHighlight";
        Transform highlightTransform = lineTransform.Find(highlightName);
        LineRenderer highlightLr;

        if (highlighted)
        {
            if (highlightTransform == null)
            {
                var go = new GameObject(highlightName);
                go.transform.SetParent(lineTransform, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localScale = Vector3.one;
                highlightLr = go.AddComponent<LineRenderer>();
                highlightLr.useWorldSpace = true;
                highlightLr.loop = false;
                highlightLr.alignment = LineAlignment.View;
                highlightLr.material = new Material(GetOrCreateLineMaterial());
                highlightLr.positionCount = 0;
                highlightLr.enabled = true;
            }
            else
            {
                highlightLr = highlightTransform.GetComponent<LineRenderer>();
            }

            if (highlightLr != null)
            {
                var pts = GetRawVertexPositions(line);
                if (pts != null && segmentIndex >= 0 && segmentIndex + 1 < pts.Count)
                {
                    Vector3 p0 = segmentIndex > 0 ? pts[segmentIndex - 1] : pts[segmentIndex];
                    Vector3 p1 = pts[segmentIndex];
                    Vector3 p2 = pts[segmentIndex + 1];
                    Vector3 p3 = segmentIndex + 2 < pts.Count ? pts[segmentIndex + 2] : pts[segmentIndex + 1];

                    var smoothPts = new List<Vector3>();
                    for (int k = 0; k < LineSmoothSubdivisions; k++)
                    {
                        float t = k / (float)LineSmoothSubdivisions;
                        var pos = CatmullRom(p0, p1, p2, p3, t);
                        pos.z = 0.5f;
                        smoothPts.Add(pos);
                    }
                    var last = CatmullRom(p0, p1, p2, p3, 1f);
                    last.z = 0.5f;
                    smoothPts.Add(last);

                    highlightLr.positionCount = smoothPts.Count;
                    for (int i = 0; i < smoothPts.Count; i++)
                        highlightLr.SetPosition(i, smoothPts[i]);

                    Color c = GetColorForLine(line.color);
                    float baseWidth = 0.15f;
                    float w = pulse ? baseWidth * (1f + 0.3f * Mathf.Sin(Time.time * 8f)) : baseWidth * 1.15f;
                    highlightLr.startWidth = highlightLr.endWidth = w;
                    Color brightColor = new Color(
                        Mathf.Min(1f, c.r * 1.5f),
                        Mathf.Min(1f, c.g * 1.5f),
                        Mathf.Min(1f, c.b * 1.5f),
                        1f
                    );
                    highlightLr.startColor = highlightLr.endColor = brightColor;
                    if (highlightLr.material != null)
                    {
                        if (highlightLr.material.HasProperty("_Color"))
                            highlightLr.material.SetColor("_Color", brightColor);
                        if (highlightLr.material.HasProperty("_BaseColor"))
                            highlightLr.material.SetColor("_BaseColor", brightColor);
                    }
                    highlightLr.gameObject.SetActive(true);
                }
            }
        }
        else
        {
            if (highlightTransform != null)
                highlightTransform.gameObject.SetActive(false);
        }
    }

    public void AddShipStock(int amount) { _shipStock += amount; }

    /// <summary>周奖励「新线路」：增加线路数量上限 1，最多 6 条。</summary>
    public void AddMaxLineCount()
    {
        if (_maxLineCount < 6) _maxLineCount++;
    }
    public void AddCarriageStock(int amount) { _carriageStock += amount; }
    public void AddStarTunnelStock(int amount) { _starTunnelStock += amount; }

    /// <summary>消耗一个客舱，升级指定飞船容量。成功返回 true。</summary>
    public bool TryUseCarriage(ShipBehaviour ship)
    {
        if (_carriageStock <= 0 || ship == null) return false;
        var balance = GameManager.Instance != null ? GameManager.Instance.gameBalance : null;
        int increment = balance != null ? balance.carriageCapacityIncrement : 2;
        ship.capacity += increment;
        _carriageStock--;
        return true;
    }

    /// <summary>
    /// 在指定航线上放置一艘飞船。consumeStock=true 时消耗 1 存量，不足返回 null；建线自带船传 false 不扣存量。
    /// </summary>
    public ShipBehaviour SpawnShip(Line line, bool consumeStock = true)
    {
        if (consumeStock && _shipStock <= 0) return null;
        if (line == null || line.stationSequence == null || line.stationSequence.Count < 2) return null;
        if (shipPrefab == null) return null;
        if (consumeStock) _shipStock--;
        // 仅当 parent 为场景对象时才传入，避免 "Cannot instantiate with a parent which is persistent"
        Transform root = shipsRoot != null ? shipsRoot : transform;
        bool parentInScene = root != null && root.gameObject.scene.IsValid();
        GameObject go = parentInScene
            ? UnityEngine.Object.Instantiate(shipPrefab, root)
            : UnityEngine.Object.Instantiate(shipPrefab);
        go.name = "Ship_" + (_nextShipId++);
        var ship = go.GetComponent<ShipBehaviour>();
        if (ship == null) ship = go.AddComponent<ShipBehaviour>();
        ship.id = go.name;
        ship.line = line;
        ship.currentSegmentIndex = 0;
        ship.direction = 1;
        ship.progressOnSegment = 0f;
        ship.state = ShipBehaviour.ShipState.Moving;
        ship.dockRemainingTime = 0f;
        var start = line.stationSequence[0].transform;
        go.transform.position = new Vector3(start.position.x, start.position.y, 0f);
        line.ships.Add(ship);
        ship.ApplyVisual();
        return ship;
    }

    private static bool HasAdjacent(List<StationBehaviour> seq, StationBehaviour a, StationBehaviour b)
    {
        for (int i = 0; i < seq.Count - 1; i++)
            if (seq[i] == a && seq[i + 1] == b || seq[i] == b && seq[i + 1] == a)
                return true;
        return false;
    }

    /// <summary>同一站点对之间多条线路时，平行偏移间距（世界单位）</summary>
    private const float ParallelLineSpacing = 0.2f;

    /// <summary>获取使用站点对 (a,b) 或 (b,a) 的所有线路，按创建顺序。</summary>
    private List<Line> GetLinesForSegment(StationBehaviour a, StationBehaviour b)
    {
        var list = new List<Line>();
        for (int i = 0; i < _lines.Count; i++)
        {
            if (HasAdjacent(_lines[i].stationSequence, a, b))
                list.Add(_lines[i]);
        }
        return list;
    }

    /// <summary>先架好的路线占中间(0)，其余依次为 1,-1,2,-2...</summary>
    private static int GetSegmentLaneIndex(int indexInSegmentList)
    {
        if (indexInSegmentList == 0) return 0;
        if (indexInSegmentList % 2 == 1) return (indexInSegmentList + 1) / 2;
        return -indexInSegmentList / 2;
    }

    /// <summary>用站点对统一顺序计算偏移，保证所有线路的“左/右”一致。</summary>
    private Vector3 GetSegmentOffset(Line line, StationBehaviour a, StationBehaviour b)
    {
        var list = GetLinesForSegment(a, b);
        int idx = list.IndexOf(line);
        if (idx < 0) return Vector3.zero;
        int lane = GetSegmentLaneIndex(idx);
        if (lane == 0) return Vector3.zero;
        // 站点对使用统一顺序，使垂直方向一致
        if (a.GetInstanceID() > b.GetInstanceID()) { var t = a; a = b; b = t; }
        Vector3 dir = b.transform.position - a.transform.position;
        dir.z = 0;
        if (dir.sqrMagnitude < 0.0001f) return Vector3.zero;
        dir.Normalize();
        Vector3 perp = new Vector3(-dir.y, dir.x, 0f);
        return perp * (lane * ParallelLineSpacing);
    }

    /// <summary>与 CreateOrUpdateLineRenderer 一致的顶点偏移（站点位置 + 平行偏移混合）。</summary>
    private Vector3 GetVertexOffset(Line line, int vertexIndex)
    {
        var seq = line.stationSequence;
        if (vertexIndex < 0 || vertexIndex >= seq.Count || seq[vertexIndex] == null) return Vector3.zero;
        Vector3 offsetLeft = vertexIndex > 0 && seq[vertexIndex - 1] != null
            ? GetSegmentOffset(line, seq[vertexIndex - 1], seq[vertexIndex])
            : Vector3.zero;
        Vector3 offsetRight = vertexIndex < seq.Count - 1 && seq[vertexIndex + 1] != null
            ? GetSegmentOffset(line, seq[vertexIndex], seq[vertexIndex + 1])
            : Vector3.zero;
        Vector3 blend = offsetLeft + offsetRight;
        if (offsetLeft.sqrMagnitude > 0.0001f && offsetRight.sqrMagnitude > 0.0001f)
            blend *= 0.5f;
        return blend;
    }

    private const int LineSmoothSubdivisions = 8;

    /// <summary>Catmull-Rom 样条插值：在 P1 与 P2 之间采样，t∈[0,1]。</summary>
    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t, t3 = t2 * t;
        return 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    /// <summary>获取线上某段某进度处的切向（单位向量，供飞船朝向）。</summary>
    public Vector3 GetTangentOnLine(Line line, int segmentIndex, float progressOnSegment)
    {
        if (line == null || line.stationSequence == null) return Vector3.right;
        var seq = line.stationSequence;
        if (segmentIndex < 0 || segmentIndex + 1 >= seq.Count || seq[segmentIndex] == null || seq[segmentIndex + 1] == null)
            return Vector3.right;
        var pts = GetRawVertexPositions(line);
        if (pts == null || pts.Count < 2) return Vector3.right;
        int i = segmentIndex;
        Vector3 p0 = i > 0 ? pts[i - 1] : pts[i];
        Vector3 p1 = pts[i];
        Vector3 p2 = pts[i + 1];
        Vector3 p3 = i + 2 < pts.Count ? pts[i + 2] : pts[i + 1];
        float t = Mathf.Clamp01(progressOnSegment);
        Vector3 tangent = CatmullRomDerivative(p0, p1, p2, p3, t);
        tangent.z = 0f;
        if (tangent.sqrMagnitude < 0.0001f) return Vector3.right;
        return tangent.normalized;
    }

    private static Vector3 CatmullRomDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        return 0.5f * ((-p0 + p2) + 2f * (2f * p0 - 5f * p1 + 4f * p2 - p3) * t + 3f * (-p0 + 3f * p1 - 3f * p2 + p3) * t2);
    }

    /// <summary>获取线上某段某进度的世界坐标（圆滑曲线），供飞船贴线移动。</summary>
    public Vector3 GetPositionOnLine(Line line, int segmentIndex, float progressOnSegment)
    {
        if (line == null || line.stationSequence == null) return Vector3.zero;
        var seq = line.stationSequence;
        if (segmentIndex < 0 || segmentIndex + 1 >= seq.Count || seq[segmentIndex] == null || seq[segmentIndex + 1] == null)
            return seq.Count > 0 && seq[0] != null ? seq[0].transform.position : Vector3.zero;
        var pts = GetRawVertexPositions(line);
        if (pts == null || pts.Count < 2) return Vector3.zero;
        int i = segmentIndex;
        Vector3 p0 = i > 0 ? pts[i - 1] : pts[i];
        Vector3 p1 = pts[i];
        Vector3 p2 = pts[i + 1];
        Vector3 p3 = i + 2 < pts.Count ? pts[i + 2] : pts[i + 1];
        return CatmullRom(p0, p1, p2, p3, Mathf.Clamp01(progressOnSegment));
    }

    private List<Vector3> GetRawVertexPositions(Line line)
    {
        var seq = line.stationSequence;
        if (seq == null || seq.Count < 2) return null;
        var pts = new List<Vector3>();
        for (int i = 0; i < seq.Count; i++)
        {
            if (seq[i] == null) continue;
            Vector3 pos = new Vector3(seq[i].transform.position.x, seq[i].transform.position.y, 0f);
            pos += GetVertexOffset(line, i);
            pts.Add(pos);
        }
        return pts.Count >= 2 ? pts : null;
    }

    /// <summary>刷新与指定线共用任意站段的所有线路的视觉（含自身），保证平行偏移正确。</summary>
    private void RefreshAllLinesSharingSegmentsWith(Line line)
    {
        var toRefresh = new HashSet<Line> { line };
        var seq = line.stationSequence;
        for (int i = 0; i < seq.Count - 1; i++)
        {
            if (seq[i] == null || seq[i + 1] == null) continue;
            var list = GetLinesForSegment(seq[i], seq[i + 1]);
            for (int j = 0; j < list.Count; j++)
                toRefresh.Add(list[j]);
        }
        foreach (var l in toRefresh)
            CreateOrUpdateLineRenderer(l);
    }

    private void CreateOrUpdateLineRenderer(Line line)
    {
        if (linesRoot == null)
            linesRoot = GameObject.Find("Map")?.transform?.Find("Lines") ?? GameObject.Find("Lines")?.transform;
        if (linesRoot == null)
            linesRoot = transform;

        string childName = "Line_视觉_" + line.id;
        Transform child = linesRoot.Find(childName);
        GameObject go;
        LineRenderer lr;
        if (child != null)
        {
            go = child.gameObject;
            lr = go.GetComponent<LineRenderer>();
        }
        else
        {
            go = new GameObject(childName);
            go.transform.SetParent(linesRoot, false);
            go.transform.localScale = Vector3.one;
            lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.loop = false;
            lr.startWidth = lr.endWidth = 0.1f;
            lr.alignment = LineAlignment.View;
            lr.material = new Material(GetOrCreateLineMaterial());
            lr.positionCount = 0;
            lr.enabled = true;
        }

        if (lr == null) return;
        Material sharedMat = GetOrCreateLineMaterial();
        if (sharedMat == null) return;
        if (lr.material == null || lr.material.shader == null || !lr.material.shader.isSupported)
            lr.material = new Material(sharedMat);
        if (lr.sharedMaterial == sharedMat)
            lr.material = new Material(sharedMat);
        lr.alignment = LineAlignment.View;
        lr.startWidth = lr.endWidth = 0.1f;
        lr.numCapVertices = 4;
        lr.numCornerVertices = 4;
        lr.enabled = true;
        lr.gameObject.SetActive(true);
        var seq = line.stationSequence;
        int validCount = 0;
        for (int i = 0; i < seq.Count; i++)
        {
            if (seq[i] != null) validCount++;
        }
        if (validCount == 0) return;
        var pts = GetRawVertexPositions(line);
        if (pts == null || pts.Count < 2) return;
        Color c = GetColorForLine(line.color);
        line.displayColor = c;

        bool hasCrossing = line.segmentStarTunnelCosts != null && line.segmentStarTunnelCosts.Count > 0;
        var solidRuns = new List<List<Vector3>>();
        var currentRun = new List<Vector3>();

        for (int i = 0; i < pts.Count - 1; i++)
        {
            bool isCrossing = hasCrossing && i < line.segmentStarTunnelCosts.Count && line.segmentStarTunnelCosts[i] > 0;
            Vector3 p0 = i > 0 ? pts[i - 1] : pts[i];
            Vector3 p1 = pts[i];
            Vector3 p2 = pts[i + 1];
            Vector3 p3 = i + 2 < pts.Count ? pts[i + 2] : pts[i + 1];

            if (isCrossing)
            {
                if (currentRun.Count > 0)
                {
                    var last = pts[i];
                    last.z = 1f;
                    currentRun.Add(last);
                    solidRuns.Add(currentRun);
                    currentRun = new List<Vector3>();
                }
                continue;
            }

            if (currentRun.Count == 0)
            {
                var first = pts[i];
                first.z = 1f;
                currentRun.Add(first);
            }
            for (int k = 0; k < LineSmoothSubdivisions; k++)
            {
                float t = k / (float)LineSmoothSubdivisions;
                var pos = CatmullRom(p0, p1, p2, p3, t);
                pos.z = 1f;
                currentRun.Add(pos);
            }
        }
        if (currentRun.Count > 0)
        {
            var last = pts[pts.Count - 1];
            last.z = 1f;
            currentRun.Add(last);
            solidRuns.Add(currentRun);
        }

        Transform solidRoot = go.transform.Find("SolidRuns");
        if (solidRoot == null)
        {
            var solidGo = new GameObject("SolidRuns");
            solidGo.transform.SetParent(go.transform, false);
            solidRoot = solidGo.transform;
        }
        while (solidRoot.childCount > solidRuns.Count)
        {
            var toDestroy = solidRoot.GetChild(solidRoot.childCount - 1);
            toDestroy.SetParent(null);
            Destroy(toDestroy.gameObject);
        }
        for (int runIdx = 0; runIdx < solidRuns.Count; runIdx++)
        {
            var runPts = solidRuns[runIdx];
            Transform runT = runIdx < solidRoot.childCount ? solidRoot.GetChild(runIdx) : null;
            if (runT == null)
            {
                var runGo = new GameObject("Run_" + runIdx);
                runGo.transform.SetParent(solidRoot, false);
                runT = runGo.transform;
                var newLr = runGo.AddComponent<LineRenderer>();
                newLr.useWorldSpace = true;
                newLr.loop = false;
                newLr.startWidth = newLr.endWidth = 0.1f;
                newLr.alignment = LineAlignment.View;
                newLr.material = new Material(GetOrCreateLineMaterial());
                newLr.numCapVertices = 4;
                newLr.numCornerVertices = 4;
            }
            var runLr = runT.GetComponent<LineRenderer>();
            if (runLr != null)
            {
                runLr.positionCount = runPts.Count;
                for (int j = 0; j < runPts.Count; j++)
                    runLr.SetPosition(j, runPts[j]);
                runLr.startColor = runLr.endColor = c;
                if (runLr.material != null)
                {
                    if (runLr.material.HasProperty("_Color")) runLr.material.SetColor("_Color", c);
                    if (runLr.material.HasProperty("_BaseColor")) runLr.material.SetColor("_BaseColor", c);
                }
                runLr.gameObject.SetActive(true);
            }
        }

        if (solidRuns.Count == 0)
        {
            lr.positionCount = 0;
            lr.enabled = false;
        }
        else
        {
            lr.positionCount = 0;
            lr.enabled = false;
        }

        UpdateDashedSegments(go, line, pts, c);
    }

    private const float DashLength = 0.1f;
    private const float GapLength = 0.08f;

    private void UpdateDashedSegments(GameObject lineRoot, Line line, List<Vector3> pts, Color c)
    {
        Transform dashedRoot = lineRoot.transform.Find("DashedSegments");
        if (line.segmentStarTunnelCosts == null || line.segmentStarTunnelCosts.Count == 0)
        {
            if (dashedRoot != null)
            {
                while (dashedRoot.childCount > 0)
                {
                    var child = dashedRoot.GetChild(0);
                    child.SetParent(null);
                    Destroy(child.gameObject);
                }
            }
            return;
        }

        if (dashedRoot == null)
        {
            var go = new GameObject("DashedSegments");
            go.transform.SetParent(lineRoot.transform, false);
            dashedRoot = go.transform;
        }

        var allDashes = new List<(Vector3 start, Vector3 end)>();
        for (int i = 0; i < pts.Count - 1 && i < line.segmentStarTunnelCosts.Count; i++)
        {
            if (line.segmentStarTunnelCosts[i] <= 0) continue;

            Vector3 p0 = i > 0 ? pts[i - 1] : pts[i];
            Vector3 p1 = pts[i];
            Vector3 p2 = pts[i + 1];
            Vector3 p3 = i + 2 < pts.Count ? pts[i + 2] : pts[i + 1];

            float arcLen = 0f;
            Vector3 prev = CatmullRom(p0, p1, p2, p3, 0f);
            Vector3 dashStart = prev;
            bool inDash = true;
            const int samples = 64;
            for (int k = 1; k <= samples; k++)
            {
                float t = k / (float)samples;
                Vector3 curr = CatmullRom(p0, p1, p2, p3, t);
                arcLen += Vector3.Distance(prev, curr);
                float phase = arcLen % (DashLength + GapLength);
                bool nowInDash = phase < DashLength;
                if (inDash && !nowInDash)
                {
                    allDashes.Add((dashStart, prev));
                    inDash = false;
                }
                else if (!inDash && nowInDash)
                {
                    dashStart = curr;
                    inDash = true;
                }
                prev = curr;
            }
            if (inDash)
                allDashes.Add((dashStart, prev));
        }

        while (dashedRoot.childCount > allDashes.Count)
        {
            var toDestroy = dashedRoot.GetChild(dashedRoot.childCount - 1);
            toDestroy.SetParent(null);
            Destroy(toDestroy.gameObject);
        }

        for (int idx = 0; idx < allDashes.Count; idx++)
        {
            var (start, end) = allDashes[idx];
            Transform segT = idx < dashedRoot.childCount ? dashedRoot.GetChild(idx) : null;
            if (segT == null)
            {
                var go = new GameObject("Dash_" + idx);
                go.transform.SetParent(dashedRoot, false);
                segT = go.transform;
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.loop = false;
                lr.startWidth = lr.endWidth = 0.1f;
                lr.alignment = LineAlignment.View;
                lr.material = new Material(GetOrCreateLineMaterial());
                lr.numCapVertices = 2;
                lr.numCornerVertices = 2;
            }
            var segLr = segT.GetComponent<LineRenderer>();
            if (segLr != null)
            {
                segLr.positionCount = 2;
                Vector3 s = new Vector3(start.x, start.y, 0.99f);
                Vector3 e = new Vector3(end.x, end.y, 0.99f);
                segLr.SetPosition(0, s);
                segLr.SetPosition(1, e);
                segLr.startColor = segLr.endColor = c;
                if (segLr.material != null)
                {
                    if (segLr.material.HasProperty("_Color")) segLr.material.SetColor("_Color", c);
                    if (segLr.material.HasProperty("_BaseColor")) segLr.material.SetColor("_BaseColor", c);
                }
                segLr.gameObject.SetActive(true);
            }
        }
    }

    private static Material GetOrCreateLineMaterial()
    {
        if (_cachedLineMaterial != null) return _cachedLineMaterial;
        // 优先使用适合 LineRenderer 的 Shader（Sprites/Default 对线段常不显示）
        string[] shaderNames = { "Unlit/Color", "Legacy Shaders/Particles/Alpha Blended", "Particles/Standard Unlit", "Universal Render Pipeline/Unlit", "Sprites/Default", "Standard" };
        foreach (string name in shaderNames)
        {
            Shader s = Shader.Find(name);
            if (s != null && s.isSupported)
            {
                _cachedLineMaterial = new Material(s);
                _cachedLineMaterial.renderQueue = 2000;
                return _cachedLineMaterial;
            }
        }
        foreach (string name in shaderNames)
        {
            Shader s = Shader.Find(name);
            if (s != null)
            {
                _cachedLineMaterial = new Material(s);
                _cachedLineMaterial.renderQueue = 2000;
                return _cachedLineMaterial;
            }
        }
        Shader fallback = Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
        if (fallback != null)
        {
            _cachedLineMaterial = new Material(fallback);
            _cachedLineMaterial.renderQueue = 2000;
        }
        return _cachedLineMaterial;
    }

    private static bool IsNearWhite(Color c)
    {
        return c.r >= 0.9f && c.g >= 0.9f && c.b >= 0.9f;
    }

    private Color GetColorForLine(LineColor lineColor)
    {
        if (visualConfig == null && GameManager.Instance != null)
            visualConfig = GameManager.Instance.visualConfig;
        if (visualConfig != null && visualConfig.lineColors != null && visualConfig.lineColors.Length > 0)
        {
            int idx = (int)lineColor;
            if (idx >= 0 && idx < visualConfig.lineColors.Length)
            {
                Color fromConfig = visualConfig.lineColors[idx];
                if (!IsNearWhite(fromConfig))
                    return fromConfig;
            }
        }
        return lineColor switch
        {
            LineColor.Red => Color.red,
            LineColor.Green => Color.green,
            LineColor.Blue => Color.blue,
            LineColor.Yellow => Color.yellow,
            LineColor.Cyan => Color.cyan,
            LineColor.Magenta => Color.magenta,
            _ => Color.white
        };
    }
}

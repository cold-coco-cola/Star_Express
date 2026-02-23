using System.Collections.Generic;
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
    [SerializeField] private int _starTunnelStock = 0;

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
            var line = new Line("Line_" + (_nextLineId++), color);
            line.stationSequence.Add(stationA);
            line.stationSequence.Add(stationB);
            _lines.Add(line);
            RefreshAllLinesSharingSegmentsWith(line);
            SpawnShip(line, true);
            return true;
        }

        var seq = existingOfColor.stationSequence;
        if (seq.Count < 2) return false;
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

        if (aIsFirst) { seq.Insert(0, stationB); RefreshAllLinesSharingSegmentsWith(existingOfColor); return true; }
        if (bIsFirst) { seq.Insert(0, stationA); RefreshAllLinesSharingSegmentsWith(existingOfColor); return true; }
        if (aIsLast) { seq.Add(stationB); RefreshAllLinesSharingSegmentsWith(existingOfColor); return true; }
        if (bIsLast) { seq.Add(stationA); RefreshAllLinesSharingSegmentsWith(existingOfColor); return true; }

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
        var smoothPts = new List<Vector3>();
        for (int i = 0; i < pts.Count - 1; i++)
        {
            Vector3 p0 = i > 0 ? pts[i - 1] : pts[i];
            Vector3 p1 = pts[i];
            Vector3 p2 = pts[i + 1];
            Vector3 p3 = i + 2 < pts.Count ? pts[i + 2] : pts[i + 1];
            for (int k = 0; k < LineSmoothSubdivisions; k++)
            {
                float t = k / (float)LineSmoothSubdivisions;
                var pos = CatmullRom(p0, p1, p2, p3, t);
                pos.z = 1f;
                smoothPts.Add(pos);
            }
        }
        var last = pts[pts.Count - 1];
        last.z = 1f;
        smoothPts.Add(last);
        lr.positionCount = smoothPts.Count;
        for (int i = 0; i < smoothPts.Count; i++)
            lr.SetPosition(i, smoothPts[i]);
        Color c = GetColorForLine(line.color);
        line.displayColor = c;
        lr.startColor = lr.endColor = c;
        if (lr.material != null)
        {
            if (lr.material.HasProperty("_Color"))
                lr.material.SetColor("_Color", c);
            if (lr.material.HasProperty("_BaseColor"))
                lr.material.SetColor("_BaseColor", c);
            lr.material.color = c;
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

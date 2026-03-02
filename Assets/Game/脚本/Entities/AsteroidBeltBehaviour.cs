using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 陨石带组件。挂载在陨石带游戏对象上，从 Path 子对象的 LineRenderer 读取路径，检测线段穿越次数。
/// 支持自动生成陨石分布。
/// </summary>
public class AsteroidBeltBehaviour : MonoBehaviour
{
    [Header("路径定义")]
    [Tooltip("指向 Path 子对象，留空则自动查找")]
    public Transform pathContainer;

    [Tooltip("带宽（世界单位），将折线段扩展为有向矩形进行碰撞检测")]
    public float width = 1.0f;

    [Header("穿越设置")]
    [Tooltip("每次穿越消耗的星燧数量")]
    public int crossCost = 1;

    [Header("陨石生成")]
    [Tooltip("陨石容器，留空则自动查找/创建")]
    public Transform asteroidsContainer;

    [Tooltip("陨石 Sprite 列表，随机选择")]
    public Sprite[] asteroidSprites;

    [Tooltip("陨石 scale 范围，32px/100ppu 的 Sprite 约 0.35~1.0 可得 0.1~0.3 世界单位")]
    public Vector2 asteroidSizeRange = new Vector2(0.35f, 1f);

    [Tooltip("陨石间距（世界单位）")]
    public float asteroidSpacing = 0.35f;

    [Tooltip("生成时的随机种子（0=每次随机）")]
    public int randomSeed = 0;

    [Tooltip("排序层级")]
    public int sortingOrder = -5;

    [Header("调试")]
    public bool showGizmos = true;

    private LineRenderer _pathLineRenderer;
    private List<Vector2> _cachedPathPoints;

    private void Awake()
    {
        if (pathContainer == null)
            pathContainer = transform.Find("Path");
        if (pathContainer != null)
            _pathLineRenderer = pathContainer.GetComponent<LineRenderer>();
    }

    private void Start()
    {
        GenerateAsteroidsIfEmpty();
    }

    private void GenerateAsteroidsIfEmpty()
    {
        if (asteroidsContainer == null)
        {
            asteroidsContainer = transform.Find("Asteroids");
            if (asteroidsContainer == null)
            {
                var go = new GameObject("Asteroids");
                go.transform.SetParent(transform);
                go.transform.localPosition = Vector3.zero;
                asteroidsContainer = go.transform;
            }
        }

        if (asteroidsContainer.childCount > 0)
            return;

#if UNITY_EDITOR
        GenerateAsteroids();
#else
        GenerateAsteroidsRuntime();
#endif
    }

#if !UNITY_EDITOR
    private void GenerateAsteroidsRuntime()
    {
        if (asteroidSprites == null || asteroidSprites.Length == 0)
        {
            Debug.LogWarning("[AsteroidBelt] 请先设置 asteroidSprites");
            return;
        }

        var pathPoints = GetPathPoints();
        if (pathPoints == null || pathPoints.Count < 2)
        {
            Debug.LogWarning("[AsteroidBelt] 路径点不足");
            return;
        }

        var rng = randomSeed == 0 ? new System.Random() : new System.Random(randomSeed);
        int asteroidCount = 0;

        for (int segIdx = 0; segIdx < pathPoints.Count - 1; segIdx++)
        {
            Vector2 p1 = pathPoints[segIdx];
            Vector2 p2 = pathPoints[segIdx + 1];
            Vector2 dir = p2 - p1;
            float segLen = dir.magnitude;
            if (segLen < 0.01f) continue;
            dir /= segLen;
            Vector2 perp = new Vector2(-dir.y, dir.x);

            int countAlongSeg = Mathf.CeilToInt(segLen / asteroidSpacing);
            int countAcrossWidth = Mathf.CeilToInt(width / asteroidSpacing);

            for (int i = 0; i <= countAlongSeg; i++)
            {
                float t = countAlongSeg > 0 ? (float)i / countAlongSeg : 0.5f;
                Vector2 center = Vector2.Lerp(p1, p2, t);

                for (int j = 0; j < countAcrossWidth; j++)
                {
                    float offset = countAcrossWidth > 1 ? (float)j / (countAcrossWidth - 1) - 0.5f : 0f;
                    offset *= width * 0.9f;

                    Vector2 pos = center + perp * offset + new Vector2(
                        (float)(rng.NextDouble() - 0.5) * asteroidSpacing * 0.5f,
                        (float)(rng.NextDouble() - 0.5) * asteroidSpacing * 0.5f
                    );

                    CreateAsteroidRuntime(pos, rng);
                    asteroidCount++;
                }
            }
        }

        Debug.Log($"[AsteroidBelt] 生成了 {asteroidCount} 个陨石");
    }

    private void CreateAsteroidRuntime(Vector2 pos, System.Random rng)
    {
        var go = new GameObject("Asteroid");
        go.transform.SetParent(asteroidsContainer);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);

        float size = Mathf.Lerp(asteroidSizeRange.x, asteroidSizeRange.y, (float)rng.NextDouble());
        go.transform.localScale = new Vector3(size, size, 1f);
        go.transform.rotation = Quaternion.Euler(0, 0, (float)(rng.NextDouble() * 360));

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = asteroidSprites[rng.Next(asteroidSprites.Length)];
        sr.sortingOrder = sortingOrder;
    }
#endif

    /// <summary>检测线段 AB 是否与陨石带有任何交集。</summary>
    public bool HasCrossing(Vector2 a, Vector2 b)
    {
        var pathPoints = GetPathPoints();
        if (pathPoints == null || pathPoints.Count < 2) return false;

        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            Vector2 p1 = pathPoints[i];
            Vector2 p2 = pathPoints[i + 1];
            if (LineSegmentIntersectsBeltSegment(a, b, p1, p2))
                return true;
        }
        return false;
    }

#if UNITY_EDITOR
    /// <summary>自动生成陨石分布（编辑器调用）</summary>
    [ContextMenu("生成陨石")]
    public void GenerateAsteroids()
    {
        if (asteroidsContainer == null)
        {
            asteroidsContainer = transform.Find("Asteroids");
            if (asteroidsContainer == null)
            {
                var go = new GameObject("Asteroids");
                go.transform.SetParent(transform);
                go.transform.localPosition = Vector3.zero;
                asteroidsContainer = go.transform;
            }
        }

        ClearAsteroids();

        if (asteroidSprites == null || asteroidSprites.Length == 0)
        {
            Debug.LogWarning("[AsteroidBelt] 请先设置 asteroidSprites");
            return;
        }

        var pathPoints = GetPathPoints();
        if (pathPoints == null || pathPoints.Count < 2)
        {
            Debug.LogWarning("[AsteroidBelt] 路径点不足");
            return;
        }

        var rng = randomSeed == 0 ? new System.Random() : new System.Random(randomSeed);
        int asteroidCount = 0;

        for (int segIdx = 0; segIdx < pathPoints.Count - 1; segIdx++)
        {
            Vector2 p1 = pathPoints[segIdx];
            Vector2 p2 = pathPoints[segIdx + 1];
            Vector2 dir = p2 - p1;
            float segLen = dir.magnitude;
            if (segLen < 0.01f) continue;
            dir /= segLen;
            Vector2 perp = new Vector2(-dir.y, dir.x);

            int countAlongSeg = Mathf.CeilToInt(segLen / asteroidSpacing);
            int countAcrossWidth = Mathf.CeilToInt(width / asteroidSpacing);

            for (int i = 0; i <= countAlongSeg; i++)
            {
                float t = countAlongSeg > 0 ? (float)i / countAlongSeg : 0.5f;
                Vector2 center = Vector2.Lerp(p1, p2, t);

                for (int j = 0; j < countAcrossWidth; j++)
                {
                    float offset = countAcrossWidth > 1 ? (float)j / (countAcrossWidth - 1) - 0.5f : 0f;
                    offset *= width * 0.9f;

                    Vector2 pos = center + perp * offset + new Vector2(
                        (float)(rng.NextDouble() - 0.5) * asteroidSpacing * 0.5f,
                        (float)(rng.NextDouble() - 0.5) * asteroidSpacing * 0.5f
                    );

                    CreateAsteroid(pos, rng);
                    asteroidCount++;
                }
            }
        }

        Debug.Log($"[AsteroidBelt] 生成了 {asteroidCount} 个陨石");
    }

    [ContextMenu("清除陨石")]
    public void ClearAsteroids()
    {
        if (asteroidsContainer == null) return;
        var children = new List<GameObject>();
        foreach (Transform child in asteroidsContainer)
            children.Add(child.gameObject);
        foreach (var child in children)
            DestroyImmediate(child);
    }

    private void CreateAsteroid(Vector2 pos, System.Random rng)
    {
        var go = new GameObject("Asteroid");
        go.transform.SetParent(asteroidsContainer);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);

        float size = Mathf.Lerp(asteroidSizeRange.x, asteroidSizeRange.y, (float)rng.NextDouble());
        go.transform.localScale = new Vector3(size, size, 1f);
        go.transform.rotation = Quaternion.Euler(0, 0, (float)(rng.NextDouble() * 360));

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = asteroidSprites[rng.Next(asteroidSprites.Length)];
        sr.sortingOrder = sortingOrder;
    }
#endif

    private List<Vector2> GetPathPoints()
    {
        if (_pathLineRenderer == null)
        {
            if (pathContainer == null)
                pathContainer = transform.Find("Path");
            if (pathContainer != null)
                _pathLineRenderer = pathContainer.GetComponent<LineRenderer>();
        }
        if (_pathLineRenderer == null) return null;

        int posCount = _pathLineRenderer.positionCount;
        if (posCount < 2) return null;

        if (_cachedPathPoints == null) _cachedPathPoints = new List<Vector2>();
        _cachedPathPoints.Clear();

        Transform space = pathContainer != null ? pathContainer : transform;
        for (int i = 0; i < posCount; i++)
        {
            Vector3 pos = _pathLineRenderer.GetPosition(i);
            if (!_pathLineRenderer.useWorldSpace)
                pos = space.TransformPoint(pos);
            _cachedPathPoints.Add(new Vector2(pos.x, pos.y));
        }
        return _cachedPathPoints;
    }

    /// <summary>检测点 p 是否在本陨石带区域内。margin>0 时扩展检测范围，用于站点生成避让（站点有半径）。</summary>
    public bool IsPointInsideBelt(Vector2 p, float margin = 0f)
    {
        var pathPoints = GetPathPoints();
        if (pathPoints == null || pathPoints.Count < 2) return false;
        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            Vector2[] corners = GetBeltSegmentCorners(pathPoints[i], pathPoints[i + 1], margin);
            if (corners != null && corners.Length >= 4 && PointInPolygon(p, corners))
                return true;
        }
        return false;
    }

    /// <summary>获取陨石带中心点（路径点平均），用于站点生成避让。</summary>
    public Vector2? GetBeltCenter()
    {
        var pathPoints = GetPathPoints();
        if (pathPoints == null || pathPoints.Count < 2) return null;
        Vector2 sum = Vector2.zero;
        foreach (var pt in pathPoints) sum += pt;
        return sum / pathPoints.Count;
    }

    /// <summary>检测线段 AB 是否与陨石带段 p1-p2（扩展为带宽矩形）相交。</summary>
    private bool LineSegmentIntersectsBeltSegment(Vector2 a, Vector2 b, Vector2 p1, Vector2 p2)
    {
        Vector2[] corners = GetBeltSegmentCorners(p1, p2);
        if (corners == null || corners.Length < 4) return false;

        if (PointInPolygon(a, corners)) return true;
        if (PointInPolygon(b, corners)) return true;
        for (int i = 0; i < 4; i++)
        {
            Vector2 c1 = corners[i];
            Vector2 c2 = corners[(i + 1) % 4];
            if (SegmentsIntersect(a, b, c1, c2)) return true;
        }
        return false;
    }

    /// <summary>将陨石带段 p1-p2 扩展为有向矩形的四个角点。margin>0 时扩展带宽，用于避让检测。</summary>
    private Vector2[] GetBeltSegmentCorners(Vector2 p1, Vector2 p2, float margin = 0f)
    {
        Vector2 dir = p2 - p1;
        float len = dir.magnitude;
        if (len < 0.0001f) return null;

        dir /= len;
        Vector2 perp = new Vector2(-dir.y, dir.x);
        float halfWidth = width * 0.5f + margin;

        return new[]
        {
            p1 + perp * halfWidth,
            p1 - perp * halfWidth,
            p2 - perp * halfWidth,
            p2 + perp * halfWidth
        };
    }

    private static bool PointInPolygon(Vector2 p, Vector2[] polygon)
    {
        if (polygon == null || polygon.Length < 3) return false;
        int n = polygon.Length;
        bool inside = false;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            if (((polygon[i].y > p.y) != (polygon[j].y > p.y)) &&
                (p.x < (polygon[j].x - polygon[i].x) * (p.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x))
                inside = !inside;
        }
        return inside;
    }

    private static bool SegmentsIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
    {
        float d1 = Cross2(b1 - a1, a2 - a1);
        float d2 = Cross2(b2 - a1, a2 - a1);
        if (d1 * d2 > 0) return false;
        float d3 = Cross2(a1 - b1, b2 - b1);
        float d4 = Cross2(a2 - b1, b2 - b1);
        if (d3 * d4 > 0) return false;
        if (Mathf.Abs(d1) < 1e-6f && Mathf.Abs(d2) < 1e-6f && Mathf.Abs(d3) < 1e-6f && Mathf.Abs(d4) < 1e-6f)
            return SegmentsOverlap(a1, a2, b1, b2);
        return true;
    }

    private static float Cross2(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

    private static bool SegmentsOverlap(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
    {
        Vector2 dir = a2 - a1;
        float len = dir.magnitude;
        if (len < 1e-6f) return Vector2.Distance(a1, b1) < 1e-6f || Vector2.Distance(a1, b2) < 1e-6f;
        dir /= len;
        float t1 = Vector2.Dot(b1 - a1, dir);
        float t2 = Vector2.Dot(b2 - a1, dir);
        float tMin = Mathf.Min(t1, t2);
        float tMax = Mathf.Max(t1, t2);
        return tMin <= len + 1e-6f && tMax >= -1e-6f;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        if (_pathLineRenderer == null && pathContainer == null)
            pathContainer = transform.Find("Path");
        if (_pathLineRenderer == null && pathContainer != null)
            _pathLineRenderer = pathContainer.GetComponent<LineRenderer>();
        var pathPoints = GetPathPoints();
        if (pathPoints == null || pathPoints.Count < 2) return;

        Gizmos.color = new Color(0.5f, 0.3f, 0.2f, 0.4f);
        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            Vector2[] corners = GetBeltSegmentCorners(pathPoints[i], pathPoints[i + 1]);
            if (corners == null || corners.Length < 4) continue;
            for (int j = 0; j < 4; j++)
            {
                Vector3 p1 = new Vector3(corners[j].x, corners[j].y, 0f);
                Vector3 p2 = new Vector3(corners[(j + 1) % 4].x, corners[(j + 1) % 4].y, 0f);
                Gizmos.DrawLine(p1, p2);
            }
        }
    }
#endif
}

using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 点击两站建线/延伸：第一击选 A 并高亮，第二击选 B 弹选色；选色后调用 LineManager，取消则清除高亮。
/// 选色面板 ColorPickPanel 应已放在场景 Hierarchy 中。
/// </summary>
public class LineDrawingInput : MonoBehaviour
{
    [HideInInspector] public ILineManager lineManager;

    [Header("调试")]
    [SerializeField] private bool debugLog = false;

    private enum State { Idle, FirstSelected }
    private State _state = State.Idle;
    private StationBehaviour _selectedA;

    private ColorPickPanel _colorPickPanel;

    private void Awake()
    {
        TryResolveReferences();
    }

    private void Start()
    {
        TryResolveReferences();
    }

    private void TryResolveReferences()
    {
        if (lineManager == null && GameManager.Instance != null)
            lineManager = GameManager.Instance.GetLineManager();
        if (lineManager == null && GameManager.Instance != null && GameManager.Instance.gameObject == gameObject)
        {
            foreach (var mb in GetComponents<MonoBehaviour>())
            {
                if (mb != null && mb is ILineManager lm) { lineManager = lm; break; }
            }
        }
        if (lineManager == null)
        {
            foreach (var mb in FindObjectsOfType<MonoBehaviour>())
            {
                if (mb != null && mb is ILineManager lm) { lineManager = lm; break; }
            }
        }

        if (_colorPickPanel == null)
            _colorPickPanel = UIManager.Get<ColorPickPanel>();
        if (_colorPickPanel == null)
            _colorPickPanel = FindObjectOfType<ColorPickPanel>(true);
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        TryResolveReferences();
        var station = GetStationUnderMouse();
        if (debugLog)
            Debug.Log("[连线输入] 点击: overUI=" + overUI + " state=" + _state + " station=" + (station != null ? station.displayName : "null"));

        if (overUI)
            return;

        if (_state == State.Idle)
        {
            if (station != null && station.isUnlocked)
            {
                _selectedA = station;
                SetHighlight(_selectedA, true);
                _state = State.FirstSelected;
                if (debugLog) Debug.Log("[连线输入] 已选中: " + station.displayName);
            }
            return;
        }

        if (_state == State.FirstSelected)
        {
            if (station == _selectedA || station == null)
            {
                ClearHighlightAndReset();
                return;
            }
            if (station.isUnlocked)
            {
                if (_colorPickPanel == null)
                    TryResolveReferences();
                if (_colorPickPanel != null)
                    _colorPickPanel.Show(_selectedA, station, OnColorSelected, OnColorCancel);
                else
                    Debug.LogWarning("[连线输入] 场景中未找到 ColorPickPanel，请在 Hierarchy 的 GameCanvas 下添加");
            }
        }
    }

    private void OnColorSelected(LineColor color)
    {
        if (_selectedA == null || lineManager == null) { ClearHighlightAndReset(); return; }
        StationBehaviour stationB = _colorPickPanel != null ? _colorPickPanel.LastStationB : null;
        if (stationB == null) { ClearHighlightAndReset(); return; }
        bool ok = lineManager.TryCreateOrExtendLine(_selectedA, stationB, color);
        if (!ok && debugLog) Debug.Log("[连线] 无法建线或延伸");
        ClearHighlightAndReset();
    }

    private void OnColorCancel()
    {
        ClearHighlightAndReset();
    }

    public void ClearHighlightAndReset()
    {
        if (_selectedA != null) SetHighlight(_selectedA, false);
        _selectedA = null;
        _state = State.Idle;
    }

    private static void SetHighlight(StationBehaviour station, bool on)
    {
        if (station == null) return;
        station.SetHighlight(on);
    }

    private static StationBehaviour GetStationUnderMouse()
    {
        var cam = Camera.main;
        if (cam == null) return null;
        Vector2 world2D = GetMouseWorld2D(cam);
        Collider2D[] hits = Physics2D.OverlapCircleAll(world2D, 0.6f);
        if (hits == null || hits.Length == 0) return null;
        foreach (var c in hits)
        {
            if (c == null) continue;
            var station = c.GetComponentInParent<StationBehaviour>();
            if (station == null) station = c.GetComponent<StationBehaviour>();
            if (station != null) return station;
        }
        return null;
    }

    private static Vector2 GetMouseWorld2D(Camera cam)
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        float z = 0f;
        if (Mathf.Abs(ray.direction.z) < 0.0001f) return new Vector2(ray.origin.x, ray.origin.y);
        float t = (z - ray.origin.z) / ray.direction.z;
        Vector3 p = ray.origin + ray.direction * t;
        return new Vector2(p.x, p.y);
    }
}

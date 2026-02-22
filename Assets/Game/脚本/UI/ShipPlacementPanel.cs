using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 飞船放置面板。CircleButton + CountText + FanPanel，点击打开选线（色块），点空白关闭。
/// </summary>
public class ShipPlacementPanel : BasePanel
{
    [Header("绑定")]
    public Button circleButton;
    public Text countText;
    public GameObject fanPanelRoot;

    private RectTransform _panelRect;
    private Transform _fanContent;
    private GameObject _closeFanBlocker;
    private GameObject _emptyHintGo;
    private readonly List<Button> _lineButtons = new List<Button>();
    private float _nextRefresh;
    private bool _ignoreNextClick; // 按下即打开后忽略同一次抬手触发的 onClick，避免立刻关闭
    private static Texture2D _whiteTex; // 色块用白纹理，RawImage 需非空 Texture 才能显示颜色

    private static Texture2D GetWhiteTexture()
    {
        if (_whiteTex != null) return _whiteTex;
#pragma warning disable 0618
        Texture2D builtin = Texture2D.whiteTexture;
#pragma warning restore 0618
        if (builtin != null)
        {
            _whiteTex = builtin;
            return _whiteTex;
        }
        _whiteTex = new Texture2D(4, 4);
        for (int i = 0; i < 16; i++) _whiteTex.SetPixel(i % 4, i / 4, Color.white);
        _whiteTex.Apply();
        _whiteTex.filterMode = FilterMode.Bilinear;
        return _whiteTex;
    }

    protected override void OnInit() { }

    private void Start()
    {
        _panelRect = GetComponent<RectTransform>();
        if (_panelRect == null) _panelRect = transform as RectTransform;

        if (circleButton == null)
        {
            var tr = FindInChildren(panelRoot != null ? panelRoot.transform : transform, "CircleButton");
            if (tr != null) circleButton = tr.GetComponent<Button>();
            if (circleButton == null) circleButton = GetComponentInChildren<Button>(true);
        }
        if (circleButton != null)
        {
            circleButton.onClick.RemoveAllListeners();
            circleButton.onClick.AddListener(OnCircleClick);
            var img = circleButton.GetComponent<Image>();
            if (img != null) img.raycastTarget = true;
            if (circleButton.targetGraphic == null) circleButton.targetGraphic = img;
            var trigger = circleButton.gameObject.GetComponent<EventTrigger>();
            if (trigger == null) trigger = circleButton.gameObject.AddComponent<EventTrigger>();
            if (trigger.triggers == null) trigger.triggers = new List<EventTrigger.Entry>();
            var hasPointerDown = false;
            for (int i = 0; i < trigger.triggers.Count; i++)
                if (trigger.triggers[i].eventID == EventTriggerType.PointerDown) { hasPointerDown = true; break; }
            if (!hasPointerDown)
            {
                var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
                entry.callback.AddListener(_ => OnCirclePointerDown());
                trigger.triggers.Add(entry);
            }
        }

        if (countText == null)
        {
            var tr = FindInChildren(panelRoot != null ? panelRoot.transform : transform, "CountText");
            if (tr != null) countText = tr.GetComponent<Text>();
            if (countText == null) countText = GetComponentInChildren<Text>(true);
        }
        if (countText != null)
        {
            countText.raycastTarget = false;
            countText.gameObject.SetActive(true);
        }

        if (fanPanelRoot == null)
        {
            var tr = FindInChildren(panelRoot != null ? panelRoot.transform : transform, "FanPanel");
            if (tr != null) fanPanelRoot = tr.gameObject;
        }
        if (fanPanelRoot != null)
        {
            fanPanelRoot.SetActive(false);
            _fanContent = fanPanelRoot.transform.Find("FanContent");
            if (_fanContent == null) _fanContent = fanPanelRoot.transform;
            var contentGo = _fanContent.gameObject;
            if (contentGo.GetComponent<ContentSizeFitter>() == null)
            {
                var csf = contentGo.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            }
        }

        EnsureCanvasAndEventSystem();
        _nextRefresh = 0f;
        RefreshCount();
    }

    private static Transform FindInChildren(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var c = FindInChildren(root.GetChild(i), name);
            if (c != null) return c;
        }
        return null;
    }

    private void EnsureCanvasAndEventSystem()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<UnityEngine.EventSystems.EventSystem>();
            go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    private void Update()
    {
        if (Time.time >= _nextRefresh)
        {
            _nextRefresh = Time.time + 0.5f;
            RefreshCount();
            if (fanPanelRoot != null && fanPanelRoot.activeSelf)
                RefreshFanButtons();
        }

    }

    private static bool PointInRect(RectTransform rect, Vector2 screenPoint)
    {
        if (rect == null) return false;
        var canvas = rect.GetComponentInParent<Canvas>();
        if (canvas == null) return false;
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceCamera ? canvas.worldCamera : null;
        return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, cam);
    }

    private void OnCirclePointerDown()
    {
        if (fanPanelRoot != null && fanPanelRoot.activeSelf) return;
        _ignoreNextClick = true;
        OpenFan();
    }

    private void OnCircleClick()
    {
        if (_ignoreNextClick)
        {
            _ignoreNextClick = false;
            return;
        }
        if (fanPanelRoot != null && fanPanelRoot.activeSelf)
        {
            CloseFan();
            return;
        }
        OpenFan();
    }

    private void OpenFan()
    {
        var lm = GetLineManager();
        int lineCount = lm != null && lm.Lines != null ? lm.Lines.Count : 0;

        if (fanPanelRoot != null)
        {
            fanPanelRoot.SetActive(true);
            fanPanelRoot.transform.SetAsLastSibling();
            if (countText != null) countText.gameObject.SetActive(false);
        }
        EnsureCloseFanBlocker();
        if (_closeFanBlocker != null) _closeFanBlocker.SetActive(true);

        BuildLineButtons(lineCount);
        RefreshFanButtons();
    }

    private void EnsureCloseFanBlocker()
    {
        if (_closeFanBlocker != null) return;
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;
        _closeFanBlocker = new GameObject("CloseFanBlocker");
        _closeFanBlocker.transform.SetParent(canvas.transform, false);
        var rect = _closeFanBlocker.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.SetAsFirstSibling();
        var img = _closeFanBlocker.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0);
        img.raycastTarget = true;
        var btn = _closeFanBlocker.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(CloseFan);
    }

    public void CloseFan()
    {
        if (fanPanelRoot != null) fanPanelRoot.SetActive(false);
        if (countText != null) countText.gameObject.SetActive(true);
        if (_closeFanBlocker != null) _closeFanBlocker.SetActive(false);
    }

    public bool IsFanPanelOpen() => fanPanelRoot != null && fanPanelRoot.activeSelf;

    private void RefreshCount()
    {
        if (countText == null) return;
        var lm = GetLineManager();
        countText.text = lm != null ? lm.ShipStock.ToString() : "0";
    }

    private LineManager GetLineManager()
    {
        var gm = GameManager.Instance;
        if (gm != null) return gm.GetLineManagerComponent();
        var g = FindObjectOfType<GameManager>();
        return g != null ? g.GetLineManagerComponent() : null;
    }

    private void BuildLineButtons(int count)
    {
        if (_fanContent == null) return;

        if (count == 0)
        {
            EnsureEmptyHint();
            while (_lineButtons.Count > 0)
            {
                var b = _lineButtons[_lineButtons.Count - 1];
                _lineButtons.RemoveAt(_lineButtons.Count - 1);
                if (b != null && b.gameObject != null) Destroy(b.gameObject);
            }
            return;
        }

        DestroyEmptyHint();
        while (_lineButtons.Count > count)
        {
            var b = _lineButtons[_lineButtons.Count - 1];
            _lineButtons.RemoveAt(_lineButtons.Count - 1);
            if (b != null && b.gameObject != null) Destroy(b.gameObject);
        }

        var lm = GetLineManager();
        if (lm == null || lm.Lines == null) return;
        var lines = lm.Lines;

        for (int i = _lineButtons.Count; i < count; i++)
        {
            int idx = i;
            var go = new GameObject("LineBtn_" + i);
            go.transform.SetParent(_fanContent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.sizeDelta = new Vector2(80, 32);

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 32;
            le.minHeight = 24;
            le.flexibleWidth = 1;

            var raw = go.AddComponent<RawImage>();
            Texture2D tex = GetWhiteTexture();
            if (tex != null)
            {
                raw.texture = tex;
                raw.uvRect = new Rect(0, 0, 1, 1);
            }
            else
            {
                var fallback = new Texture2D(1, 1);
                fallback.SetPixel(0, 0, Color.white);
                fallback.Apply();
                raw.texture = fallback;
            }
            raw.color = WithAlpha(GetColorForLine(lines[idx]), 1f);
            raw.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = raw;
            btn.transition = Selectable.Transition.None; // 不覆盖 RawImage 颜色，色块才能显示红/绿/蓝

            btn.onClick.AddListener(() =>
            {
                var manager = GetLineManager();
                if (manager == null || manager.Lines == null || idx < 0 || idx >= manager.Lines.Count) return;
                if (manager.ShipStock <= 0) return;
                manager.SpawnShip(manager.Lines[idx], true);
                CloseFan();
                RefreshCount();
            });
            if (btn.GetComponent<ButtonClickAnim>() == null)
                btn.gameObject.AddComponent<ButtonClickAnim>();

            _lineButtons.Add(btn);
        }
    }

    private void EnsureEmptyHint()
    {
        if (_emptyHintGo != null) return;
        _emptyHintGo = new GameObject("EmptyHint");
        _emptyHintGo.transform.SetParent(_fanContent, false);
        var rect = _emptyHintGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.sizeDelta = new Vector2(0, 40);
        var le = _emptyHintGo.AddComponent<LayoutElement>();
        le.preferredHeight = 40;
        le.flexibleWidth = 1;
        var txt = _emptyHintGo.AddComponent<Text>();
        txt.text = "暂无航线";
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 14;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = new Color(0.7f, 0.7f, 0.7f, 0.9f);
        txt.raycastTarget = false;
    }

    private void DestroyEmptyHint()
    {
        if (_emptyHintGo != null)
        {
            Destroy(_emptyHintGo);
            _emptyHintGo = null;
        }
    }

    private void RefreshFanButtons()
    {
        var lm = GetLineManager();
        if (lm == null || lm.Lines == null || _lineButtons.Count == 0) return;
        int stock = lm.ShipStock;
        for (int i = 0; i < _lineButtons.Count && i < lm.Lines.Count; i++)
        {
            _lineButtons[i].interactable = stock > 0;
            var raw = _lineButtons[i].GetComponent<RawImage>();
            if (raw != null) raw.color = WithAlpha(GetColorForLine(lm.Lines[i]), 1f);
        }
    }

    private static Color WithAlpha(Color c, float a)
    {
        return new Color(c.r, c.g, c.b, a);
    }

    private static Color GetColorForLine(Line line)
    {
        if (line == null) return Color.gray;
        if (line.displayColor != default(Color)) return line.displayColor;
        var vc = GameManager.Instance != null ? GameManager.Instance.visualConfig : null;
        if (vc != null && vc.lineColors != null && vc.lineColors.Length > 0)
        {
            int i = (int)line.color;
            if (i >= 0 && i < vc.lineColors.Length) return vc.lineColors[i];
        }
        return line.color switch
        {
            LineColor.Red => Color.red,
            LineColor.Green => Color.green,
            LineColor.Blue => Color.blue,
            LineColor.Yellow => Color.yellow,
            LineColor.Cyan => Color.cyan,
            LineColor.Magenta => Color.magenta,
            _ => Color.gray
        };
    }

    public override void Hide()
    {
        CloseFan();
        base.Hide();
    }
}

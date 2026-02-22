using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 客舱放置面板。仿照 ShipPlacementPanel：CircleButton + CountText，点击进入放置客舱模式。
/// </summary>
public class ShipUpgradePanel : BasePanel
{
    [Header("绑定")]
    public Button circleButton;
    public Text countText;

    private float _nextRefresh;
    private bool _ignoreNextClick;

    private void Start()
    {
        if (circleButton == null)
            circleButton = FindInChildren(panelRoot != null ? panelRoot.transform : transform, "CircleButton")?.GetComponent<Button>();
        if (circleButton == null)
            circleButton = GetComponentInChildren<Button>(true);
        if (circleButton != null)
        {
            circleButton.onClick.RemoveAllListeners();
            circleButton.onClick.AddListener(OnClick);
            var trigger = circleButton.gameObject.GetComponent<EventTrigger>();
            if (trigger == null) trigger = circleButton.gameObject.AddComponent<EventTrigger>();
            if (trigger.triggers == null) trigger.triggers = new System.Collections.Generic.List<EventTrigger.Entry>();
            var hasPointerDown = false;
            for (int i = 0; i < trigger.triggers.Count; i++)
                if (trigger.triggers[i].eventID == EventTriggerType.PointerDown) { hasPointerDown = true; break; }
            if (!hasPointerDown)
            {
                var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
                entry.callback.AddListener(_ => OnPointerDown());
                trigger.triggers.Add(entry);
            }
        }
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

    private void Update()
    {
        if (Time.time >= _nextRefresh)
        {
            _nextRefresh = Time.time + 0.5f;
            RefreshCount();
            RefreshInteractable();
        }
    }

    private void RefreshInteractable()
    {
        if (circleButton == null) return;
        var lm = GetLineManager();
        bool canUse = lm != null && lm.CarriageStock > 0;
        circleButton.interactable = canUse;
    }

    private void OnPointerDown()
    {
        _ignoreNextClick = true;
        TryEnterPlacingCarriage();
    }

    private void OnClick()
    {
        if (_ignoreNextClick) { _ignoreNextClick = false; return; }
        TryEnterPlacingCarriage();
    }

    private void TryEnterPlacingCarriage()
    {
        if (GameplayUIController.Instance == null) return;
        var lm = GetLineManager();
        if (lm == null) return;
        if (lm.CarriageStock <= 0) return;
        var s = GameplayUIController.Instance.CurrentState;
        if (s != GameplayUIState.Idle && s != GameplayUIState.PlacingShip && s != GameplayUIState.PlacingHub && s != GameplayUIState.LineRemoving) return;
        GameplayUIController.Instance.TryTransition(GameplayUIState.PlacingCarriage);
        UIManager.Show<CarriagePlacementPanel>();
    }

    private void RefreshCount()
    {
        if (countText == null) return;
        var lm = GetLineManager();
        countText.text = lm != null ? lm.CarriageStock.ToString() : "0";
    }

    private LineManager GetLineManager()
    {
        var gm = GameManager.Instance;
        if (gm != null) return gm.GetLineManagerComponent();
        return FindObjectOfType<GameManager>()?.GetLineManagerComponent();
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 星隧面板。与 ShipUpgradePanel 相同交互：CircleButton + CountText，点击显示说明弹窗。
/// 星隧储存量为 0 时 circleButton 变灰。
/// </summary>
public class StarTunnelPanel : BasePanel
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
        {
            var area = FindInChildren(panelRoot != null ? panelRoot.transform : transform, "Area");
            if (area != null)
            {
                circleButton = area.GetComponent<Button>();
                if (circleButton == null) circleButton = area.gameObject.AddComponent<Button>();
            }
        }
        if (circleButton == null)
            circleButton = GetComponentInChildren<Button>(true);
        if (circleButton != null)
        {
            circleButton.onClick.RemoveAllListeners();
            circleButton.onClick.AddListener(OnClick);
            if (circleButton.GetComponent<GameplayButtonHoverSound>() == null)
                circleButton.gameObject.AddComponent<GameplayButtonHoverSound>();
            if (circleButton.GetComponent<ButtonClickAnim>() == null)
                circleButton.gameObject.AddComponent<ButtonClickAnim>();
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
        RefreshInteractable();
        StartCoroutine(RefreshInteractableAfterFrame());
    }

    private IEnumerator RefreshInteractableAfterFrame()
    {
        yield return null;
        RefreshInteractable();
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
        bool canUse = lm != null && lm.StarTunnelStock > 0;
        circleButton.interactable = canUse;
    }

    private void OnPointerDown()
    {
        _ignoreNextClick = true;
        GameplayAudio.Instance?.PlayGeneralClick();
        ShowHintPopup();
    }

    private void OnClick()
    {
        if (_ignoreNextClick) { _ignoreNextClick = false; return; }
        GameplayAudio.Instance?.PlayGeneralClick();
        ShowHintPopup();
    }

    private void ShowHintPopup()
    {
        UIManager.Show<StarTunnelHintPopup>();
    }

    private void RefreshCount()
    {
        if (countText == null) return;
        var lm = GetLineManager();
        countText.text = lm != null ? lm.StarTunnelStock.ToString() : "0";
    }

    private LineManager GetLineManager()
    {
        var gm = GameManager.Instance;
        if (gm != null) return gm.GetLineManagerComponent();
        return FindObjectOfType<GameManager>()?.GetLineManagerComponent();
    }
}

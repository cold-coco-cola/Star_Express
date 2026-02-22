using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI 集中管理器（单例）。
/// - 所有 BasePanel 在 Awake 时自动注册；
/// - 提供泛型 Show/Hide/Get 方法按类型操作面板；
/// - 在 Inspector 中可查看当前注册的面板列表。
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private static readonly Dictionary<Type, BasePanel> _panels = new Dictionary<Type, BasePanel>();

    [Header("已注册面板（只读）")]
    [SerializeField] private List<string> registeredPanelNames = new List<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        RegisterAllPanelsInScene();
    }

    /// <summary>注册场景中所有 BasePanel（含未激活的），解决 inactive 面板 Awake 未调用导致未注册的问题。</summary>
    private void RegisterAllPanelsInScene()
    {
        var canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            var panels = canvas.GetComponentsInChildren<BasePanel>(true);
            foreach (var p in panels)
            {
                if (p != null && !_panels.ContainsKey(p.GetType()))
                    Register(p);
            }
        }
    }

    /// <summary>由 BasePanel.Awake 自动调用。</summary>
    public static void Register(BasePanel panel)
    {
        if (panel == null) return;
        var type = panel.GetType();
        _panels[type] = panel;
        if (Instance != null && !Instance.registeredPanelNames.Contains(type.Name))
            Instance.registeredPanelNames.Add(type.Name);
    }

    /// <summary>由 BasePanel.OnDestroy 自动调用。</summary>
    public static void Unregister(BasePanel panel)
    {
        if (panel == null) return;
        var type = panel.GetType();
        if (_panels.ContainsKey(type) && _panels[type] == panel)
            _panels.Remove(type);
        if (Instance != null)
            Instance.registeredPanelNames.Remove(type.Name);
    }

    /// <summary>获取指定类型面板。</summary>
    public static T Get<T>() where T : BasePanel
    {
        _panels.TryGetValue(typeof(T), out var panel);
        return panel as T;
    }

    /// <summary>显示指定类型面板。</summary>
    public static void Show<T>() where T : BasePanel
    {
        var panel = Get<T>();
        if (panel != null) panel.Show();
        else Debug.LogWarning($"[UIManager] 面板 {typeof(T).Name} 未注册，请确认场景中存在该面板");
    }

    /// <summary>隐藏指定类型面板。</summary>
    public static void Hide<T>() where T : BasePanel
    {
        var panel = Get<T>();
        if (panel != null) panel.Hide();
    }

    /// <summary>隐藏所有面板。</summary>
    public static void HideAll()
    {
        foreach (var kv in _panels)
            if (kv.Value != null) kv.Value.Hide();
    }

    /// <summary>指定类型面板是否正在显示。</summary>
    public static bool IsShowing<T>() where T : BasePanel
    {
        var panel = Get<T>();
        return panel != null && panel.IsVisible;
    }
}

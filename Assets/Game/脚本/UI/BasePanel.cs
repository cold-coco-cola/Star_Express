using UnityEngine;

/// <summary>
/// 所有 UI 面板的基类。提供统一的 Show/Hide 生命周期，
/// 并在 Awake 时自动向 UIManager 注册，使面板可通过
/// UIManager.Show&lt;T&gt;() / UIManager.Hide&lt;T&gt;() 统一管理。
/// </summary>
public abstract class BasePanel : MonoBehaviour
{
    [Tooltip("面板根节点（留空则使用自身 GameObject）")]
    [SerializeField] protected GameObject panelRoot;

    private bool _initialized;

    public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

    protected virtual void Awake()
    {
        EnsureInitialized();
    }

    protected virtual void OnDestroy()
    {
        UIManager.Unregister(this);
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;
        if (panelRoot == null)
            panelRoot = gameObject;
        UIManager.Register(this);
        OnInit();
    }

    /// <summary>初始化回调，仅调用一次。子类重写以缓存引用。</summary>
    protected virtual void OnInit() { }

    /// <summary>显示面板。子类可重写以添加动画或刷新数据。</summary>
    public virtual void Show()
    {
        EnsureInitialized();
        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    /// <summary>隐藏面板。子类可重写以添加关闭动画。</summary>
    public virtual void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    /// <summary>切换显示/隐藏。</summary>
    public void Toggle()
    {
        if (IsVisible) Hide();
        else Show();
    }
}

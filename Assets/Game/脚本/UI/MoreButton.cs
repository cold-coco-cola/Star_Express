using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 顶部「更多」按钮：切换操作指南显示。
/// </summary>
public class MoreButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private OperationGuidePanel guidePanel;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveListener(OnClicked);
            button.onClick.AddListener(OnClicked);
            if (button.GetComponent<GameplayButtonHoverSound>() == null)
                button.gameObject.AddComponent<GameplayButtonHoverSound>();
            if (button.GetComponent<ButtonClickAnim>() == null)
                button.gameObject.AddComponent<ButtonClickAnim>();
        }
    }

    private void Start()
    {
        ResolveGuidePanel();
    }

    private void ResolveGuidePanel()
    {
        if (guidePanel != null) return;
        guidePanel = UIManager.Get<OperationGuidePanel>();
        if (guidePanel == null)
            guidePanel = FindObjectOfType<OperationGuidePanel>(true);
    }

    private void OnClicked()
    {
        GameplayAudio.Instance?.PlayGeneralClick();
        ResolveGuidePanel();
        if (guidePanel == null) return;

        if (guidePanel.IsVisible) guidePanel.Hide();
        else guidePanel.ShowGuide();
    }
}

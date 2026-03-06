using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 点击按钮切换 TimeSpeedPanel 的显示/隐藏。
/// </summary>
public class TimeSpeedPanelToggle : MonoBehaviour
{
    private TimeSpeedPanel _cachedPanel;
    private Button _button;

    public void BindButton(Button btn)
    {
        _button = btn;
        if (_button != null)
        {
            _button.onClick.AddListener(TogglePanel);
        }
    }

    private void Start()
    {
        if (_button == null)
        {
            _button = GetComponent<Button>();
            if (_button != null)
            {
                _button.onClick.AddListener(TogglePanel);
            }
        }
    }

    private void TogglePanel()
    {
        if (_cachedPanel == null)
        {
            _cachedPanel = UIManager.Get<TimeSpeedPanel>();
            if (_cachedPanel == null)
            {
                _cachedPanel = FindObjectOfType<TimeSpeedPanel>(true);
            }
        }

        if (_cachedPanel != null)
        {
            if (_cachedPanel.IsVisible)
                _cachedPanel.Hide();
            else
                _cachedPanel.Show();
        }
        else
        {
            var panelGo = GameObject.Find("TimeSpeedPanel");
            if (panelGo != null)
            {
                panelGo.SetActive(!panelGo.activeSelf);
            }
        }
        GameplayAudio.Instance?.PlayGeneralClick();
    }
}

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 时间调节面板：显示三个速度按钮（1x、1.5x、2x），位于暂停键下方。
/// </summary>
public class TimeSpeedPanel : BasePanel
{
    [Header("按钮绑定")]
    public Button speed1xButton;
    public Button speed1_5xButton;
    public Button speed2xButton;

    private float _currentSpeed = 1f;
    private bool _eventsBound;

    private static readonly Color NormalColor = new Color(0.35f, 0.4f, 0.5f, 0.9f);
    private static readonly Color SelectedColor = new Color(0.4f, 0.7f, 0.5f, 1f);

    private void Start()
    {
        BindEvents();
    }

    public void BindEvents()
    {
        if (_eventsBound) return;

        if (speed1xButton != null)
            speed1xButton.onClick.AddListener(() => SetSpeed(1f));
        if (speed1_5xButton != null)
            speed1_5xButton.onClick.AddListener(() => SetSpeed(1.5f));
        if (speed2xButton != null)
            speed2xButton.onClick.AddListener(() => SetSpeed(2f));

        _eventsBound = true;
        UpdateVisuals();
    }

    private void SetSpeed(float speed)
    {
        _currentSpeed = speed;

        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.SetGameSpeed(speed);
        }

        UpdateVisuals();
        GameplayAudio.Instance?.PlayGeneralClick();
    }

    private void UpdateVisuals()
    {
        SetButtonState(speed1xButton, Mathf.Approximately(_currentSpeed, 1f));
        SetButtonState(speed1_5xButton, Mathf.Approximately(_currentSpeed, 1.5f));
        SetButtonState(speed2xButton, Mathf.Approximately(_currentSpeed, 2f));
    }

    private void SetButtonState(Button btn, bool selected)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null)
            img.color = selected ? SelectedColor : NormalColor;
    }

    public void ResetToNormalSpeed()
    {
        _currentSpeed = 1f;
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.SetGameSpeed(1f);
        }
        UpdateVisuals();
    }

    public float CurrentSpeed => _currentSpeed;
}

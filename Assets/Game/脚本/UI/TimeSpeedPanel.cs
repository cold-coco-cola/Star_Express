using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 时间调节面板：显示四个速度按钮（0x、1x、1.5x、2x），位于暂停键下方。
/// 0x = 暂停但可操作路线
/// </summary>
public class TimeSpeedPanel : BasePanel
{
    [Header("按钮绑定")]
    public Button speed0xButton;
    public Button speed1xButton;
    public Button speed1_5xButton;
    public Button speed2xButton;

    private float _currentSpeed = 1f;
    private bool _eventsBound;

    private static readonly Color NormalColor = new Color(0.7f, 0.75f, 0.85f, 1f);
    private static readonly Color SelectedColor = new Color(0.5f, 0.9f, 0.6f, 1f);
    private static readonly Color PauseColor = new Color(0.9f, 0.55f, 0.55f, 1f);

    private void Start()
    {
        BindEvents();
    }

    public void BindEvents()
    {
        if (_eventsBound) return;

        SetTransitionNone(speed0xButton);
        SetTransitionNone(speed1xButton);
        SetTransitionNone(speed1_5xButton);
        SetTransitionNone(speed2xButton);

        if (speed0xButton != null)
            speed0xButton.onClick.AddListener(() => SetSpeed(0f));
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
        SetButtonState(speed0xButton, Mathf.Approximately(_currentSpeed, 0f), true);
        SetButtonState(speed1xButton, Mathf.Approximately(_currentSpeed, 1f), false);
        SetButtonState(speed1_5xButton, Mathf.Approximately(_currentSpeed, 1.5f), false);
        SetButtonState(speed2xButton, Mathf.Approximately(_currentSpeed, 2f), false);
    }

    private static void SetTransitionNone(Button btn)
    {
        if (btn != null) btn.transition = Selectable.Transition.None;
    }

    private void SetButtonState(Button btn, bool selected, bool isPause)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null)
        {
            if (selected)
                img.color = isPause ? PauseColor : SelectedColor;
            else
                img.color = NormalColor;
        }
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

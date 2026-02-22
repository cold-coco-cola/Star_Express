using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 暂停菜单：继续按钮、音量滑块。暂停时显示，周奖励弹窗出现时自动隐藏。
/// </summary>
public class PauseMenu : BasePanel
{
    [Header("绑定（留空则运行时创建）")]
    public Button resumeButton;
    public Slider volumeSlider;
    public Text volumeLabel;

    private void Start()
    {
        if (resumeButton != null) resumeButton.onClick.AddListener(OnResume);
        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value = PlayerPrefs.GetFloat(BackgroundMusic.VolumePrefKey, 0.6f);
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        var gm = GameManager.Instance;
        if (gm != null) gm.OnWeekRewardSelectionRequired += OnWeekRewardShown;

        RefreshVolumeLabel();
    }

    protected override void OnDestroy()
    {
        var gm = GameManager.Instance;
        if (gm != null) gm.OnWeekRewardSelectionRequired -= OnWeekRewardShown;
        base.OnDestroy();
    }

    private void OnWeekRewardShown(int _)
    {
        var gm = GameManager.Instance;
        if (gm != null) gm.SetUserPaused(false);
        Hide();
    }

    private void OnResume()
    {
        var gm = GameManager.Instance;
        if (gm != null) gm.SetUserPaused(false);
        Hide();
    }

    private void OnVolumeChanged(float v)
    {
        var music = GameManager.Instance != null ? GameManager.Instance.GetComponent<BackgroundMusic>() : null;
        if (music != null) music.SetVolume(v);
        RefreshVolumeLabel();
    }

    private void RefreshVolumeLabel()
    {
        if (volumeLabel == null) return;
        float v = volumeSlider != null ? volumeSlider.value : 0.6f;
        volumeLabel.text = v <= 0.01f ? "音量: 静音" : $"音量: {(int)(v * 100)}%";
    }
}

using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 暂停菜单：上部分音量控制（背景音乐、音效），底部返回主菜单。与 StartMenu 按钮同风格。
/// </summary>
public class PauseMenu : BasePanel
{
    [Header("绑定（留空则运行时创建）")]
    public Slider musicSlider;
    public Slider sfxSlider;
    public Button backToMenuButton;

    private const string MusicVolumeKey = "MusicVolume";
    private const string SFXVolumeKey = "SFXVolume";
    private const string StartMenuScene = "StartMenu";

    private void Start()
    {
        if (musicSlider != null)
        {
            musicSlider.minValue = 0f;
            musicSlider.maxValue = 1f;
            musicSlider.value = PlayerPrefs.GetFloat(MusicVolumeKey, 0.6f);
            musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }
        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;
            sfxSlider.value = PlayerPrefs.GetFloat(SFXVolumeKey, 0.7f);
            sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
        if (backToMenuButton != null)
        {
            backToMenuButton.onClick.AddListener(() => { GameplayAudio.Instance?.PlayGeneralClick(); OnBackToMenu(); });
        }

        var gm = GameManager.Instance;
        if (gm != null) gm.OnWeekRewardSelectionRequired += OnWeekRewardShown;
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

    private void OnMusicVolumeChanged(float v)
    {
        PlayerPrefs.SetFloat(MusicVolumeKey, v);
        PlayerPrefs.Save();
        if (GlobalBackgroundMusic.Instance != null)
            GlobalBackgroundMusic.Instance.SetVolume(v);
    }

    private void OnSFXVolumeChanged(float v)
    {
        PlayerPrefs.SetFloat(SFXVolumeKey, v);
        PlayerPrefs.Save();
    }

    private void OnBackToMenu()
    {
        var gm = GameManager.Instance;
        if (gm != null) Destroy(gm.gameObject);
        if (SceneExists(StartMenuScene))
            SceneManager.LoadScene(StartMenuScene);
        else
            SceneManager.LoadScene(0);
    }

    private static bool SceneExists(string name)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            var path = SceneUtility.GetScenePathByBuildIndex(i);
            if (Path.GetFileNameWithoutExtension(path) == name) return true;
        }
        return false;
    }
}

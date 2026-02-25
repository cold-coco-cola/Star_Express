using UnityEngine;

/// <summary>
/// 关卡内 UI 与站点音效。挂到 GameManager，供 ColorPickPanel、ShipPlacementPanel、StationBehaviour 等调用。
/// 音量受设置面板「音效」控制。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class GameplayAudio : MonoBehaviour
{
    [Header("音效")]
    [Tooltip("悬停，留空则加载 音乐/In-Level Sounds/in_level_hover")]
    public AudioClip hoverClip;
    [Tooltip("选色/选线点击，留空则加载 音乐/In-Level Sounds/colorpick_click")]
    public AudioClip clickClip;
    [Tooltip("通用按键点击，留空则加载 音乐/Menu Sounds/level_select_click")]
    public AudioClip generalClickClip;
    [Tooltip("站点点击弹起，留空则加载 音乐/In-Level Sounds/station_click")]
    public AudioClip stationClickClip;

    [Header("音量")]
    [Range(0f, 1f)] public float hoverVolume = 0.5f;
    [Range(0f, 1f)] public float clickVolume = 0.7f;
    [Range(0f, 1f)] public float generalClickVolume = 0.7f;
    [Range(0f, 1f)] public float stationClickVolume = 0.8f;

    private AudioSource _source;
    public static GameplayAudio Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _source = GetComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.loop = false;

        if (hoverClip == null) hoverClip = Resources.Load<AudioClip>("音乐/In-Level Sounds/in_level_hover");
        if (clickClip == null) clickClip = Resources.Load<AudioClip>("音乐/In-Level Sounds/colorpick_click");
        if (generalClickClip == null) generalClickClip = Resources.Load<AudioClip>("音乐/Menu Sounds/level_select_click");
        if (stationClickClip == null) stationClickClip = Resources.Load<AudioClip>("音乐/In-Level Sounds/station_click");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private float SfxVolume => Game.Scripts.UI.SettingsPanelController.GetSFXVolume();

    public void PlayHover()
    {
        if (hoverClip != null && _source != null)
            _source.PlayOneShot(hoverClip, hoverVolume * SfxVolume);
    }

    public void PlayClick()
    {
        if (clickClip != null && _source != null)
            _source.PlayOneShot(clickClip, clickVolume * SfxVolume);
    }

    /// <summary>通用按键点击（暂停、客舱等），使用 level_select_click。</summary>
    public void PlayGeneralClick()
    {
        if (generalClickClip != null && _source != null)
            _source.PlayOneShot(generalClickClip, generalClickVolume * SfxVolume);
    }

    public void PlayStationClick()
    {
        if (stationClickClip != null && _source != null)
            _source.PlayOneShot(stationClickClip, stationClickVolume * SfxVolume);
    }
}

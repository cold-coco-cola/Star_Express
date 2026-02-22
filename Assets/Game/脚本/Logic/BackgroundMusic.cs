using UnityEngine;

/// <summary>
/// 游戏开始后播放全局背景音乐（循环）。
/// 优先使用 Inspector 中拖入的 AudioClip；若为空则从 Resources/音乐 加载 speed。
/// 音量由 PlayerPrefs "MusicVolume" 持久化，可通过 SetVolume 调节。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class BackgroundMusic : MonoBehaviour
{
    public const string VolumePrefKey = "MusicVolume";

    [Header("背景音乐")]
    [Tooltip("留空则从 Resources/音乐 加载 Andrew Prahlow - Travelers' encore")]
    public AudioClip musicClip;

    [Range(0f, 1f)]
    public float volume = 0.6f;

    private AudioSource _source;

    private void Awake()
    {
        _source = GetComponent<AudioSource>();
        if (_source == null) _source = gameObject.AddComponent<AudioSource>();

        _source.playOnAwake = false;
        _source.loop = true;
        volume = PlayerPrefs.GetFloat(VolumePrefKey, 0.6f);
        _source.volume = volume;
    }

    private void Start()
    {
        if (musicClip == null)
            musicClip = Resources.Load<AudioClip>("音乐/Andrew Prahlow - Travelers' encore");

        if (musicClip != null)
        {
            _source.clip = musicClip;
            _source.volume = volume;
            _source.Play();
        }
        else
            Debug.LogWarning("[BackgroundMusic] 未找到背景音乐。请确保 Andrew Prahlow - Travelers' encore.mp3 在 Assets/Game/Resources/音乐/ 下，或在 Inspector 指定 musicClip。");
    }

    /// <summary>设置音量并持久化。</summary>
    public void SetVolume(float v)
    {
        volume = Mathf.Clamp01(v);
        if (_source != null) _source.volume = volume;
        PlayerPrefs.SetFloat(VolumePrefKey, volume);
        PlayerPrefs.Save();
    }
}

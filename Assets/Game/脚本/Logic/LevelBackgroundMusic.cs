using UnityEngine;

/// <summary>
/// 关卡内背景音乐控制器。两首音乐依次循环播放。
/// 挂载在关卡场景中，音量由 PlayerPrefs "MusicVolume" 控制。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class LevelBackgroundMusic : MonoBehaviour
{
    public const string VolumePrefKey = "MusicVolume";

    [Header("背景音乐（依次循环播放）")]
    [Tooltip("留空则从 Resources/音乐 加载 启程 和 新星")]
    public AudioClip[] musicClips;

    [Range(0f, 1f)]
    public float volume = 0.6f;

    private AudioSource _source;
    private int _currentIndex = 0;

    private void Awake()
    {
        _source = GetComponent<AudioSource>();
        if (_source == null) _source = gameObject.AddComponent<AudioSource>();

        _source.playOnAwake = false;
        _source.loop = false;
        volume = PlayerPrefs.GetFloat(VolumePrefKey, 0.6f);
        _source.volume = volume;
    }

    private void Start()
    {
        if (musicClips == null || musicClips.Length == 0)
        {
            var clip1 = Resources.Load<AudioClip>("音乐/启程");
            var clip2 = Resources.Load<AudioClip>("音乐/新星");
            if (clip1 != null && clip2 != null)
            {
                musicClips = new AudioClip[] { clip1, clip2 };
            }
            else
            {
                Debug.LogWarning("[LevelBackgroundMusic] 未找到背景音乐文件。请确保 启程.mp3 和 新星.mp3 在 Assets/Game/Resources/音乐/ 下。");
                return;
            }
        }

        PlayCurrentClip();
    }

    private void Update()
    {
        if (_source != null && !_source.isPlaying && musicClips != null && musicClips.Length > 0)
        {
            _currentIndex = (_currentIndex + 1) % musicClips.Length;
            PlayCurrentClip();
        }
    }

    private void PlayCurrentClip()
    {
        if (musicClips == null || musicClips.Length == 0) return;
        if (_currentIndex < 0 || _currentIndex >= musicClips.Length) return;
        if (musicClips[_currentIndex] == null) return;

        _source.clip = musicClips[_currentIndex];
        _source.volume = volume;
        _source.Play();
    }

    /// <summary>设置音量并持久化。</summary>
    public void SetVolume(float v)
    {
        volume = Mathf.Clamp01(v);
        if (_source != null) _source.volume = volume;
        PlayerPrefs.SetFloat(VolumePrefKey, volume);
        PlayerPrefs.Save();
    }

    /// <summary>获取当前音量。</summary>
    public float GetVolume()
    {
        return volume;
    }
}

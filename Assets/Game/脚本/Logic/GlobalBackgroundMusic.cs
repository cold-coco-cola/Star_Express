using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 关卡之外的全局背景音乐（主菜单、关卡选择等）。进入关卡时停止。
/// 从 Resources/音乐/backgroundmusic 加载。单例，避免返回主菜单时重复播放。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class GlobalBackgroundMusic : MonoBehaviour
{
    public const string VolumePrefKey = "MusicVolume";

    public static GlobalBackgroundMusic Instance { get; private set; }

    [Header("背景音乐")]
    [Tooltip("留空则从 Resources/音乐 加载 backgroundmusic")]
    public AudioClip musicClip;

    [Range(0f, 1f)] public float volume = 0.6f;

    private AudioSource _source;
    private static readonly string[] NonLevelScenes = { "StartMenu", "LevelSelect" };

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
        _source.loop = true;
        volume = PlayerPrefs.GetFloat(VolumePrefKey, 0.6f);
        _source.volume = volume;

        if (musicClip == null)
            musicClip = Resources.Load<AudioClip>("音乐/backgroundmusic");

        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool isNonLevel = false;
        foreach (var name in NonLevelScenes)
        {
            if (scene.name == name)
            {
                isNonLevel = true;
                break;
            }
        }

        if (isNonLevel)
        {
            if (musicClip != null && _source != null)
            {
                if (!_source.isPlaying)
                {
                    _source.clip = musicClip;
                    _source.volume = volume;
                    _source.Play();
                }
            }
        }
        else
        {
            if (_source != null && _source.isPlaying)
                _source.Stop();
        }
    }

    private void Start()
    {
        if (musicClip != null && _source != null && !_source.isPlaying)
        {
            _source.clip = musicClip;
            _source.volume = volume;
            _source.Play();
        }
    }

    public void SetVolume(float v)
    {
        volume = Mathf.Clamp01(v);
        if (_source != null) _source.volume = volume;
        PlayerPrefs.SetFloat(VolumePrefKey, volume);
        PlayerPrefs.Save();
    }
}

using UnityEngine;

namespace Game.Scripts.UI
{
    /// <summary>
    /// 菜单交互音效。挂到 MainMenuManager。留空则从 Resources/音乐 加载 start_menu_hover、start_menu_click。
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MenuAudio : MonoBehaviour
    {
        [Header("音效")]
        [Tooltip("鼠标悬停时播放，留空则加载 音乐/Menu Sounds/start_menu_hover")]
        public AudioClip hoverClip;
        [Tooltip("点击时播放，留空则加载 音乐/Menu Sounds/start_menu_click")]
        public AudioClip clickClip;

        [Header("音量")]
        [Range(0f, 1f)] public float hoverVolume = 0.5f;
        [Range(0f, 1f)] public float clickVolume = 0.7f;

        private AudioSource _source;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = false;
            if (hoverClip == null) hoverClip = Resources.Load<AudioClip>("音乐/Menu Sounds/start_menu_hover");
            if (clickClip == null) clickClip = Resources.Load<AudioClip>("音乐/Menu Sounds/start_menu_click");
        }

        public void PlayHover()
        {
            if (hoverClip != null && _source != null)
                _source.PlayOneShot(hoverClip, hoverVolume * SettingsPanelController.GetSFXVolume());
        }

        public void PlayClick()
        {
            if (clickClip != null && _source != null)
                _source.PlayOneShot(clickClip, clickVolume * SettingsPanelController.GetSFXVolume());
        }
    }
}

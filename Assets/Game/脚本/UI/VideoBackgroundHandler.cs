using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;

namespace Game.Scripts.UI
{
    /// <summary>
    /// 将视频播放到 RawImage 上，用于主菜单星空背景。
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class VideoBackgroundHandler : MonoBehaviour
    {
        public VideoClip videoClip;
        public bool loop = true;
        public bool mute = true;
        public int renderWidth = 1920;
        public int renderHeight = 1080;

        [Header("调试")]
        [Tooltip("勾选后若视频失败会显示测试色块，用于确认 RawImage 是否可见")]
        public bool showDebugColorOnFail = true;

        private VideoPlayer _videoPlayer;
        private RenderTexture _renderTexture;
        private Texture2D _debugTexture;
        private RawImage _rawImage;

        private void Awake()
        {
            _rawImage = GetComponent<RawImage>();
            EnsureFullScreen();
        }

        private void EnsureFullScreen()
        {
            var rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }

        private void Start()
        {
            if (videoClip == null)
                TryLoadDefaultVideo();
            if (videoClip != null)
                StartCoroutine(SetupAndPlayCoroutine());
            else if (showDebugColorOnFail)
                ShowDebugColor();
        }

        private void TryLoadDefaultVideo()
        {
#if UNITY_EDITOR
            videoClip = UnityEditor.AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Game/美术/Animations/粒子星空.mp4");
#else
            videoClip = Resources.Load<VideoClip>("Video/粒子星空");
#endif
        }

        private IEnumerator SetupAndPlayCoroutine()
        {
            if (_rawImage == null || videoClip == null)
            {
                if (showDebugColorOnFail) ShowDebugColor();
                yield break;
            }

            _rawImage.color = Color.white;

            _renderTexture = new RenderTexture(renderWidth, renderHeight, 0, RenderTextureFormat.ARGB32);
            _renderTexture.filterMode = FilterMode.Bilinear;
            _renderTexture.wrapMode = TextureWrapMode.Clamp;
            _renderTexture.Create();

            _videoPlayer = gameObject.AddComponent<VideoPlayer>();
            _videoPlayer.playOnAwake = false;
            _videoPlayer.waitForFirstFrame = false;
            _videoPlayer.isLooping = loop;
            _videoPlayer.clip = videoClip;
            _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            _videoPlayer.targetTexture = _renderTexture;
            _videoPlayer.audioOutputMode = mute ? VideoAudioOutputMode.None : VideoAudioOutputMode.Direct;

            if (loop)
                _videoPlayer.loopPointReached += OnLoopPointReached;
            _videoPlayer.errorReceived += (vp, msg) => Debug.LogWarning($"[VideoBackground] {msg}");

            _rawImage.texture = _renderTexture;
            _rawImage.uvRect = new Rect(0, 0, 1, 1);

            _videoPlayer.Prepare();
            while (!_videoPlayer.isPrepared && _videoPlayer != null)
                yield return null;

            if (_videoPlayer == null) yield break;

            _videoPlayer.Play();

            yield return new WaitForSecondsRealtime(1.5f);

            if (_videoPlayer != null && !_videoPlayer.isPlaying && showDebugColorOnFail)
            {
                Debug.LogWarning("[VideoBackground] 视频未成功播放，显示备用色块。请检查视频编码（建议 H.264）或 Console 错误。");
                ShowDebugColor();
            }

            LightenVideoOverlay();
        }

        private void OnLoopPointReached(VideoPlayer vp)
        {
            if (loop && vp != null) vp.Play();
        }

        private void ShowDebugColor()
        {
            if (_rawImage == null) return;
            _debugTexture = new Texture2D(2, 2);
            _debugTexture.SetPixels(new Color[] { Color.cyan, Color.cyan, Color.cyan, Color.cyan });
            _debugTexture.Apply();
            _rawImage.texture = _debugTexture;
            _rawImage.color = new Color(0.3f, 0.5f, 0.8f, 0.9f);
        }

        private void LightenVideoOverlay()
        {
            var overlay = transform.parent?.Find("VideoOverlay");
            if (overlay != null)
            {
                var img = overlay.GetComponent<Image>();
                if (img != null)
                    img.color = new Color(0.02f, 0.02f, 0.06f, 0.08f);
            }
        }

        private void OnDestroy()
        {
            if (_videoPlayer != null && loop)
                _videoPlayer.loopPointReached -= OnLoopPointReached;
            if (_renderTexture != null && _renderTexture.IsCreated())
                _renderTexture.Release();
            if (_debugTexture != null)
                Destroy(_debugTexture);
        }
    }
}

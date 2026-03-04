using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 首次进入关卡时播放背景故事，支持打字机与点击跳过。
/// </summary>
public class StoryPanel : BasePanel
{
    [Header("UI")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Text storyText;

    [Header("Timing")]
    [SerializeField] private float charInterval = 0.04f;
    [SerializeField] private float lineEndDelay = 0.3f;
    [SerializeField] private float autoNextScreenDelay = 1f;
    [SerializeField] private float fadeOutDuration = 1f;

    [Header("Typing Audio")]
    [SerializeField] private float baseTypingVolume = 0.5f;
    [SerializeField] private float typingVolumeFadeDuration = 0.1f;
    [SerializeField] private string typingClipPath = "音乐/In-Level Sounds/typing";

    private readonly List<string[]> _screens = new List<string[]>
    {
        new[]
        {
            "星穹纪年 233 年：",
            "这是宇安匹人走向太空、实现星穹探索的第 233 年，他们从自己的星系走向了深空……"
        },
        new[]
        {
            "星穹铁道公司",
            "新员工入职指引：",
            "",
            "欢迎加入星穹铁道。",
            "在这里，你将负责在宇安匹人新到达的星系规划航线，",
            "运送乘客穿越浩瀚星空。"
        },
        new[]
        {
            "每个星球都有乘客等待，",
            "他们需要前往特定形状的星球。",
            "建立航线，将他们送达目的地。",
            "",
            "合理规划线路，",
            "避免站点过度拥挤。",
            "祝你好运，调度员。",
            "",
            "[ 点击任意处开始 ]"
        }
    };

    private Action _onComplete;
    private Coroutine _flowCoroutine;
    private Coroutine _volumeCoroutine;
    private AudioSource _typingSource;
    private AudioClip _typingClip;

    private bool _isTyping;
    private bool _skipTypingRequested;
    private bool _advanceRequested;
    private bool _isFinishing;

    protected override void OnInit()
    {
        if (panelRoot == null) panelRoot = gameObject;
        if (backgroundImage == null) backgroundImage = GetComponentInChildren<Image>(true);
        if (storyText == null) storyText = GetComponentInChildren<Text>(true);
        if (storyText != null)
        {
            storyText.font = GameUIFonts.Default;
            storyText.alignment = TextAnchor.MiddleCenter;
            storyText.color = new Color(0.906f, 0.796f, 0.612f);
        }

        _typingSource = GetComponent<AudioSource>();
        if (_typingSource == null) _typingSource = gameObject.AddComponent<AudioSource>();
        _typingSource.playOnAwake = false;
        _typingSource.loop = true;
        _typingSource.volume = 0f;
        _typingClip = Resources.Load<AudioClip>(typingClipPath);
    }

    private void Update()
    {
        if (!IsVisible || _isFinishing) return;

        if (Input.GetMouseButtonDown(0))
            Skip();
    }

    public void ShowStory(Action onComplete)
    {
        Show();
        transform.SetAsLastSibling();
        _onComplete = onComplete;
        _isFinishing = false;
        _skipTypingRequested = false;
        _advanceRequested = false;
        if (storyText != null) storyText.text = string.Empty;
        if (backgroundImage != null)
        {
            var c = backgroundImage.color;
            c.a = 1f;
            backgroundImage.color = c;
        }

        if (_flowCoroutine != null) StopCoroutine(_flowCoroutine);
        _flowCoroutine = StartCoroutine(RunStoryFlow());
    }

    public void Skip()
    {
        if (_isFinishing) return;
        if (_isTyping)
            _skipTypingRequested = true;
        else
            _advanceRequested = true;
    }

    private IEnumerator RunStoryFlow()
    {
        for (int i = 0; i < _screens.Count; i++)
        {
            _advanceRequested = false;
            _skipTypingRequested = false;
            yield return TypeScreen(_screens[i]);

            if (i < _screens.Count - 1)
            {
                float t = 0f;
                while (!_advanceRequested && t < autoNextScreenDelay)
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            else
            {
                while (!_advanceRequested)
                    yield return null;
            }
        }

        yield return FadeOutAndFinish();
        _flowCoroutine = null;
    }

    private IEnumerator TypeScreen(string[] lines)
    {
        _isTyping = true;
        if (storyText != null) storyText.text = string.Empty;
        StartTypingSound();

        var sb = new StringBuilder();
        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            string line = lines[lineIndex] ?? string.Empty;
            for (int charIndex = 0; charIndex < line.Length; charIndex++)
            {
                if (_skipTypingRequested)
                    break;

                sb.Append(line[charIndex]);
                if (storyText != null) storyText.text = sb.ToString();
                yield return new WaitForSecondsRealtime(charInterval);
            }

            if (_skipTypingRequested)
            {
                FillFullScreenText(lines);
                break;
            }

            if (lineIndex < lines.Length - 1)
                sb.Append('\n');

            if (storyText != null) storyText.text = sb.ToString();

            if (lineIndex < lines.Length - 1)
                yield return new WaitForSecondsRealtime(lineEndDelay);
        }

        StopTypingSound();
        _isTyping = false;
    }

    private void FillFullScreenText(string[] lines)
    {
        if (storyText == null) return;
        storyText.text = string.Join("\n", lines);
    }

    private void StartTypingSound()
    {
        if (_typingSource == null || _typingClip == null) return;
        _typingSource.clip = _typingClip;
        _typingSource.loop = true;
        if (!_typingSource.isPlaying) _typingSource.Play();
        FadeTypingVolume(GetTypingTargetVolume());
    }

    private void StopTypingSound()
    {
        if (_typingSource == null) return;
        FadeTypingVolume(0f, true);
    }

    private float GetTypingTargetVolume()
    {
        float sfx = PlayerPrefs.GetFloat("SFXVolume", 0.7f);
        return Mathf.Clamp01(baseTypingVolume * sfx);
    }

    private void FadeTypingVolume(float target, bool stopWhenDone = false)
    {
        if (_typingSource == null) return;
        if (_volumeCoroutine != null) StopCoroutine(_volumeCoroutine);
        _volumeCoroutine = StartCoroutine(FadeTypingVolumeRoutine(target, stopWhenDone));
    }

    private IEnumerator FadeTypingVolumeRoutine(float target, bool stopWhenDone)
    {
        float start = _typingSource.volume;
        float t = 0f;
        while (t < typingVolumeFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = typingVolumeFadeDuration > 0.0001f ? t / typingVolumeFadeDuration : 1f;
            _typingSource.volume = Mathf.Lerp(start, target, p);
            yield return null;
        }
        _typingSource.volume = target;
        if (stopWhenDone && _typingSource.isPlaying)
            _typingSource.Stop();
        _volumeCoroutine = null;
    }

    private IEnumerator FadeOutAndFinish()
    {
        _isFinishing = true;

        float t = 0f;
        Color bg = backgroundImage != null ? backgroundImage.color : Color.black;
        Color txt = storyText != null ? storyText.color : Color.white;
        while (t < fadeOutDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(1f, 0f, t / fadeOutDuration);
            if (backgroundImage != null)
            {
                bg.a = a;
                backgroundImage.color = bg;
            }
            if (storyText != null)
            {
                txt.a = a;
                storyText.color = txt;
            }
            yield return null;
        }

        Hide();
        _isFinishing = false;
        _onComplete?.Invoke();
    }

    public override void Hide()
    {
        if (_flowCoroutine != null)
        {
            StopCoroutine(_flowCoroutine);
            _flowCoroutine = null;
        }
        if (_volumeCoroutine != null)
        {
            StopCoroutine(_volumeCoroutine);
            _volumeCoroutine = null;
        }
        if (_typingSource != null)
        {
            _typingSource.volume = 0f;
            _typingSource.Stop();
        }
        _isTyping = false;
        _skipTypingRequested = false;
        _advanceRequested = false;
        _isFinishing = false;

        if (storyText != null)
        {
            var c = storyText.color;
            c.a = 1f;
            storyText.color = c;
        }
        if (backgroundImage != null)
        {
            var c = backgroundImage.color;
            c.a = 1f;
            backgroundImage.color = c;
        }
        base.Hide();
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI
{
    /// <summary>
    /// 设置弹窗控制器。挂到 MainMenuManager，运行时为 OptionsPanel 填充声音板块。
    /// 若场景中已有 OptionsPanel 但无声音 UI，则自动创建。
    /// </summary>
    public class SettingsPanelController : MonoBehaviour
    {
        [Header("引用")]
        [Tooltip("留空则自动查找 OptionsPanel")]
        public Transform optionsPanel;

        private const string MusicVolumeKey = "MusicVolume";
        private const string SFXVolumeKey = "SFXVolume";

        private void Start()
        {
            var panel = optionsPanel != null ? optionsPanel : transform.Find("OptionsPanel");
            if (panel == null) return;

            var content = panel.Find("Box/Content");
            if (content == null) return;

            var soundSection = content.Find("SoundSection");
            if (soundSection != null)
            {
                BindSoundSection(content);
                return;
            }
            BuildSoundSection(content);
        }

        private void BuildSoundSection(Transform content)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null) return;

            var section = new GameObject("SoundSection");
            section.AddComponent<Image>().color = new Color(0, 0, 0, 0); // 透明，用于获得 RectTransform
            section.transform.SetParent(content, false);

            var vlg = section.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 16;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;

            var sectionRt = section.GetComponent<RectTransform>();
            if (sectionRt != null)
            {
                sectionRt.anchorMin = Vector2.zero;
                sectionRt.anchorMax = Vector2.one;
                sectionRt.offsetMin = sectionRt.offsetMax = Vector2.zero;
            }

            // 移除旧占位
            var ph = content.Find("Placeholder");
            if (ph != null) Destroy(ph.gameObject);

            // 标题：声音
            var title = CreateLabel(section.transform, "声音", font, 22);
            var titleRt = title != null ? title.GetComponent<RectTransform>() : null;
            if (titleRt != null) titleRt.sizeDelta = new Vector2(-1, 32);

            // 背景音乐
            CreateVolumeRow(section.transform, "背景音乐", MusicVolumeKey, 0.6f, font, OnMusicVolumeChanged);
            // 音效
            CreateVolumeRow(section.transform, "音效", SFXVolumeKey, 0.7f, font, OnSFXVolumeChanged);
        }

        private GameObject CreateLabel(Transform parent, string text, Font font, int fontSize)
        {
            var go = new GameObject("Label");
            var t = go.AddComponent<Text>();
            t.text = text;
            t.font = font;
            t.fontSize = fontSize;
            t.color = Color.white;
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(-1, 24);
            return go;
        }

        private void CreateVolumeRow(Transform parent, string label, string prefKey, float defaultValue, Font font, System.Action<float> onChanged)
        {
            var row = new GameObject(label + "Row");
            row.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            row.transform.SetParent(parent, false);

            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlHeight = true;
            hlg.childControlWidth = false;
            hlg.childForceExpandWidth = false;

            var rowRt = row.GetComponent<RectTransform>();
            rowRt.sizeDelta = new Vector2(-1, 36);

            var lbl = CreateLabel(row.transform, label, font, 18);
            var lblLayout = lbl.AddComponent<LayoutElement>();
            lblLayout.preferredWidth = 80;
            lblLayout.preferredHeight = 28;

            var sliderObj = new GameObject("Slider");
            sliderObj.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            sliderObj.transform.SetParent(row.transform, false);
            var slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = PlayerPrefs.GetFloat(prefKey, defaultValue);
            slider.onValueChanged.AddListener(v => onChanged?.Invoke(v));

            var sliderRt = sliderObj.GetComponent<RectTransform>();
            var sliderLayout = sliderObj.AddComponent<LayoutElement>();
            sliderLayout.flexibleWidth = 1;
            sliderLayout.preferredWidth = 200;
            sliderLayout.preferredHeight = 24;

            // Slider 背景
            var bg = new GameObject("Background");
            bg.transform.SetParent(sliderObj.transform, false);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.25f, 0.35f, 0.9f);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0, 0.25f);
            bgRt.anchorMax = new Vector2(1, 0.75f);
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

            // Fill Area
            var fillArea = new GameObject("Fill Area");
            fillArea.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            fillArea.transform.SetParent(sliderObj.transform, false);
            var fillAreaRt = fillArea.GetComponent<RectTransform>();
            fillAreaRt.anchorMin = new Vector2(0, 0.25f);
            fillAreaRt.anchorMax = new Vector2(1, 0.75f);
            fillAreaRt.offsetMin = new Vector2(5, 0);
            fillAreaRt.offsetMax = new Vector2(-5, 0);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.4f, 0.55f, 0.8f, 0.95f);
            slider.fillRect = fill.GetComponent<RectTransform>();
            var fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;

            // Handle
            var handleArea = new GameObject("Handle Slide Area");
            handleArea.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            handleArea.transform.SetParent(sliderObj.transform, false);
            var handleAreaRt = handleArea.GetComponent<RectTransform>();
            handleAreaRt.anchorMin = Vector2.zero;
            handleAreaRt.anchorMax = Vector2.one;
            handleAreaRt.offsetMin = new Vector2(10, 0);
            handleAreaRt.offsetMax = new Vector2(-10, 0);

            var handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            var handleImg = handle.AddComponent<Image>();
            handleImg.color = Color.white;
            var handleRt = handle.GetComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(20, 0);
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImg;
        }

        private void BindSoundSection(Transform content)
        {
            var section = content.Find("SoundSection");
            if (section == null) return;

            var musicRow = section.Find("背景音乐Row");
            var sfxRow = section.Find("音效Row");
            if (musicRow != null)
            {
                var slider = musicRow.Find("Slider")?.GetComponent<Slider>();
                if (slider != null)
                {
                    slider.value = PlayerPrefs.GetFloat(MusicVolumeKey, 0.6f);
                    slider.onValueChanged.AddListener(OnMusicVolumeChanged);
                }
            }
            if (sfxRow != null)
            {
                var slider = sfxRow.Find("Slider")?.GetComponent<Slider>();
                if (slider != null)
                {
                    slider.value = PlayerPrefs.GetFloat(SFXVolumeKey, 0.7f);
                    slider.onValueChanged.AddListener(OnSFXVolumeChanged);
                }
            }
        }

        private void OnMusicVolumeChanged(float v)
        {
            PlayerPrefs.SetFloat(MusicVolumeKey, v);
            PlayerPrefs.Save();
            var bg = FindObjectOfType<GlobalBackgroundMusic>();
            if (bg != null) bg.SetVolume(v);
        }

        private void OnSFXVolumeChanged(float v)
        {
            PlayerPrefs.SetFloat(SFXVolumeKey, v);
            PlayerPrefs.Save();
        }

        /// <summary>供 MenuAudio 等读取音效音量。</summary>
        public static float GetSFXVolume()
        {
            return PlayerPrefs.GetFloat(SFXVolumeKey, 0.7f);
        }
    }
}

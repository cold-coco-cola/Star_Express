using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace Game.Scripts.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("References")]
        public GameObject menuContainer;
        public Text versionText;

        [Tooltip("点击 Start 后加载的场景（关卡选择）")]
        public string levelSelectSceneName = "LevelSelect";

        public static MainMenuController Instance { get; private set; }

        private Transform _root;

        private void Awake()
        {
            Instance = this;
            _root = transform;
            if (versionText != null)
                versionText.text = "v" + Application.version;
        }

        private void Start()
        {
            EnsureFadeComponents();
            NormalizeStartButton();
            NormalizeSettingsButton();
            BindMenuButtons();
            BindPopupPanels();
        }

        private void NormalizeSettingsButton()
        {
            if (menuContainer == null) return;
            var btn = menuContainer.transform.Find("设置Button") ?? menuContainer.transform.Find("选项Button");
            if (btn == null) return;
            var text = btn.Find("Text")?.GetComponent<Text>();
            if (text != null) text.text = "设置";
        }

        private void NormalizeStartButton()
        {
            if (menuContainer == null) return;
            foreach (Transform t in menuContainer.transform)
            {
                var outline = t.GetComponent<Outline>();
                if (outline != null) UnityEngine.Object.Destroy(outline);
            }
        }

        private void EnsureFadeComponents()
        {
            if (GetComponent<MainMenuFadeIn>() == null)
                gameObject.AddComponent<MainMenuFadeIn>();
            if (GetComponent<MenuAudio>() == null)
                gameObject.AddComponent<MenuAudio>();
            var bgChild = transform.Find("BackgroundMusic");
            if (bgChild == null)
            {
                bgChild = new GameObject("BackgroundMusic").transform;
                bgChild.SetParent(transform);
            }
            if (bgChild.GetComponent<GlobalBackgroundMusic>() == null)
                bgChild.gameObject.AddComponent<GlobalBackgroundMusic>();
            if (menuContainer != null && menuContainer.GetComponent<MenuEntryAnim>() == null)
                menuContainer.AddComponent<MenuEntryAnim>();
            if (GetComponent<SettingsPanelController>() == null)
                gameObject.AddComponent<SettingsPanelController>();
            foreach (var name in new[] { "OptionsPanel", "CreditsPanel", "QuitConfirmPanel" })
            {
                var panel = _root.Find(name);
                if (panel != null && panel.GetComponent<PanelFadeAnim>() == null)
                    panel.gameObject.AddComponent<PanelFadeAnim>();
            }
        }

        private void BindMenuButtons()
        {
            if (menuContainer == null) return;
            BindButton("开始游戏Button", OnStartGameClicked);
            BindButton("设置Button", OnOptionsClicked);
            BindButton("选项Button", OnOptionsClicked); // 兼容旧场景
            BindButton("制作人员Button", OnCreditsClicked);
            BindButton("退出Button", OnQuitClicked);
        }

        private void BindButton(string buttonName, UnityAction handler)
        {
            var t = menuContainer.transform.Find(buttonName);
            if (t == null) return;
            var btn = t.GetComponent<Button>();
            if (btn != null)
            {
                if (handler != null)
                    btn.onClick.AddListener(handler);
                var audio = GetComponent<MenuAudio>();
                if (audio != null)
                    btn.onClick.AddListener(audio.PlayClick);
            }
        }

        private void BindPopupPanels()
        {
            BindPanelClose("OptionsPanel");
            BindPanelClose("CreditsPanel");
            BindPanelClose("QuitConfirmPanel", isQuit: true);
        }

        private void BindPanelClose(string panelName, bool isQuit = false)
        {
            var panel = _root.Find(panelName);
            if (panel == null) return;
            var audio = GetComponent<MenuAudio>();
            var overlay = panel.Find("Overlay");
            if (overlay != null)
            {
                var overlayBtn = overlay.GetComponent<Button>();
                if (overlayBtn != null)
                {
                    EnsureMenuButtonStyle(overlay.gameObject, isOverlay: true);
                    overlayBtn.onClick.AddListener(() => HidePanel(panelName));
                    if (audio != null) overlayBtn.onClick.AddListener(audio.PlayClick);
                }
            }
            var closeBtn = panel.Find("Box/CloseButton")?.GetComponent<Button>();
            if (closeBtn != null)
            {
                EnsureMenuButtonStyle(closeBtn.gameObject, isOverlay: false);
                if (isQuit)
                    closeBtn.onClick.AddListener(OnQuitConfirmed);
                else
                    closeBtn.onClick.AddListener(() => HidePanel(panelName));
                if (audio != null) closeBtn.onClick.AddListener(audio.PlayClick);
            }
        }

        public void OnStartGameClicked()
        {
            if (!string.IsNullOrEmpty(levelSelectSceneName))
                SceneManager.LoadScene(levelSelectSceneName);
            else if (SceneManager.sceneCountInBuildSettings > 1)
                SceneManager.LoadScene(1);
            else
                Debug.LogWarning("[MainMenu] 未找到关卡选择场景，请检查 Build Settings。");
        }

        public void OnOptionsClicked()
        {
            ShowPanel("OptionsPanel");
        }

        public void OnCreditsClicked()
        {
            ShowPanel("CreditsPanel");
        }

        public void OnQuitClicked()
        {
            ShowPanel("QuitConfirmPanel");
        }

        public void OnQuitConfirmed()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        public void ShowPanel(string panelName)
        {
            var panel = _root.Find(panelName);
            if (panel != null)
                panel.gameObject.SetActive(true);
        }

        /// <summary>确保按钮具有与 StartMenu 一致的 MenuButton 交互（音效+视效）。</summary>
        private void EnsureMenuButtonStyle(GameObject buttonGo, bool isOverlay)
        {
            if (buttonGo.GetComponent<MenuButton>() == null)
            {
                var mb = buttonGo.AddComponent<MenuButton>();
                if (isOverlay)
                {
                    var img = buttonGo.GetComponent<UnityEngine.UI.Image>();
                    if (img != null) mb.normalBgColor = img.color;
                    mb.hoverBgColor = new Color(0.15f, 0.15f, 0.18f, 0.4f);
                    mb.scaleMultiplier = 1f;
                }
                else if (buttonGo.GetComponent<ButtonClickAnim>() == null)
                {
                    buttonGo.AddComponent<ButtonClickAnim>();
                }
            }
        }

        public void HidePanel(string panelName)
        {
            var panel = _root.Find(panelName);
            if (panel == null) return;
            var fade = panel.GetComponent<PanelFadeAnim>();
            if (fade != null)
                fade.HideWithFade();
            else
                panel.gameObject.SetActive(false);
        }
    }
}

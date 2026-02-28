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
        [Tooltip("留空则自动查找")]
        [SerializeField] private Transform _optionsPanel;
        [SerializeField] private Transform _creditsPanel;
        [SerializeField] private Transform _quitConfirmPanel;

        [Tooltip("点击 Start 后加载的场景（关卡选择）")]
        public string levelSelectSceneName = "LevelSelect";

        public static MainMenuController Instance { get; private set; }

        private Transform _root;

        private void Awake()
        {
            Instance = this;
            _root = transform;
            EnsureCustomCursor();
        }

        private void EnsureCustomCursor()
        {
            if (GetComponent<CustomCursor>() == null)
                gameObject.AddComponent<CustomCursor>();
        }

        private void Start()
        {
            ResolvePanelRefs();
            EnsureFadeComponents();
            BindMenuButtons();
            BindPopupPanels();
        }

        private void ResolvePanelRefs()
        {
            if (_optionsPanel == null) _optionsPanel = _root.Find("OptionsPanel");
            if (_creditsPanel == null) _creditsPanel = _root.Find("CreditsPanel");
            if (_quitConfirmPanel == null) _quitConfirmPanel = _root.Find("QuitConfirmPanel");
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
            var spc = GetComponent<SettingsPanelController>();
            if (spc == null) spc = gameObject.AddComponent<SettingsPanelController>();
            if (spc != null && spc.optionsPanel == null && _optionsPanel != null)
                spc.optionsPanel = _optionsPanel;
            foreach (var panel in new[] { GetPanel("OptionsPanel"), GetPanel("CreditsPanel"), GetPanel("QuitConfirmPanel") })
            {
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
            BindPanelClose(GetPanel("OptionsPanel"), isQuit: false);
            BindPanelClose(GetPanel("CreditsPanel"), isQuit: false);
            BindPanelClose(GetPanel("QuitConfirmPanel"), isQuit: true);
        }

        private Transform GetPanel(string name)
        {
            if (name == "OptionsPanel" && _optionsPanel != null) return _optionsPanel;
            if (name == "CreditsPanel" && _creditsPanel != null) return _creditsPanel;
            if (name == "QuitConfirmPanel" && _quitConfirmPanel != null) return _quitConfirmPanel;
            return _root.Find(name);
        }

        private void BindPanelClose(Transform panel, bool isQuit = false)
        {
            if (panel == null) return;
            var audio = GetComponent<MenuAudio>();
            var overlay = panel.Find("Overlay");
            if (overlay != null)
            {
                var overlayBtn = overlay.GetComponent<Button>();
                if (overlayBtn != null)
                {
                    EnsureMenuButtonStyle(overlay.gameObject, isOverlay: true);
                    overlayBtn.onClick.RemoveAllListeners();
                    overlayBtn.onClick.AddListener(() => HidePanelInternal(panel));
                    if (audio != null) overlayBtn.onClick.AddListener(audio.PlayClick);
                }
            }
            var box = panel.Find("Box") ?? panel.Find("Box ");
            var closeBtn = box?.Find("CloseButton")?.GetComponent<Button>();
            if (closeBtn != null)
            {
                EnsureMenuButtonStyle(closeBtn.gameObject, isOverlay: false);
                closeBtn.onClick.RemoveAllListeners();
                if (isQuit)
                    closeBtn.onClick.AddListener(OnQuitConfirmed);
                else
                    closeBtn.onClick.AddListener(() => HidePanelInternal(panel));
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
            ShowPanel(GetPanel("OptionsPanel"));
        }

        public void OnCreditsClicked()
        {
            ShowPanel(GetPanel("CreditsPanel"));
        }

        public void OnQuitClicked()
        {
            ShowPanel(GetPanel("QuitConfirmPanel"));
        }

        public void OnQuitConfirmed()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        public void ShowPanel(Transform panel)
        {
            if (panel != null)
                panel.gameObject.SetActive(true);
        }

        /// <summary>确保按钮具有 MenuButton 交互（音效+视效）。仅补充缺失组件，不覆盖场景中的值。</summary>
        private void EnsureMenuButtonStyle(GameObject buttonGo, bool isOverlay)
        {
            var mb = buttonGo.GetComponent<MenuButton>();
            if (mb == null)
            {
                var img = buttonGo.GetComponent<UnityEngine.UI.Image>();
                mb = buttonGo.AddComponent<MenuButton>();
                if (img != null) mb.normalBgColor = img.color;
                if (isOverlay)
                {
                    mb.hoverBgColor = new Color(0.15f, 0.15f, 0.18f, 0.4f);
                    mb.scaleMultiplier = 1f;
                }
                else if (buttonGo.GetComponent<ButtonClickAnim>() == null)
                {
                    buttonGo.AddComponent<ButtonClickAnim>();
                }
            }
        }

        public void HidePanel(Transform panel)
        {
            if (panel != null) HidePanelInternal(panel);
        }

        /// <summary>供 MainMenuBuilder 在创建 UI 时写入引用，便于持久化到场景。</summary>
        public void SetPanelRefs(Transform options, Transform credits, Transform quit)
        {
            _optionsPanel = options;
            _creditsPanel = credits;
            _quitConfirmPanel = quit;
        }

        private void HidePanelInternal(Transform panel)
        {
            if (panel == null) return;
            var fade = panel.GetComponent<PanelFadeAnim>();
            if (fade != null)
                fade.HideWithFade();
            else
                panel.gameObject.SetActive(false);
        }
    }
}

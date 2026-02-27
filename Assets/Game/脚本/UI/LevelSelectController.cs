using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace Game.Scripts.UI
{
    /// <summary>关卡选择界面：运行时构建 UI，点击关卡进入对应场景。</summary>
    public class LevelSelectController : MonoBehaviour
    {
        [System.Serializable]
        public struct LevelEntry
        {
            public string displayName;
            public string sceneName;
        }

        [Header("关卡列表")]
        public LevelEntry[] levels = new LevelEntry[]
        {
            new LevelEntry { displayName = "第一关：太阳系", sceneName = "SolarSystem_01" }
        };

        [Header("场景名")]
        public string backSceneName = "StartMenu";

        [Header("由 LevelSelectBuilder 赋值")]
        public GameObject levelContainer;

        private void Awake()
        {
            // 隐藏游戏 UI，保持关卡选择界面纯净
            var gameCanvas = GameObject.Find("GameCanvas");
            if (gameCanvas != null) gameCanvas.SetActive(false);
            EnsureMenuAudio();
            EnsureCamera();
            BuildUI();
        }

        private void EnsureMenuAudio()
        {
            var root = GetComponentInParent<Canvas>()?.gameObject ?? gameObject;
            if (root.GetComponent<MenuAudio>() == null)
            {
                if (root.GetComponent<AudioSource>() == null)
                    root.AddComponent<AudioSource>();
                root.AddComponent<MenuAudio>();
            }
        }

        private void Start()
        {
            // 若由 LevelSelectBuilder 构建，则绑定按钮
            if (levelContainer != null)
                BindLevelButtons();
        }

        private void BindLevelButtons()
        {
            if (levels == null || levels.Length == 0) return;
            var audio = GetComponentInParent<MenuAudio>();
            for (int i = 0; i < levelContainer.transform.childCount && i < levels.Length; i++)
            {
                var btnGo = levelContainer.transform.GetChild(i).gameObject;
                var btn = btnGo.GetComponent<Button>();
                if (btn != null)
                {
                    EnsureMenuButtonStyle(btnGo);
                    var sceneName = levels[i].sceneName;
                    btn.onClick.RemoveAllListeners();
                    if (audio != null) btn.onClick.AddListener(audio.PlayClick);
                    btn.onClick.AddListener(() => SceneManager.LoadScene(sceneName));
                }
            }
            var backBtn = transform.Find("BackButton")?.GetComponent<Button>();
            if (backBtn != null)
            {
                EnsureMenuButtonStyle(backBtn.gameObject);
                backBtn.onClick.RemoveAllListeners();
                if (audio != null) backBtn.onClick.AddListener(audio.PlayClick);
                backBtn.onClick.AddListener(() => SceneManager.LoadScene(backSceneName));
            }
        }

        private void EnsureMenuButtonStyle(GameObject btnGo)
        {
            if (btnGo.GetComponent<MenuButton>() == null)
            {
                var mb = btnGo.AddComponent<MenuButton>();
                mb.hoverBgColor = new Color(0.8943129f, 0.8943129f, 0.9339623f, 0.93333334f);
                mb.normalBgColor = new Color(1f, 1f, 1f, 1f);
                mb.highlightColor = new Color(0.8867924f, 0.7494347f, 0.29699183f, 1f);
                mb.scaleMultiplier = 1.05f;
                mb.transitionDuration = 0.15f;
                var t = btnGo.GetComponentInChildren<Text>();
                if (t != null) mb.buttonText = t;
            }
            if (btnGo.GetComponent<ButtonClickAnim>() == null)
                btnGo.AddComponent<ButtonClickAnim>();
            if (btnGo.GetComponent<Outline>() == null)
            {
                var outline = btnGo.AddComponent<Outline>();
                outline.effectColor = new Color(0.3f, 0.35f, 0.5f, 0.5f);
                outline.effectDistance = new Vector2(0, -1);
            }
            var btn = btnGo.GetComponent<Button>();
            if (btn != null && btn.transition == Selectable.Transition.ColorTint)
            {
                var colors = btn.colors;
                colors.colorMultiplier = 2.64f;
                btn.colors = colors;
            }
        }

        private void EnsureCamera()
        {
            var cam = FindObjectOfType<Camera>();
            if (cam != null && cam.CompareTag("MainCamera")) return;
            if (cam != null) { cam.tag = "MainCamera"; return; }

            var camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.08f, 1f);
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            camObj.AddComponent<AudioListener>();
        }

        private void BuildUI()
        {
            // 若已有 LevelSelectCanvas 则跳过（避免重复）
            if (GameObject.Find("LevelSelectCanvas") != null) return;
            // 若存在 LevelSelectBuilder，由其负责构建（支持编辑模式预构建）
            if (GetComponent<LevelSelectBuilder>() != null) return;

            var canvasObj = new GameObject("LevelSelectCanvas");
            if (canvasObj.GetComponent<MenuAudio>() == null)
                canvasObj.AddComponent<MenuAudio>();
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            var root = new GameObject("Root");
            root.transform.SetParent(canvasObj.transform, false);
            var rootRt = root.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            // 背景
            var bg = CreateImage(root.transform, "Background", new Color(0.02f, 0.02f, 0.08f, 1f));
            SetFullRect(bg.rectTransform);

            // 标题
            var title = CreateText(root.transform, "Title", "选择关卡", 36);
            var titleRt = title.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0, -80);
            titleRt.sizeDelta = new Vector2(400, 60);
            title.alignment = TextAnchor.MiddleCenter;

            // 关卡按钮容器
            var container = new GameObject("LevelContainer");
            container.transform.SetParent(root.transform, false);
            var vlg = container.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 20;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlHeight = false;
            vlg.childControlWidth = false;
            var containerRt = container.GetComponent<RectTransform>();
            containerRt.anchorMin = new Vector2(0.5f, 0.5f);
            containerRt.anchorMax = new Vector2(0.5f, 0.5f);
            containerRt.pivot = new Vector2(0.5f, 0.5f);
            containerRt.anchoredPosition = Vector2.zero;
            containerRt.sizeDelta = new Vector2(400, 400);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            for (int i = 0; i < levels.Length; i++)
            {
                var entry = levels[i];
                var btn = CreateLevelButton(container.transform, entry.displayName, entry.sceneName, font);
            }

            // 返回按钮
            var backBtn = CreateLevelButton(root.transform, "返回", backSceneName, font);
            var backRt = backBtn.GetComponent<RectTransform>();
            backRt.anchorMin = new Vector2(0, 0);
            backRt.anchorMax = new Vector2(0, 0);
            backRt.pivot = new Vector2(0, 0);
            backRt.anchoredPosition = new Vector2(80, 80);
            backRt.sizeDelta = new Vector2(200, 56);
        }

        private GameObject CreateLevelButton(Transform parent, string label, string sceneName, Font font)
        {
            var go = new GameObject(label + "Button");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);

            var btn = go.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;

            var menuBtn = go.AddComponent<MenuButton>();
            menuBtn.highlightColor = new Color(1f, 0.6f, 0.2f, 1f);
            go.AddComponent<ButtonClickAnim>();

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(go.transform, false);
            var t = textObj.AddComponent<Text>();
            t.text = label;
            t.font = font;
            t.fontSize = 28;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleLeft;
            var shadow = textObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.5f);
            shadow.effectDistance = new Vector2(1, -1);

            menuBtn.buttonText = t;

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(320, 56);
            var textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(20, 0);
            textRt.offsetMax = Vector2.zero;

            var audio = go.GetComponentInParent<MenuAudio>();
            if (audio != null) btn.onClick.AddListener(audio.PlayClick);
            btn.onClick.AddListener(() => SceneManager.LoadScene(sceneName));
            return go;
        }

        private Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private Text CreateText(Transform parent, string name, string content, int fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.text = content;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.fontSize = fontSize;
            t.color = Color.white;
            return t;
        }

        private void SetFullRect(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}

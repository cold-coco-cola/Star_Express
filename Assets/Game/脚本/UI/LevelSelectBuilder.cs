using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace Game.Scripts.UI
{
    /// <summary>关卡选择界面 UI 构建器。编辑模式与运行时自动构建，打开场景即可见。</summary>
    [ExecuteAlways]
    public class LevelSelectBuilder : MonoBehaviour
    {
        public MainMenuStyle style;

        public void BuildUI()
        {
#if UNITY_EDITOR
            RemoveGameUIFromScene();
#endif

            var children = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in transform) children.Add(child.gameObject);
            foreach (var child in children) DestroyImmediate(child);

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                var canvasObj = new GameObject("LevelSelectCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                var scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                canvasObj.AddComponent<GraphicRaycaster>();
                transform.SetParent(canvasObj.transform, false);

                if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
                {
                    var es = new GameObject("EventSystem");
                    es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }
            }

            EnsureMainCamera();
            CreateBackground();
            CreateTitle();
            CreateLevelButtons();
            CreateBackButton();
            EnsureController();
        }

        private void CreateBackground()
        {
            var go = new GameObject("Background");
            go.transform.SetParent(transform, false);
            var img = go.AddComponent<Image>();
            img.color = style != null ? style.backgroundColor : new Color(0.05f, 0.05f, 0.1f, 1f);
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private void CreateTitle()
        {
            var go = new GameObject("Title");
            go.transform.SetParent(transform, false);
            var t = go.AddComponent<Text>();
            t.text = "选择关卡";
            t.font = GetFont();
            t.fontSize = 64;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = style != null ? style.textColor : Color.white;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -80);
            rt.sizeDelta = new Vector2(400, 80);
        }

        private void CreateLevelButtons()
        {
            var controller = GetComponent<LevelSelectController>();
            if (controller == null) controller = gameObject.AddComponent<LevelSelectController>();

            var container = new GameObject("LevelContainer");
            container.transform.SetParent(transform, false);
            var vlg = container.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 24;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlHeight = vlg.childControlWidth = false;
            vlg.childForceExpandHeight = vlg.childForceExpandWidth = false;

            var rt = container.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(480, 400);

            controller.levelContainer = container;

            var entries = controller.levels;
            if (entries == null || entries.Length == 0)
                entries = new LevelSelectController.LevelEntry[] { new LevelSelectController.LevelEntry { displayName = "第一关 太阳系", sceneName = "SolarSystem_01" } };

            for (int i = 0; i < entries.Length; i++)
            {
                CreateLevelButton(container.transform, i, entries[i].displayName);
            }
        }

        private void CreateLevelButton(Transform parent, int index, string text)
        {
            var go = new GameObject($"Level_{index}_Button");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);

            var btn = go.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;

            var menuBtn = go.AddComponent<MenuButton>();
            if (style != null) menuBtn.highlightColor = style.highlightColor;
            go.AddComponent<ButtonClickAnim>();

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var t = textGo.AddComponent<Text>();
            t.text = text;
            t.font = GetFont();
            t.fontSize = style != null ? style.fontSize : 32;
            t.color = style != null ? style.textColor : Color.white;
            t.alignment = TextAnchor.MiddleLeft;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            var shadow = textGo.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.5f);
            shadow.effectDistance = new Vector2(1, -1);

            menuBtn.buttonText = t;

            var btnRt = go.GetComponent<RectTransform>();
            btnRt.sizeDelta = new Vector2(400, 72);

            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(20, 0);
            textRt.offsetMax = Vector2.zero;
        }

        private void CreateBackButton()
        {
            var go = new GameObject("BackButton");
            go.transform.SetParent(transform, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);

            var btn = go.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;

            var menuBtn = go.AddComponent<MenuButton>();
            if (style != null) menuBtn.highlightColor = style.highlightColor;
            go.AddComponent<ButtonClickAnim>();

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var t = textGo.AddComponent<Text>();
            t.text = "返回";
            t.font = GetFont();
            t.fontSize = 28;
            t.color = style != null ? style.textColor : Color.white;
            t.alignment = TextAnchor.MiddleLeft;
            var shadow = textGo.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.5f);
            shadow.effectDistance = new Vector2(1, -1);

            menuBtn.buttonText = t;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = new Vector2(120, 80);
            rt.sizeDelta = new Vector2(160, 48);

            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(20, 0);
            textRt.offsetMax = Vector2.zero;
        }

        private void EnsureController()
        {
            if (GetComponent<LevelSelectController>() == null)
                gameObject.AddComponent<LevelSelectController>();
            if (GetComponent<MenuAudio>() == null)
                gameObject.AddComponent<MenuAudio>();
        }

        private void EnsureMainCamera()
        {
            var cam = FindObjectOfType<Camera>();
            if (cam != null && cam.CompareTag("MainCamera")) return;
            if (cam != null) { cam.tag = "MainCamera"; return; }

            var camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.1f, 1f);
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            camObj.AddComponent<AudioListener>();
        }

#if UNITY_EDITOR
        private void RemoveGameUIFromScene()
        {
            var gc = GameObject.Find("GameCanvas");
            if (gc != null) DestroyImmediate(gc);
        }
#endif

        private Font GetFont()
        {
            if (style != null && style.mainFont != null) return style.mainFont;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null) font = Resources.FindObjectsOfTypeAll<Font>().FirstOrDefault(f => f.name == "Arial");
            return font;
        }

        private void OnEnable()
        {
            if (transform.childCount == 0)
                BuildUI();
        }
    }
}

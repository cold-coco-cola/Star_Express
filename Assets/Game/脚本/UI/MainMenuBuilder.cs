#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Linq;

namespace Game.Scripts.UI
{
    /// <summary>
    /// 主菜单 UI：仅在完全为空时创建初始结构。
    /// 之后请直接手动编辑布局，保存场景即可，不会覆盖你的调整。
    /// </summary>
    public class MainMenuBuilder : MonoBehaviour
    {
        public MainMenuStyle style;

        [ContextMenu("创建主菜单 UI（仅当完全为空时）")]
        public void BuildUI()
        {
            // 已有 UI 时不再重建，手动编辑的布局即为最终结果
            if (transform.childCount > 0)
            {
#if UNITY_EDITOR
                Debug.Log("[MainMenuBuilder] 已有 UI 元素，不会重建。请直接手动编辑布局，保存场景即可。");
#endif
                return;
            }

#if UNITY_EDITOR
            RemoveGameUIFromScene();
#endif

            // 清理（此时应为空）
            var children = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in transform) children.Add(child.gameObject);
            foreach (var child in children) DestroyImmediate(child);

            // 3. 检查是否需要 Canvas
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                // 如果当前物体没有 Canvas，且父级也没有，创建一个新的 Canvas 物体作为父级或者作为自身组件
                if (GetComponent<Canvas>() == null)
                {
                    GameObject canvasObj = new GameObject("MainMenuCanvas");
                    canvas = canvasObj.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 100;
                    canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
                    canvasObj.AddComponent<GraphicRaycaster>();
                    
                    // 将自身挂载到 Canvas 下
                    transform.SetParent(canvasObj.transform, false);
                    
                    // 添加 EventSystem 如果不存在
                    if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
                    {
                        GameObject eventSystem = new GameObject("EventSystem");
                        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                    }
                }
            }

            EnsureMainCamera();

            // 4. 创建背景层（含粒子星空视频）
            CreateBackground();

            // 5. 创建星球占位
            CreatePlanet();

            // 6. 创建 Logo
            CreateLogo();

            // 7. 创建菜单按钮
            CreateMenuButtons();

            // 8. 创建页脚
            CreateFooter();

            // 9. 创建弹窗占位（选项、制作人员、退出确认）
            CreatePopupPanels();

            // 10. 添加控制器
            EnsureController();
        }

        private void CreateBackground()
        {
            // 底层：纯色或图片背景（视频加载失败时的兜底）
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(transform, false);
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = style != null ? style.backgroundColor : new Color(0.02f, 0.02f, 0.08f, 1f);
            if (style != null && style.backgroundSprite != null)
                bgImg.sprite = style.backgroundSprite;
            bgImg.raycastTarget = false;
            var rt = bgImg.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            // 视频层：粒子星空背景（路径：Assets/Game/美术/Animations/粒子星空.mp4）
            var videoClip = GetBackgroundVideoClip();
            if (videoClip != null)
            {
                var videoObj = new GameObject("VideoBackground");
                videoObj.transform.SetParent(transform, false);
                var rawImg = videoObj.AddComponent<RawImage>();
                rawImg.color = Color.white;
                rawImg.raycastTarget = false;
                var videoRt = rawImg.rectTransform;
                videoRt.anchorMin = Vector2.zero;
                videoRt.anchorMax = Vector2.one;
                videoRt.offsetMin = videoRt.offsetMax = Vector2.zero;
                var handler = videoObj.AddComponent<VideoBackgroundHandler>();
                handler.videoClip = videoClip;
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(videoObj);
#endif
                // 视频上方半透明遮罩，提升前景 UI 可读性
                var overlayObj = new GameObject("VideoOverlay");
                overlayObj.transform.SetParent(transform, false);
                overlayObj.transform.SetSiblingIndex(videoObj.transform.GetSiblingIndex() + 1);
                var overlayImg = overlayObj.AddComponent<Image>();
                overlayImg.color = new Color(0.02f, 0.02f, 0.06f, 0.12f);
                overlayImg.raycastTarget = false;
                var overlayRt = overlayImg.rectTransform;
                overlayRt.anchorMin = Vector2.zero;
                overlayRt.anchorMax = Vector2.one;
                overlayRt.offsetMin = overlayRt.offsetMax = Vector2.zero;
            }
        }

        private VideoClip GetBackgroundVideoClip()
        {
            if (style != null && style.backgroundVideo != null)
                return style.backgroundVideo;
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Game/美术/Animations/粒子星空.mp4");
#else
            return Resources.Load<VideoClip>("Video/粒子星空");
#endif
        }

        private void CreatePlanet()
        {
            GameObject planetObj = new GameObject("PlanetPlaceholder");
            planetObj.transform.SetParent(transform, false);
            Image planetImg = planetObj.AddComponent<Image>();
            if (style != null && style.planetSprite != null)
                planetImg.sprite = style.planetSprite;
            else
                planetImg.color = new Color(0.8f, 0.4f, 0.1f); // 默认橙色星球

            RectTransform rt = planetImg.rectTransform;
            rt.anchorMin = new Vector2(1, 0); // 右下角
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(1, 0);
            rt.anchoredPosition = new Vector2(-80, 80);
            rt.sizeDelta = new Vector2(480, 480);
        }

        private void CreateLogo()
        {
            GameObject logoObj = new GameObject("Logo");
            logoObj.transform.SetParent(transform, false);

            if (style != null && style.logoSprite != null)
            {
                Image logoImg = logoObj.AddComponent<Image>();
                logoImg.sprite = style.logoSprite;
                logoImg.SetNativeSize();
            }
            else
            {
                Text logoText = logoObj.AddComponent<Text>();
                logoText.text = "STAR EXPRESS";
                logoText.font = GetDefaultFont();
                logoText.fontSize = 80;
                logoText.alignment = TextAnchor.UpperLeft;
                logoText.color = style != null ? style.textColor : Color.white;
                var logoOutline = logoObj.AddComponent<Outline>();
                logoOutline.effectColor = new Color(0, 0, 0, 0.8f);
                logoOutline.effectDistance = new Vector2(2, -2);
                var logoShadow = logoObj.AddComponent<Shadow>();
                logoShadow.effectColor = new Color(0.2f, 0.3f, 0.5f, 0.6f);
                logoShadow.effectDistance = new Vector2(4, -4);
            }

            RectTransform rt = logoObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); // 左上角
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(80, -80);
            rt.sizeDelta = new Vector2(480, 120);
        }

        private void CreateMenuButtons()
        {
            GameObject menuContainerObj = new GameObject("MenuContainer");
            menuContainerObj.transform.SetParent(transform, false);

            VerticalLayoutGroup vlg = menuContainerObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = style != null ? style.buttonSpacing : 24;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlHeight = false;
            vlg.childControlWidth = false;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = false;

            RectTransform rt = menuContainerObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.5f); // 左侧居中
            rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot = new Vector2(0, 0.5f);
            rt.anchoredPosition = new Vector2(80, 0);
            rt.sizeDelta = new Vector2(360, 400);

            string[] btnNames = { "开始游戏", "设置", "制作人员", "退出" };
            string[] methodNames = { "OnStartGameClicked", "OnOptionsClicked", "OnCreditsClicked", "OnQuitClicked" };

            MainMenuController controller = FindObjectOfType<MainMenuController>();
            if (controller == null) controller = gameObject.AddComponent<MainMenuController>();
            controller.menuContainer = menuContainerObj;
            menuContainerObj.AddComponent<MenuEntryAnim>();

            for (int i = 0; i < btnNames.Length; i++)
                CreateButton(menuContainerObj.transform, btnNames[i], methodNames[i], controller);
        }

        private void CreateButton(Transform parent, string btnText, string methodName, MainMenuController controller)
        {
            GameObject btnObj = new GameObject(btnText + "Button");
            btnObj.transform.SetParent(parent, false);
            
            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(1f, 1f, 1f, 0f);

            Button btn = btnObj.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            
            MenuButton menuBtn = btnObj.AddComponent<MenuButton>();
            if (style != null)
                menuBtn.highlightColor = style.highlightColor;
            // 添加点击缩放动画，使交互更丝滑
            btnObj.AddComponent<ButtonClickAnim>();

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            Text t = textObj.AddComponent<Text>();
            t.text = btnText;
            t.font = GetDefaultFont();
            t.fontSize = style != null ? style.fontSize : 32;
            t.color = style != null ? style.textColor : Color.white;
            t.alignment = TextAnchor.MiddleLeft;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            var textShadow = textObj.AddComponent<Shadow>();
            textShadow.effectColor = new Color(0, 0, 0, 0.5f);
            textShadow.effectDistance = new Vector2(1, -1);
            
            menuBtn.buttonText = t; // 关联文字组件
            
            RectTransform btnRt = btnObj.GetComponent<RectTransform>();
            float btnHeight = style != null ? style.buttonHeight : 56;
            btnRt.sizeDelta = new Vector2(300, btnHeight);
            
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(20, 0);
            textRt.offsetMax = Vector2.zero;

            // 绑定点击事件 (运行时需要反射或 UnityEvent，这里使用 AddListener 仅运行时有效)
            // 在编辑器下，我们只能提示用户手动绑定，或者使用 UnityEditor.Events.UnityEventTools.AddPersistentListener
#if UNITY_EDITOR
            // 这是一个比较复杂的操作，为了简化，我们仅提示
            Debug.Log($"Button '{btnText}' created. Please assign OnClick event manually to {methodName} in MainMenuController.");
#else
            // 运行时绑定
            // btn.onClick.AddListener(() => controller.SendMessage(methodName));
#endif
        }

        private void CreatePopupPanels()
        {
            var font = GetDefaultFont();
            CreatePopup("OptionsPanel", "设置", font, false, ""); // 内容由 SettingsPanelController 填充
            CreatePopup("CreditsPanel", "制作人员", font, false, "（内容待添加）");
            CreatePopup("QuitConfirmPanel", "确认退出？", font, true, "确定要退出游戏吗？");
        }

        private void CreatePopup(string panelName, string title, Font font, bool isQuit = false, string placeholderText = "（内容待添加）")
        {
            var panel = new GameObject(panelName);
            panel.transform.SetParent(transform, false);
            panel.SetActive(false);
            panel.AddComponent<PanelFadeAnim>();

            var panelRt = panel.AddComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = panelRt.offsetMax = Vector2.zero;

            var overlay = new GameObject("Overlay");
            overlay.transform.SetParent(panel.transform, false);
            var overlayImg = overlay.AddComponent<Image>();
            overlayImg.color = new Color(0, 0, 0, 0.6f);
            overlayImg.raycastTarget = true;
            var overlayRt = overlay.GetComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = overlayRt.offsetMax = Vector2.zero;

            var box = new GameObject("Box");
            box.transform.SetParent(panel.transform, false);
            var boxImg = box.AddComponent<Image>();
            boxImg.color = new Color(0.06f, 0.08f, 0.14f, 0.98f);
            var boxOutline = box.AddComponent<Outline>();
            boxOutline.effectColor = new Color(0.4f, 0.5f, 0.7f, 0.4f);
            boxOutline.effectDistance = new Vector2(0, -2);
            var boxRt = box.GetComponent<RectTransform>();
            boxRt.anchorMin = boxRt.anchorMax = new Vector2(0.5f, 0.5f);
            boxRt.pivot = new Vector2(0.5f, 0.5f);
            boxRt.sizeDelta = new Vector2(480, 320);
            boxRt.anchoredPosition = Vector2.zero;
            if (box.GetComponent<PopupShowAnim>() == null)
                box.AddComponent<PopupShowAnim>();

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(box.transform, false);
            var titleT = titleGo.AddComponent<Text>();
            titleT.text = title;
            titleT.font = font;
            titleT.fontSize = 28;
            titleT.color = Color.white;
            titleT.alignment = TextAnchor.MiddleCenter;
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 1);
            titleRt.anchorMax = new Vector2(1, 1);
            titleRt.pivot = new Vector2(0.5f, 1);
            titleRt.anchoredPosition = new Vector2(0, -24);
            titleRt.sizeDelta = new Vector2(-48, 40);

            var content = new GameObject("Content");
            content.transform.SetParent(box.transform, false);
            var contentRt = content.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 0);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.offsetMin = new Vector2(24, 80);
            contentRt.offsetMax = new Vector2(-24, -24);
            var placeholder = new GameObject("Placeholder");
            placeholder.transform.SetParent(content.transform, false);
            var phText = placeholder.AddComponent<Text>();
            phText.text = placeholderText;
            phText.font = font;
            phText.fontSize = 18;
            phText.color = new Color(1, 1, 1, 0.4f);
            phText.alignment = TextAnchor.MiddleCenter;
            var phRt = placeholder.GetComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero;
            phRt.anchorMax = Vector2.one;
            phRt.offsetMin = phRt.offsetMax = Vector2.zero;

            var closeBtn = new GameObject("CloseButton");
            closeBtn.transform.SetParent(box.transform, false);
            var closeImg = closeBtn.AddComponent<Image>();
            closeImg.color = new Color(0.18f, 0.25f, 0.38f, 0.95f);
            var closeOutline = closeBtn.AddComponent<Outline>();
            closeOutline.effectColor = new Color(0.5f, 0.6f, 0.8f, 0.3f);
            closeOutline.effectDistance = new Vector2(0, -1);
            var closeBtnComp = closeBtn.AddComponent<Button>();
            var closeRt = closeBtn.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.5f, 0);
            closeRt.anchorMax = new Vector2(0.5f, 0);
            closeRt.pivot = new Vector2(0.5f, 0);
            closeRt.anchoredPosition = new Vector2(0, 24);
            closeRt.sizeDelta = new Vector2(160, 48);
            closeBtn.AddComponent<ButtonClickAnim>();

            var closeText = new GameObject("Text");
            closeText.transform.SetParent(closeBtn.transform, false);
            var closeT = closeText.AddComponent<Text>();
            closeT.text = isQuit ? "退出" : "关闭";
            closeT.font = font;
            closeT.fontSize = 24;
            closeT.color = Color.white;
            closeT.alignment = TextAnchor.MiddleCenter;
            var closeTextRt = closeText.GetComponent<RectTransform>();
            closeTextRt.anchorMin = Vector2.zero;
            closeTextRt.anchorMax = Vector2.one;
            closeTextRt.offsetMin = closeTextRt.offsetMax = Vector2.zero;

            overlay.AddComponent<Button>().targetGraphic = overlayImg;
        }

        private void CreateFooter()
        {
            GameObject footerObj = new GameObject("VersionText");
            footerObj.transform.SetParent(transform, false);
            Text t = footerObj.AddComponent<Text>();
            t.text = "v0.1.0";
            t.font = GetDefaultFont();
            t.fontSize = 20;
            t.color = new Color(1, 1, 1, 0.5f);
            t.alignment = TextAnchor.LowerLeft;

            RectTransform rt = footerObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.anchoredPosition = new Vector2(24, 24);
            rt.sizeDelta = new Vector2(120, 24);
            
            MainMenuController controller = GetComponent<MainMenuController>();
            if (controller != null) controller.versionText = t;
        }

        private void EnsureController()
        {
            if (GetComponent<MainMenuController>() == null)
                gameObject.AddComponent<MainMenuController>();
            if (GetComponent<MainMenuFadeIn>() == null)
                gameObject.AddComponent<MainMenuFadeIn>();
        }

#if UNITY_EDITOR
        private void RemoveGameUIFromScene()
        {
            var gameCanvas = GameObject.Find("GameCanvas");
            if (gameCanvas != null)
            {
                DestroyImmediate(gameCanvas);
            }
        }
#endif

        private void EnsureMainCamera()
        {
            var cam = FindObjectOfType<Camera>();
            if (cam != null && cam.CompareTag("MainCamera")) return;

            if (cam != null)
            {
                cam.tag = "MainCamera";
                return;
            }

            var camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.08f, 1f);
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            camObj.AddComponent<AudioListener>();
        }

        private Font GetDefaultFont()
        {
            if (style != null && style.mainFont != null) return style.mainFont;
            
            // 尝试加载内置字体
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null) font = Resources.FindObjectsOfTypeAll<Font>().FirstOrDefault(f => f.name == "Arial");
            
            return font;
        }
    }
}

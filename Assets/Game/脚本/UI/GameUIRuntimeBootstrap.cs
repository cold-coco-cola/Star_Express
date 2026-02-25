using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 运行时确保必要 UI 存在。AutoSetupGameUI 仅在编辑器中运行，
/// 若场景未保存弹窗等 UI，运行时由此脚本补全。
/// 每次加载场景时执行，确保关卡场景有暂停键等 UI。
/// </summary>
public class GameUIRuntimeBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        var go = new GameObject("GameUIRuntimeBootstrap");
        DontDestroyOnLoad(go);
        var b = go.AddComponent<GameUIRuntimeBootstrap>();
        SceneManager.sceneLoaded += (_, __) => b.EnsureUI();
        b.EnsureUI();
    }

    private void EnsureUI()
    {
        EnsureWeekRewardSelectionPopup();
        EnsureGameOverPopup();
        EnsurePauseMenu();
        EnsurePauseButton();
    }

    private static void EnsureWeekRewardSelectionPopup()
    {
        if (Object.FindObjectOfType<WeekRewardSelectionPopup>() != null) return;

        var canvas = GameObject.Find("GameCanvas");
        if (canvas == null) return;

        var panel = new GameObject("WeekRewardSelectionPopup");
        panel.transform.SetParent(canvas.transform, false);

        var pr = panel.AddComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(420, 280);
        pr.anchoredPosition = Vector2.zero;

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.08f, 0.12f, 0.98f);
        bg.raycastTarget = true;

        var weekT = CreateText(panel.transform, "WeekText", "第 1 周", new Vector2(0, 80), new Vector2(380, 36), 26);
        var hintT = CreateText(panel.transform, "HintText", "选择 1 项奖励", new Vector2(0, 40), new Vector2(380, 24), 18);

        float optY = -20, optW = 140, optH = 50, gap = 20;
        var opt1 = CreateOptionButton(panel.transform, "Option1", "客舱", new Vector2(-optW * 0.5f - gap * 0.5f, optY), new Vector2(optW, optH));
        var opt2 = CreateOptionButton(panel.transform, "Option2", "星隧", new Vector2(optW * 0.5f + gap * 0.5f, optY), new Vector2(optW, optH));

        var comp = panel.AddComponent<WeekRewardSelectionPopup>();
        comp.weekText = weekT;
        comp.hintText = hintT;
        comp.option1Button = opt1;
        comp.option1Label = opt1.GetComponentInChildren<Text>();
        comp.option2Button = opt2;
        comp.option2Label = opt2.GetComponentInChildren<Text>();

        AddButtonClickAnim(opt1, opt2);
        opt1.gameObject.AddComponent<GameplayButtonHoverSound>();
        opt2.gameObject.AddComponent<GameplayButtonHoverSound>();
        panel.AddComponent<PopupShowAnim>();
        panel.SetActive(false);
        Debug.Log("[GameUIRuntimeBootstrap] 已创建 WeekRewardSelectionPopup");
    }

    private static void EnsureGameOverPopup()
    {
        if (Object.FindObjectOfType<GameOverPopup>() != null) return;

        var canvas = GameObject.Find("GameCanvas");
        if (canvas == null) return;

        var panel = new GameObject("GameOverPopup");
        panel.transform.SetParent(canvas.transform, false);

        var pr = panel.AddComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(380, 240);
        pr.anchoredPosition = Vector2.zero;

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.06f, 0.08f, 0.98f);
        bg.raycastTarget = true;

        var title = CreateText(panel.transform, "TitleText", "游戏结束", new Vector2(0, 70), new Vector2(320, 36), 24);
        var scoreT = CreateText(panel.transform, "ScoreText", "得分: 0", new Vector2(0, 30), new Vector2(320, 28), 18);
        var reasonT = CreateText(panel.transform, "ReasonText", "站点拥挤超阈值", new Vector2(0, -10), new Vector2(320, 40), 16);

        var retryBtn = CreateButton(panel.transform, "RetryButton", "重试", new Vector2(-70, -70), new Vector2(100, 36), new Color(0.3f, 0.6f, 0.3f));
        var backBtn = CreateButton(panel.transform, "BackButton", "返回", new Vector2(70, -70), new Vector2(100, 36), new Color(0.5f, 0.3f, 0.3f));

        var comp = panel.AddComponent<GameOverPopup>();
        comp.scoreText = scoreT;
        comp.reasonText = reasonT;
        comp.retryButton = retryBtn;
        comp.backButton = backBtn;

        AddButtonClickAnim(retryBtn, backBtn);
        retryBtn.gameObject.AddComponent<GameplayButtonHoverSound>();
        backBtn.gameObject.AddComponent<GameplayButtonHoverSound>();
        panel.AddComponent<PopupShowAnim>();
        panel.SetActive(false);
        Debug.Log("[GameUIRuntimeBootstrap] 已创建 GameOverPopup");
    }

    private static void EnsurePauseMenu()
    {
        if (Object.FindObjectOfType<PauseMenu>() != null) return;

        var canvas = GameObject.Find("GameCanvas");
        if (canvas == null) return;

        var panel = new GameObject("PauseMenu");
        panel.transform.SetParent(canvas.transform, false);

        var pr = panel.AddComponent<RectTransform>();
        pr.anchorMin = Vector2.zero;
        pr.anchorMax = Vector2.one;
        pr.offsetMin = pr.offsetMax = Vector2.zero;

        var overlay = new GameObject("Overlay");
        overlay.transform.SetParent(panel.transform, false);
        var overlayRt = overlay.AddComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = overlayRt.offsetMax = Vector2.zero;
        var overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = new Color(0, 0, 0, 0.6f);
        overlayImg.raycastTarget = true;
        var overlayBtn = overlay.AddComponent<Button>();
        overlayBtn.transition = Selectable.Transition.None;
        overlayBtn.targetGraphic = overlayImg;

        var box = new GameObject("Box");
        box.transform.SetParent(panel.transform, false);
        var boxRt = box.AddComponent<RectTransform>();
        boxRt.anchorMin = boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.sizeDelta = new Vector2(360, 320);
        boxRt.anchoredPosition = Vector2.zero;

        var bg = box.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.1f, 0.14f, 0.95f);
        bg.raycastTarget = true;

        var title = CreateText(box.transform, "TitleText", "暂停", new Vector2(0, 120), new Vector2(280, 32), 24);

        float volY1 = 50, volY2 = 0, volW = 200, volH = 24;
        var musicLabel = CreateText(box.transform, "MusicLabel", "背景音乐", new Vector2(-volW * 0.5f - 50, volY1), new Vector2(80, 24), 16);
        musicLabel.alignment = TextAnchor.MiddleLeft;
        var musicSlider = CreateVolumeSlider(box.transform, "MusicSlider", new Vector2(0, volY1), new Vector2(volW, volH), GlobalBackgroundMusic.VolumePrefKey, 0.6f);

        var sfxLabel = CreateText(box.transform, "SFXLabel", "音效", new Vector2(-volW * 0.5f - 50, volY2), new Vector2(80, 24), 16);
        sfxLabel.alignment = TextAnchor.MiddleLeft;
        var sfxSlider = CreateVolumeSlider(box.transform, "SFXSlider", new Vector2(0, volY2), new Vector2(volW, volH), "SFXVolume", 0.7f);

        float btnY = -70, btnW = 160, btnH = 48;
        var backBtn = CreateMenuStyleButton(box.transform, "BackToMenuButton", "返回主菜单", new Vector2(0, btnY), new Vector2(btnW, btnH));
        backBtn.gameObject.AddComponent<GameplayButtonHoverSound>();

        var comp = panel.AddComponent<PauseMenu>();
        comp.musicSlider = musicSlider;
        comp.sfxSlider = sfxSlider;
        comp.backToMenuButton = backBtn;

        overlay.AddComponent<GameplayButtonHoverSound>();
        overlayBtn.onClick.AddListener(() =>
        {
            GameplayAudio.Instance?.PlayGeneralClick();
            var gm = GameManager.Instance;
            if (gm != null && gm.IsPausedByUser)
            {
                gm.SetUserPaused(false);
                comp.Hide();
            }
        });

        panel.AddComponent<PopupShowAnim>();
        panel.SetActive(false);
        Debug.Log("[GameUIRuntimeBootstrap] 已创建 PauseMenu");
    }

    /// <summary>与 StartMenu 按钮同风格：透明背景、MenuButton（悬停音效+缩放+颜色）、ButtonClickAnim。</summary>
    private static Button CreateMenuStyleButton(Transform parent, string name, string label, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0f);
        var btn = go.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        var labelGo = new GameObject("Text");
        labelGo.transform.SetParent(go.transform, false);
        var lr = labelGo.AddComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = new Vector2(20, 0);
        lr.offsetMax = Vector2.zero;
        var txt = labelGo.AddComponent<Text>();
        txt.text = label;
        txt.font = GameUIFonts.Default;
        txt.fontSize = 24;
        txt.alignment = TextAnchor.MiddleLeft;
        txt.color = Color.white;
        var shadow = labelGo.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.5f);
        shadow.effectDistance = new Vector2(1, -1);
        var menuBtn = go.AddComponent<Game.Scripts.UI.MenuButton>();
        menuBtn.buttonText = txt;
        menuBtn.highlightColor = new Color(1f, 0.6f, 0.2f, 1f);
        go.AddComponent<ButtonClickAnim>();
        return btn;
    }

    private static void EnsurePauseButton()
    {
        if (GameObject.Find("PauseButton") != null) return;

        var canvas = GameObject.Find("GameCanvas");
        if (canvas == null) return;

        const float size = 64f;
        const float margin = 8f;
        var btn = CreateButton(canvas.transform, "PauseButton", "⏸", Vector2.zero, new Vector2(size, size), new Color(0.35f, 0.4f, 0.5f));
        btn.gameObject.name = "PauseButton";
        var r = btn.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(1, 1);
        r.anchorMax = new Vector2(1, 1);
        r.pivot = new Vector2(1, 1);
        r.anchoredPosition = new Vector2(-margin, -margin);
        r.sizeDelta = new Vector2(size, size);
        r.SetAsLastSibling();
        btn.GetComponentInChildren<Text>().fontSize = 28;

        btn.gameObject.AddComponent<GameplayButtonHoverSound>();
        btn.onClick.AddListener(() =>
        {
            GameplayAudio.Instance?.PlayGeneralClick();
            var gm = GameManager.Instance;
            if (gm == null || gm.IsGameOver || gm.IsPausedForWeekReward) return;
            var menu = Object.FindObjectOfType<PauseMenu>(true);
            if (menu != null)
            {
                if (gm.IsPausedByUser)
                {
                    gm.SetUserPaused(false);
                    menu.Hide();
                }
                else
                {
                    gm.SetUserPaused(true);
                    menu.Show();
                    menu.transform.SetAsLastSibling();
                }
            }
        });
        AddButtonClickAnim(btn);
        Debug.Log("[GameUIRuntimeBootstrap] 已创建 PauseButton");
    }

    private static Slider CreateVolumeSlider(Transform parent, string name, Vector2 pos, Vector2 size, string prefKey, float defaultValue)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = size;

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.22f, 0.28f);

        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(go.transform, false);
        var faRect = fillArea.AddComponent<RectTransform>();
        faRect.anchorMin = new Vector2(0, 0.25f);
        faRect.anchorMax = new Vector2(1, 0.75f);
        faRect.offsetMin = new Vector2(4, 2);
        faRect.offsetMax = new Vector2(-4, -2);

        var fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.35f, 0.5f, 0.4f);

        var handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(go.transform, false);
        var haRect = handleArea.AddComponent<RectTransform>();
        haRect.anchorMin = Vector2.zero;
        haRect.anchorMax = Vector2.one;
        haRect.offsetMin = new Vector2(10, 0);
        haRect.offsetMax = new Vector2(-10, 0);

        var handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        var hRect = handle.AddComponent<RectTransform>();
        hRect.sizeDelta = new Vector2(20, 0);
        var hImg = handle.AddComponent<Image>();
        hImg.color = new Color(0.6f, 0.7f, 0.65f);

        var slider = go.AddComponent<Slider>();
        slider.fillRect = fillRect;
        slider.handleRect = hRect;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = PlayerPrefs.GetFloat(prefKey, defaultValue);
        return slider;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = size;
        go.AddComponent<Image>().color = color;
        var btn = go.AddComponent<Button>();
        var labelGo = new GameObject("Text");
        labelGo.transform.SetParent(go.transform, false);
        var lr = labelGo.AddComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = lr.offsetMax = Vector2.zero;
        var txt = labelGo.AddComponent<Text>();
        txt.text = label;
        txt.font = GameUIFonts.Default;
        txt.fontSize = 18;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        return btn;
    }

    private static void AddButtonClickAnim(params Button[] buttons)
    {
        foreach (var b in buttons)
            if (b != null && b.GetComponent<ButtonClickAnim>() == null)
                b.gameObject.AddComponent<ButtonClickAnim>();
    }

    private static Text CreateText(Transform parent, string name, string content, Vector2 pos, Vector2 size, int fontSize = 18)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = size;
        var t = go.AddComponent<Text>();
        t.text = content;
        t.font = GameUIFonts.Default;
        t.fontSize = fontSize;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        return t;
    }

    private static Button CreateOptionButton(Transform parent, string name, string label, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = size;
        go.AddComponent<Image>().color = new Color(0.25f, 0.3f, 0.38f);
        var btn = go.AddComponent<Button>();
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);
        var lr = labelGo.AddComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = lr.offsetMax = Vector2.zero;
        var txt = labelGo.AddComponent<Text>();
        txt.text = label;
        txt.font = GameUIFonts.Default;
        txt.fontSize = 18;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        return btn;
    }
}

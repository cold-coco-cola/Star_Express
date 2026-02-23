using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 运行时确保必要 UI 存在。AutoSetupGameUI 仅在编辑器中运行，
/// 若场景未保存弹窗等 UI，运行时由此脚本补全。
/// </summary>
public class GameUIRuntimeBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnSceneLoaded()
    {
        var go = new GameObject("GameUIRuntimeBootstrap");
        var b = go.AddComponent<GameUIRuntimeBootstrap>();
        b.EnsureUI();
        Object.Destroy(go);
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
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(320, 220);
        pr.anchoredPosition = Vector2.zero;

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.1f, 0.14f, 0.95f);
        bg.raycastTarget = true;

        var title = CreateText(panel.transform, "TitleText", "暂停", new Vector2(0, 70), new Vector2(280, 32), 24);
        var resumeBtn = CreateButton(panel.transform, "ResumeButton", "继续游戏", new Vector2(0, -20), new Vector2(160, 40), new Color(0.25f, 0.5f, 0.35f));

        float volY = -70, volW = 200, volH = 24;
        var volLabel = CreateText(panel.transform, "VolumeLabel", "音量: 60%", new Vector2(-volW * 0.5f - 50, volY), new Vector2(80, 24), 16);
        volLabel.alignment = TextAnchor.MiddleLeft;
        var slider = CreateVolumeSlider(panel.transform, "VolumeSlider", new Vector2(0, volY), new Vector2(volW, volH));

        var comp = panel.AddComponent<PauseMenu>();
        comp.resumeButton = resumeBtn;
        comp.volumeSlider = slider;
        comp.volumeLabel = volLabel;

        AddButtonClickAnim(resumeBtn);
        panel.AddComponent<PopupShowAnim>();
        panel.SetActive(false);
        Debug.Log("[GameUIRuntimeBootstrap] 已创建 PauseMenu");
    }

    private static void EnsurePauseButton()
    {
        if (GameObject.Find("PauseButton") != null) return;

        var canvas = GameObject.Find("GameCanvas");
        if (canvas == null) return;

        var hud = Object.FindObjectOfType<GameHUD>();
        var parent = hud != null ? hud.transform : canvas.transform;

        var btn = CreateButton(parent, "PauseButton", "⏸", Vector2.zero, new Vector2(44, 44), new Color(0.35f, 0.4f, 0.5f));
        btn.gameObject.name = "PauseButton";
        var r = btn.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0, 1);
        r.anchorMax = new Vector2(0, 1);
        r.pivot = new Vector2(0, 1);
        r.anchoredPosition = new Vector2(12, -12);
        r.sizeDelta = new Vector2(44, 44);
        btn.GetComponentInChildren<Text>().fontSize = 22;

        btn.onClick.AddListener(() =>
        {
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

    private static Slider CreateVolumeSlider(Transform parent, string name, Vector2 pos, Vector2 size)
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
        slider.value = PlayerPrefs.GetFloat(BackgroundMusic.VolumePrefKey, 0.6f);
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

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
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 18;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        return btn;
    }
}

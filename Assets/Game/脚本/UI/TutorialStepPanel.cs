using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 新手引导步骤提示面板。
/// </summary>
public class TutorialStepPanel : BasePanel
{
    [SerializeField] private Text hintText;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button skipButton;

    private TutorialManager _tutorialManager;

    protected override void OnInit()
    {
        if (hintText == null)
            hintText = GetComponentInChildren<Text>(true);
        if (hintText != null)
            hintText.font = GameUIFonts.Default;

        if (continueButton == null || skipButton == null)
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                if (btn == null) continue;
                if (continueButton == null && btn.name.Contains("继续"))
                    continueButton = btn;
                else if (skipButton == null && btn.name.Contains("跳过"))
                    skipButton = btn;
            }
            if (continueButton == null && buttons.Length > 0) continueButton = buttons[0];
            if (skipButton == null && buttons.Length > 1) skipButton = buttons[1];
        }

        SetupButton(continueButton, OnContinueClicked);
        SetupButton(skipButton, OnSkipClicked);
    }

    public void Bind(TutorialManager manager)
    {
        _tutorialManager = manager;
    }

    public void ShowStep(int stepIndex, string hint, bool showContinueButton)
    {
        Show();
        transform.SetAsLastSibling();
        if (hintText != null)
            hintText.text = hint;
        if (continueButton != null)
            continueButton.gameObject.SetActive(showContinueButton);
    }

    public override void Hide()
    {
        base.Hide();
    }

    private void SetupButton(Button btn, UnityEngine.Events.UnityAction onClick)
    {
        if (btn == null) return;
        btn.onClick.RemoveListener(onClick);
        btn.onClick.AddListener(onClick);
        if (btn.GetComponent<GameplayButtonHoverSound>() == null)
            btn.gameObject.AddComponent<GameplayButtonHoverSound>();
        if (btn.GetComponent<ButtonClickAnim>() == null)
            btn.gameObject.AddComponent<ButtonClickAnim>();
    }

    private void OnContinueClicked()
    {
        GameplayAudio.Instance?.PlayGeneralClick();
        _tutorialManager?.OnStepContinue();
    }

    private void OnSkipClicked()
    {
        GameplayAudio.Instance?.PlayGeneralClick();
        _tutorialManager?.SkipTutorial();
    }
}

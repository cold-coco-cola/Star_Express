using UnityEngine;
using UnityEngine.SceneManagement;

public enum TutorialState
{
    Inactive,
    ShowingStory,
    Step1_BuildLine,
    Step2_EditLine,
    Step3_LinePanel,
    Step4_MoreButton,
    Completed
}

/// <summary>
/// 新手引导状态机：背景故事（每次进第一关都播）-> 四步引导（仅未完成时）-> 完成。
/// </summary>
public class TutorialManager : MonoBehaviour
{
    private const string TutorialCompletedKey = "TutorialCompleted";

    [Tooltip("第一关场景名，每次进入此场景都会播放背景故事")]
    [SerializeField] private string firstLevelSceneName = "SolarSystem_01";

    [SerializeField] private StoryPanel storyPanel;
    [SerializeField] private TutorialStepPanel stepPanel;

    [SerializeField] private string step1Hint = "点击站点开始建线，再点击另一个站点完成连线";
    [SerializeField] private string step2Hint = "右键点击一段线路可编辑：\n左键点击其他站点可将其加入线路、右键再次点击线路可删除该段";
    [SerializeField] private string step3Hint = "右侧线路面板也可同样操作";
    [SerializeField] private string step4Hint = "点击顶部「更多」可随时查看操作指南";

    private TutorialState currentState = TutorialState.Inactive;
    private bool hasCompletedTutorial;
    private bool _started;

    public TutorialState CurrentState => currentState;

    private bool IsFirstLevelScene()
    {
        var scene = SceneManager.GetActiveScene();
        return scene.IsValid() && scene.name == firstLevelSceneName;
    }

    private void Awake()
    {
        hasCompletedTutorial = PlayerPrefs.GetInt(TutorialCompletedKey, 0) == 1;
        currentState = TutorialState.Inactive;
        ResolveReferences();
    }

    private void Start()
    {
        ResolveReferences();
        if (stepPanel != null)
        {
            stepPanel.Bind(this);
            stepPanel.Hide();
        }
        if (storyPanel != null)
            storyPanel.Hide();

        if (IsFirstLevelScene())
            StartTutorial();
    }

    private void ResolveReferences()
    {
        if (storyPanel == null)
            storyPanel = FindObjectOfType<StoryPanel>(true);
        if (stepPanel == null)
            stepPanel = FindObjectOfType<TutorialStepPanel>(true);
    }

    public void StartTutorial()
    {
        if (_started) return;
        _started = true;
        ResolveReferences();

        currentState = TutorialState.ShowingStory;
        GameManager.Instance?.SetTutorialPaused(true);
        Time.timeScale = 0f;
        if (stepPanel != null) stepPanel.Hide();
        if (storyPanel != null)
        {
            storyPanel.ShowStory(OnStoryCompleted);
            storyPanel.transform.SetAsLastSibling();
        }
        else
        {
            OnStoryCompleted();
        }
    }

    public void SkipTutorial()
    {
        if (currentState == TutorialState.Completed || hasCompletedTutorial) return;
        CompleteTutorial();
    }

    public void OnLineCreated()
    {
        if (currentState != TutorialState.Step1_BuildLine) return;
        EnterStep2();
    }

    public void OnStepContinue()
    {
        switch (currentState)
        {
            case TutorialState.Step2_EditLine:
                EnterStep3();
                break;
            case TutorialState.Step3_LinePanel:
                EnterStep4();
                break;
            case TutorialState.Step4_MoreButton:
                CompleteTutorial();
                break;
        }
    }

    private void OnStoryCompleted()
    {
        if (currentState == TutorialState.Completed) return;
        Time.timeScale = 1f;
        if (hasCompletedTutorial)
        {
            GameManager.Instance?.SetTutorialPaused(false);
            return;
        }
        EnterStep1();
    }

    private void EnterStep1()
    {
        currentState = TutorialState.Step1_BuildLine;
        ShowStep(1, step1Hint, false);
    }

    private void EnterStep2()
    {
        currentState = TutorialState.Step2_EditLine;
        ShowStep(2, step2Hint, true);
    }

    private void EnterStep3()
    {
        currentState = TutorialState.Step3_LinePanel;
        ShowStep(3, step3Hint, true);
    }

    private void EnterStep4()
    {
        currentState = TutorialState.Step4_MoreButton;
        ShowStep(4, step4Hint, true);
    }

    private void ShowStep(int stepIndex, string hint, bool showContinue)
    {
        ResolveReferences();
        if (storyPanel != null) storyPanel.Hide();
        if (stepPanel != null)
        {
            stepPanel.Bind(this);
            stepPanel.ShowStep(stepIndex, hint, showContinue);
            stepPanel.transform.SetAsLastSibling();
        }
    }

    private void CompleteTutorial()
    {
        hasCompletedTutorial = true;
        currentState = TutorialState.Completed;

        PlayerPrefs.SetInt(TutorialCompletedKey, 1);
        PlayerPrefs.Save();

        if (storyPanel != null) storyPanel.Hide();
        if (stepPanel != null) stepPanel.Hide();

        Time.timeScale = 1f;
        GameManager.Instance?.SetTutorialPaused(false);
    }

    /// <summary>重置引导完成标记，下次进入第一关会再播故事并显示四步引导。可用于设置里的「再看一次新手引导」。</summary>
    public static void ResetTutorialCompleted()
    {
        PlayerPrefs.SetInt(TutorialCompletedKey, 0);
        PlayerPrefs.Save();
    }
}

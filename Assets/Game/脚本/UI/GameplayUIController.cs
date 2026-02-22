using System;
using UnityEngine;

/// <summary>
/// 全局 UI 状态枚举，与设计文档 §4.1 完全一致。
/// </summary>
public enum GameplayUIState
{
    Idle,
    LineFirstSelected,
    LineColorChoosing,
    PlacingShip,
    PlacingCarriage,
    PlacingHub,
    LineRemoving,
    GameOver
}

/// <summary>
/// UI 状态机控制器。维护当前 UI 状态，驱动面板显隐，
/// 并仅允许设计文档中定义的合法状态转换。
/// 用法：GameplayUIController.Instance.TryTransition(GameplayUIState.XXX)
/// </summary>
public class GameplayUIController : MonoBehaviour
{
    public static GameplayUIController Instance { get; private set; }

    [Header("当前状态（只读）")]
    [SerializeField] private GameplayUIState currentState = GameplayUIState.Idle;

    public GameplayUIState CurrentState => currentState;

    public event Action<GameplayUIState, GameplayUIState> OnStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        var gm = GameManager.Instance;
        if (gm != null)
            gm.OnGameOver += OnGameOver;
    }

    private void OnDestroy()
    {
        var gm = GameManager.Instance;
        if (gm != null)
            gm.OnGameOver -= OnGameOver;
    }

    private void OnGameOver(StationBehaviour failedStation)
    {
        TryTransition(GameplayUIState.GameOver);
        var popup = UIManager.Get<GameOverPopup>();
        if (popup != null)
        {
            popup.ShowWithScore(GameManager.Instance != null ? GameManager.Instance.Score : 0, failedStation);
        }
        else
        {
            UIManager.Show<GameOverPopup>();
        }
    }

    /// <summary>
    /// 尝试切换到目标状态。仅允许合法转换（见设计文档 §4.2.1）。
    /// 返回 true 表示切换成功。
    /// </summary>
    public bool TryTransition(GameplayUIState target)
    {
        if (target == currentState) return true;

        if (!IsTransitionAllowed(currentState, target))
        {
            Debug.LogWarning($"[UIController] 非法状态转换: {currentState} → {target}");
            return false;
        }

        var prev = currentState;
        currentState = target;
        OnStateChanged?.Invoke(prev, target);
        ApplyStateVisuals(prev, target);
        return true;
    }

    /// <summary>强制重置到 Idle，用于重试关卡。</summary>
    public void ForceReset()
    {
        var prev = currentState;
        currentState = GameplayUIState.Idle;
        OnStateChanged?.Invoke(prev, GameplayUIState.Idle);
        ApplyStateVisuals(prev, GameplayUIState.Idle);
    }

    private static bool IsTransitionAllowed(GameplayUIState from, GameplayUIState to)
    {
        switch (from)
        {
            case GameplayUIState.Idle:
                return to == GameplayUIState.LineFirstSelected
                    || to == GameplayUIState.PlacingShip
                    || to == GameplayUIState.PlacingCarriage
                    || to == GameplayUIState.PlacingHub
                    || to == GameplayUIState.LineRemoving
                    || to == GameplayUIState.GameOver;

            case GameplayUIState.LineFirstSelected:
                return to == GameplayUIState.Idle
                    || to == GameplayUIState.LineColorChoosing;

            case GameplayUIState.LineColorChoosing:
                return to == GameplayUIState.Idle;

            case GameplayUIState.PlacingShip:
            case GameplayUIState.PlacingHub:
            case GameplayUIState.LineRemoving:
                return to == GameplayUIState.Idle || to == GameplayUIState.PlacingCarriage;
            case GameplayUIState.PlacingCarriage:
                return to == GameplayUIState.Idle;

            case GameplayUIState.GameOver:
                return to == GameplayUIState.Idle;

            default:
                return false;
        }
    }

    private void ApplyStateVisuals(GameplayUIState prev, GameplayUIState next)
    {
        if (prev == GameplayUIState.LineColorChoosing)
            UIManager.Hide<ColorPickPanel>();
        if (prev == GameplayUIState.PlacingCarriage)
            UIManager.Hide<CarriagePlacementPanel>();

        switch (next)
        {
            case GameplayUIState.LineColorChoosing:
                UIManager.Show<ColorPickPanel>();
                break;
            case GameplayUIState.PlacingCarriage:
                UIManager.Show<CarriagePlacementPanel>();
                break;
            case GameplayUIState.GameOver:
                UIManager.Hide<ColorPickPanel>();
                break;
            case GameplayUIState.Idle:
                break;
        }
        // ResourcePanel 始终显示，不随状态切换隐藏
    }
}

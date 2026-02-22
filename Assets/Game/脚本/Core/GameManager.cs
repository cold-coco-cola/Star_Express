using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 单例；Start 中加载关卡并注册站点；Update 中按 PRD §7.3 全局时序执行：
///   1. 周计时 → 资源发放（PRD §4.6：飞船+1, 轮转客舱/星隧）
///   2. 站点解锁
///   3. 各站乘客生成（由 StationBehaviour.Update 独立驱动）
///   4. 飞船移动与停靠（由 ShipBehaviour.Update 独立驱动）
///   5. 拥挤/失败检查
/// UI 由场景 Hierarchy 中的对象管理，GameManager 不创建任何 UI。
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("关卡与资源")]
    public LevelConfig levelConfig;
    public GameObject stationPrefab;
    public VisualConfig visualConfig;
    public GameBalance gameBalance;
    [Tooltip("留空则通过 Find 获取 Map/Stations 或 Stations")]
    public Transform stationsParent;

    [Header("周计时（只读）")]
    [SerializeField] private int currentWeek;
    [SerializeField] private float weekTimer;

    [Header("得分与状态")]
    [SerializeField] private int score;
    [SerializeField] private bool isGameOver;

    /// <summary>每周资源发放后触发，参数 = (周数, 轮转资源类型)。</summary>
    public event Action<int, ResourceType> OnWeekReward;
    /// <summary>每周到时需玩家选择奖励，参数 = 周数。选择完成后由 ApplyWeekRewardSelection 发放。</summary>
    public event Action<int> OnWeekRewardSelectionRequired;
    /// <summary>得分变化时触发，参数 = 当前总分。</summary>
    public event Action<int> OnScoreChanged;
    /// <summary>游戏失败时触发，参数 = 导致失败的站点。</summary>
    public event Action<StationBehaviour> OnGameOver;
    /// <summary>新站点生成时触发，相机可据此自动调整视野。</summary>
    public event Action<StationBehaviour> OnStationSpawned;

    private const string LevelConfigPath = "Assets/Game/配置/LevelConfig_SolarSystem_01.asset";
    private const string StationPrefabPath = "Assets/Game/预制体/Station.prefab";
    private const string VisualConfigPath = "Assets/Game/配置/VisualConfig.asset";
    private const string GameBalancePath = "Assets/Game/配置/GameBalance.asset";

    private static readonly ResourceType[] RotationTable = { ResourceType.Carriage, ResourceType.StarTunnel };

    private Dictionary<string, StationBehaviour> _stationsById = new Dictionary<string, StationBehaviour>();
    private ILineManager _cachedLineManager;
    private float _crowdCheckTimer;
    private Dictionary<StationBehaviour, float> _deathCrowdTimers = new Dictionary<StationBehaviour, float>();
    private bool _waitingWeekRewardSelection;
    private int _pendingWeekForReward;

    public int CurrentWeek => currentWeek;
    public bool IsPausedForWeekReward => _waitingWeekRewardSelection;
    public bool IsPausedByUser { get; private set; }
    public int Score => score;
    public bool IsGameOver => isGameOver;
    public float WeekDurationSeconds => gameBalance != null ? gameBalance.weekDurationSeconds : 60f;
    /// <summary>距离下周的剩余秒数。</summary>
    public float WeekTimerRemaining => Mathf.Max(0, WeekDurationSeconds - weekTimer);

    /// <summary>供乘客生成使用的站点字典（只读）。</summary>
    public Dictionary<string, StationBehaviour> GetAllStations() => _stationsById;

    /// <summary>站点候船人数达死亡阈值后的持续秒数，供过载视觉用。</summary>
    public float GetDeathCrowdTimer(StationBehaviour station)
    {
        return station != null && _deathCrowdTimers.TryGetValue(station, out float t) ? t : 0f;
    }

    private void Awake()
    {
        gameObject.SetActive(true);
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureComponent<LineManager>();
        EnsureComponent<LineDrawingInput>();
        EnsureComponent<GameplayUIController>();
        EnsureComponent<CarriagePlacementInput>();
        EnsureComponent<StationSpawner>();
        EnsureComponent<BackgroundMusic>();
        // PRD §3.1：周 0 持续 60 秒后首次发放，开局不发放。不在此处添加飞船。
    }

    private void EnsureComponent<T>() where T : Component
    {
        if (GetComponent<T>() == null)
            gameObject.AddComponent<T>();
    }

    public ILineManager GetLineManager()
    {
        if (_cachedLineManager != null) return _cachedLineManager;
        _cachedLineManager = GetComponent<LineManager>() as ILineManager;
        return _cachedLineManager;
    }

    /// <summary>供 UI 等直接取 LineManager 用，保证与发船逻辑同一实例。</summary>
    public LineManager GetLineManagerComponent()
    {
        return GetComponent<LineManager>();
    }

    private bool _levelLoaded;

    private void Start()
    {
        ResolveStationsParent();
        TryResolveAssetRefsInEditor();

        currentWeek = 0;
        weekTimer = 0f;
        score = 0;
        isGameOver = false;
        IsPausedByUser = false;
        _levelLoaded = false;
        Time.timeScale = 1f;

        if (levelConfig == null)
        {
            Debug.LogError("[GameManager] levelConfig 为空，请在 Inspector 中指定 Level Config");
        }
        if (stationPrefab == null)
        {
            Debug.LogError("[GameManager] stationPrefab 为空，请在 Inspector 中指定 Station 预制体");
        }
        if (stationsParent == null)
        {
            Debug.LogError("[GameManager] 未找到 Stations 节点，请确认场景中有 Map/Stations 或名为 Stations 的节点");
        }

        if (levelConfig != null && stationPrefab != null && stationsParent != null)
        {
            LevelLoader.Load(levelConfig, stationsParent, stationPrefab, visualConfig, _stationsById);
            _levelLoaded = true;

            var cam = Camera.main;
            if (cam != null && cam.GetComponent<GameCamera>() == null)
                cam.gameObject.AddComponent<GameCamera>();

            if (gameBalance != null)
            {
                foreach (var kv in _stationsById)
                {
                    kv.Value.queueCapacity = gameBalance.queueCapacity;
                    kv.Value.crowdingThreshold = gameBalance.crowdingThreshold;
                }
            }
        }

        if (GameplayUIController.Instance != null)
            GameplayUIController.Instance.TryTransition(GameplayUIState.PlacingShip);

        var lm = GetComponent<LineManager>();
        Debug.Log($"[GameManager] 启动完成：levelLoaded={_levelLoaded}, weekDuration={WeekDurationSeconds}s, 站点={_stationsById.Count}, 飞船存量={lm?.ShipStock ?? 0}");
    }

    private void Update()
    {
        if (isGameOver) return;
        if (_waitingWeekRewardSelection) return;
        if (IsPausedByUser) return;

        // PRD §7.3 步骤 1：周计时 — 始终运行，不依赖关卡是否加载
        float duration = WeekDurationSeconds;
        weekTimer += Time.deltaTime;
        if (weekTimer >= duration)
        {
            currentWeek++;
            weekTimer -= duration;
            _waitingWeekRewardSelection = true;
            _pendingWeekForReward = currentWeek;
            Time.timeScale = 0f;
            OnWeekRewardSelectionRequired?.Invoke(currentWeek);
            var popup = UIManager.Get<WeekRewardSelectionPopup>();
            if (popup == null) popup = UnityEngine.Object.FindObjectOfType<WeekRewardSelectionPopup>();
            if (popup != null)
            {
                popup.ShowForWeek(currentWeek);
                popup.transform.SetAsLastSibling();
            }
            else
                Debug.LogWarning("[GameManager] WeekRewardSelectionPopup 未找到，请运行 Star Express/自动设置 Game UI 或确认场景中有该弹窗");
        }

        // PRD §7.3 步骤 6：拥挤/失败检查（每 0.5 秒）
        if (_levelLoaded)
        {
            _crowdCheckTimer += Time.deltaTime;
            if (_crowdCheckTimer >= 0.5f)
            {
                _crowdCheckTimer = 0f;
                CheckCrowding();
            }
        }
    }

    /// <summary>
    /// PRD §4.6 资源发放：飞船 +1（固定）+ 轮转资源 ×1。
    /// 轮转序列：客舱→星隧→客舱→星隧...（首版无 Hub）
    /// </summary>
    private void DistributeWeeklyResources(int week)
    {
        var lm = GetComponent<LineManager>();
        if (lm == null) return;

        lm.AddShipStock(1);

        int rotationIndex = (week - 1) % RotationTable.Length;
        ResourceType rotating = RotationTable[rotationIndex];

        switch (rotating)
        {
            case ResourceType.Carriage:
                lm.AddCarriageStock(1);
                break;
            case ResourceType.StarTunnel:
                lm.AddStarTunnelStock(1);
                break;
        }

        string rotName = rotating == ResourceType.Carriage ? "客舱" : rotating == ResourceType.StarTunnel ? "星隧" : "资源";
        Debug.Log($"[GameManager] 周{week} 资源发放：飞船+1, {rotName}+1");
        OnWeekReward?.Invoke(week, rotating);
    }

    /// <summary>玩家完成周奖励选择后调用，发放飞船 + 选中的 1 项资源。</summary>
    public void ApplyWeekRewardSelection(WeekRewardSelectionPopup.RewardOption chosen)
    {
        if (!_waitingWeekRewardSelection) return;
        var lm = GetComponent<LineManager>();
        if (lm != null)
        {
            lm.AddShipStock(1);
            ApplyRewardOption(lm, chosen);
        }
        _waitingWeekRewardSelection = false;
        Time.timeScale = 1f;
        OnWeekReward?.Invoke(_pendingWeekForReward, ResourceType.Ship);
        string optName = chosen == WeekRewardSelectionPopup.RewardOption.Carriage ? "客舱" :
            chosen == WeekRewardSelectionPopup.RewardOption.StarTunnel ? "星隧" : "新线路";
        Debug.Log($"[GameManager] 周{_pendingWeekForReward} 奖励发放：飞船+1, {optName}+1");
    }

    /// <summary>通知新站点已生成，供 StationSpawner 调用。</summary>
    public void NotifyStationSpawned(StationBehaviour station)
    {
        OnStationSpawned?.Invoke(station);
    }

    /// <summary>用户暂停/继续。周奖励选择期间不生效。</summary>
    public void SetUserPaused(bool paused)
    {
        if (_waitingWeekRewardSelection) return;
        IsPausedByUser = paused;
        Time.timeScale = paused ? 0f : 1f;
    }

    private static void ApplyRewardOption(LineManager lm, WeekRewardSelectionPopup.RewardOption opt)
    {
        switch (opt)
        {
            case WeekRewardSelectionPopup.RewardOption.Carriage:
                lm.AddCarriageStock(1);
                break;
            case WeekRewardSelectionPopup.RewardOption.StarTunnel:
                lm.AddStarTunnelStock(1);
                break;
            case WeekRewardSelectionPopup.RewardOption.NewLine:
                lm.AddMaxLineCount();
                break;
        }
    }

    /// <summary>由飞船卸客时调用，PRD §7.1：每送达 1 人得 1 分。</summary>
    public void AddScore(int points)
    {
        score += points;
        OnScoreChanged?.Invoke(score);
    }

    /// <summary>PRD §7.2 死亡检查：任一站候船人数 >= deathThreshold 且持续超过 deathDurationSeconds 即失败。</summary>
    private void CheckCrowding()
    {
        int threshold = gameBalance != null ? gameBalance.deathThreshold : 8;
        float duration = gameBalance != null ? gameBalance.deathDurationSeconds : 20f;
        float checkInterval = 0.5f;

        foreach (var kv in _stationsById)
        {
            var station = kv.Value;
            if (!station.isUnlocked) continue;

            int count = station.waitingPassengers.Count;
            if (count >= threshold)
            {
                if (!_deathCrowdTimers.TryGetValue(station, out float t)) t = 0f;
                t += checkInterval;
                _deathCrowdTimers[station] = t;
                if (t >= duration)
                {
                    TriggerGameOver(station);
                    return;
                }
            }
            else
            {
                _deathCrowdTimers[station] = 0f;
            }
        }
    }

    private void TriggerGameOver(StationBehaviour failedStation)
    {
        isGameOver = true;
        Debug.LogWarning($"[GameManager] 游戏失败！站点 {failedStation.displayName} 拥挤超阈值。得分：{score}");
        OnGameOver?.Invoke(failedStation);
    }

    private void TryResolveAssetRefsInEditor()
    {
#if UNITY_EDITOR
        if (levelConfig == null)
            levelConfig = AssetDatabase.LoadAssetAtPath<LevelConfig>(LevelConfigPath);
        if (stationPrefab == null)
            stationPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StationPrefabPath);
        if (visualConfig == null)
            visualConfig = AssetDatabase.LoadAssetAtPath<VisualConfig>(VisualConfigPath);
        if (gameBalance == null)
            gameBalance = AssetDatabase.LoadAssetAtPath<GameBalance>(GameBalancePath);
#endif
    }

    private void ResolveStationsParent()
    {
        if (stationsParent != null) return;
        var map = GameObject.Find("Map");
        if (map != null)
        {
            var stations = map.transform.Find("Stations");
            if (stations != null) { stationsParent = stations; return; }
        }
        var stationsGo = GameObject.Find("Stations");
        if (stationsGo != null)
            stationsParent = stationsGo.transform;
    }
}

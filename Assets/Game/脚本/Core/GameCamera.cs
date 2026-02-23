using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 根据图中站点分布调整相机视野：移动、缩放，使所有站点在视野内并留出边距。
/// 支持鼠标滚轮以鼠标位置为中心缩放，便于点击飞船等小目标。
/// 自动查找或创建背景，使背景随摄像机移动缩放。
/// </summary>
public class GameCamera : MonoBehaviour
{
    [Header("视野参数")]
    [Tooltip("站点边界外的边距（世界单位）")]
    public float padding = 2f;
    [Tooltip("最小正交尺寸（缩放到此值后不能再放大，便于点击飞船）")]
    public float minOrthoSize = 4f;
    [Tooltip("最大正交尺寸（避免缩放过大）")]
    public float maxOrthoSize = 20f;
    [Tooltip("平滑跟随速度")]
    public float smoothSpeed = 2f;

    [Header("滚轮缩放")]
    [Tooltip("滚轮缩放灵敏度")]
    public float scrollZoomSpeed = 2f;
    [Tooltip("缩放平滑速度")]
    public float zoomSmoothSpeed = 12f;

    [Header("游戏失败聚焦")]
    [Tooltip("聚焦到失败站点的动画时长")]
    public float gameOverFocusDuration = 1.8f;
    [Tooltip("聚焦时的目标正交尺寸（放大）")]
    public float gameOverFocusOrtho = 3.5f;

    private Camera _cam;
    private Vector3 _targetPosition;
    private float _targetOrthoSize;
    private float _baseMaxOrthoSize; // 正常状态（视野最大）对应的 ortho，由站点边界计算
    private bool _forceFitToBounds; // 新站点生成时强制适配视野

    private StationBehaviour _gameOverFocusStation;
    private float _gameOverFocusElapsed;
    private Vector3 _gameOverFocusStartPos;
    private float _gameOverFocusStartOrtho;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        if (_cam == null) _cam = Camera.main;
        if (_cam != null)
        {
            _targetPosition = _cam.transform.position;
            _targetOrthoSize = _cam.orthographicSize;
            _baseMaxOrthoSize = _cam.orthographicSize;
        }
        EnsureBackgroundFollow();
    }

    private void Start()
    {
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnStationSpawned += OnStationSpawned;
            gm.OnGameOver += OnGameOver;
        }
    }

    private void OnDestroy()
    {
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnStationSpawned -= OnStationSpawned;
            gm.OnGameOver -= OnGameOver;
        }
    }

    private void OnGameOver(StationBehaviour failedStation)
    {
        _gameOverFocusStation = failedStation;
        _gameOverFocusElapsed = 0f;
        if (_cam != null)
        {
            _gameOverFocusStartPos = _cam.transform.position;
            _gameOverFocusStartOrtho = _cam.orthographicSize;
        }
    }

    private void OnStationSpawned(StationBehaviour _)
    {
        _forceFitToBounds = true;
    }

    private void RunGameOverFocus()
    {
        if (_cam == null || _gameOverFocusStation == null) return;

        _gameOverFocusElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_gameOverFocusElapsed / gameOverFocusDuration);
        float smoothT = 1f - (1f - t) * (1f - t) * (1f - t) * (1f - t);

        Vector3 targetPos = new Vector3(_gameOverFocusStation.transform.position.x, _gameOverFocusStation.transform.position.y, _cam.transform.position.z);
        _cam.transform.position = Vector3.Lerp(_gameOverFocusStartPos, targetPos, smoothT);
        _cam.orthographicSize = Mathf.Lerp(_gameOverFocusStartOrtho, gameOverFocusOrtho, smoothT);
    }

    /// <summary>游戏失败聚焦动画是否已完成（供 Popup 延迟显示）。</summary>
    public bool IsGameOverFocusComplete => _gameOverFocusStation != null && _gameOverFocusElapsed >= gameOverFocusDuration;

    private void EnsureBackgroundFollow()
    {
        var bg = GameObject.Find("Background");
        if (bg == null) return;
        if (bg.GetComponent<BackgroundCameraFollow>() == null)
            bg.AddComponent<BackgroundCameraFollow>();
    }

    private void LateUpdate()
    {
        if (_cam == null) return;
        var gm = GameManager.Instance;
        if (gm == null) return;
        if (gm.IsGameOver)
        {
            RunGameOverFocus();
            return;
        }

        ComputeTargetBounds(gm.GetAllStations());
        HandleScrollZoom();
        _cam.transform.position = Vector3.Lerp(_cam.transform.position, _targetPosition, smoothSpeed * Time.deltaTime);
        _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, _targetOrthoSize, zoomSmoothSpeed * Time.deltaTime);
    }

    private void HandleScrollZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Approximately(scroll, 0f)) return;

        float oldOrtho = _targetOrthoSize;
        float delta = -scroll * scrollZoomSpeed;
        float newOrtho = Mathf.Clamp(_targetOrthoSize + delta, minOrthoSize, _baseMaxOrthoSize);
        if (Mathf.Approximately(newOrtho, oldOrtho)) return;

        // 以鼠标位置为中心缩放：保持鼠标下的世界点不变
        float camDist = Mathf.Abs(_cam.transform.position.z);
        Vector3 mouseScreen = new Vector3(Input.mousePosition.x, Input.mousePosition.y, camDist);
        Vector3 worldPoint = _cam.ScreenToWorldPoint(mouseScreen);
        float t = 1f - newOrtho / oldOrtho;
        _targetPosition += (worldPoint - _targetPosition) * t;
        _targetOrthoSize = newOrtho;
    }

    private void ComputeTargetBounds(Dictionary<string, StationBehaviour> stations)
    {
        if (stations == null || stations.Count == 0) return;

        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        int count = 0;
        foreach (var kv in stations)
        {
            if (kv.Value == null || !kv.Value.isUnlocked) continue;
            var p = kv.Value.transform.position;
            minX = Mathf.Min(minX, p.x);
            maxX = Mathf.Max(maxX, p.x);
            minY = Mathf.Min(minY, p.y);
            maxY = Mathf.Max(maxY, p.y);
            count++;
        }
        if (count == 0) return;

        minX -= padding;
        maxX += padding;
        minY -= padding;
        maxY += padding;

        float width = maxX - minX;
        float height = maxY - minY;
        float centerX = (minX + maxX) * 0.5f;
        float centerY = (minY + maxY) * 0.5f;

        float aspect = _cam.aspect;
        float sizeByWidth = (width / aspect) * 0.5f;
        float sizeByHeight = height * 0.5f;
        _baseMaxOrthoSize = Mathf.Clamp(Mathf.Max(sizeByWidth, sizeByHeight), minOrthoSize, maxOrthoSize);

        // 新站点生成时强制适配；否则仅在视野最大时跟随，用户缩放后保持其视角
        bool atMaxZoom = _targetOrthoSize >= _baseMaxOrthoSize * 0.98f;
        if (_forceFitToBounds || atMaxZoom)
        {
            _targetPosition = new Vector3(centerX, centerY, _cam.transform.position.z);
            _targetOrthoSize = _baseMaxOrthoSize;
            _forceFitToBounds = false;
        }
        else
        {
            _targetOrthoSize = Mathf.Clamp(_targetOrthoSize, minOrthoSize, _baseMaxOrthoSize);
        }
    }
}

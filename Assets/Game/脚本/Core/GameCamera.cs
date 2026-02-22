using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 根据图中站点分布调整相机视野：移动、缩放，使所有站点在视野内并留出边距。
/// 自动查找或创建背景，使背景随摄像机移动缩放。
/// </summary>
public class GameCamera : MonoBehaviour
{
    [Header("视野参数")]
    [Tooltip("站点边界外的边距（世界单位）")]
    public float padding = 2f;
    [Tooltip("最小正交尺寸（避免缩放过小）")]
    public float minOrthoSize = 4f;
    [Tooltip("最大正交尺寸（避免缩放过大）")]
    public float maxOrthoSize = 20f;
    [Tooltip("平滑跟随速度")]
    public float smoothSpeed = 2f;

    private Camera _cam;
    private Vector3 _targetPosition;
    private float _targetOrthoSize;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        if (_cam == null) _cam = Camera.main;
        if (_cam != null)
        {
            _targetPosition = _cam.transform.position;
            _targetOrthoSize = _cam.orthographicSize;
        }
        EnsureBackgroundFollow();
    }

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
        if (gm == null || gm.IsGameOver) return;

        ComputeTargetBounds(gm.GetAllStations());
        _cam.transform.position = Vector3.Lerp(_cam.transform.position, _targetPosition, smoothSpeed * Time.deltaTime);
        _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, _targetOrthoSize, smoothSpeed * Time.deltaTime);
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

        _targetPosition = new Vector3(centerX, centerY, _cam.transform.position.z);

        float aspect = _cam.aspect;
        float sizeByWidth = (width / aspect) * 0.5f;
        float sizeByHeight = height * 0.5f;
        _targetOrthoSize = Mathf.Clamp(Mathf.Max(sizeByWidth, sizeByHeight), minOrthoSize, maxOrthoSize);
    }
}

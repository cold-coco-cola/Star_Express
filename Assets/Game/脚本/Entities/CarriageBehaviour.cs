using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 车厢行为：沿轨道跟随车头运动，有独立的乘客容量。
/// 使用与车头相同的视觉样式。
/// </summary>
public class CarriageBehaviour : MonoBehaviour
{
    [Header("跟随设置")]
    public ShipBehaviour leadShip;
    public int carriageIndex;
    public float followDistance = 0.6f;

    [Header("载客")]
    public int capacity = 4;
    public List<Passenger> passengers = new List<Passenger>();

    [Header("轨道位置")]
    public int currentSegmentIndex;
    public float progressOnSegment;
    public int direction = 1;

    private SpriteRenderer _spriteRenderer;
    private bool _initialized;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer == null)
        {
            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
    }

    private void Start()
    {
        var balance = GameManager.Instance != null ? GameManager.Instance.gameBalance : null;
        capacity = balance != null ? balance.shipCapacity : 4;
    }

    public void InitializeTrackPosition(int segmentIndex, float progress, int dir)
    {
        currentSegmentIndex = segmentIndex;
        progressOnSegment = progress;
        direction = dir;
        _initialized = true;
    }

    private void Update()
    {
        if (leadShip == null)
        {
            if (Application.isPlaying)
                Destroy(gameObject);
            return;
        }
        if (leadShip.line == null)
        {
            return;
        }

        if (!_initialized)
        {
            SyncWithLeadShip();
        }

        UpdatePositionOnTrack();
        UpdateVisual();
    }

    private void SyncWithLeadShip()
    {
        if (carriageIndex == 0)
        {
            currentSegmentIndex = leadShip.currentSegmentIndex;
            progressOnSegment = leadShip.progressOnSegment;
            direction = leadShip.direction;
        }
        else
        {
            var prevCarriage = leadShip.GetCarriage(carriageIndex - 1);
            if (prevCarriage != null)
            {
                currentSegmentIndex = prevCarriage.currentSegmentIndex;
                progressOnSegment = prevCarriage.progressOnSegment;
                direction = prevCarriage.direction;
            }
        }
        _initialized = true;
    }

    private void UpdatePositionOnTrack()
    {
        var line = leadShip.line;
        if (line == null || line.stationSequence == null || line.stationSequence.Count < 2) return;

        var lineManager = GameManager.Instance != null ? GameManager.Instance.GetLineManager() : null;
        if (lineManager == null) return;

        int targetSegmentIndex;
        float targetProgress;
        int targetDirection;

        if (carriageIndex == 0)
        {
            targetSegmentIndex = leadShip.currentSegmentIndex;
            targetProgress = leadShip.progressOnSegment;
            targetDirection = leadShip.direction;
        }
        else
        {
            var prevCarriage = leadShip.GetCarriage(carriageIndex - 1);
            if (prevCarriage == null) return;
            targetSegmentIndex = prevCarriage.currentSegmentIndex;
            targetProgress = prevCarriage.progressOnSegment;
            targetDirection = prevCarriage.direction;
        }

        direction = targetDirection;

        Vector3 targetPos = lineManager.GetPositionOnLine(line, targetSegmentIndex, targetProgress);
        targetPos.z = ShipBehaviour.ShipZ;

        float distanceToTarget = Vector3.Distance(transform.position, targetPos);

        if (distanceToTarget <= followDistance)
        {
            UpdateRotation(lineManager, line);
            return;
        }

        FindPositionAtDistanceFromTarget(lineManager, line, targetSegmentIndex, targetProgress, followDistance);
    }

    private void FindPositionAtDistanceFromTarget(ILineManager lineManager, Line line, int targetSegmentIndex, float targetProgress, float targetDistance)
    {
        var seq = line.stationSequence;
        if (seq == null || seq.Count < 2) return;

        int searchDir = -direction;

        int segIdx = targetSegmentIndex;
        float progress = targetProgress;
        float accumulatedDistance = 0f;
        int maxSteps = 1000;
        int steps = 0;

        Vector3 prevPos = lineManager.GetPositionOnLine(line, segIdx, progress);
        prevPos.z = ShipBehaviour.ShipZ;

        while (accumulatedDistance < targetDistance && steps < maxSteps)
        {
            steps++;

            float stepSize = 0.02f;
            float nextProgress = progress + searchDir * stepSize;
            int nextSegIdx = segIdx;

            if (nextProgress > 1f)
            {
                if (segIdx < seq.Count - 2)
                {
                    nextSegIdx = segIdx + 1;
                    nextProgress = nextProgress - 1f;
                }
                else
                {
                    break;
                }
            }
            else if (nextProgress < 0f)
            {
                if (segIdx > 0)
                {
                    nextSegIdx = segIdx - 1;
                    nextProgress = 1f + nextProgress;
                }
                else
                {
                    break;
                }
            }

            Vector3 nextPos = lineManager.GetPositionOnLine(line, nextSegIdx, nextProgress);
            nextPos.z = ShipBehaviour.ShipZ;
            float stepDist = Vector3.Distance(prevPos, nextPos);

            if (accumulatedDistance + stepDist >= targetDistance)
            {
                float remaining = targetDistance - accumulatedDistance;
                float t = remaining / Mathf.Max(stepDist, 0.0001f);
                Vector3 finalPos = Vector3.Lerp(prevPos, nextPos, t);
                finalPos.z = ShipBehaviour.ShipZ;
                float finalProgress = Mathf.Lerp(progress, nextProgress, t);

                currentSegmentIndex = nextSegIdx;
                progressOnSegment = finalProgress;
                transform.position = finalPos;
                UpdateRotation(lineManager, line);
                return;
            }

            accumulatedDistance += stepDist;
            prevPos = nextPos;
            segIdx = nextSegIdx;
            progress = nextProgress;
        }

        currentSegmentIndex = segIdx;
        progressOnSegment = progress;
        transform.position = prevPos;
        UpdateRotation(lineManager, line);
    }

    private void UpdateRotation(ILineManager lineManager, Line line)
    {
        Vector3 tangent = lineManager.GetTangentOnLine(line, currentSegmentIndex, progressOnSegment);
        if (tangent.sqrMagnitude > 0.0001f)
        {
            Vector2 dir2D = new Vector2(tangent.x, tangent.y) * direction;
            float angle = Mathf.Atan2(dir2D.y, dir2D.x) * Mathf.Rad2Deg;
            transform.eulerAngles = new Vector3(0f, 0f, angle);
        }
    }

    private void UpdateVisual()
    {
        if (_spriteRenderer == null) return;

        Sprite shipSprite = leadShip.GetCurrentSprite();
        if (shipSprite != null)
        {
            _spriteRenderer.sprite = shipSprite;
        }

        Color shipColor = Color.white;
        if (leadShip.line != null && leadShip.line.displayColor != default(Color))
        {
            shipColor = leadShip.line.displayColor;
        }
        _spriteRenderer.color = shipColor;

        _spriteRenderer.sortingLayerID = SortingOrderConstants.ShipsLayerId;
        _spriteRenderer.sortingOrder = SortingOrderConstants.Ship - carriageIndex - 1;

        transform.localScale = new Vector3(0.5f, 0.25f, 1f);
    }

}

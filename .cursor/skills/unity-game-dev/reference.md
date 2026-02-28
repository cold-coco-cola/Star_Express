# Unity Game Logic Reference

## State Machine Pattern

```csharp
public enum PassengerState { Waiting, OnShip, Arrived }

public void BoardShip()
{
    if (State != PassengerState.Waiting) return;
    State = PassengerState.OnShip;
}
```

## Validation Before Operations

```csharp
public bool TryRemoveSegment(int segmentIndex)
{
    if (stationSequence == null || stationSequence.Count < 2) return false;
    if (segmentIndex < 0 || segmentIndex >= stationSequence.Count - 1) return false;
    // then modify
    return true;
}
```

## Edge Case Handling

```csharp
public bool IsEndSegment(int segmentIndex)
{
    if (segmentIndex == 0 || segmentIndex == stationSequence.Count - 2)
        return true;
    if (IsLoop()) return true;  // loop special case
    return false;
}

public bool IsLoop()
{
    if (stationSequence == null || stationSequence.Count < 3) return false;
    return stationSequence[0] == stationSequence[stationSequence.Count - 1];
}
```

## Route Management

- Loops cannot be extended
- Loop segment removal needs special handling (reorder from break point)

## Passenger Transport Order

1. Disembark destination passengers
2. Disembark transfer passengers
3. Board passengers (direct first)

## BFS Pathfinding

```csharp
public static bool CanReachStation(StationBehaviour from, StationBehaviour to, LineColor lineColor)
{
    var visited = new HashSet<StationBehaviour>();
    var queue = new Queue<StationBehaviour>();
    queue.Enqueue(from);
    visited.Add(from);

    while (queue.Count > 0)
    {
        var current = queue.Dequeue();
        if (current == to) return true;
        foreach (var neighbor in GetConnectedStations(current, lineColor))
            if (!visited.Contains(neighbor))
            {
                visited.Add(neighbor);
                queue.Enqueue(neighbor);
            }
    }
    return false;
}
```

## Project Conventions (Star_Express)

| Class | Key Members |
|-------|-------------|
| Line | `stationSequence`, `IsLoop()`, `IsEndSegment(index)`, `GetEndStationOfSegment(index)` |
| ShipBehaviour | `ProcessDocking()`, `MoveAlongLine()` |
| StationBehaviour | `waitingPassengers`, `GeneratePassenger()`, `shape` |
| ScriptableObject | `LevelConfig`, `GameBalance`, `VisualConfig` |

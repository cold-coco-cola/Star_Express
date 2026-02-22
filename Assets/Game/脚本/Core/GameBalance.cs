using UnityEngine;

[CreateAssetMenu(fileName = "GameBalance", menuName = "Star Express/Game Balance", order = 0)]
public class GameBalance : ScriptableObject
{
    [Header("站点")]
    [Tooltip("站点排队上限")]
    public int queueCapacity = 8;
    [Tooltip("站点拥挤阈值（用于提示等）")]
    public int crowdingThreshold = 6;
    [Tooltip("死亡条件：候船人数 >= 此值")]
    public int deathThreshold = 8;
    [Tooltip("死亡条件：候船人数超阈值持续超过此秒数即失败")]
    public float deathDurationSeconds = 20f;

    [Header("乘客生成")]
    [Tooltip("乘客生成间隔(秒/站)，数值越大乘客来得越慢")]
    public float passengerSpawnInterval = 10f;
    [Tooltip("每站同时生成数量")]
    public int passengerSpawnCountPerStation = 1;

    [Header("时间")]
    [Tooltip("游戏内 1 周(秒)")]
    public float weekDurationSeconds = 60f;

    [Header("飞船")]
    [Tooltip("飞船容量(人)")]
    public int shipCapacity = 4;
    [Tooltip("客舱升级增量")]
    public int carriageCapacityIncrement = 2;
    [Tooltip("飞船移动速度")]
    public float shipSpeedUnitsPerSecond = 1.5f;
    [Tooltip("停靠时间(秒)")]
    public float dockDurationSeconds = 1f;

    [Header("类型数量")]
    [Tooltip("形状类型数量")]
    public int shapeTypeCount = 4;
    [Tooltip("航线颜色数量")]
    public int lineColorCount = 3;
}

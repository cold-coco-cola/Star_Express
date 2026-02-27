using UnityEngine;

[System.Serializable]
public struct RewardPopupLayout
{
    public Vector2 panelSize;
    public Vector2 buttonSize;
    public float buttonGap;
}

[CreateAssetMenu(fileName = "VisualConfig", menuName = "Star Express/Visual Config", order = 2)]
public class VisualConfig : ScriptableObject
{
    [Tooltip("站点形状与乘客头顶形状图，按 ShapeType 索引。0-3: Circle/Triangle/Square/Star，4-7: Hexagon/Sector/Cross/Capsule")]
    public Sprite[] shapeSprites;
    [Tooltip("航线颜色，至少 3 色，按 LineColor 索引")]
    public Color[] lineColors;
    [Tooltip("飞船外观")]
    public Sprite shipSprite;
    [Tooltip("乘客身体表现")]
    public Sprite passengerSprite;

    [Tooltip("奖励选择界面图标，顺序: Carriage, StarTunnel, NewLine")]
    public Sprite[] rewardIcons;

    [Tooltip("奖励选择界面布局，留空则用默认值(480x400, 180x200, 30)")]
    public RewardPopupLayout rewardPopupLayout;
}

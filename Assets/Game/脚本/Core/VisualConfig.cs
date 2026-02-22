using UnityEngine;

[CreateAssetMenu(fileName = "VisualConfig", menuName = "Star Express/Visual Config", order = 2)]
public class VisualConfig : ScriptableObject
{
    [Tooltip("站点形状与乘客头顶形状图，按 ShapeType 索引")]
    public Sprite[] shapeSprites;
    [Tooltip("航线颜色，至少 3 色，按 LineColor 索引")]
    public Color[] lineColors;
    [Tooltip("飞船外观")]
    public Sprite shipSprite;
    [Tooltip("乘客身体表现")]
    public Sprite passengerSprite;
}

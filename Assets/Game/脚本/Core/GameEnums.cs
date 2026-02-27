// 与 PRD、技术文档 §8.3 一致。索引 0-3 为基础形状，4-7 为高级形状。
public enum ShapeType { Circle, Triangle, Square, Star, Hexagon, Sector, Cross, Capsule }

// 选线/建线用，6 色不重合
public enum LineColor { Red, Green, Blue, Yellow, Cyan, Magenta }

// 资源类型（PRD §4.6）
public enum ResourceType { Ship, Carriage, StarTunnel, Hub }

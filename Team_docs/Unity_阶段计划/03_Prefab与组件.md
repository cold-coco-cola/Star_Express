# Unity 阶段计划 — 03：Prefab 与组件

> 依据 [技术文档_星穹铁道_Unity.md](../技术文档_星穹铁道_Unity.md) §4、§14 编写。  
> **目的**：在 `_Project/Prefabs` 下创建 Station、Passenger、Ship 等实体 Prefab 及其组件占位，满足技术文档与 PRD 的实体定义，便于后续挂脚本与美术替换。  
> **依赖**：完成 [01_资源管理与文件布局](./01_资源管理与文件布局.md)、建议先完成 [02_场景与根节点](./02_场景与根节点.md)。  
> **索引**：[00_索引与文件管理](./00_索引与文件管理.md)。

---

## 1. 目标（做出什么样）

- **Assets/Game/预制体/** 下存在：**Station**、**Passenger**、**Ship** 三个实体 Prefab（Line 为纯数据+视觉由 LineManager 生成，可不做 LineSegment.prefab 或做可选占位）。
- 每个 Prefab 的**节点结构与组件**与技术文档 §4、§14 一致，便于后续绑定 StationBehaviour、PassengerBehaviour、ShipBehaviour 与 VisualConfig。

---

## 2. Station Prefab

### 2.1 结构（技术文档 §4.1、§14.2）

- **根节点**：空 GameObject，命名为 **Station**；Transform 位置 (0,0,0)，运行时由 LevelLoader 从 LevelConfig 写入 position。
- **子节点 Visual**：挂 **SpriteRenderer**，用于显示站点形状（圆/三角/方/星）；运行时从 VisualConfig.shapeSprites 按 shapeType 取 Sprite。Visual 下可预留 **Animator**（本阶段可不挂）。
- **根节点**上还需：**Collider2D**（用于点击检测）、**StationBehaviour**（后续阶段挂脚本）。

### 2.2 在 Unity 内的操作步骤

1. 在 Hierarchy 中 **GameObject → Create Empty**，命名为 **Station**。
2. 在 Station 上 **Add Component → Circle Collider 2D**（或 Box/Polygon，能覆盖站点显示区域即可），调整 Radius/Size 便于点击。
3. 在 Station 下 **Create Empty** 子节点，命名为 **Visual**；在 Visual 上 **Add Component → Sprite Renderer**，Sprite 可先留空或临时用 Unity 内置 2D 图形（如 Knob）。
4. 将 Station 从 Hierarchy 拖到 **Assets/Game/预制体/**，生成 **Station.prefab**；若需保留场景中的实例可保留，否则删除场景中的实例。

**结果**：Prefabs 目录下有 Station.prefab，结构为「根 Station（Collider2D）+ 子 Visual（SpriteRenderer）」；StationBehaviour 在脚本阶段再挂。

---

## 3. Passenger Prefab

### 3.1 结构（技术文档 §4.2、§14.2）

- **根节点**：命名为 **Passenger**；位置由当前站或当前船决定。
- **子节点 Body**：SpriteRenderer，从 VisualConfig.passengerSprite 取图；可预留 Animator。
- **子节点 TargetIcon**：用于头顶目标形状，从 VisualConfig.shapeSprites 取图。
- **根节点**上：**PassengerBehaviour**（后续挂脚本）。

### 3.2 在 Unity 内的操作步骤

1. **GameObject → Create Empty**，命名为 **Passenger**。
2. 在 Passenger 下创建子节点 **Body**，Add Component → **Sprite Renderer**（Sprite 可先空或占位）。
3. 在 Passenger 下再创建子节点 **TargetIcon**，Add Component → **Sprite Renderer**（用于头顶形状图标，可设 Sorting Order 略高于 Body）。
4. 将 Passenger 拖到 **Assets/Game/预制体/**，生成 **Passenger.prefab**。

**结果**：Prefabs 下有 Passenger.prefab，结构为「根 Passenger + Body（SpriteRenderer）+ TargetIcon（SpriteRenderer）」；PassengerBehaviour 在脚本阶段再挂。

---

## 4. Ship Prefab

### 4.1 结构（技术文档 §4.4、§14.2）

- **根节点**：命名为 **Ship**；位置由 Line 上 currentSegmentIndex + progressOnSegment 每帧更新。
- **子节点 Visual**：SpriteRenderer，从 VisualConfig.shipSprite 取图，运行时按 line.color 着色；可预留 Animator（Idle/Move/Dock）。
- **根节点**上：**Collider2D**（点击放置客舱用）、**ShipBehaviour**（后续挂脚本）。

### 4.2 在 Unity 内的操作步骤

1. **GameObject → Create Empty**，命名为 **Ship**。
2. 在 Ship 上 **Add Component → Circle Collider 2D**（半径略大于飞船显示，便于点击）。
3. 在 Ship 下创建子节点 **Visual**，Add Component → **Sprite Renderer**（Sprite 可先空或占位）。
4. 将 Ship 拖到 **Assets/Game/预制体/**，生成 **Ship.prefab**。

**结果**：Prefabs 下有 Ship.prefab，结构为「根 Ship（Collider2D）+ 子 Visual（SpriteRenderer）」；ShipBehaviour 在脚本阶段再挂。

---

## 5. LineSegment（可选）

- 技术文档 §4.3：Line 为纯 C# 数据，视觉由 LineManager 用 **LineRenderer** 等绘制，不强制要求 LineSegment.prefab。
- 若希望「线段」有统一 Prefab（例如每段一个 GameObject 带 LineRenderer），可在 Prefabs 下创建 **LineSegment**，根节点挂 **LineRenderer** + 可选 **EdgeCollider2D** 用于点击；本阶段可跳过，待 B 分工实现连线时再补。

---

## 6. UI Prefab 占位（可选）

- 资源栏、选色面板、结算界面等 UI Prefab 放在 **Assets/Game/预制体/UI/** 下。
- 本阶段可在 Prefabs/UI 下建空文件夹或简单占位 Panel，具体控件与布局见 UI 文档与后续阶段。

---

## 7. 执行后自检清单

- [ ] **Station.prefab**：根有 Collider2D，子节点 Visual 有 SpriteRenderer；保存在 _Project/Prefabs。
- [ ] **Passenger.prefab**：根下 Body、TargetIcon 均有 SpriteRenderer；保存在 _Project/Prefabs。
- [ ] **Ship.prefab**：根有 Collider2D，子节点 Visual 有 SpriteRenderer；保存在 _Project/Prefabs。
- [ ] 未强制要求 LineSegment.prefab；UI 占位可按需在 Prefabs/UI 下添加。

---

## 8. 与后续阶段的衔接

- **脚本**：在「脚本与 ScriptableObject 骨架」阶段，为 Station、Passenger、Ship 根节点挂 StationBehaviour、PassengerBehaviour、ShipBehaviour，并引用 VisualConfig 等。
- **美术**：将站点形状、乘客、飞船的 Sprite 放入 Art/Sprites 对应目录，在 VisualConfig 中配置，Prefab 的 Visual/Body/TargetIcon 在运行时或初始化时从 VisualConfig 取图。

---

*文档结束。*

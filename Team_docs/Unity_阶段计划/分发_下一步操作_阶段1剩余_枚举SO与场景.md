# 分发给任务负责人 — 下一步操作：阶段 1 剩余（枚举、SO、场景与数据）

> **适用对象**：负责「项目骨架与配置」的操作任务负责人。  
> **前置条件**：已按 [01_资源管理与文件布局](./01_资源管理与文件布局.md) 完成 `Assets/Game/` 目录结构及 Scripts 子目录（01 为第一步，已完成）。  
> **对应计划**：[星穹铁道实现计划] 阶段 1 — 任务 3～9。  
> **依据**：技术文档 §8、PRD §3.4.6、§8、计划附录 A。

---

## 本步目标与验收

**目标**：完成阶段 1 剩余内容，使场景可打开、三个 ScriptableObject 可在 Inspector 中编辑且无报错。

**验收**（计划原文）：

- 场景能打开（SolarSystem_01.unity）。
- LevelConfig、GameBalance、VisualConfig 能在 Inspector 中编辑，无报错。
- 第一关太阳系 7 站数据已填入 LevelConfig。

**资源需求**：无。Sprite 可暂不配置，仅验证数据结构。

---

## 一、创建枚举（计划任务 3）

### 1.1 放置位置与命名

- 在 **`Assets/Game/脚本/`** 下选择合适子目录（建议 **Core** 或 **Entities**），创建 C# 脚本。
- 若希望枚举单独管理，可在 **Scripts/Core** 下创建 **GameEnums.cs**（或拆成两个文件见下）。

### 1.2 ShapeType 枚举

- 与 PRD、技术文档 §8.3 一致：**Circle, Triangle, Square, Star**。
- 示例：

```csharp
public enum ShapeType { Circle, Triangle, Square, Star }
```

### 1.3 LineColor 枚举

- 至少 3 色，用于选线/建线；计划示例为 Red, Green, Blue。
- 示例：

```csharp
public enum LineColor { Red, Green, Blue }
```

### 1.4 操作步骤

1. 在 Project 中进入 `_Project/Scripts/Core`（或 Entities）。
2. 右键 → **Create → C# Script**，命名为 **GameEnums**（或 **ShapeType** / **LineColor** 分文件）。
3. 在脚本中定义上述两个枚举并保存。
4. 等待 Unity 编译无报错。

---

## 二、创建 GameBalance ScriptableObject（计划任务 4）

### 2.1 字段与默认值（对应 PRD §8）

| 字段名（建议） | 类型 | 默认值 | 说明 |
|----------------|------|--------|------|
| queueCapacity | int | 8 | 站点排队上限 |
| crowdingThreshold | int | 6 | 站点拥挤阈值 |
| crowdingDurationSeconds | float | 0 | 拥挤持续失败时间，0=立即失败 |
| passengerSpawnInterval | float | 5 | 乘客生成间隔(秒/站) |
| passengerSpawnCountPerStation | int | 1 | 每站同时生成数量 |
| weekDurationSeconds | float | 60 | 游戏内 1 周(秒) |
| shipCapacity | int | 4 | 飞船容量(人) |
| carriageCapacityIncrement | int | 2 | 客舱升级增量 |
| shipSpeedUnitsPerSecond | float | 1.5f | 飞船移动速度 |
| dockDurationSeconds | float | 1 | 停靠时间(秒) |
| shapeTypeCount | int | 4 | 形状类型数量 |
| lineColorCount | int | 3 | 航线颜色数量 |

### 2.2 操作步骤

1. 在 **`_Project/Scripts/Core`** 下创建 C# 脚本 **GameBalance**。
2. 类继承 **ScriptableObject**，并添加 `[CreateAssetMenu(...)]` 便于在 Project 中右键创建资产。
3. 声明上表字段（可加 `[Header]` / `[Tooltip]` 便于 Inspector 阅读）。
4. 在 **`Assets/Game/配置/`** 下：右键 → **Create** → 选择你菜单中的 GameBalance，命名为 **GameBalance**，保存。
5. 在 Inspector 中确认默认值已为上表（或在脚本中设默认值/在 Inspector 中手动填一次）。
6. 保存场景与项目，确保无报错。

---

## 三、创建 LevelConfig ScriptableObject（计划任务 5）

### 3.1 结构（对应 PRD 3.4.6、技术文档 §8.1）

- **关卡级**：levelId (string), displayName (string)。
- **站点列表**：stations — 每个站点含：id, displayName, shapeType（用 ShapeType 枚举）, position (Vector2 或 float x,y), unlockPhase（建议枚举或 string："fixed" / "random_pool"）, unlockAfterWeeks（int，仅 fixed 时有效）。
- **随机池**：randomPoolStations（string[] 或 List<string>，站点 id 列表）, randomUnlockPerWeek（int 或 int[2] 表示 [min, max]，如 [1,2]）。
- **覆盖**：overrides — 可选，用于覆盖 GameBalance 的：passengerSpawnInterval, passengerSpawnIntervalAfterWeeks, passengerSpawnIntervalLate（float / int 按你实现）。
- **可选**：startGift（计划已确认可选，支持开局赠送线或飞船）。

### 3.2 操作步骤

1. 在 **`_Project/Scripts/Core`** 下创建 **StationConfig**（或 LevelConfig 内嵌类）与 **LevelConfig** 脚本。
2. **StationConfig** 建议字段：id, displayName, shapeType (ShapeType), position (Vector2), unlockPhase (string 或枚举), unlockAfterWeeks (int)。
3. **LevelConfig** 继承 ScriptableObject，字段：levelId, displayName, stations (List<StationConfig>), randomPoolStations (List<string>), randomUnlockPerWeekMin, randomUnlockPerWeekMax（或 int[2]）, overrides（可选类，含 passengerSpawnInterval 等）, startGift（可选）。
4. 在 **ScriptableObjects** 下右键创建 **LevelConfig** 资产，命名为 **SolarSystem_01** 或 **LevelConfig_SolarSystem_01**。

---

## 四、创建 VisualConfig ScriptableObject（计划任务 6）

### 4.1 字段（技术文档 §8.4、计划附录 A.1）

| 字段名（建议） | 类型 | 说明 |
|----------------|------|------|
| shapeSprites | Sprite[] 或按 ShapeType 索引的数组/列表 | 站点形状与乘客头顶形状图，按 shapeType 取 |
| lineColors | Color[] 或按 LineColor 索引 | 航线颜色，至少 3 色 |
| shipSprite | Sprite | 飞船外观，运行时可按 line.color 着色 |
| passengerSprite | Sprite | 乘客身体表现 |

首版 Sprite 可留空，仅验证结构。

### 4.2 操作步骤

1. 在 **Scripts/Core** 下创建 **VisualConfig** 脚本，继承 ScriptableObject，声明上表字段。
2. 在 **ScriptableObjects** 下右键创建 **VisualConfig** 资产，命名为 **VisualConfig**。Inspector 中 Sprite 可暂不拖入，保存无报错即可。

---

## 五、填充第一关太阳系数据（计划任务 7）

### 5.1 数据来源（PRD 3.4.1、3.4.6）

| id | displayName | shapeType | position (x, y) | unlockPhase | unlockAfterWeeks |
|----|-------------|-----------|-----------------|-------------|------------------|
| sun | 太阳 | Circle | (0, 0) | fixed | 0 |
| mercury | 水星 | Triangle | (1.8, 0) | fixed | 0 |
| venus | 金星 | Square | (2.8, 1.6) | fixed | 1 |
| earth | 地球 | Star | (3.8, 0) | fixed | 2 |
| mars | 火星 | Circle | (4.2, -1.5) | random_pool | — |
| jupiter | 木星 | Triangle | (5.2, 1.0) | random_pool | — |
| saturn | 土星 | Square | (5.8, -0.8) | random_pool | — |

- **randomPoolStations**：["mars", "jupiter", "saturn"]  
- **randomUnlockPerWeek**：[1, 2] 表示每周随机解锁 1 或 2 站  
- **overrides**：passengerSpawnInterval = 6，passengerSpawnIntervalAfterWeeks = 2，passengerSpawnIntervalLate = 5

### 5.2 操作步骤

1. 在 Project 中选中已创建的 LevelConfig 资产（SolarSystem_01 或 LevelConfig_SolarSystem_01）。
2. 在 Inspector 中逐项填写：levelId = "solar_system_01"，displayName = "太阳系"，stations 列表 7 条（按上表），randomPoolStations、randomUnlockPerWeek、overrides 按上。
3. 保存资产（Ctrl+S 或 File → Save）。

---

## 六、创建 SolarSystem_01 场景与根节点（计划任务 8、9）

### 6.1 场景与相机（与 02_场景与根节点 一致）

1. **File → New Scene**，选 **Basic 2D** 或 **Empty**（空场景则需自带或添加 Main Camera）。
2. **File → Save As**，路径 **Assets/Game/场景/**，名称 **SolarSystem_01**，保存。
3. 选中 **Main Camera**：**Projection = Orthographic**，**Orthographic Size = 4～5**，**Position** 如 (0, 0, -10)。

### 6.2 根节点（技术文档 §3.1）

在 Hierarchy 中创建空节点，命名**必须一致**：

- **GameManager**（根级）
- **Map**（根级）
- **Stations**（建议拖为 Map 子节点）
- **Lines**（建议 Map 子节点）
- **Ships**（建议 Map 子节点）
- **Passengers**（建议 Map 子节点）
- **Canvas**（根级；若无则 **GameObject → UI → Canvas**）
- **EventSystem**（若无则 **GameObject → UI → Event System**）

**推荐层级**：

```
├── GameManager
├── Map
│   ├── Stations
│   ├── Lines
│   ├── Ships
│   └── Passengers
├── Canvas
└── EventSystem
```

### 6.3 保存

**File → Save**，确认场景在 `Assets/Game/场景/SolarSystem_01.unity`。

---

## 七、执行后自检（交回前必做）

请逐项勾选后交付：

- [ ] **枚举**：ShapeType、LineColor 已定义且编译通过。
- [ ] **GameBalance**：ScriptableObject 类与资产已创建，PRD §8 默认值已填或可编辑，Inspector 无报错。
- [ ] **LevelConfig**：ScriptableObject 类与资产已创建，含 stations、randomPoolStations、overrides 等；第一关 7 站数据已按 PRD 3.4.1/3.4.6 填妥。
- [ ] **VisualConfig**：ScriptableObject 类与资产已创建，含 shapeSprites、lineColors、shipSprite、passengerSprite（Sprite 可暂空）。
- [ ] **场景**：SolarSystem_01.unity 位于 _Project/Scenes，根节点/Map 子节点命名与上文一致，相机为 2D 正交、Size 4～5。
- [ ] **整体**：场景能打开，三个 SO 在 Inspector 中可编辑，控制台无报错。

---

## 八、参考与衔接

- **详细场景步骤**：见 [02_场景与根节点](./02_场景与根节点.md)。
- **后续阶段**：本步通过后进入阶段 2（站点生成与展示），将使用 LevelConfig、GameBalance、VisualConfig 与 Stations 根节点。
- **文档与计划**：技术文档 §8、PRD §3.4、§8、计划阶段 1 与附录 A。

---

*文档结束。请按上述顺序执行，自检全部勾选后交付。*

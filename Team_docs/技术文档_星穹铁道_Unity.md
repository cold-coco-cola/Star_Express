# 星穹铁道 — Unity 技术实现文档

> 基于 [PRD_星穹铁道.md](./PRD_星穹铁道.md) 的 Unity 实现指南。  
> **对应 PRD 版本：1.2**；PRD 升级时请同步更新此处。  
> 版本：1.2 · 目标：20 天内完成 1–2 个可玩关卡（无尽生存模式）。  
> **实现环境**：Unity 2022.3.62f1c1（或同系列 LTS）。

---

## 1. 文档目的与阅读对象

- **目的**：将 PRD 中的实体、行为、规则转化为 Unity 场景、Prefab、脚本、配置与执行顺序，供开发在编辑器和代码中直接落地。
- **阅读对象**：负责站点/乘客/UI（A）、连线/航线/动画（B）、运输/换乘/失败与得分（C）的开发者，以及负责关卡配置与资源系统的协作者。

**分工阅读指引**：产品/策划建议读 PRD 全文 → 本技术文档 §9、§10、§11；开发 A/B/C 建议读本技术文档 §4–§7、§10 → PRD 对应章节；新人/外部先读 PRD §1–3、§9 → 本技术文档 §2、§9。

---

## 2. 架构总览

### 2.1 高层架构

```
┌─────────────────────────────────────────────────────────────────┐
│                        GameManager (单例/场景根)                    │
│  - 周计时、资源发放、失败/得分事件、全局配置引用                      │
└─────────────────────────────────────────────────────────────────┘
         │                    │                    │
         ▼                    ▼                    ▼
┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│ LevelConfig  │    │ LineManager  │    │ Station[]    │
│ (SO/JSON)    │    │ Line 列表    │    │ 站点实例     │
└──────────────┘    └──────────────┘    └──────────────┘
         │                    │                    │
         │                    │                    │
         ▼                    ▼                    ▼
┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│ 站点解锁节奏  │    │ Ship[] 归属  │    │ Passenger[]  │
│ 乘客生成规则  │    │ 航线绘制     │    │ 排队/在船    │
└──────────────┘    └──────────────┘    └──────────────┘
```

- **数据驱动**：关卡（站点位置、形状、解锁阶段）由 `LevelConfig`（ScriptableObject 或 JSON）驱动，运行时生成 Station、按节奏解锁。
- **事件驱动**：得分、失败、资源发放、周切换等通过 C# 事件或 UnityEvent 通知 UI 与存档，避免强耦合。

### 2.2 建议目录结构（Assets）

```
Assets/
├── _Project/
│   ├── Scenes/
│   │   └── SolarSystem_01.unity
│   ├── Prefabs/
│   │   ├── Station.prefab
│   │   ├── Passenger.prefab
│   │   ├── Ship.prefab
│   │   ├── LineSegment.prefab（可选，用于线段渲染）
│   │   └── UI/（资源栏、选色面板、结算界面等）
│   ├── Scripts/
│   │   ├── Core/           # GameManager, LevelLoader
│   │   ├── Entities/       # Station, Passenger, Ship, Line
│   │   ├── Logic/          # Pathfinding, Boarding, Unboarding
│   │   ├── Input/          # LineDrawingInput, ResourcePlacementInput
│   │   └── UI/             # ScoreUI, FailUI, ResourcePanel
│   ├── ScriptableObjects/
│   │   ├── LevelConfig.asset（或 Levels/SolarSystem_01.asset）
│   │   ├── GameBalance.asset（全局数值）
│   │   ├── VisualConfig.asset（美术集中配置：形状/航线/飞船/乘客 Sprite，见 §8.4、§14）
│   │   └── ShapeTypes.asset（可选，形状与图标映射）
│   ├── Config/             # 若用 JSON，可放此处
│   │   └── solar_system_01.json
│   └── Art/                # 精灵、材质、背景（从简即可；结构见 §14）
│       ├── Sprites/Stations、Ships、Passengers、UI
│       └── Animations/     # 预留：Animator Controller、Animation Clip
```

---

## 3. 场景与层级（Scene Hierarchy）

### 3.1 建议根节点

| 节点名 | 说明 |
|--------|------|
| **GameManager** | 挂载 GameManager 脚本；子节点可挂周计时、资源计数、失败/得分逻辑 |
| **Map** | 地图容器；子节点为动态生成的 Station、Line 视觉 |
| **Stations** | **必须存在**：空节点，其子对象为运行时按 LevelConfig 生成的 Station 实例。GameManager 通过 `GameObject.Find("Stations")` 或 `Map/Stations` 解析父节点，**禁止使用 FindObjectsOfType&lt;Transform&gt; 等重开销兜底**。 |
| **Lines** | 空节点，其子对象为 Line 的视觉表示（如 LineRenderer 或 Sprite 线段） |
| **Ships** | 空节点，其子对象为运行时放置的 Ship 实例 |
| **Passengers** | 可选：若乘客为独立 GameObject，可统一放此节点下；若为 Station 下子对象则不必 |
| **Canvas / UI** | 所有 UI：资源栏、选色面板、结算界面、分数、倒计时等 |
| **EventSystem** | Unity 默认，用于 UI 与点击 |

### 3.2 坐标系与相机（详见 §10.1）

- **2D 正交**：建议使用 Orthographic Camera，Y 轴向上或 Z 轴向前按项目约定统一。
- **坐标单位**：**1 世界单位 = 1 Unity 单位**；PRD 太阳系约 x: 0~6, y: -1.5~1.6，可直接用于 `Transform.position`。
- **相机视野**：Orthographic Size 建议 4~5 以覆盖整图（7 站范围）；可被 LevelConfig 或项目设置覆盖。
- **UI 与世界坐标**：若需在世界坐标显示排队数等，用 `RectTransform` 或 `Camera.WorldToScreenPoint` 做转换。

---

## 4. Prefab 与组件清单（对应 PRD §4）

### 4.1 Station（站点/星星）

| 组件 | 说明 |
|------|------|
| **Transform** | position 由 LevelConfig 注入，运行时仅解锁前后可能改变显示（灰显/高亮） |
| **StationBehaviour**（自定义） | 持有：id, shapeType, displayName, queueCapacity, crowdingThreshold, waitingPassengers（List<Passenger>）, isUnlocked；引用 LevelConfig 或从 GameManager 取配置 |
| **Collider2D** | 用于点击检测（射线或 Overlap）；未解锁时仍可点击则需在逻辑里拦截 |
| **SpriteRenderer / 形状子节点** | 显示星星与形状（圆/三角/方/星）；形状可与 shapeType 对应不同 Sprite 或子 GameObject |
| **可选：StationView** | 仅负责 UI：排队人数、拥挤条、形状图标；从 StationBehaviour 读数据 |

**Prefab 约定**：position 在预制体上可为 (0,0)，运行时由 LevelLoader 从 LevelConfig 写入。为便于换图与动画扩展，采用**根节点 + Visual 子节点**（挂 SpriteRenderer，从 VisualConfig 按 shapeType 取 Sprite）；Visual 下可预留 Animator 组件（见 §14）。

### 4.2 Passenger（乘客）

| 组件 | 说明 |
|------|------|
| **Transform** | 位置跟随 currentStation 或 currentShip；在站时可为站点的子节点或相对偏移排队 |
| **PassengerBehaviour**（自定义） | 持有：id, targetShape, targetStationId（可选）, state（Waiting/OnShip/Arrived）, currentStation, currentShip |
| **Body 子节点**（SpriteRenderer） | 从 VisualConfig.passengerSprite 取图；可预留 Animator |
| **TargetIcon 子节点** | 头顶目标形状，从 VisualConfig.shapeSprites 取图 |
| **可选：PassengerView** | 仅负责头顶 targetShape 图标更新 |

**对象池**：乘客会频繁生成与移除，建议用对象池（Pool）管理 Passenger GameObject，Arrived 后回收到池而非 Destroy。

### 4.3 Line（航线）

- **数据结构**：Line 可为纯 C# 类（无 GameObject），由 LineManager 持有；视觉由单独 GameObject 负责。
- **LineView / LineRenderer**：根据 `stationSequence` 的站点顺序，用 `LineRenderer`（或多段 Sprite）在 Map 上绘制折线；颜色由 Line.color 决定。**实现约定**：航线视觉子物体统一命名 `"Line_视觉_" + line.id`；绘制时**过滤 stationSequence 中的 null**（站点被销毁时防空引用）；材质使用**缓存**（如 GetOrCreateLineMaterial），Shader 需做空与 isSupported 检查，**保证不返回 null**。
- **点击检测**：若需「点击某条线以放置飞船」，可用线段上的 Collider2D 或射线检测最近线段，并映射回 Line 实例。
- **延伸边界**：同色线延伸时要求 `stationSequence.Count >= 2`，否则不执行延伸，避免越界。

**Line 数据（C#）**：id, color（枚举或 Color）, stationSequence（List<Station>）, ships（List<Ship>）, segmentsPendingRemoval（首版可空列表）。

### 4.4 Ship（飞船）

| 组件 | 说明 |
|------|------|
| **Transform** | 位置由 Line 上 currentSegmentIndex + progressOnSegment 插值计算，每帧更新 |
| **ShipBehaviour**（自定义） | 持有：line, speed, capacity, passengers, currentSegmentIndex, direction, progressOnSegment, state（Moving/Docked）, dockRemainingTime；引用 GameBalance 获取速度、停靠时间等 |
| **Visual 子节点**（SpriteRenderer） | 从 VisualConfig.shipSprite 取图，按 line.color 着色；可预留 Animator（Idle/Move/Dock） |
| **Collider2D** | 若需「点击飞船放置客舱」，需可点击 |

**移动计算**：  
`segmentLength = Vector2.Distance(stationA.position, stationB.position)`  
`progressOnSegment += direction * speed * deltaTime / segmentLength`  
到达 1 或 0 时进入 Docked，当前站 = 对应端点，执行卸客→载客，然后倒计时结束后调头（若端点）或继续下一段。

### 4.5 Hub（空间站）

**首版不做**。可预留空 Prefab 或仅预留接口（如 `IHub`），换乘逻辑不依赖 Hub，全站可换乘。

---

## 5. 脚本（Script）清单与职责（对应 PRD §4、§6）

### 5.1 核心

| 脚本名 | 挂载位置 | 职责摘要 |
|--------|----------|----------|
| **GameManager** | 场景根 | 周计时（周 0 持续 60s 后首次发放）、资源发放（飞船+轮换客舱/星隧）、失败/得分事件、全局 GameBalance 引用、失败后停止计时与生成 |
| **LevelLoader** | **不挂到场景**（静态工具类） | 为 **public static class**，不继承 MonoBehaviour；由 GameManager 等调用 `LevelLoader.Load(...)`，在传入的 Stations 根下实例化 Station Prefab 并注入 position/shapeType/unlock 等；初始化 randomPoolStations 与阶段一/二逻辑 |
| **LevelConfig** | ScriptableObject 资产 | 对应 PRD 3.4.6 的 JSON 结构：levelId, displayName, stations[], randomPoolStations, randomUnlockPerWeek, overrides；详见 §8.1、§10.3 |

### 5.2 实体

| 脚本名 | 挂载位置 | 职责摘要 |
|--------|----------|----------|
| **StationBehaviour** | Station Prefab | 站点数据与行为：解锁、乘客生成计时、waitingPassengers、拥挤判定（通知 GameManager）、载客/卸客时的队列增删 |
| **PassengerBehaviour** | Passenger Prefab | 乘客状态机（Waiting/OnShip/Arrived）、targetShape/targetStationId、被上船/下船时更新 currentStation/currentShip |
| **Line**（纯数据 class） | 由 LineManager 持有 | 航线数据：color, stationSequence, ships；建线/延伸时修改 stationSequence；**无 GameObject**，视觉由 LineManager 驱动的 LineView/LineRenderer 单独负责。 |
| **LineManager** | GameManager 或 Map | 创建/延伸 Line、维护 Line 列表、为每条 Line 生成并更新视觉（LineRenderer）；选色规则（同色仅一条线） |
| **ShipBehaviour** | Ship Prefab | 移动与停靠、progressOnSegment 更新、到站时调用「卸客→载客」逻辑（可委托给 BoardingController） |

### 5.3 逻辑

| 脚本名 | 挂载位置 | 职责摘要 |
|--------|----------|----------|
| **PathfindingService** 或静态类 | GameManager 或独立 | 可达性：从站 S 到目标形状 T 的 BFS/DFS；节点=Station，边=同线相邻+同站换乘（首版全站可换乘） |
| **BoardingController** 或同模块 | GameManager 或 Ship | 单站单船：先卸客（目的地→换乘），再按载客优先级（首版先到先上）载客；多船时按 Line.id、Ship.id 顺序执行（id 排序规则详见 §10.4） |
| **UnlockController** | GameManager | 阶段一：周数 = unlockAfterWeeks 时解锁；阶段二：周 3 起每周末从 randomPoolStations 随机 1–2 站解锁 |

### 5.4 输入与 UI

| 脚本名 | 挂载位置 | 职责摘要 |
|--------|----------|----------|
| **LineDrawingInput** | Map 或 Camera | 点击站点 A→B，弹出选色；调用 LineManager 建线/延伸；需校验 isUnlocked、同色 A–B 不重复、延伸仅端点；选色与取消逻辑详见 §10.2 |
| **ResourcePlacementInput** | 与 UI 联动 | 放置飞船：点击 Line；放置客舱：点击 Ship；扣减资源并执行 4.6 行为；判定方式详见 §10.2 |
| **ResourcePanelUI** | Canvas 下 | 显示 shipCount, carriageCount, starTunnelCount；周发放时刷新；可选倒计时 |
| **ScoreUI / FailUI** | Canvas 下 | 分数显示；失败时弹出结算（得分、重试、返回），并通知 GameManager 停止逻辑 |

---

## 6. 数据流与关键接口（对应 PRD §4、§5）

### 6.1 配置数据流

- **LevelConfig（SO 或 JSON）** → LevelLoader 生成 Station 并注册到 GameManager 或 StationRegistry。
- **GameBalance（SO）** → GameManager、StationBehaviour（生成间隔、queueCapacity、crowdingThreshold）、ShipBehaviour（speed、capacity、停靠时间）、资源发放规则。
- **第一关 overrides**：LevelConfig.overrides 覆盖 GameBalance 的对应键（与 PRD 3.4.6 一致：passengerSpawnInterval、passengerSpawnIntervalAfterWeeks、passengerSpawnIntervalLate 等）。
- **开局赠送（可选）**：首版不严格要求「无预设航线与飞船」。若 LevelConfig 含 `startGift`，LevelLoader 在开局按 PRD 3.1 生成对应内容。例如：`startGift: { "line": ["sun", "mercury"] }` 表示预设一条太阳–水星线；`startGift: { "ship": 1 }` 表示开局多 1 艘飞船（需已有线）。无 startGift 时即无预设；有则与「无预设」检查项二选一。

### 6.2 运行时数据流

- **站点解锁**：UnlockController 每周末根据周数与 randomPoolStations 设置 Station.isUnlocked。
- **乘客生成**：StationBehaviour 内每站独立计时，满足间隔且 isUnlocked 时创建 Passenger，targetShape 按「已解锁形状均匀」、targetStationId 首版随机；加入 waitingPassengers。
- **船移动**：ShipBehaviour.Update 中 state==Moving 时更新 progressOnSegment，到达端点则 Docked，执行卸客→载客，倒计时结束后 state=Moving 并可能调头。
- **卸客/载客**：由 BoardingController 实现：输入当前站、当前船、当前线路图；输出为 passengers 与 waitingPassengers 的变更；可达性由 PathfindingService 提供。

### 6.3 建议接口（便于分工）

```csharp
// 资源类型（首版不含 Hub）
public enum ResourceType { Ship, Carriage, StarTunnel }

// 可达性（C 实现，A/B 调用）
bool PathfindingService.CanReach(Station from, ShapeType targetShape);

// 载客优先级（首版保持队列顺序即可）
IEnumerable<Passenger> GetBoardingOrder(Station station);

// 事件（GameManager 提供，UI 订阅）
event Action<int> OnScoreChanged;
event Action<Station> OnGameOver;
event Action<int, ResourceType> OnResourceChanged;
event Action<int> OnWeekAdvanced;
```

---

## 7. 每帧/每步执行顺序（对应 PRD §7.3）

在 **GameManager** 或统一 **GameLoop** 中按以下顺序更新（**未失败且未暂停时**）：

1. **周计时**：累计 deltaTime，若满一周则周数 +1，发放资源（shipCount+=1 + 轮换一项），触发 OnWeekAdvanced / 本周奖励 UI。
2. **站点解锁**：若本周刚进入新周，执行阶段一（unlockAfterWeeks）与阶段二（randomPoolStations 随机 1–2）。
3. **乘客生成**：遍历所有已解锁站，更新每站生成计时器；达到间隔则生成 N 人（N 来自配置），目标形状按已解锁形状均匀，targetStationId 首版随机。
4. **飞船更新**：遍历所有 Ship：Moving 的更新 progressOnSegment 并处理到站→Docked；Docked 的更新 dockRemainingTime，若到 0 则离站并可能调头。
5. **停靠时卸客/载客**：在「船刚进入 Docked」的那一帧内执行一次：先卸客（目的地 + 换乘），再载客（按优先级、可达且未满）。多船同站按 Line.id、Ship.id 顺序。
6. **取消线段**（首版不执行）：若某段在 segmentsPendingRemoval 且该段上无船，则从 stationSequence 移除并更新视觉。实现时可用 `#if false` 或条件编译屏蔽，便于日后打开。
7. **拥挤/失败检查**：遍历已解锁站，若某站 waitingPassengers.Count >= crowdingThreshold，触发失败（立即或持续超时），停止上述所有逻辑，弹出结算。

**注意**：卸客/载客应在「进入 Docked 的当帧」完成，而不是在倒计时结束的当帧，与 PRD 5.1 一致。

---

## 8. 配置与 ScriptableObject 结构

### 8.1 LevelConfig（ScriptableObject 或 JSON）

与 **PRD 3.4.6** 逐项对齐。必填字段：`id`, `displayName`, `shapeType`, `position`, `unlockPhase`；若 `unlockPhase=="fixed"` 则需 `unlockAfterWeeks`。另：`randomPoolStations`, `randomUnlockPerWeek`, `overrides`。overrides 与 PRD 3.4.6 字段说明一致：`passengerSpawnInterval`（前 N 周生成间隔）、`passengerSpawnIntervalAfterWeeks`（自第几周起切换）、`passengerSpawnIntervalLate`（周 2 起使用的间隔）；可选 `startGift`（见 §6.1）。

### 8.2 GameBalance（ScriptableObject）

对应 PRD 第 8 节：queueCapacity, crowdingThreshold, 拥挤持续失败时间, passengerSpawnInterval, 每站同时生成数量, 一周(秒), 飞船容量, 客舱升级增量, 飞船速度, 停靠时间, 形状类型数量, 航线颜色数量等。LevelConfig.overrides 可覆盖其中部分。

### 8.3 形状与颜色

- **ShapeType**：枚举 Circle, Triangle, Square, Star（与 PRD 一致）。
- **LineColor**：枚举或 Color 数组，至少 3 色；新建/延伸时选色由 UI 提供。

### 8.4 VisualConfig（ScriptableObject，美术集中配置）

为实现「简单换图、添加动画」的扩展空间，新增 **VisualConfig**，集中管理：

| 字段 | 说明 |
|------|------|
| shapeSprites | Sprite[] 或 Dictionary&lt;ShapeType, Sprite&gt;，按 shapeType 取站点/乘客头顶形状图 |
| lineColors | Color[] 或 Dictionary&lt;LineColor, Color&gt;，航线颜色 |
| shipSprite | Sprite，飞船外观（运行时可按 line.color 着色） |
| passengerSprite | Sprite，乘客身体表现 |

**换图方式**：在 Project 中替换 Sprite 资源，或在 Inspector 中修改 VisualConfig 的引用，无需改业务代码。详见 §14。

---

## 9. 首版实现检查清单（与 PRD 9.1 对齐）

- [ ] 太阳系单图、站点固定位置、两阶段解锁（周 0–2 固定，周 3 起随机池每周 1–2 站）。
- [ ] 开局按 LevelConfig 决定（无 startGift 则无预设航线与飞船）；首次资源在周 0 结束（60s）后发放。
- [ ] 3–4 种形状、3 条航线颜色；连线与延伸（不支持取消）。
- [ ] 乘客生成、头顶目标形状、飞船自动往返、卸客（目的地+换乘）与载客（先到先上）；全站可换乘。
- [ ] 资源：周、发放飞船+轮换客舱/星隧；放置飞船、客舱；**首关资源栏显示星隧，数量为 0**。
- [ ] 得分（每人 1 分）、失败（站点拥挤≥阈值）、结算界面（得分、重试、返回）。

### 9.1 实现前检查（启动前过审）

| 检查项 | 参考 |
|--------|------|
| **PRD 与技术文档版本对应关系已确认**（如「技术文档 1.2 对应 PRD 1.2」） | 文档头、PRD 头 |
| **LevelConfig / GameBalance 字段与 PRD 3.4.6、第 8 节一致** | §8.1、§8.2 |
| **首版不做项**（Hub、取消航线、空间站）在两份文档中表述一致 | PRD 9.2、§10.5 |
| **世界单位、生成间隔切换、多船同站顺序**等关键约定已对齐 | §10.1、§10.3、§10.4 |
| 世界单位与相机 Orthographic Size 是否确定 | §10.1 |
| 选色与取消操作逻辑是否明确 | §10.2 |
| 多船同站排序规则（Line.id、Ship.id）是否确定 | §10.4 |
| 扩展预留（取消航线、Hub、陨石带）实现深度是否约定 | §10.5 |
| 第一关生成间隔切换（周 2 起 5s）是否明确 | §10.3 |
| LevelConfig 字段与 PRD 3.4.6 是否对齐 | §8.1 |
| VisualConfig 与美术扩展（换图/动画预留）是否就绪 | §8.4、§14 |
| Unity 版本（2022.3.62f1c1 或同系列 LTS）是否一致 | 文档头、PRD 9.3 |
| **LevelLoader 为静态工具类**（不挂场景，由 GameManager 调用 Load） | §5.1 |
| **场景根节点 Stations 必须存在**（Map/Stations 或根 "Stations"）；Stations 父节点解析禁止 FindObjectsOfType | §3.1、§10.6 |

---

## 10. 实现细节与约定（结合 PRD 1.2 已确定）

以下为结合 PRD 1.2 与实现分工整理出的**可直接写入的实现约定**；可被 LevelConfig 或项目设置覆盖的项已注明。

### 10.1 视觉与表现

| 项 | 建议/已确定 |
|----|-------------|
| **世界单位与像素比** | 1 世界单位 = 1 Unity 单位；PRD 太阳系约 x:0~6, y:-1.5~1.6；Orthographic Size 建议 4~5 以覆盖整图；UI 标排队数用 RectTransform 或 WorldToScreenPoint 做世界坐标转换；可被 LevelConfig 或项目设置覆盖。 |
| **站点/星星美术** | 最低实现：4 个独立 Sprite（圆/三角/方/星）或 Sprite 表切片；未解锁站：`SpriteRenderer.color` 灰阶或 alpha 0.5；未解锁站不显示排队人数；displayName 可用 TextMeshPro 或 UI 显示，可选。 |
| **乘客表现** | 排队用固定偏移序列（如每人间隔 0.3 单位沿站台方向）；层叠：`SortingOrder = Station.baseOrder + 排队索引`，避免被站点遮挡；20 天工期紧时可先用简单堆叠（随机小偏移）替代。 |
| **航线绘制** | LineRenderer 宽度 0.1~0.15 单位；首版不做圆角；整条线始终绘制，不按解锁隐藏；选中/悬停：宽度×1.2 或 Material 高亮，由 LineManager 控制。 |
| **飞船外观** | 飞船按 line.color 着色（`SpriteRenderer.color = Line.color`）；停靠时不做动画，或仅做 0.1s 简单 Scale 脉冲；首版不做粒子。 |

### 10.2 交互与输入

| 项 | 建议/已确定 |
|----|-------------|
| **选色方式** | 新建线时弹出 3 色选色面板（3 按钮）；延伸时沿用该 Line 颜色，不再选色；取消选色时清除第一站高亮、退出建线模式。 |
| **放置飞船** | 射线检测 LineRenderer 所在 GameObject 的 Collider2D（线段用 EdgeCollider2D 或 BoxCollider2D 覆盖）；多条线重叠时选**最近线段**（射线交点距离最短）；若无 Collider，可用「点到线段距离 < 阈值」做二次判定。 |
| **放置客舱** | 允许移动中点击；Ship 挂 CircleCollider2D，半径略大于 Sprite 即可。 |
| **取消操作** | 选站 A 后，再点 A 或空白 → 取消第一站选择、清除高亮；选色面板必须有「取消」按钮，行为同上。 |
| **选色面板与 Canvas** | **优先复用场景已有 Canvas**：选色面板（ColorPickPanelUI）在需要时先 `FindObjectOfType<Canvas>()`，有则在其下挂面板；**仅在没有 Canvas 时才新建** ColorPickCanvas，避免重复创建多个 Canvas。EventSystem 使用 `EventSystem.current` 判断，无需 FindObjectOfType&lt;EventSystem&gt;。 |

### 10.3 数值与节奏

| 项 | 建议/已确定 |
|----|-------------|
| **第一关生成间隔切换** | `passengerSpawnIntervalAfterWeeks == 2` 表示**自周 2 第 0 秒起**使用 passengerSpawnIntervalLate（5s）；周 0、1 使用 passengerSpawnInterval（6s）。LevelConfig 字段说明同步。 |
| **随机池解锁数量** | `randomUnlockPerWeek = [1, 2]` 时，`Random.Range(1, 3)` 得到 1 或 2，概率 50%:50%；池中剩 1 站时只解锁该站；剩 0 站时不执行。 |
| **拥挤持续失败时间** | 首版第一关 crowdingDuration = 0，立即失败。**crowdingDuration > 0 时**（后续关卡）：危险站采用红色闪烁或 SpriteRenderer 颜色变化；可选 3~10s 倒计时进度条，具体 UI 可后续定。 |

### 10.4 边界与异常

| 项 | 建议/已确定 |
|----|-------------|
| **同色仅一条线** | A、B 均非 C 线端点时，提示固定为「请选择其他颜色，或将新站连到现有线路端点」；选色面板中，已满 3 条线时不再提供「新建」选项（仅延伸），或新建时自动过滤不可用颜色。 |
| **多船同站顺序** | Line.id、Ship.id 采用 `string.CompareOrdinal` 或 `System.StringComparer.Ordinal` 排序；id 生成格式建议 `"Line_0"`, `"Line_1"`, `"Ship_0"` 等，保证字典序与创建顺序一致。 |
| **周 0 时长** | 周 0 也显示「本周剩余时间」倒计时；首次发放与后续周使用同一「本周奖励」弹窗，内容为「飞船×1 + 轮换一项（首周为客舱）」与 PRD 4.6 一致。 |

### 10.5 扩展预留的实现深度

| 项 | 建议/已确定 |
|----|-------------|
| **取消航线** | 本阶段**仅预留**：Line 保留 `segmentsPendingRemoval` 字段（可空列表）；移除段、路径分裂、船重定位等逻辑写注释与接口，**不实现 UI 与交互**；LineManager 预留 `RequestRemoveSegment(Line, Station, Station)` 空实现或 `#if false` 占位。 |
| **Hub / 空间站** | Station 预留 `Hub hub` 字段（可 null）；PathfindingService 当前以「任意共站多线可换乘」实现；日后改为「仅 hub != null 的站可换乘」时，在 BFS 边的构建逻辑中增加 `station.Hub != null` 判断；放置空间站入口可预留灰色按钮，OnClick 暂不实现。 |
| **陨石带与星隧** | LevelConfig 预留 `List<ObstacleRegion> obstacles`（可空）；LineSegment 或连线逻辑预留 `requiresStarTunnel: bool`；首版不读取、不使用；第二关若启用，再实现障碍检测与星隧消耗。 |

### 10.6 性能与健壮性（实现必守）

| 项 | 建议/已确定 |
|----|-------------|
| **GameManager 获取 LineManager** | 使用 **ILineManager** 接口；首次解析后**缓存**（如 `_cachedLineManager`），避免每帧或每次点击都用反射/GetComponent。 |
| **Stations 父节点解析** | 仅通过 `GameObject.Find("Stations")` 或 `Map/Stations` 查找；**禁止**使用 `FindObjectsOfType<Transform>()` 等全场景遍历兜底。 |
| **LineManager 材质** | GetOrCreateLineMaterial 需做 Shader 空检查与 isSupported；最终 fallback 用 `Shader.Find("Sprites/Default")` 或 `"Standard"` 时也需判 null 再 `new Material(shader)`，**保证永不返回 null**。 |
| **LineManager 绘制** | 遍历 stationSequence 时跳过 **null**（站点被销毁或未初始化），只对有效站点 SetPosition，避免 NullReferenceException。 |

### 10.7 存档与元进度

| 项 | 建议/已确定 |
|----|-------------|
| **存档** | 首版不做「继续游戏」；失败/退出后仅有「重试」（重新加载关卡）和「返回」；不做持久化。若后续需要存档，建议持久化：周数、资源、Line 列表（含 stationSequence）、Ship 列表（含 line、capacity、currentSegmentIndex 等）、Passenger 列表（含 state、currentStation 等）、得分。 |
| **关卡选择** | 若有 2 关，结算界面增加「选择关卡」或「下一关」按钮；场景命名 `SolarSystem_01`、`SolarSystem_02`，或通过 LevelConfig 列表选 levelId 再加载同一场景并注入对应配置。 |

---

## 11. 与 PRD 的映射索引

| PRD 章节 | 本技术文档对应 |
|----------|----------------|
| 3 地图与生成规则 | §3 场景与层级、§4.1 Station、§5.1 LevelLoader/UnlockController、§8.1 LevelConfig |
| 4 实体与数据定义 | §4 Prefab 与组件、§5.2 实体脚本、§6 数据流 |
| 5 运输与换乘 | §5.3 PathfindingService、BoardingController、§7 执行顺序 |
| 6 玩家操作 | §5.4 LineDrawingInput、ResourcePlacementInput、§10.2 交互具体化 |
| 7 得分与失败 | §5.1 GameManager、§5.4 ScoreUI/FailUI、§7 失败检查 |
| 8 数值与参数 | §8.2 GameBalance、§8.1 overrides |

---

## 12. PRD 与技术文档差异表（约定对齐用）

| PRD 表述 | 技术文档约定 | 备注 |
|----------|--------------|------|
| 1 单位 ≈ 100px | 1 单位 = 1 Unity 单位（§3.2、§10.1） | 技术文档以 Unity 单位为准；像素比可由相机/画布另行设置 |
| 前 2 周 6s、第 3 周起 5s | 周 0、1 用 6s；自周 2 起 5s（§10.3） | 等价，技术文档采用更精确描述 |
| 轮换客舱/星隧 | 首版不含 Hub，客舱→星隧→客舱（§6.3、§10.4） | 一致 |
| 多船同站顺序 | Line.id、Ship.id 的 Ordinal 排序（§10.4） | PRD 5.1 补充后对齐 |
| 开局赠送 | LevelConfig.startGift 可选（§6.1、§9） | 有 startGift 时与「无预设」检查项二选一 |

---

## 13. 文档维护约定

| 约定 | 说明 |
|------|------|
| **变更同步** | PRD 规则类变更（如 §4–§6、§7）时，同步更新技术文档对应节；技术实现约定（如 §10）变更若影响玩法，需在 PRD 中体现或注明。 |
| **版本对应** | PRD 与技术文档版本号可独立，但需在各自头部注明对应关系（如「技术文档 1.2 对应 PRD 1.2」）。 |
| **争议处理** | 规则冲突时以 PRD 为准；实现细节（如坐标、Collider、执行顺序）以技术文档为准。 |

---

## 14. 美术修改方法与扩展空间（对应 PRD 9.3）

为实现「简单换图、添加动画」的修改空间，实施时采用以下约定。

### 14.1 VisualConfig 与换图

- 使用 **VisualConfig**（§8.4）集中管理：站点形状 `shapeSprites`、航线 `lineColors`、飞船 `shipSprite`、乘客 `passengerSprite`。
- **换图**：将新 Sprite 放入 `Art/Sprites/` 对应子目录（Stations、Ships、Passengers、UI），在 VisualConfig 的 Inspector 中拖拽替换引用，无需改代码。

### 14.2 Prefab 结构与动画扩展

| 实体 | 结构 | 换图入口 | 动画扩展 |
|------|------|----------|----------|
| Station | 根 + **Visual** 子节点（SpriteRenderer） | VisualConfig.shapeSprites[shapeType] | Visual 下预留 Animator，可挂 AnimatorController |
| Ship | 根 + **Visual** 子节点（SpriteRenderer） | VisualConfig.shipSprite，按 line.color 着色 | Visual 可挂 Animator，预留 Idle/Move/Dock 等状态 |
| Passenger | 根 + **Body**（Sprite）+ **TargetIcon**（头顶形状） | Body→passengerSprite，TargetIcon→shapeSprites | Body 可挂 Animator，预留 Idle/Walk 等 |
| Line | LineRenderer 的 Material/Color | VisualConfig.lineColors | 可选：线段发光、流动材质 |

### 14.3 Art 目录建议

```
Assets/Game/美术/
├── Sprites/
│   ├── Stations/      # 圆/三角/方/星四种形状
│   ├── Ships/
│   ├── Passengers/
│   └── UI/           # 资源栏图标等
├── Animations/       # 预留：.anim 或 Animator Controller
│   ├── Station/
│   ├── Ship/
│   └── Passenger/
└── Materials/        # 航线材质等
```

### 14.4 修改流程简述

1. **换图**：新 Sprite 放入对应目录，在 VisualConfig 中替换引用。
2. **加动画**：在 Prefab 的 Visual/Body 子节点添加 Animator；新建 Animator Controller 与 Animation Clip，按状态触发（如 Ship 的 Docked 时播放停靠动画）。
3. **换色**：修改 VisualConfig 中 lineColors 或 GameBalance 中颜色字段。

---

*文档结束。实现时请以 PRD 为准，本技术文档为落地时的结构与分工参考。§10 中的约定可被 LevelConfig 或项目设置覆盖。*

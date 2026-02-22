# Star Express - UI Toolkit 界面

本目录包含所有 UI Toolkit 相关资源，统一用 UI Builder 调整。

## 文件结构

| 文件 | 用途 |
|------|------|
| `_Variables.uss` | 共用样式（如 `.hidden`），由各界面 USS 导入 |
| `ShipPlacementUI.uxml` / `.uss` | 飞船放置：圆形按钮 + 储备数字 + 扇环路线选择 |
| `ColorPickPanel.uxml` / `.uss` | 选色面板：连线时选择红/绿/蓝 |

## 使用方式

1. 菜单 **Star Express → 创建 GameUI (UI Toolkit)** 在场景中创建完整 GameUI（推荐）
2. 使用 **Window → UI Toolkit → UI Builder** 打开对应 UXML 进行可视化和样式调整
3. 修改 `.uss` 或先在 UI Builder 中编辑后保存

## 飞船放置 UI 手动挂载

若需单独创建：新建空物体 → 添加 UIDocument（Source Asset 指定 `ShipPlacementUI.uxml`）→ 添加 ShipPlacementUIToolkit。

## 样式导入

各 USS 通过 `@import url("_Variables.uss");` 引用共用样式。

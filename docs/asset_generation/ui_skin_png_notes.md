# UI 皮肤 PNG 生成记录

## 本次范围

子任务 H 只生成 UI 面板、按钮和列表行 PNG，不修改 C#、`.tres`、`.tscn`、`project.godot`、现有 SVG 或其他资源。

写入文件：

- `assets/ui/orbit/buttons/button_normal.png`
- `assets/ui/orbit/buttons/button_hover.png`
- `assets/ui/orbit/buttons/button_pressed.png`
- `assets/ui/orbit/buttons/button_disabled.png`
- `assets/ui/orbit/panels/orbit_panel_frame.png`
- `assets/ui/orbit/panels/orbit_list_row.png`
- `assets/ui/buttons/button_primary.png`
- `assets/ui/panels/panel_frame.png`

## 设计依据

已读取并对齐以下文档：

- `docs/UI设计文档.md` 12.1-12.4：面板、按钮、页签和列表行通用组件规范。
- `docs/UI效果图提示词.md`：暗金属底、硬边工业终端、青绿信息光、琥珀警示、锈红危险和低饱和数据紫。
- `docs/统一库存容器UI设计方案.md`：统一库存容器、列表视图、道具所在位置和操作区的一致视觉语言。

执行时按以下规则控制：

- 面板由外框、内底和标题条组成，中心区域保持低噪声暗底，便于后续承载 UI 内容。
- 按钮使用矩形切角、机械边框和 2-6 px 视觉倒角。
- 列表行保持固定高度和高密度扫描结构，不做卡片式大留白。
- 所有图均不包含文字、图标或业务数值，避免拉伸后出现语义残留。

## 生成方式

- 输出格式：PNG，RGBA。
- 生成工具：临时仓库外 Python venv 中的 Pillow `10.4.0`。
- 参考来源：`docs/ui_mockups/orbit_station_inventory_concept_v2.png` 和 `docs/ui_mockups/surface_hud_inventory_concept_v2.png` 的无文字金属区域，仅用于取色、噪声和边缘磨损参考。
- 按钮、面板和列表行结构均由 Pillow 原创绘制，包括切角遮罩、边线、内阴影、扫描线、磨损划痕和状态高亮。
- 效果图中的按钮、列表文字和图标区域不适合作为直接切图来源，因此未把带文字区域裁入目标资产。
- 未读取任何现有 `.svg` 作为图像源。
- 未使用 SVG 栅格化。

## 尺寸和九宫格建议

| 文件 | 尺寸 | 用途 | 建议九宫格边距 |
|---|---:|---|---|
| `assets/ui/orbit/buttons/button_normal.png` | `320x72` | 轨道按钮默认态 | 左右 24，上下 14 |
| `assets/ui/orbit/buttons/button_hover.png` | `320x72` | 轨道按钮悬停态 | 左右 24，上下 14 |
| `assets/ui/orbit/buttons/button_pressed.png` | `320x72` | 轨道按钮按下态 | 左右 24，上下 14 |
| `assets/ui/orbit/buttons/button_disabled.png` | `320x72` | 轨道按钮禁用态 | 左右 24，上下 14 |
| `assets/ui/buttons/button_primary.png` | `320x72` | 通用主要确认按钮 | 左右 24，上下 14 |
| `assets/ui/orbit/panels/orbit_panel_frame.png` | `480x320` | 轨道站主面板 / 详情面板框 | 左右 32，上 56，下 28 |
| `assets/ui/panels/panel_frame.png` | `480x320` | 通用 HUD / 子面板框 | 左右 32，上 36，下 28 |
| `assets/ui/orbit/panels/orbit_list_row.png` | `640x86` | 轨道库存和审计列表行 | 左右 18，上下 12 |

## 状态差异

- `button_normal.png`：暗金属底，低强度青绿边线。
- `button_hover.png`：更高亮的青绿色外框和内发光。
- `button_pressed.png`：更暗的内陷底，琥珀确认边线和下沿反馈。
- `button_disabled.png`：低对比灰化、斜向遮罩和弱边线。
- `button_primary.png`：琥珀主确认边框，区别于轨道普通按钮的青绿信息态。

## 验证记录

- 8 个目标 PNG 均已生成。
- 所有输出均为 RGBA PNG。
- 所有输出尺寸与子任务 H 要求一致。
- 视觉检查确认目标 PNG 中没有文字、图标或由效果图带入的业务数值。
- 未修改现有 SVG、`.import`、运行时代码、数据定义、场景或项目配置。

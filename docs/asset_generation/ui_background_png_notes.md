# UI Background PNG Generation Notes

## 生成范围

本次只生成并写入以下 PNG 背景资源：

| 文件 | 尺寸 | 用途 |
| --- | --- | --- |
| `assets/ui/orbit/backgrounds/orbit_station_command_deck.png` | 1600x900 | 轨道母舰后勤终端 / 命令甲板背景 |
| `assets/ui/backgrounds/orbit_station_background.png` | 1600x900 | 轨道站全屏功能界面背景 |
| `assets/ui/backgrounds/surface_expedition_background.png` | 1280x720 | 地表远征俯视废土基地背景 |
| `assets/ui/backgrounds/return_summary_background.png` | 1600x900 | 回归结算 / 轨道写回审计终端背景 |

## 依据

- `docs/UI设计文档.md`
- `docs/UI效果图提示词.md`

## 生成方式

- 使用内置 `image_gen` 生成原始位图。
- 使用本地位图缩放将生成结果整理为目标尺寸。
- 未读取现有 `.svg` 作为图像源。
- 未执行 SVG 转 PNG。
- 未使用 SVG 栅格化。

## 统一约束

- 16:9 横向背景。
- 不包含可读 UI 文本、中文标签、英文标签、数字面板或 HUD 文本。
- 不作为完整 UI 截图；只作为后续 UI 覆盖的背景底图。
- 风格遵循暗金属、旧工业、低功耗青绿信息光、少量琥珀警示光。
- 不使用全局资源池表现，不加入调试入口、直跳场景按钮或临时验证 UI。

## 单图说明

### `orbit_station_command_deck.png`

- 画面内容：废弃轨道母舰内部命令甲板、舷窗、旧控制台、线缆和暗金属地面。
- 设计目的：为轨道母舰后勤终端提供空间感，中央和下方保留较暗区域，便于覆盖高密度 UI 面板。
- 约束：不带具体文字，不做现代桌面窗口，不做明亮赛博霓虹。

### `orbit_station_background.png`

- 画面内容：轨道站后勤终端墙面、边缘设备、空白暗金属中央背景。
- 设计目的：适合作为轨道站库存、交易、研发、角色和空投页签的底层背景。
- 约束：中央区域保持低干扰，不出现文件夹树、路径栏、系统窗口按钮或可读终端字样。

### `surface_expedition_background.png`

- 画面内容：地表俯视废土基地，包含空投舱、仓储、太阳能板、维修站、装配设施、矿产点和可通行地形。
- 设计目的：保留 RTS 地图可读性，便于后续叠加顶部状态、选择面板、小地图、消息和命令按钮。
- 约束：不带 HUD 文本，不把矿产表现为顶部全局资源栏。

### `return_summary_background.png`

- 画面内容：轨道写回审计终端空间，暗金属记录墙、空白审计屏、底部旧控制台和沉稳灯光。
- 设计目的：为回归结算界面的收益、损失、遗留、发现和写回确认提供沉稳背景。
- 约束：不做胜利庆典，不带具体审计文字或数字。

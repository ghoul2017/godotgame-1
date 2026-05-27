# 轨道 UI 功能图标 PNG 旧生成记录

> 本记录为已废弃的早期生成记录。该批资源曾通过 SVG 栅格化得到 PNG，后续已由 `orbit_ui_icon_png_notes.md`、`orbit_trade_research_png_notes.md` 和 `icon_style_audit.md` 记录的透明背景位图规则覆盖。后续不得按本文方式生成正式图标。

## 生成依据

- `docs/UI设计文档.md`：轨道站图标需要清晰剪影、低饱和金属底、少量状态色，并在 32 px 下能识别分类。
- `docs/UI效果图提示词.md`：轨道站界面使用硬边工业终端、暗金属底、青绿信息光、琥珀警示、锈红危险和低饱和数据紫。
- `docs/3. 轨道站主框架.md`：第三步需要落地页签、分类、状态、首批交易条目和首批研发条目图标，且不能是缺失图标、纯色块或通用占位。

## 生成方式

- 源文件：目标目录中现有同名 `.svg`。
- 输出文件：同目录同名 `.png`。
- 渲染工具：macOS Quick Look `qlmanage -t -s 128`。
- 输出规格：`128x128` RGBA PNG。
- 调色说明：`assets/ui/orbit/status/completed.png` 在生成 PNG 时将完成状态主色归一为数据紫，用于补齐状态组的色彩差异；未修改对应 SVG 源文件。
- 本次未修改 SVG、C#、`.tres`、`.tscn` 或 `project.godot`。

## 覆盖清单

### 通用 UI 图标

- `assets/ui/icons/summary_cargo.png`
- `assets/ui/icons/summary_discovery.png`
- `assets/ui/icons/summary_loss.png`
- `assets/ui/icons/surface_command.png`
- `assets/ui/icons/surface_minimap.png`
- `assets/ui/icons/tab_characters.png`
- `assets/ui/icons/tab_drop.png`
- `assets/ui/icons/tab_inventory.png`
- `assets/ui/icons/tab_research.png`
- `assets/ui/icons/tab_trade.png`

### 轨道库存分类图标

- `assets/ui/orbit/categories/category_all.png`
- `assets/ui/orbit/categories/category_blueprint.png`
- `assets/ui/orbit/categories/category_chip.png`
- `assets/ui/orbit/categories/category_equipment.png`
- `assets/ui/orbit/categories/category_key_item.png`
- `assets/ui/orbit/categories/category_material.png`
- `assets/ui/orbit/categories/category_mineral.png`
- `assets/ui/orbit/categories/category_unit_platform.png`

### 轨道状态图标

- `assets/ui/orbit/status/available.png`
- `assets/ui/orbit/status/completed.png`
- `assets/ui/orbit/status/credits.png`
- `assets/ui/orbit/status/insufficient.png`
- `assets/ui/orbit/status/locked.png`

### 轨道交易图标

- `assets/ui/orbit/trade/trade_ai_chip_basic.png`
- `assets/ui/orbit/trade/trade_basic_energy_cells.png`
- `assets/ui/orbit/trade/trade_basic_repair_tools.png`
- `assets/ui/orbit/trade/trade_basic_scanner.png`
- `assets/ui/orbit/trade/trade_service_bot_platform.png`

### 轨道研发图标

- `assets/ui/orbit/research/research_basic_assembly.png`
- `assets/ui/orbit/research/research_basic_scanning_protocol.png`
- `assets/ui/orbit/research/research_drop_pod_capacity_1.png`
- `assets/ui/orbit/research/research_field_repair_protocol.png`
- `assets/ui/orbit/research/research_rocket_part_fabrication.png`

## 验证记录

- SVG 数量：33。
- PNG 数量：33。
- 所有 PNG 尺寸均为 `128x128`。
- 交易图标分别表达能源块、维修工具、扫描器、服务型平台和 AI 芯片。
- 研发图标分别表达基础组装、野外维修协议、扫描协议、火箭部件制造和空投舱容量审计。
- 状态图标保留可执行绿、信用琥珀、危险锈红、锁定灰和完成数据紫的差异。

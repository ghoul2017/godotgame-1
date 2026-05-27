# 数据图标 PNG 生成记录

## 本次范围

返工子任务 L 覆盖生成数据图标 PNG，只写入以下范围：

- `assets/data/items/*.png`
- `assets/data/skills/*.png`
- `assets/data/events/*.png`
- `assets/data/drop_pods/*.png`
- `docs/asset_generation/data_icon_png_notes.md`

本次没有修改 C#、`.tres`、`.tscn`、`project.godot`、SVG 源文件或其他 UI 资源。

## 返工原因

上一版数据图标虽然是 PNG，但视觉上仍有明显矢量图标库残留：

- 多数图标被放进统一方框、终端卡片或道具格边框中。
- 画面使用扫描线背景和细线几何符号，导致图标像 UI 符号而不是库存物件。
- 多个对象依赖平面几何轮廓，缺少体积、材质、磨损、局部高光和软投影。

本轮新规则：

- 图标必须是透明背景上的具体物件、设备、矿石或工具。
- 不复刻上一版卡框、扫描线、统一底板、线框符号或纯几何徽标。
- 主体按 128x128 画布约 75-85% 的尺寸绘制和归一化，保证 48px 下仍可辨识。
- 每个对象使用体积面、材质噪点、局部高光、磨损斑点、不规则边缘和软投影增强实物感。

## 依据

已读取并对齐以下文档和反馈：

- `docs/UI设计文档.md`
- `docs/UI效果图提示词.md`
- 本文件上一版记录
- 返工反馈：角色/单位可以保留，数据图标必须摆脱矢量库感

执行时按以下规则检查图标语义：

- 图标服务统一道具、库存、技能、事件和空投舱数据，不表现全局资源池。
- 风格保持旧工业金属、低饱和暗底、清晰剪影，使用少量青绿、琥珀和数据紫。
- 道具图标按具体物件绘制，区分矿产、加工产物、数据存储、芯片、工具、武器、平台、建筑模块、火箭部件和改装件。
- 技能图标用具体设备或工具承载含义，避免抽象线框徽标。
- 事件图标表达废墟中的实体信号缓存设备。
- 空投舱图标表达一次性投送舱实体。

## 生成方式

- 生成方式：按对象语义原创绘制位图 PNG。
- 工具：Python 标准库脚本，直接绘制 RGBA 像素、多边形、椭圆、渐变、噪点、磨损斑点、局部高光、软投影和 PNG chunk。
- 输入图像：无。
- 输出：同目录同名 `.png`。
- 尺寸：`128x128`。
- 格式：PNG，RGBA。
- 明确声明：未读取现有 SVG，未使用 SVG 栅格化，未从 SVG 转 PNG，未复刻上一版图标结构。

## 输出清单

道具：

- `assets/data/items/ai_chip.png`
- `assets/data/items/ai_chip_basic.png`
- `assets/data/items/alloy.png`
- `assets/data/items/building_module.png`
- `assets/data/items/clean_data.png`
- `assets/data/items/data_core.png`
- `assets/data/items/electronic_parts.png`
- `assets/data/items/energy_cell.png`
- `assets/data/items/metal.png`
- `assets/data/items/mod_part.png`
- `assets/data/items/rare_earth.png`
- `assets/data/items/repair_tool_basic.png`
- `assets/data/items/rifle_basic.png`
- `assets/data/items/rocket_part.png`
- `assets/data/items/scanner_basic.png`
- `assets/data/items/scrap.png`
- `assets/data/items/service_bot_platform.png`
- `assets/data/items/servo_mod_basic.png`
- `assets/data/items/silicon.png`
- `assets/data/items/tool.png`
- `assets/data/items/unit_platform.png`
- `assets/data/items/weapon.png`

技能：

- `assets/data/skills/control.png`
- `assets/data/skills/engineering.png`
- `assets/data/skills/mining.png`
- `assets/data/skills/shooting.png`

事件：

- `assets/data/events/ruin_signal_cache.png`

空投舱：

- `assets/data/drop_pods/drop_pod_single_use.png`

## 验证记录

- 输出数量：28 个 PNG。
- 所有输出均为 `128x128`。
- 所有输出均为 PNG RGBA。
- 所有输出四角 alpha 为 0，背景透明。
- 所有输出主体最大边已归一化到约 104px。
- 视觉检查确认没有统一方框、终端卡片、道具格边框或扫描线背景。
- 所有输出均为原创位图绘制，不是现有 SVG 的栅格化结果。
- 未修改运行时代码、数据资源定义、场景或项目配置。

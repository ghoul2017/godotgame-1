# 轨道交易和研发 PNG 图标生成记录

## 本次范围

本次返工覆盖以下 10 个轨道站交易和研发图标 PNG：

- `assets/ui/orbit/trade/trade_basic_energy_cells.png`
- `assets/ui/orbit/trade/trade_basic_repair_tools.png`
- `assets/ui/orbit/trade/trade_basic_scanner.png`
- `assets/ui/orbit/trade/trade_service_bot_platform.png`
- `assets/ui/orbit/trade/trade_ai_chip_basic.png`
- `assets/ui/orbit/research/research_basic_assembly.png`
- `assets/ui/orbit/research/research_field_repair_protocol.png`
- `assets/ui/orbit/research/research_basic_scanning_protocol.png`
- `assets/ui/orbit/research/research_rocket_part_fabrication.png`
- `assets/ui/orbit/research/research_drop_pod_capacity_1.png`

## 返工原因

上一版把 UI 框、硬边终端面板、扫描线和蓝图底烘焙进了图标本体，导致交易和研发条目在 48px 下读起来像矢量面板或蓝图卡片，而不是可交易、可研发的具体资产。研发图标尤其容易退化为整张方形蓝图 UI，交易图标也缺少补给物件自身的体积和材质。

新规则是：图标必须是透明背景上的具体物件。交易图标以交易得到的物件、补给箱或设备为主体；研发图标以蓝图纸卷、协议板、制造夹具、火箭夹具、空投舱扩容结构等具体资产为主体。图标内部可以有材质磨损、体积阴影和少量状态色，但不能把统一方框、终端卡片、蓝图网格底、扫描线背景、细线线稿或纯几何符号作为图标结构。

## 依据

生成前已阅读：

- `docs/UI设计文档.md`
- `docs/UI效果图提示词.md`
- `docs/3. 轨道站主框架.md`

返工后的图标仍遵循轨道站的旧工业、废土机械、低功耗终端气质，但只把这些特征落实为物件材质、磨损、螺栓、夹具、舱体和设备发光，不再把 UI 面板样式烘焙进 PNG。

## 生成方式

- 使用 Python 标准库直接绘制 RGBA 位图并写入 PNG。
- 先以 4 倍分辨率绘制，再降采样到 `192x192`。
- 每个图标保留透明背景，主体下方使用透明通道内的软投影。
- 生成阶段未读取现有 SVG。
- 未从 SVG 转 PNG。
- 未使用 SVG 栅格化。
- 未复刻上一版的方框、蓝图底、扫描线或线稿结构。

## 语义说明

交易图标：

- `trade_basic_energy_cells.png`：带夹具的能源块补给托盘，主体是三组可交易能源块。
- `trade_basic_repair_tools.png`：打开的维修工具箱，主体是维修箱、厚重扳手、驱动工具和补丁包。
- `trade_basic_scanner.png`：手持扫描器设备，主体是带握把、镜头、天线和青绿色感应窗的探索工具。
- `trade_service_bot_platform.png`：折叠式服务机器人平台机体，主体是机体核心、连接臂、插槽和资产铭牌。
- `trade_ai_chip_basic.png`：通用 AI 芯片实体模块，主体是带触点、陶瓷封装和数据核心的芯片。

研发图标：

- `research_basic_assembly.png`：蓝图纸卷加装配夹具，表达把旧世界资料落实为基础组装工艺。
- `research_field_repair_protocol.png`：维修协议板和实体扳手，表达野外维修规则与能源调度协议。
- `research_basic_scanning_protocol.png`：扫描校准模块、传感器和扫描胶卷，表达基础扫描协议。
- `research_rocket_part_fabrication.png`：火箭喷口制造夹具，表达火箭部件的制造规范。
- `research_drop_pod_capacity_1.png`：空投舱扩容加固框架，表达空投舱载荷审计和容量改进入口。

## 验证

- 所有输出均为 `192x192`、8-bit RGBA、非交错 PNG。
- 四角 alpha 均为 0，背景透明。
- 已检查 48px 缩略图可辨识主体剪影。
- 旧规则中的硬边 UI 框、蓝图网格底、扫描线背景和整张方形蓝图 UI 未再作为图标底层结构出现。

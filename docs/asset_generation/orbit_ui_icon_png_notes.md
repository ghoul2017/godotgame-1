# 轨道 UI 图标 PNG 生成记录

生成日期：2026-05-27

## 范围

本次返工覆盖生成 23 个 128x128 RGBA PNG：

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
- `assets/ui/orbit/categories/category_all.png`
- `assets/ui/orbit/categories/category_blueprint.png`
- `assets/ui/orbit/categories/category_chip.png`
- `assets/ui/orbit/categories/category_equipment.png`
- `assets/ui/orbit/categories/category_key_item.png`
- `assets/ui/orbit/categories/category_material.png`
- `assets/ui/orbit/categories/category_mineral.png`
- `assets/ui/orbit/categories/category_unit_platform.png`
- `assets/ui/orbit/status/available.png`
- `assets/ui/orbit/status/completed.png`
- `assets/ui/orbit/status/credits.png`
- `assets/ui/orbit/status/insufficient.png`
- `assets/ui/orbit/status/locked.png`

## 返工原因

上一版页签、分类和状态图标把统一方框、终端卡片、道具格边框、扫描线底和细线几何符号烘焙进图标本体，导致缩略图整体仍像一套矢量 app 图标，而不是游戏 UI 里的独立物件或状态角标。

本次新规则：

- 图标本体使用透明背景，边框、插槽、选中态和道具格由 UI 组件提供。
- 页签和分类图标只画粗实剪影或小型物件，保留少量金属厚度、磨损和低功耗状态色。
- 状态图标只做透明角标语义：锁、勾、警告、信用凭证等，使用形状加颜色表达，不做完整卡片。
- 不读取、不栅格化、不复刻任何 SVG 或旧图标结构。

## 依据

读取并按以下文档约束生成：

- `docs/UI设计文档.md`
- `docs/UI效果图提示词.md`
- `docs/asset_generation/orbit_ui_icon_png_notes.md`

风格仍遵循硬边工业终端、低饱和金属、青绿信息光、琥珀警示、锈红危险、冷灰锁定和低饱和数据紫，但不把终端外框画进图标本体。所有图标以 48px 缩放后仍能辨识为目标。

## 生成方式

- 使用 Python 标准库自写 RGBA 像素绘制和 PNG 写入器。
- 从透明画布直接绘制，先高分辨率栅格绘制，再降采样到 128x128。
- 对非透明像素加入少量局部磨损、颗粒和明暗扰动，避免干净矢量线稿感。
- 未使用 SVG 栅格化。
- 未读取任何现有 `.svg` 作为图像源。
- 未从 SVG 转 PNG。
- 未使用现有 PNG 作为图像源或贴图来源。
- 输出为 128x128、8-bit RGBA PNG。

## 语义设计

通用和页签图标：

- `summary_cargo`：货箱和审计单物件，表达回归货舱或带回物资；无终端卡框。
- `summary_discovery`：实体扫描盘、信标和发现星标，表达新坐标、线索和旧世界信号。
- `summary_loss`：破损金属警示板和断裂斜杆，表达损失、遗留或失败复盘。
- `surface_command`：粗实四向指令节点，表达地表 RTS 命令区。
- `surface_minimap`：折叠地形板、路径块和实体标记，表达地表小地图。
- `tab_inventory`：堆叠货箱，表达轨道永久库存。
- `tab_trade`：厚重天平和结算币，表达交易。
- `tab_research`：数据芯核、实体节点和放大镜，表达研发、蓝图解码和协议分析。
- `tab_characters`：机器人头像群组，表达觉醒者和量产单位资产。
- `tab_drop`：降落伞和空投舱，表达空投准备。

分类图标：

- `category_all`：混合物件堆，表达全部分类。
- `category_blueprint`：磨损蓝图纸片，表达蓝图。
- `category_chip`：厚实体芯片和接脚，表达 AI 芯片。
- `category_equipment`：粗实工具交叉，表达装备与工具。
- `category_key_item`：工业钥匙和星形识别物，表达关键物。
- `category_material`：金属锭和材料堆叠，表达物资与加工材料。
- `category_mineral`：实体晶体矿簇，表达矿产。
- `category_unit_platform`：履带式平台机体，表达单位平台。

状态图标：

- `available`：独立绿色粗勾角标，使用勾形和可执行绿色表达可用。
- `completed`：青绿色双审计勾角标，使用双勾形和完成色表达已完成。
- `credits`：琥珀硬币堆和六角结算凭证，使用币形和颜色共同表达信用点。
- `insufficient`：琥珀警告三角、锈红感叹号和断裂容量条，使用形状和颜色共同表达不足。
- `locked`：冷灰锁体、锁孔和闭合锁梁，使用锁形和颜色共同表达锁定。

## 验证记录

- 已按清单覆盖 23 个目标 PNG。
- 已验证输出均为 128x128、8-bit RGBA PNG。
- 已验证四角 alpha 均为 0，图标本体为透明背景。
- 已用临时拼图预览检查，未保留统一卡框、道具格边框、扫描线背景或完整状态卡片。
- 状态图标均采用形状 + 颜色表达，不只依赖颜色。
- 本次未修改代码、`.svg` 或 `.import` 文件。

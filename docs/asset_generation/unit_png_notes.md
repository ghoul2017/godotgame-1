# 单位 PNG 生成记录

## 2026-05-27 `dexter` 透明 PNG 重生成记录

- 本次只生成 / 更新 `assets/data/units/dexter.png`。
- 旧 `dexter` 单位图已因不透明背景问题删除；本次从零生成原创栅格源图，未读取旧单位图，未抠图迭代，未读取或栅格化 SVG。
- 生成方式：使用内置 `image_gen` 生成单个服务型觉醒主角，先使用纯 `#ff00ff` chroma-key 背景，再用本机 `remove_chroma_key.py` 和 Pillow 转为透明背景、重采样并居中为项目目标 PNG。
- 画面要求：三分之四俯视，全身可见，旧工业废土机械，磨损金属，青色传感器脸，多工具手臂，轻型背包模块，少量青绿 / 琥珀状态灯，48 px 下保持可辨识轮廓。
- 输出规格：`256x256` PNG，8-bit RGBA，透明背景；无暗色矩形底、无环境背景、无地面、无文字、无水印。
- 执行注意：本机调用色键工具使用 `python3`；`python` 命令不可用。
- 验证记录：`sips` 确认 `pixelWidth=256`、`pixelHeight=256`、`hasAlpha=yes`；`file` 确认 PNG 8-bit/color RGBA；最终 PNG 读取回检 `corner_alphas=[0, 0, 0, 0]`、`edge_opaque_pixels=0`、alpha 包围盒 `195x210`、最大边占画布 `0.820`。

## 2026-05-27 `service_bot` 透明 PNG 重生成记录

- 本次只生成 / 更新 `assets/data/units/service_bot.png`。
- 旧 `service_bot` 单位图已按不透明背景问题删除；本次从零生成原创栅格源图，未读取旧单位图，未抠图迭代，未读取或栅格化 SVG。
- 生成方式：使用内置 `image_gen` 生成单个量产服务机器人，先使用纯 `#ff00ff` chroma-key 背景，再用本机 Swift / CoreGraphics 转为透明背景 PNG。
- 画面要求：三分之四俯视，全身可见，标准化服务底盘，简单矩形传感器头，工作夹爪，朴素模块面板，旧工业废土机械，磨损金属，少量状态灯。
- 输出规格：`256x256` PNG，8-bit RGBA，透明背景；无暗色矩形底、无环境背景、无地面、无文字、无水印。
- 验证记录：`sips` 确认 `pixelWidth=256`、`pixelHeight=256`、`hasAlpha=yes`；`file` 确认 PNG 8-bit/color RGBA；最终 PNG 读取回检 `corner_alphas=[0, 0, 0, 0]`、`edge_opaque_pixels=0`、alpha 包围盒 `140x207`。

## 本次最终范围

单位 PNG 生成任务最终只写入以下范围：

- `assets/data/units/dexter.png`
- `assets/data/units/service_bot.png`
- `assets/data/units/light_cargo_drone.png`
- `assets/data/units/heavy_cargo_spider.png`
- `assets/data/units/rockbreaker.png`
- `docs/asset_generation/unit_png_notes.md`

单位图早期不透明 / 暗底版本已删除，不作为正式资产、不作为后续迭代输入。本记录只描述当前最终 PNG 和失败原因留痕。

## 依据

已读取并对齐以下文档：

- `docs/UI设计文档.md`
- `docs/5. 地表 RTS 控制与单位系统.md`

执行时按以下规则控制资源语义：

- 单位资源服务第五步地表 RTS 控制与单位系统，可作为 UI 图标、头像和早期精灵引用。
- 画面保持三分之四俯视或俯视机械单位构图，轮廓在 48 px 下应可辨识。
- 美术方向遵守旧工业、废土机械、低功耗终端、暗金属底、少量青绿和琥珀状态光。
- `dexter` 表现为服务型觉醒主角，使用更有个人识别度的服务机体、青色传感器和多工具轮廓。
- `service_bot` 表现为量产服务机器人，使用更规整、朴素、可复制的服务机体。
- `light_cargo_drone` 表现为四旋 / 轻型运输无人机，强调飞行、侦察和轻量货运。
- `heavy_cargo_spider` 表现为重型多足运输平台，强调大载重、多足底盘和货架结构。
- `rockbreaker` 表现为重型采掘觉醒者，强调采掘工具、重型矿工机体和独立角色轮廓。

## 生成方式

- 生成方式：使用内置 `image_gen` 生成原创栅格源图，再用 macOS Swift / AppKit 重采样为项目目标 PNG。
- 输入图像：未读取项目内现有图像文件；用户提供截图仅作为当前对话中的风格方向参考。
- 输出尺寸：`256x256`。
- 输出格式：PNG，8-bit RGBA。
- 背景：当前最终 5 张单位图均为透明背景；四角 alpha 和边缘透明度已回检。早期暗底版本判定不合格后已删除，未继续抠图、修边、改色或局部迭代。
- 明确声明：未使用 SVG 栅格化，未读取现有 `.svg` 作为图像源，未从 SVG 转 PNG。

## 提示词摘要

所有单位共用约束：

- 用途：PC RTS 生存探索游戏的单位 PNG 图标 / 早期精灵引用。
- 构图：单个机械单位，居中，三分之四俯视或俯视，全身可见，留有边距。
- 风格：旧工业废土机械、低饱和暗金属、少量青绿 / 琥珀状态光，栅格绘制质感。
- 禁止：文字、水印、logo、多单位、环境杂物、SVG / 矢量图标质感。

单位差异提示：

- `dexter`：服务型觉醒主角，紧凑人形服务机体，非对称修补装甲，青色传感器脸，多工具手臂，轻型背包模块。
- `service_bot`：量产服务机器人，标准化服务底盘，简单矩形传感器头，工作夹爪，朴素模块面板。
- `light_cargo_drone`：轻型运输无人机，四旋翼 / 旋翼护圈，中央货舱，吊挂货夹，细长机臂。
- `heavy_cargo_spider`：重型多足运输机器人，六足或八足底盘，宽货架，侧挂货箱，液压腿和重型支撑框架。
- `rockbreaker`：重型采掘觉醒者，强化腿部，钻头或破碎锤手臂，矿业肩部模块，粗重矿工装甲。

## 输出清单

- `assets/data/units/dexter.png`
- `assets/data/units/service_bot.png`
- `assets/data/units/light_cargo_drone.png`
- `assets/data/units/heavy_cargo_spider.png`
- `assets/data/units/rockbreaker.png`

## 验证记录

- 输出数量：5 个 PNG。
- 所有输出均为 `256x256`。
- 所有输出均为 PNG 8-bit RGBA。
- 所有输出均保留 alpha 通道。
- 仅新增 / 更新本记录和指定 `assets/data/units/*.png`。
- 未使用 SVG 栅格化，未读取现有 `.svg` 作为图像源，未从 SVG 转 PNG。

## 2026-05-27 light_cargo_drone 透明 PNG 重生成

- 范围：仅重生成 `assets/data/units/light_cargo_drone.png`。
- 原因：旧单位图存在不透明背景，已删除；本次从零生成新栅格图。
- 生成方式：使用内置 `image_gen` 生成纯 `#ff00ff` 色键背景源图，再用 macOS Swift / AppKit 做本地色键转 alpha、重采样和居中。
- 输入约束：未读取旧 `light_cargo_drone.png`，未读取项目内旧单位图，未读取或栅格化 SVG。
- 画面要求：单个轻型运输无人机，三分之四俯视或俯视，全身可见，四旋翼 / 旋翼护圈，中央货舱，吊挂货夹，细长机臂，旧工业废土机械，磨损金属，青绿色传感器灯。
- 输出格式：`256x256` PNG，8-bit RGBA，透明背景。
- 验证记录：四角 alpha 为 `[0, 0, 0, 0]`；主体 alpha 包围盒为 `197x191`，最大边占画布 `0.770`，满足主体占画布 75-85%。

## 2026-05-27 heavy_cargo_spider 透明 PNG 重生成

- 范围：仅重生成 `assets/data/units/heavy_cargo_spider.png`。
- 原因：旧单位图存在不透明背景，已删除；本次从零生成新栅格图。
- 生成方式：使用内置 `image_gen` 生成纯 `#ff00ff` 色键背景源图，再做本地色键转 alpha、重采样和居中。
- 输入约束：未读取旧 `heavy_cargo_spider.png`，未读取项目内旧单位图，未读取或栅格化 SVG。
- 画面要求：单个重型多足运输平台，三分之四俯视或俯视，全身可见，六足或八足底盘，宽货架，侧挂货箱，液压腿和重型支撑框架，旧工业废土机械，磨损金属，少量状态灯。
- 输出格式：`256x256` PNG，8-bit RGBA，透明背景。
- 验证记录：四角 alpha 为 `[0, 0, 0, 0]`；边缘不透明像素为 `0`；主体 alpha 包围盒为 `212x214`，主体覆盖率 `0.385`。

## 2026-05-27 rockbreaker 透明 PNG 重生成

- 范围：仅重生成 `assets/data/units/rockbreaker.png`。
- 原因：旧单位图存在不透明背景，已删除；本次从零生成新栅格图。
- 生成方式：使用内置 `image_gen` 生成纯 `#ff00ff` 色键背景源图，再做本地色键转 alpha、重采样和居中。
- 输入约束：未读取旧 `rockbreaker.png`，未读取项目内旧单位图，未读取或栅格化 SVG。
- 画面要求：单个重型采掘觉醒者，三分之四俯视或俯视，全身可见，强化腿部，钻头或破碎锤手臂，矿业肩部模块，粗重矿工装甲，旧工业废土机械，磨损金属，少量状态灯。
- 输出格式：`256x256` PNG，8-bit RGBA，透明背景。
- 验证记录：四角 alpha 为 `[0, 0, 0, 0]`；边缘不透明像素为 `0`；主体 alpha 包围盒为 `205x208`，主体覆盖率 `0.365`。

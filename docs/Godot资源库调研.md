# Godot 资源库调研

调研日期：2026-05-27

## 1. 调研范围

本次只看 Godot Asset Library 中对当前项目近期有帮助的资源。项目当前使用 Godot 4.6 .NET，运行时代码默认 C#，近期程序实现顺序是：

1. 空投配置与远征创建。
2. 地表 RTS 控制与单位系统。
3. 地表矿产、物品物流、建造、生产和电力系统。

筛选优先级：

- Godot 4.6 或可被 4.6 兼容检索到。
- MIT、CC0、Unlicense、BSD 等宽松许可证优先。
- 能降低测试、验证、项目诊断、寻路、资源导入、音频和调试风险。
- 不破坏统一道具、统一库存、具体位置流转和 C# 运行时代码约定。

Godot 官方文档说明 Asset Library 可按分类、支持级别、引擎版本和许可证过滤；编辑器内 AssetLib 会按当前 Godot 版本取资源。本次按 4.6、Community / Featured 和相关关键词检索。

参考：

- https://docs.godotengine.org/en/stable/community/asset_library/using_assetlib.html
- https://godotengine.org/asset-library/asset

## 2. 优先试用

### 2.1 Godot Project Doctor Mini

- 链接：https://godotengine.org/asset-library/asset/5159
- 版本信息：Tools，Godot 4.6，Community，MIT，0.2.8，2026-05-25。
- 用途：扫描项目并输出 Markdown / JSON 诊断报告；检查断脚本、断资源引用、大纹理、重场景、空目录、导出准备、导入设置和 `_process()` 使用；支持编辑器 dock 和 headless / CI。
- 适配判断：非常适合当前阶段。我们已有资源导入、脚本 UID、headless 验证和项目加载风险，Project Doctor 可以成为 `dotnet build` 与 Godot headless 之外的补充验证。
- 接入建议：先在单独分支或隔离提交中安装，只把它作为编辑器 / CI 诊断工具，不让它参与运行时玩法逻辑。

### 2.2 GdUnit4 / gdUnit4Net

- 资源库链接：https://godotengine.org/asset-library/asset/4390
- C# 支持文档：https://github.com/godot-gdunit-labs/gdUnit4Net
- 版本信息：Tools，Godot 4.5，Community，MIT，GdUnit4 6.0.0，2025-10-07。
- 用途：Godot 测试框架，支持 GDScript 和 C#，有场景 Runner、断言、参数化测试、mock / spy、JUnit XML、命令行和 GitHub Action 集成；gdUnit4Net 提供 C# API、VSTest adapter 和 analyzer。
- 适配判断：最贴近我们“C# 至少 dotnet build，新增逻辑考虑测试”的规则。风险是资源库当前页标记为 Godot 4.5 mono 构建，需要在 Godot 4.6 项目里做最小 spike。
- 接入建议：优先验证纯 C# 逻辑测试和少量 Godot Runtime 测试；不要一开始把所有场景测试都迁进去。若 4.6 兼容性不稳定，再退回标准 `dotnet test` 加少量 Godot headless 场景验证。

### 2.3 GUT - Godot Unit Testing

- 链接：https://godotengine.org/asset-library/asset/1709
- 版本信息：Tools，Godot 4.6，Community，MIT，9.6.0，2026-02-24。
- 用途：Godot 4.6 单元测试框架，支持编辑器、命令行、VSCode、断言、double、stub、spy、参数化测试和 JUnit XML。
- 适配判断：版本最贴近 Godot 4.6，但 README 明确主要是用 GDScript 测 GDScript；与本项目 C# 优先原则不完全匹配。
- 接入建议：不作为首选测试框架。只有当需要测试 GDScript 插件或资源库自带 GDScript 工具时，再局部使用。

## 3. 可作为近期参考或小规模试验

### 3.1 A-Star Grid Map

- 链接：https://godotengine.org/asset-library/asset/4950
- 版本信息：2D Tools，Godot 4.4，Community，MIT，0.1.0，2026-03-25。
- 用途：管理 `AStarGrid2D` 的节点；连接 `TileMapLayer` 和动态碰撞体组，自动构建网格并设置 solid points。
- 适配判断：适合第 5 步地表单位移动和第 6 步建筑阻挡验证。需要确认它是否方便从 C# 调用，或者只借鉴网格更新策略。
- 接入建议：先下载阅读实现；若插件脚本偏 GDScript，优先把思路转成我们自己的 C# `SurfaceNavigationService`。

### 3.2 Grid-based Navigation with AStarGrid2D Demo

- 链接：https://godotengine.org/asset-library/asset/2723
- 版本信息：Demos，Godot 4.2，Featured，MIT，官方示例。
- 用途：展示 2D `AStarGrid2D` 导航，并带 steering behaviors 平滑移动。
- 适配判断：不适合直接导入运行时，但很适合作为第五步移动、路径线、转向和平滑移动的参考实现。
- 接入建议：作为学习资料；核心代码仍按 C# 和当前单位指令模型实现。

### 3.3 RTS Entity Controller

- 链接：https://godotengine.org/asset-library/asset/4784
- 版本信息：Misc，Godot 4.4，Community，MIT，1.1.0，2026-02-16。
- 用途：RTS 选择、移动、攻击的模块化 entity-component 工具包，包含样例单位和可玩场景。
- 适配判断：主题很贴近第 5 步，但直接接入风险大。我们的单位、指令、行为模式、库存、建造和战斗阶段归属已经被文档明确约束，不能让外部组件反向决定数据流。
- 接入建议：只作为参考实现，看它如何处理框选、多单位命令、目标反馈和攻击组件；不要直接替换项目单位系统。

### 3.4 Better Terrain

- 链接：https://godotengine.org/asset-library/asset/1806
- 版本信息：2D Tools，Godot 4.3，Community，Unlicense，1.1，2026-02-14。
- 用途：替代 Godot 4 `TileMapLayer` terrain 规则，提供更灵活的自动地形和代码更新 API。
- 适配判断：如果第 7 步探索 / 地图或第 11 步生成需要 2D tile 地形，它可能有价值；但当前第 4-6 步不应提前引入地图生成依赖。
- 接入建议：等确定地表地图是 TileMapLayer 驱动后再试；现在只记录候选。

### 3.5 Aseprite Spritesheet Importer

- 链接：https://godotengine.org/asset-library/asset/4936
- 版本信息：Tools，Godot 4.4，Community，MIT，1.0，2026-05-24。
- 用途：把 Aseprite spritesheet 和动画导入 Godot，让 `.ase` 文件按源文件管理。
- 适配判断：适合第 5、6 步批量落地单位、建筑、状态反馈和动画资源时使用。前提是美术源文件确定用 Aseprite。
- 接入建议：美术管线确定后再接；若继续使用 SVG / PNG 生成资源，暂不需要。

### 3.6 Godot Audio Manager

- 链接：https://godotengine.org/asset-library/asset/4591
- 版本信息：Tools，Godot 4.5，Community，MIT，2.0.2，2026-01-29。
- 用途：集中管理 omnidirectional、2D、3D 音频流，支持窗口失焦 / 标签切换自动暂停。
- 适配判断：当前阶段音频需求主要是 UI 和短反馈音效，项目自有轻量服务足够。到第 5、6 步大量单位、建造、生产、电力反馈同时出现时可重新评估。
- 接入建议：暂不直接安装；后续若音频管理开始分散，再做 spike。

## 4. 后期候选

### 4.1 Godot State Charts

- 链接：https://godotengine.org/asset-library/asset/1778
- 版本信息：Tools，Godot 4.0，Community，MIT，0.22.4，2026-03-24。
- 用途：比传统 FSM 更强的 state charts，用于避免状态爆炸。
- 适配判断：适合第 8 步战斗行为、第 9 步角色行为模式和复杂 UI 流程；当前阶段用它会过早引入抽象。
- 接入建议：等行为模式真的变复杂再试；简单状态先用显式 C# 枚举和服务层。

### 4.2 LimboAI: Behavior Trees & State Machines

- 链接：https://godotengine.org/asset-library/asset/4852
- 版本信息：Tools，Godot 4.6，Community，MIT，1.7.0，2026-03-02。
- 用途：C++ 插件，提供行为树、状态机、编辑器、内置文档和可视化调试器。
- 适配判断：功能强，但插件页强调用 GDScript 编写 task / state；与本项目 C# 默认运行时代码存在摩擦。
- 接入建议：第 8 步敌人 AI 或防御行为确实复杂时再评估。若采用，必须先验证 C# 调用边界、导出稳定性和平台兼容。

## 5. 不建议直接接入

### 5.1 通用库存系统

候选包括：

- Universal Inventory System：https://godotengine.org/asset-library/asset/271
- P0nni Inventory System：https://godotengine.org/asset-library/asset/4388

这些资源有道具、装备、商人、UI、堆叠、原子交换、库存间安全转移等能力，思路上有参考价值。但本项目的经济对象必须是具体位置上的道具，库存容器、地上物、施工点、建筑输入 / 输出、火箭货舱和轨道库存都要服从统一权威状态与事务式转移。直接接入外部库存系统，极容易把 UI、装备、商店或玩家背包范式带进核心数据流。

结论：不直接导入。可以阅读它们的原子交换、slot validation、tooltip 和 editor tools 设计。

### 5.2 SaveFlow Lite

- 链接：https://godotengine.org/asset-library/asset/5043
- 版本信息：Tools，Godot 4.6，Community，MIT，1.0.4，2026-05-22。
- 用途：显式 scene-authored save graph、node state、typed data、runtime entity、scope、slot metadata、autosave / checkpoint、C# typed state、校验和警告。
- 适配判断：理念不错，但项目已拥有 `SaveGame` / `SaveGameService`、库存引用校验、轮次记录、交易审计和回归 payload 规则。直接迁移会扩大边界。
- 结论：暂不接入。后续做正式存档槽 UI 和迁移策略时，可借鉴它的显式 ownership、scope、validator 和 authoring warnings。

### 5.3 UUID 生成器

候选包括 `GD UUID`、`uuid` 等。C# 项目可直接使用 `System.Guid`，除非需要与 Godot 资源编辑器深度集成，否则没有必要引入 GDScript UUID 插件。

### 5.4 MCP、CEF、3D 控制器和外部服务类资源

近期检索结果中有 Godot MCP、GDCEF、Discord RPC、Talo Game Services、3D 第一 / 第三人称控制器、3D inventory 等资源。它们与当前 PC 单人生存探索 / RTS、2D 俯视、离线核心循环没有直接关系，不应引入。

## 6. 接入规则

1. 任何插件先在单独分支或独立提交中试用，不混入玩法改动。
2. 运行时代码默认仍用 C#；GDScript 插件只能作为编辑器工具、参考实现或隔离运行时模块。
3. 插件不能接管核心道具、库存、远征、单位和存档权威状态。
4. 插件安装后必须验证：
   - `dotnet build`
   - `dotnet build godotgame1.sln`
   - Godot headless 项目加载
   - 若是测试插件，还要能命令行运行测试并输出可读报告。
5. 插件生成的报告、缓存、临时文件需要明确加入忽略规则或固定输出目录。
6. 若插件引入许可证、导出平台、C# 兼容性或资源导入风险，先记录到 `docs/遗留问题.md`，再决定是否继续。

## 7. 推荐下一步

短期最值得做三个 spike：

1. Project Doctor Mini：验证能否 headless 扫描本项目并产出报告。
2. GdUnit4 / gdUnit4Net：验证 C# 纯逻辑测试、Godot Runtime 测试和 `dotnet test` / CI 输出。
3. A-Star Grid Map + 官方 AStarGrid2D demo：学习并转写成适配项目单位指令模型的 C# 寻路服务原型。

库存、存档、RTS 控制和行为树类资源先作为设计参考，不直接安装进主项目。

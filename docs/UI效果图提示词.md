# UI 效果图生成提示词

## 1. 依据和用途

本文用于生成游戏主要场景的 UI 效果图，依据：

- `docs/原始设计.txt`
- `docs/整体策划案.md`
- `docs/UI设计文档.md`
- `docs/UI功能性设计文档.md`
- `docs/统一库存容器UI设计方案.md`

效果图只用于美术方向、布局密度、材质气质和界面层级验证，不作为最终可直接切图资源。若图像生成结果与源设计冲突，以源设计为准。

## 2. 全局母提示词

所有主要场景都应先使用以下母提示词，再拼接对应场景提示词。

```text
PC game UI concept art, 16:9 full screen interface mockup, top-down 2D survival exploration RTS, post-human abandoned planet, autonomous machine civilization, old orbital mothership terminal, utilitarian high-density sci-fi interface, unified inventory container UI language for item slots and cargo manifests, oxidized metal panels, worn industrial frames, low-power monitor glow, subtle scanlines, fine screen noise, hard-edged modular panels, short bevels, tactical command readability, dark metal background, muted green cyan information light, amber warning accents, rust red danger accents, low-saturation data purple for blueprints and old-world signals, crisp icon silhouettes, readable layout hierarchy, original design, not a copy of any existing game UI
```

## 3. 全局负面提示词

```text
marketing landing page, hero website, mobile game UI, fantasy magic UI, cute cartoon, glossy glassmorphism, bright neon cyberpunk, purple-blue gradient dominated palette, beige parchment, rounded pill cards, large empty decorative spaces, global resource bar for every material, literal Windows file explorer, folder tree, file path bar, desktop OS window controls, fake debug buttons, scene jump debug menu, countdown pressure UI, minigame interface, unreadable clutter, overlapping panels, text covering important content, oversized decorative illustration, direct copy of existing game screenshot
```

## 4. 生成规格建议

- 画幅：`1920x1080`，16:9。
- 视角：界面截图式效果图，不用倾斜透视展示 UI。
- 文字：允许短标签和数字占位，但不要依赖生成模型输出大量准确中文。
- UI 层级：全屏功能界面强调信息密度；地表 HUD 强调地图可见和低遮挡。
- 色彩：暗金属底色、青绿信息光、琥珀警示、锈红危险、低饱和数据紫。
- 图标：优先清晰剪影，32px 级别仍能识别。
- 库存：道具格、容器头、容量条、状态角标和详情面板在轨道库存、单位背包、建筑库存和货舱中保持同源，但不能像现代文件浏览器。

## 5. 主入口菜单

### 5.1 画面目标

主入口是母舰低功耗启动终端，只承载继续游戏、新游戏、设置、退出和最近存档摘要，不出现轨道站、地表、结算等调试直达入口。

### 5.2 提示词

```text
Main menu UI mockup for a PC survival exploration RTS, low-power boot terminal inside an abandoned orbital mothership, dark maintenance deck, distant ruined planet visible through narrow reinforced window, old cables and mechanical arms in background, title area near upper center, four main rectangular hard-edged buttons on left-middle: continue game, new game, settings, quit, recent save summary panel on right or lower right, small system status strip at bottom, worn metal frames, muted green and amber terminal lights, restrained industrial atmosphere, clean readable spacing, no debug shortcuts, no orbit station tabs, no battlefield controls
```

## 6. 轨道站总界面

### 6.1 画面目标

轨道站是永久成长层和远征准备中枢。效果图应直接呈现库存、交易、研发、角色、空投五个页签的功能布局，不做大图首页。

### 6.2 通用轨道站提示词

```text
Full-screen orbital station logistics terminal UI, high-density functional sci-fi interface, old industrial mothership command deck background mostly covered by practical panels, top status bar with credits, key inventory summary, orbit status, recent expedition summary, fixed left vertical tabs for inventory, trade, research, characters, drop preparation, central dense inventory container list area with rows, item icons, quantities, weight, cost and status badges, right detail inspector panel with selected item icon, current container location, description, requirements, cost, reward, restriction and failure reason, bottom operation bar with filters, sorting, cancel and primary confirmation button, Starsector-like information density without copying, original hard-edged metal terminal design, readable PC UI, muted green cyan data glow, amber warning markers, no folder tree or file explorer path bar
```

### 6.3 库存页提示词

```text
Orbital station inventory page UI, permanent mothership inventory container audit terminal, container header labeled orbital permanent inventory with capacity and weight, central high-density list of item rows using the shared item slot language, icons for metal, silicon, rare earth, energy cell, scrap, alloy, electronic parts, clean data, AI chip, tools and unit platforms, columns for category, quantity in this container, unit weight, total weight, value and status badges, category filter buttons along top of list, selected item detail panel on the right showing current location, uses in trade, research and drop preparation, bottom area showing current filter and lock reasons, no global resource pool fantasy counter, every material appears as inventory items in the orbital storage, not a desktop file manager
```

### 6.4 交易页提示词

```text
Orbital station trade page UI, automated supply and salvage exchange terminal, central trade offers list showing consume-to-receive rows, icons for repair tools, scanner, energy cells, AI chip and service robot platform, each row displays required item costs and credit cost, right detail panel previews transaction result and inventory after trade, status badges for available, insufficient, locked and completed, bottom right confirm trade button with clear disabled reason area, dense utilitarian sci-fi trading interface, no shop hero banner, no decorative storefront
```

### 6.5 研发页提示词

```text
Orbital station research page UI, old-world blueprint decoding and protocol unlock terminal, central research project rows instead of a linear skill tree, categories for building blueprints, weapon blueprints, drone protocols, scanning, drop pod improvements, completed projects remain as audit records, right detail panel shows consumed items, clean data, blueprint fragments, data core requirements and unlocked expedition options, low-saturation purple data highlights, amber locked prerequisites, bottom right confirm research button, high-density planning screen
```

### 6.6 角色页提示词

```text
Orbital station character and unit assets page UI, awakened robot roster and mass-produced unit platform management, list of awakened units, mass units and unit platforms, selected awakened service robot "Lingqiao" portrait with engineering, shooting, mining and control skill summaries, a heavy cargo spider miner "Suishi" placeholder or locked trace, equipment and modification summary panel, unit platform rows with quantity and drop availability, right side details for status and next expedition suitability, practical roster screen, no RPG fantasy character sheet
```

### 6.7 空投页提示词

```text
Orbital station drop preparation page UI, overview before formal drop configuration, central left known coordinates list with region type, risk level, mineral tags and repeat visit state, central right available drop pod overview with weight limit, grid slots and unit capacity, right preparation summary showing available awakened units, unit platforms, key items and missing requirements, bottom right button to enter formal drop configuration, button may show locked reason if module is not connected, does not create expedition directly
```

## 7. 空投配置界面

### 7.1 画面目标

空投配置是轨道投送审计终端。必须强调坐标、空投舱、觉醒者、量产单位、物资、装备、容量和风险取舍。

### 7.2 提示词

```text
Formal drop configuration UI, orbital deployment audit terminal, full-screen high-density interface, top status bar showing selected coordinate, selected drop pod, total weight, grid slots and unit capacity, left column contains coordinate list and drop pod list with risk icons, mineral tags and unlock state, central source inventory container list grouped by awakened units, mass-produced unit platforms, materials, equipment, chips and tools, plus add buttons and quantity steppers, right side current drop pod cargo container manifest grouped like a cargo audit receipt with shared item slots, capacity bars with numeric values, tick marks and state colors, bottom validation strip showing overweight, over slots, unit capacity limit, insufficient inventory, locked prerequisite and warning messages, bottom right primary confirm drop button, bottom left clear loadout and return to orbital station buttons, no drag-and-drop ambiguity, no hidden inventory deduction
```

## 8. 地表远征 HUD

### 8.1 画面目标

地表 HUD 是 RTS 主操作界面，中心必须保留地表地图、单位、建筑、矿产点的可见性。顶部不能画成全局材料栏，道具只在位置、物流、选中对象、施工点和建筑详情里出现。

### 8.2 提示词

```text
Surface expedition RTS HUD mockup, top-down 2D abandoned planet battlefield with early temporary robot base, visible service robot, light cargo drone, heavy cargo spider, drop pod, storage box, solar panel, assembler, repair station, mineral deposits and scattered ground items, Warcraft-like clear RTS layout adapted to post-apocalyptic machine survival, top slim status strip only for logistics orders, power status and alerts, no global inventory bar, lower left minimap with explored and unexplored regions, mineral markers, ruins, signals and threat markers, bottom central selection information panel showing selected unit portrait, durability, energy, compact backpack inventory container using shared item slots, current command and behavior mode, lower right command button grid with icons for move, gather, haul, build, repair, attack, guard, scan, hack, return to repair station and cancel, right side message and event feed, low obstruction, readable tactical UI, muted industrial palette
```

### 8.3 多单位选择提示词

```text
Surface expedition RTS HUD with multiple robot units selected, bottom panel shows portrait grid, unit count, type groups, shared commands and control group number, selected units have clear green-cyan selection rings on the map, move target marker and path feedback visible, command grid remains stable in lower right, messages on right remain compact, no overlapping UI over units
```

### 8.4 建筑选中提示词

```text
Surface expedition RTS HUD with a production building selected, bottom panel shows building icon, name, durability, power state, shared input inventory container slots, shared output inventory container slots and production status, right command grid includes open production, logistics, repair and cancel buttons, top power status indicates low power amber warning, map remains visible behind HUD, no materials are deducted from a global counter
```

## 9. 地表子面板

### 9.1 建造菜单提示词

```text
Surface build menu overlay expanded from lower right command area, compact engineering terminal over the battlefield, building categories for infrastructure, production, power, defense and rocket, building icon grid with storage box, repair station, assembler, solar panel, fluid tank and rocket pad, selected building detail shows footprint, required item list as construction-site inventory requirements, construction time, power need and whether materials have a reachable location, placement preview on map with green valid cells and rust red blocked cells, confirming placement creates a construction site inventory, no instant resource deduction
```

### 9.2 物流订单面板提示词

```text
Surface logistics order panel, left filter tabs for pending, in transit, blocked and complete, central order list shows item icon, source location, target location and carrier unit, right detail panel shows reservation, pickup, transport, delivery and blocked reason, buttons for locate source, locate target, pause and cancel order, every item movement is from a concrete place to a concrete place, utilitarian field operations terminal, battlefield still visible around the panel
```

### 9.3 生产和电力面板提示词

```text
Surface production and power management panel for early robot base, left available recipes, center production queue, right paired shared inventory containers for building input inventory and output inventory plus power demand, bottom controls for start, pause, cancel and loop production, separate power summary strip showing generation, consumption, battery storage, stopped buildings and insufficient power warning, output blocked state visible when output inventory is full, information density lower than orbital station, no obstruction of tactical map
```

### 9.4 探索地图界面提示词

```text
Surface exploration map interface, full-screen tactical map overlay, central map with discovered regions, fog of war, mineral deposits, ruins, special signals, threat points and rocket pad location, left legend and region list, right selected region details with risk, known resources and signal traces, bottom buttons for scan, mark target, focus camera and close map, old-world data noise and low-saturation purple signal accents, clear icon legend
```

### 9.5 行为模式面板提示词

```text
Robot behavior mode panel opened from selected unit HUD, compact command terminal with icons for hold position, skirmish, gather priority, guard and retreat to repair station, selected mode highlighted, buttons for apply to same type, restore default and close, each mode shown as a tactical automation tendency, not a direct spell or active skill interface
```

## 10. 火箭装载界面

### 10.1 画面目标

火箭装载是地表回归前的取舍界面，必须同时表达带回、遗留、载重和超载风险。

### 10.2 提示词

```text
Rocket loading UI, return cargo audit terminal on the surface expedition, top status bar with rocket build state, cargo weight, capacity and overload risk, left source container list of returnable items, equipment, chips, blueprints and awakened units, center current rocket cargo inventory container manifest with shared item slots, quantities and weight, right abandoned-on-surface preview showing remaining materials, mass units, equipment and risk notes, critical items, AI chips, blueprints and awakened robots are visually prioritized, amber and rust red overload warnings, bottom buttons for auto select key items, clear cargo, save loadout, confirm launch and return to surface, audit receipt style, sober and weighty, no victory celebration
```

## 11. 回归结算界面

### 11.1 画面目标

回归结算用于写回轨道永久层并复盘轮次。不能只展示收益，必须包含损失、遗留和发现。

### 11.2 提示词

```text
Return summary UI, orbital write-back audit screen after rocket launch, full-screen industrial terminal, top summary with expedition coordinate, seed, return status and run duration, central columns for returned materials, returned chips, returned blueprints, awakened units returned, lost units and equipment, abandoned items, new discovered coordinates and clues, each column has compact item rows with icons and quantities, bottom area previews changes to orbital permanent inventory, primary button to write into orbital inventory and return to orbital station, secondary button for run record details, calm mechanical confirmation feedback, no exaggerated victory screen
```

## 12. 单图组合展示提示词

如果需要一张图同时展示多个主界面方向，可使用该提示词生成 UI 概念板。

```text
Game UI concept board showing six 16:9 interface thumbnails for a post-human robot survival RTS: main menu low-power mothership terminal, orbital station high-density logistics screen, drop configuration audit terminal, surface expedition RTS HUD, rocket loading cargo audit, return summary orbital write-back screen. Consistent oxidized metal UI kit, muted green cyan data glow, amber warnings, rust red danger, low-saturation purple blueprint signals, hard-edged panels, crisp icons, original cohesive UI art direction, no marketing page, no mobile UI, no debug shortcuts
```

## 13. 纠偏检查词

生成或筛选效果图时，用以下词检查是否偏离设计：

- 主入口只应出现：继续游戏、新游戏、设置、退出、存档摘要。
- 轨道站必须是功能界面，不是大图首页。
- 库存、背包、建筑仓库、输入输出槽、施工点、空投舱和火箭货舱必须使用同源道具格、容器头、容量条和状态角标。
- 库存界面可以有对象管理的清晰度，但不能像现代文件浏览器，不能出现文件夹树、路径栏和系统窗口按钮。
- 空投配置必须表达：坐标、空投舱、重量、格位、单位容量、装载清单、校验失败原因。
- 地表 HUD 顶部只显示物流、电力、提醒，不显示所有矿产的全局资源池。
- 资源和矿产必须是道具，位于具体库存、地上物、施工点、建筑输入输出、火箭货舱或轨道库存。
- 火箭装载必须同时显示带回和遗留。
- 回归结算必须同时显示收益、损失、遗留和发现。
- 正式 UI 不出现调试入口、直接跳场景按钮或假载荷。

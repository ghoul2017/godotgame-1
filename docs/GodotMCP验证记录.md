# GodotMCP 验证记录

日期：2026-05-27

## 结论

- 当前 Codex 会话已能通过 `tool_search` 懒加载 `mcp__godot__` 工具命名空间，可直接调用 `run_project`、`stop_project`、`get_debug_output`、`game_get_scene_tree` 和 `game_change_scene`。
- 本机旧项目存在可复用的 GodotMCP 包：`/Users/gejiayi/godots/game1/.claude/godot-mcp`。
- 使用 Node MCP SDK 通过 stdio 手动启动旧 GodotMCP 包，可以连通当前项目 `/Users/gejiayi/godots/godotgame-1`。
- Godot 可执行文件路径为：`/Applications/Godot_mono.app/Contents/MacOS/Godot`。

## 2026-05-28 并行 MCP 验证

- 已按 `docs/工作编排文档.md` 的并行验证通道，由只读 MCP 验证子代理先执行 `ls docs`，再通过 GodotMCP 启动当前项目。
- `mcp__godot__.get_project_info` 识别项目名 `godotgame1`，Godot 版本为 `4.6.3.stable.mono.official.7d41c59c4`。
- `mcp__godot__.run_project` 成功启动项目，`mcp__godot__.get_debug_output` / 日志读取到：

```text
McpInteractionServer: Listening on 127.0.0.1:9090
[启动] 主入口初始化开始
[输入] Input Map 基础行为已确认
[启动] 主入口初始化完成
McpInteractionServer: Client connected
```

- `mcp__godot__.game_get_scene_tree` 读取到运行时树，关键节点包括：

```text
root Window
├── McpInteractionServer Node
└── GameRoot Control
    ├── MainBackground TextureRect
    └── RootLayout VBoxContainer
        ├── MainMenu PanelContainer
        └── SceneContainer Control
```

- `mcp__godot__.stop_project` 成功停止项目。
- 本轮未发现项目启动失败或主场景缺失。仍可见 MCP 注入脚本 `res://mcp_interaction_server.gd` 的 GDScript warning；该 warning 属于 MCP 交互脚本，不作为当前项目 C# 主入口缺陷处理。

## 2026-05-27 本轮 Codex MCP 实测

- `tool_search` 查询 Godot 后成功暴露 `mcp__godot__` 精简工具集。
- `mcp__godot__.run_project` 成功启动当前项目主场景。
- `mcp__godot__.get_debug_output` 可读取 Godot 输出；项目日志正常，当前警告来自 MCP 注入脚本 `res://mcp_interaction_server.gd`，不是项目 C# 脚本。
- `mcp__godot__.game_get_scene_tree` 成功读取运行时树，能看到 `GameRoot`、`MainMenu` 和 `SceneContainer`。
- `mcp__godot__.game_change_scene` 成功切换到 `res://scenes/drop/drop_config.tscn`，运行时树能看到 `DropConfig`、配置面板和确认按钮节点。
- `mcp__godot__.stop_project` 成功停止运行中的项目。
- Godot 自定义启动参数本轮确认要直接跟在命令后，例如 `tools/run_godot_console.sh --headless --quit --debug-scene=drop_config --seed=460001`；放在 `--` 后本项目 `OS.GetCmdlineArgs()` 读取不到。

## 2026-05-27 MCP 停止与注入脚本警告

- 现象：通过 `mcp__godot__.stop_project` 关闭 Godot 后，macOS 可能弹出“Godot 意外关闭”的系统提示。
- 判断：这更像是 MCP 宿主终止 Godot 进程的方式触发了 macOS 崩溃提示，不是项目运行时代码崩溃。证据是 `dotnet build`、Godot headless `--quit` 和关键调试场景均能正常返回，项目日志没有 C# 异常。
- 建议：日常验证优先使用 `tools/run_godot_console.sh --headless --quit` 或带调试参数的 headless 命令；MCP 主要用于运行时场景树、日志读取和切场景检查。
- 处理级别：如果只是偶发 macOS 弹窗，不把它作为项目缺陷处理；如果需要长期高频使用 MCP 运行窗口，再考虑改 MCP 服务器的停止逻辑，让 Godot 先执行正常退出而不是被直接终止。

`res://mcp_interaction_server.gd` 当前会输出以下类型警告：

- 局部 `for` 迭代变量 `name` 遮蔽了 `Node.name` 属性。
- 整数被用于需要枚举值的位置。

处理判断：

- 这些警告来自 MCP 注入到运行中 Godot 的交互脚本，不属于本项目 `scripts/` 下的 C# 运行时代码，也不影响当前主入口、空投配置、地表远征和结算场景加载。
- 当前不需要修项目代码来处理这些警告。
- 只有在以下情况才需要处理 MCP 注入脚本本身：警告升级为错误、阻断 `game_get_scene_tree` / `game_change_scene` / `get_debug_output`，或决定把 MCP 交互脚本作为本项目长期维护工具纳入仓库。
- 若要修 MCP 脚本，优先在 MCP 包源头修改变量名和枚举显式转换，而不是在当前项目里绕过。

## 验证结果

- `tools/list` 成功，返回 154 个工具。
- `get_godot_version` 成功，返回 `4.6.3.stable.mono.official.7d41c59c4`。
- `get_project_info` 成功，识别项目名为 `godotgame1`。
- `read_project_settings` 成功，能读取 `project.godot`，主场景为 `res://scenes/main/game_root.tscn`。
- `list_project_files` 成功，当前返回 348 个项目文件。

## Codex 注册结果

- 已在 `/Users/gejiayi/.codex/config.toml` 注册 `mcp_servers.godot`。
- 注册命令为 `node /Users/gejiayi/godots/game1/.claude/godot-mcp/build/index.js`。
- 已显式设置 `GODOT_PATH=/Applications/Godot_mono.app/Contents/MacOS/Godot`。
- 已设置 `DEBUG=false`，避免旧 Claude 配置中的调试日志噪声。
- `codex mcp list` 已能识别 `godot`，状态为 `enabled`。
- 当前已打开的 Codex 对话不会热加载新增 MCP；需要新开或重载会话后再检查工具是否出现。

## 性能实测

同一进程内手动 MCP 客户端测试：

| 操作 | 耗时 |
| --- | ---: |
| MCP connect | 644.4 ms |
| tools/list | 4.6 ms |
| get_godot_version | 146.0 ms |
| read_project_settings | 1.8 ms |
| list_project_files | 5.3 ms |
| read_file AGENTS.md | 0.7 ms |
| get_project_info | 159.1 ms |
| read_scene scenes/main/game_root.tscn | 720.6 ms |
| read_scene scenes/orbit/orbit_station.tscn | 469.4 ms |

结论：轻量文件和配置读取不慢；慢感主要可能来自三类开销：

- 154 个工具导致宿主加载和模型选择工具的上下文负担变重。
- 需要 Godot headless 的场景或工程操作会启动 Godot 进程，单次约 0.5-0.8 秒起步。
- 旧配置中 `DEBUG=true` 会增加日志输出和噪声。

## 加速方案

- 保留当前已落地优化：固定 `GODOT_PATH`，关闭 `DEBUG`。
- 日常开发优先用命令行和项目脚本验证，MCP 只用于需要引擎态、场景态或运行时态的检查。
- 连续运行时测试时，优先让 Godot 项目保持运行，再使用 `game_*` TCP 工具；避免每个动作都走 headless 冷启动。
- 对常用操作做轻量 MCP 包装，只暴露少量工具，例如版本、项目设置、文件列表、读场景、启动/停止项目、截图、读取日志。这样能减少工具 schema 数量和模型选择成本。
- 对多步运行时检查使用 `game_eval` 或自定义批处理命令一次返回结果，减少多次 MCP 往返。
- 避免频繁调用全量 `list_project_files`，项目文件检索优先用 `rg --files`。
- 如果全量 GodotMCP 明显拖慢普通对话，可先用 `codex mcp remove godot` 移除全量工具，再改用轻量 MCP 或按需配置文件启动。

## 使用注意

- 直接运行 GodotMCP 服务器前要设置 `GODOT_PATH`，否则它会按默认路径寻找 `/Applications/Godot.app/Contents/MacOS/Godot`，与本机实际安装的 `Godot_mono.app` 不一致。
- 本轮验证先使用“手动 MCP 客户端调用”确认链路，随后已注册到 Codex 配置；如果当前对话仍看不到工具，重载会话后再检查。
- 该 GodotMCP 包包含写文件、改场景、运行项目等强能力；用于修改项目前仍需按项目规则先判断范围、影响文件和验证方式。

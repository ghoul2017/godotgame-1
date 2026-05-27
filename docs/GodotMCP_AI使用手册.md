# GodotMCP AI 使用手册

本文面向后续接手本项目的 AI，用于说明如何在当前 Godot .NET 项目中安全、高效地使用 GodotMCP。

## 当前状态

- 项目路径：`/Users/gejiayi/godots/godotgame-1`
- Godot 可执行文件：`/Applications/Godot_mono.app/Contents/MacOS/Godot`
- Godot 版本：`4.6.3.stable.mono.official.7d41c59c4`
- GodotMCP 包路径：`/Users/gejiayi/godots/game1/.claude/godot-mcp`
- Codex MCP 名称：`godot`
- Codex 配置位置：`/Users/gejiayi/.codex/config.toml`
- 验证记录：`docs/GodotMCP验证记录.md`

如果当前对话中看不到 `godot` 相关 MCP 工具，先执行 `codex mcp list` 确认配置；新增 MCP 通常需要重开或重载会话后才会进入工具列表。

## 使用原则

- 先读项目入口规则和相关设计文档，再使用 MCP。
- 修改前必须判断任务范围、影响文件和验证方式。
- GodotMCP 是引擎态和场景态辅助工具，不替代代码阅读、`rg` 检索、`dotnet build` 和 Godot headless 验证。
- 轻量文件检索优先用 `rg --files`、`rg`、`sed`，不要为了读普通文本启动 GodotMCP。
- 涉及 `project.godot`、主场景、自动加载、场景结构和资源写入时，必须在最终回复中说明验证结果。

## 推荐工作流

1. 先确认上下文：
   - 读 `AGENTS.md`。
   - 读 `docs/GodotMCP验证记录.md`。
   - 用 `git status --short` 区分自己改动和用户已有改动。

2. 选择工具层级：
   - 普通代码和文档：优先 shell、`rg`、`dotnet build`。
   - 项目设置读取：可用 `read_project_settings`。
   - 场景结构读取：可用 `read_scene`。
   - 运行时 UI、截图、输入、节点状态：优先让 Godot 项目保持运行，再使用 `game_*` 工具。
   - 批量运行时检查：优先用一次 `game_eval` 或项目内调试入口返回完整结果，减少多次往返。

3. 执行修改：
   - 默认运行时代码使用 C#，不要因为 MCP 方便就新增业务 GDScript。
   - 场景或资源改动前先列出目标文件。
   - 不写入本机绝对路径到运行时代码或项目资源。

4. 验证：
   - C# 代码至少运行 `dotnet build`。
   - 有 `.sln` 时优先同时验证 `dotnet build godotgame1.sln`。
   - 场景、入口、资源路径变化后，用 Godot console/headless 或 MCP 读取场景确认。
   - 新增入口场景后确认 `application/run/main_scene` 指向有效 `.tscn`。

## 常用能力

- `get_godot_version`：确认 Godot 可执行文件和版本。
- `read_project_settings`：读取 `project.godot`。
- `list_project_files`：读取项目文件列表；频繁检索仍优先用 `rg --files`。
- `read_scene`：读取 `.tscn` 节点树和属性。
- `run_project` / `stop_project` / `get_debug_output`：运行项目和读取日志。
- `game_screenshot`：运行时截图。
- `game_get_ui`：读取可见 UI。
- `game_get_scene_tree` / `game_get_node_info`：运行时节点检查。
- `game_click` / `game_key_press` / `game_mouse_drag`：模拟输入。
- `game_eval`：合并多步运行时检查，减少工具往返。

## 性能规则

- 当前全量 GodotMCP 暴露 154 个工具，能力很全，但可能拖慢工具选择和会话加载。
- `DEBUG` 必须保持 `false`，除非正在排查 MCP 服务器自身问题。
- 需要 Godot headless 的操作存在冷启动成本，单次约 0.5-0.8 秒起步。
- 连续运行时验证时，应保持 Godot 项目运行，再调用 `game_*` 工具。
- 如果普通对话明显变慢，可以建议切换为轻量 `godot-fast` MCP，只暴露常用少量工具。

## 禁止事项

- 不为了使用运行时工具而把 `mcp_interaction_server.gd` 当成正式业务系统提交，除非任务明确要求接入运行时 MCP 调试通道。
- 不用 MCP 写文件绕过项目编码规则和 C# 优先规则。
- 不在未说明影响范围时调用删除、重命名、改主场景、改自动加载等高风险工具。
- 不把调试入口、临时场景或错误概念写入正式主流程。

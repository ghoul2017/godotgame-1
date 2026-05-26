# godotgame1

Godot 4.6 .NET 项目初始化仓库。

## 项目文件

- `project.godot`: Godot 项目配置
- `docs/原始设计.txt`: 初始设计文档

## 开发

使用 Godot 4.6 .NET 或兼容版本打开本目录。

Rider / Visual Studio 打开 C# 项目时，优先打开：

```text
godotgame1.sln
```

不要只打开目录后直接构建，否则 Rider 可能显示 `No loaded projects`，并调用 Visual Studio 的 MSBuild 触发 `MSBuild.rsp` 重复响应文件问题。

本机 Godot .NET 位于：

```text
D:\Godot_v4.6.3-stable_mono_win64\
```

如果直接双击 Godot 遇到 MSBuild / .NET SDK 问题，使用项目脚本启动：

```powershell
powershell -ExecutionPolicy Bypass -File tools\run_godot.ps1
```

控制台验证：

```powershell
powershell -ExecutionPolicy Bypass -File tools\run_godot_console.ps1 --headless --quit
```

调试进入地表：

```powershell
powershell -ExecutionPolicy Bypass -File tools\run_godot_console.ps1 --headless --debug-scene=surface_expedition --seed=777 --quit
```

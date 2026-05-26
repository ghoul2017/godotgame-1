$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$godotExe = "D:\Godot_v4.6.3-stable_mono_win64\Godot_v4.6.3-stable_mono_win64_console.exe"
$dotnetRoot = "$env:USERPROFILE\.dotnet"

if (-not (Test-Path -LiteralPath $godotExe)) {
	Write-Error "未找到 Godot .NET Console：$godotExe"
}

if (-not (Test-Path -LiteralPath (Join-Path $dotnetRoot "dotnet.exe"))) {
	Write-Error "未找到 .NET SDK：$dotnetRoot"
}

$env:DOTNET_ROOT = $dotnetRoot
if (($env:PATH -split ";") -notcontains $dotnetRoot) {
	$env:PATH = "$dotnetRoot;$env:PATH"
}

Push-Location $projectRoot
try {
	& $godotExe --path $projectRoot @args
}
finally {
	Pop-Location
}

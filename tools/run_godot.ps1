param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $GodotArgs
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Resolve-Path (Join-Path $scriptDir "..")
$godotPath = $env:GODOT_PATH

if ([string]::IsNullOrWhiteSpace($godotPath)) {
    $godotPath = "/Applications/Godot_mono.app/Contents/MacOS/Godot"
}

if ([string]::IsNullOrWhiteSpace($env:DOTNET_ROOT)) {
    $dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($null -ne $dotnetCommand) {
        $env:DOTNET_ROOT = Split-Path -Parent $dotnetCommand.Source
    }
}

if (-not [string]::IsNullOrWhiteSpace($env:DOTNET_ROOT)) {
    $env:PATH = "$env:DOTNET_ROOT$([System.IO.Path]::PathSeparator)$env:PATH"
}

if (-not (Test-Path -LiteralPath $godotPath)) {
    throw "Godot executable not found: $godotPath. Set GODOT_PATH to the Godot .NET executable path."
}

& $godotPath --path $projectRoot @GodotArgs
exit $LASTEXITCODE

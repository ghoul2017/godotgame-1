#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
GODOT_BIN="${GODOT_PATH:-/Applications/Godot_mono.app/Contents/MacOS/Godot}"

if [[ -z "${DOTNET_ROOT:-}" ]] && command -v dotnet >/dev/null 2>&1; then
  export DOTNET_ROOT="$(dirname "$(command -v dotnet)")"
fi

if [[ -n "${DOTNET_ROOT:-}" ]]; then
  export PATH="${DOTNET_ROOT}:${PATH}"
fi

if [[ ! -x "${GODOT_BIN}" ]]; then
  echo "Godot executable not found: ${GODOT_BIN}" >&2
  echo "Set GODOT_PATH to the Godot .NET executable path." >&2
  exit 1
fi

exec "${GODOT_BIN}" --path "${PROJECT_ROOT}" "$@"

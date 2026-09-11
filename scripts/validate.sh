#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT_DIR="$ROOT_DIR/project"
SDK_PROJECT="$ROOT_DIR/sdk/UniversalRPG.Sdk/UniversalRPG.Sdk.csproj"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "ERROR: dotnet SDK is required for UniversalRPG." >&2
  exit 2
fi

echo "[1/6] Public SDK restore"
dotnet restore "$SDK_PROJECT"

echo "[2/6] Public SDK build"
dotnet build "$SDK_PROJECT" --no-restore

cd "$PROJECT_DIR"

echo "[3/6] Godot .NET project restore"
dotnet restore

echo "[4/6] Godot .NET project build"
dotnet build --no-restore

find_godot() {
  if [[ -n "${GODOT_BIN:-}" && -x "${GODOT_BIN}" ]]; then
    printf '%s\n' "$GODOT_BIN"
    return 0
  fi

  local candidates=(
    "$ROOT_DIR/tools/godot/editors/4.7.2/linux-x86_64/Godot_v4.7.2-stable_mono_linux_x86_64/Godot_v4.7.2-stable_mono_linux.x86_64"
    "$ROOT_DIR/tools/godot/editors/4.7.2/linux-x86_64/Godot_v4.7.2-stable_linux.x86_64"
    "$ROOT_DIR/tools/godot/editors/4.7.2/windows-x86_64/Godot_v4.7.2-stable_mono_win64_console.exe"
    "$ROOT_DIR/tools/godot/editors/4.7.2/windows-x86_64/Godot_v4.7.2-stable_mono_win64.exe"
    "$ROOT_DIR/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe"
    "$ROOT_DIR/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64.exe"
  )
  local candidate
  for candidate in "${candidates[@]}"; do
    if [[ -x "$candidate" ]]; then
      printf '%s\n' "$candidate"
      return 0
    fi
  done

  if command -v godot >/dev/null 2>&1; then
    command -v godot
    return 0
  fi
  if command -v godot4 >/dev/null 2>&1; then
    command -v godot4
    return 0
  fi

  return 1
}

if ! GODOT="$(find_godot)"; then
  echo "ERROR: Godot 4.7.2 was not found." >&2
  echo "Set GODOT_BIN=/absolute/path/to/Godot or install the editor under tools/godot/editors/4.7.2/." >&2
  exit 127
fi

echo "Using Godot: $GODOT"
"$GODOT" --version

GODOT_PROJECT_PATH="$PROJECT_DIR"
if [[ "$GODOT" == *.exe ]] && command -v cygpath >/dev/null 2>&1; then
  GODOT_PROJECT_PATH="$(cygpath -m "$PROJECT_DIR")"
fi

echo "[5/6] Godot import validation"
"$GODOT" --headless --editor --quit --path "$GODOT_PROJECT_PATH"

echo "[6/6] C# core, SDK-adapter and smoke tests"
"$GODOT" --headless --path "$GODOT_PROJECT_PATH" res://tests/csharp_runner.tscn

echo "UniversalRPG validation passed."

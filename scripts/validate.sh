#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "ERROR: dotnet SDK is required for the .NET project." >&2
  exit 2
fi

echo "[1/5] .NET restore"
dotnet restore

echo "[2/5] .NET build"
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

GODOT_PROJECT_PATH="$ROOT_DIR"
if [[ "$GODOT" == *.exe ]] && command -v cygpath >/dev/null 2>&1; then
  GODOT_PROJECT_PATH="$(cygpath -m "$ROOT_DIR")"
fi

echo "[3/5] Godot import/syntax validation"
"$GODOT" --headless --editor --quit --path "$GODOT_PROJECT_PATH"

echo "[4/5] Core test suite"
"$GODOT" --headless --path "$GODOT_PROJECT_PATH" --script res://tests/runner.gd

echo "[5/5] Smoke tests"
"$GODOT" --headless --path "$GODOT_PROJECT_PATH" --script res://tests/smoke_runner.gd

echo "UniversalRPG validation passed."

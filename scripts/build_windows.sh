#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT_DIR="$ROOT_DIR/project"
OUTPUT_DIR="$ROOT_DIR/build/windows"
EVIDENCE_DIR="$ROOT_DIR/build/verification"

if [[ -z "${GODOT_BIN:-}" ]]; then
  for candidate in \
    "$ROOT_DIR/tools/godot/editors/4.7.2/windows-x86_64/Godot_v4.7.2-stable_mono_win64_console.exe" \
    "$ROOT_DIR/tools/godot/editors/4.7.2/linux-x86_64/Godot_v4.7.2-stable_mono_linux_x86_64/Godot_v4.7.2-stable_mono_linux.x86_64"; do
    if [[ -x "$candidate" ]]; then GODOT_BIN="$candidate"; break; fi
  done
fi
if [[ -z "${GODOT_BIN:-}" || ! -x "$GODOT_BIN" ]]; then
  printf '%s\n' 'Set GODOT_BIN to the Godot 4.7.2 .NET editor executable.' >&2
  exit 2
fi
export GODOT_BIN
mkdir -p "$OUTPUT_DIR" "$EVIDENCE_DIR"

# A release is produced only after the canonical build/import/regression suite.
"$ROOT_DIR/scripts/validate.sh" > "$EVIDENCE_DIR/validation.log" 2>&1

PROJECT_NATIVE="$PROJECT_DIR"
OUTPUT_NATIVE="$OUTPUT_DIR/UniversalRPG.exe"
if [[ "$GODOT_BIN" == *.exe ]] && command -v cygpath >/dev/null 2>&1; then
  PROJECT_NATIVE="$(cygpath -m "$PROJECT_DIR")"
  OUTPUT_NATIVE="$(cygpath -m "$OUTPUT_DIR/UniversalRPG.exe")"
fi
"$GODOT_BIN" --headless --path "$PROJECT_NATIVE" \
  --export-release "Windows Desktop" "$OUTPUT_NATIVE" \
  > "$EVIDENCE_DIR/export.log" 2>&1

if [[ ! -s "$OUTPUT_DIR/UniversalRPG.exe" || ! -s "$OUTPUT_DIR/UniversalRPG.pck" \
  || ! -s "$OUTPUT_DIR/data_UniversalRPG_windows_x86_64/UniversalRPG.dll" ]]; then
  printf '%s\n' 'Export is incomplete: executable, PCK or .NET assembly missing.' >&2
  exit 3
fi

case "$(uname -s)" in
  MINGW*|MSYS*|CYGWIN*)
    (cd "$OUTPUT_DIR" && ./UniversalRPG.exe --headless --quit-after 60) \
      > "$EVIDENCE_DIR/windows-headless.log" 2>&1
    ;;
  *) printf '%s\n' 'Windows executable smoke test must still run on Windows.' ;;
esac
printf 'Windows release: %s\nVerification logs: %s\n' "$OUTPUT_DIR" "$EVIDENCE_DIR"

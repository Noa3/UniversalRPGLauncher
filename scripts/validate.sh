#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT_DIR="$ROOT_DIR/project"
SDK_PROJECT="$ROOT_DIR/sdk/UniversalRPG.Sdk/UniversalRPG.Sdk.csproj"
JINT_PROJECT="$ROOT_DIR/runtime/UniversalRPG.JavaScript.Jint/UniversalRPG.JavaScript.Jint.csproj"
JINT_SMOKE_PROJECT="$ROOT_DIR/runtime/UniversalRPG.JavaScript.Jint.Smoke/UniversalRPG.JavaScript.Jint.Smoke.csproj"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "ERROR: dotnet SDK is required for UniversalRPG." >&2
  exit 2
fi

LOG_DIR="${URPG_VALIDATION_LOG_DIR:-$(mktemp -d "${TMPDIR:-/tmp}/urpg-validation.XXXXXX")}"
mkdir -p "$LOG_DIR"
LOG_DIR="$(cd "$LOG_DIR" && pwd)"
echo "Validation logs: $LOG_DIR"

run_logged() {
  local stage="$1"
  shift
  local raw="$LOG_DIR/$stage.raw.log"
  local normalized="$LOG_DIR/$stage.log"
  local code
  # Truncate each stage's log: an old success marker must never validate a new
  # run. Keep both raw and CRLF-normalized text for diagnosis across platforms.
  if "$@" >"$raw" 2>&1; then
    tr -d '\r' <"$raw" >"$normalized"
    cat "$normalized"
  else
    code=$?
    tr -d '\r' <"$raw" >"$normalized"
    cat "$normalized" >&2
    echo "ERROR: $stage failed (exit $code). See $raw" >&2
    exit "$code"
  fi
}

fail_validation() {
  echo "ERROR: $1. Logs: $LOG_DIR" >&2
  exit 1
}

reject_engine_errors() {
  if grep -Eq '^[[:space:]]*(SCRIPT ERROR:|Unhandled exception\.)' "$1"; then
    fail_validation "Engine/script error reported despite a successful process exit"
  fi
}

echo "[1/9] Public SDK restore"
run_logged 01-sdk-restore dotnet restore "$SDK_PROJECT"
echo "[2/9] Public SDK build"
run_logged 02-sdk-build dotnet build "$SDK_PROJECT" --no-restore

echo "[3/9] Jint JavaScript adapter restore/build"
run_logged 03-jint-restore dotnet restore "$JINT_PROJECT"
run_logged 03-jint-build dotnet build "$JINT_PROJECT" --no-restore

echo "[4/9] Jint JavaScript VM smoke"
run_logged 04-jint-smoke dotnet run --project "$JINT_SMOKE_PROJECT" --configuration Debug
if grep -q '^UniversalRPG Jint smoke FAILED:' "$LOG_DIR/04-jint-smoke.log"; then
  fail_validation "Jint smoke reported failure"
fi
reject_engine_errors "$LOG_DIR/04-jint-smoke.log"
if ! grep -Eq '^UniversalRPG Jint smoke passed \([1-9][0-9]* checks\)\.$' "$LOG_DIR/04-jint-smoke.log"; then
  fail_validation "Jint smoke did not confirm a non-empty completed test run"
fi

cd "$PROJECT_DIR"
echo "[5/9] Godot .NET project restore"
run_logged 05-godot-restore dotnet restore
echo "[6/9] Godot .NET project build"
run_logged 06-godot-build dotnet build --no-restore

find_godot() {
  # An explicit override is authoritative. Do not silently test a different
  # installation if the requested binary is missing or not executable.
  if [[ -n "${GODOT_BIN:-}" ]]; then
    [[ -x "$GODOT_BIN" ]] || return 1
    printf '%s\n' "$GODOT_BIN"
    return 0
  fi
  local candidates=(
    "$ROOT_DIR/tools/godot/editors/4.7.2/linux-x86_64/Godot_v4.7.2-stable_mono_linux_x86_64/Godot_v4.7.2-stable_mono_linux.x86_64"
    "$ROOT_DIR/tools/godot/editors/4.7.2/windows-x86_64/Godot_v4.7.2-stable_mono_win64_console.exe"
    "$ROOT_DIR/tools/godot/editors/4.7.2/windows-x86_64/Godot_v4.7.2-stable_mono_win64.exe"
    "$ROOT_DIR/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe"
    "$ROOT_DIR/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64.exe"
  )
  local candidate
  for candidate in "${candidates[@]}"; do
    if [[ -x "$candidate" ]]; then printf '%s\n' "$candidate"; return 0; fi
  done
  if command -v godot >/dev/null 2>&1; then command -v godot; return 0; fi
  if command -v godot4 >/dev/null 2>&1; then command -v godot4; return 0; fi
  return 1
}

if ! GODOT="$(find_godot)"; then
  echo "ERROR: requested Godot .NET binary was not found or is not executable." >&2
  echo "Set GODOT_BIN=/absolute/path/to/Godot or install under tools/godot/editors/4.7.2/." >&2
  exit 127
fi

echo "Using Godot: $GODOT"
run_logged 06-godot-version "$GODOT" --version
if ! grep -Eq '^4\.7\.2\.stable\.mono(\.|$)' "$LOG_DIR/06-godot-version.log"; then
  fail_validation "Godot must be the pinned 4.7.2 stable .NET/Mono build"
fi

GODOT_PROJECT_PATH="$PROJECT_DIR"
if [[ "$GODOT" == *.exe ]] && command -v cygpath >/dev/null 2>&1; then
  GODOT_PROJECT_PATH="$(cygpath -m "$PROJECT_DIR")"
fi

echo "[7/9] Godot import validation"
run_logged 07-godot-import "$GODOT" --headless --editor --quit --path "$GODOT_PROJECT_PATH"
reject_engine_errors "$LOG_DIR/07-godot-import.log"

echo "[8/9] C# core, SDK-adapter and smoke tests"
run_logged 08-godot-tests "$GODOT" --headless --path "$GODOT_PROJECT_PATH" res://tests/csharp_runner.tscn
reject_engine_errors "$LOG_DIR/08-godot-tests.log"
if grep -Eq '^[[:space:]]*[1-9][0-9]*/[0-9]+ tests failed' "$LOG_DIR/08-godot-tests.log"; then
  fail_validation "Godot runner reported test failures"
fi
if ! grep -Eq '^All [1-9][0-9]* tests passed$' "$LOG_DIR/08-godot-tests.log"; then
  fail_validation "Godot runner did not confirm a non-empty completed test run"
fi

echo "[9/9] Validation complete"
echo "UniversalRPG validation passed."

#!/usr/bin/env python3
"""Test the actual validate.sh control flow with isolated fake tool processes.

These tests exercise logs, exit codes and failure gates, NOT .NET/Jint/Godot.
They use only Python's standard library and Bash and require no network.
"""
from __future__ import annotations

import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest

DRIVER = Path(__file__).resolve().with_name("validate.sh")

DOTNET = r'''#!/usr/bin/env bash
set -eu
printf 'dotnet %s\n' "$*" >> "$CALLS"
if [[ "$SCENARIO" == sdk_fail && "$1" == build && "$*" == *sdk/UniversalRPG.Sdk/* ]]; then
  echo 'synthetic SDK build failure'; exit 37
fi
if [[ "$SCENARIO" == adapter_fail && "$1" == build && "$*" == *runtime/UniversalRPG.JavaScript.Jint/* ]]; then
  echo 'synthetic adapter build failure'; exit 38
fi
if [[ "$1" == run ]]; then
  case "$SCENARIO" in
    jint_missing) echo 'Process exited without running the smoke suite';;
    jint_zero) echo 'UniversalRPG Jint smoke passed (0 checks).';;
    jint_failure) echo 'UniversalRPG Jint smoke FAILED: synthetic'; echo 'UniversalRPG Jint smoke passed (3 checks).';;
    crlf) printf 'UniversalRPG Jint smoke passed (3 checks).\r\n';;
    *) echo 'UniversalRPG Jint smoke passed (3 checks).';;
  esac
fi
'''

GODOT = r'''#!/usr/bin/env bash
set -eu
printf 'godot %s\n' "$*" >> "$CALLS"
if [[ "$1" == --version ]]; then
  case "$SCENARIO" in
    standard_godot) echo '4.7.2.stable.official.fixture';;
    wrong_version) echo '4.6.0.stable.mono.official.fixture';;
    crlf) printf '4.7.2.stable.mono.official.fixture\r\n';;
    *) echo '4.7.2.stable.mono.official.fixture';;
  esac
  exit 0
fi
if [[ "$*" == *--editor* ]]; then
  [[ "$SCENARIO" != editor_error ]] || echo 'SCRIPT ERROR: synthetic import error'
  exit 0
fi
case "$SCENARIO" in
  missing|stale) echo 'Godot started, but no test completion';;
  zero) echo 'All 0 tests passed';;
  wrapped_failure) echo '256/900 tests failed'; exit 0;;
  fail_then_pass) echo '1/10 tests failed'; echo 'All 10 tests passed';;
  pass_then_fail) echo 'All 10 tests passed'; echo '1/10 tests failed';;
  script_error) echo 'All 10 tests passed'; echo 'SCRIPT ERROR: synthetic runtime error';;
  unhandled) echo 'Unhandled exception. SyntheticException'; echo 'All 10 tests passed';;
  pass_nonzero) echo 'All 10 tests passed'; exit 9;;
  crlf) printf 'All 10 tests passed\r\n';;
  *) echo 'All 10 tests passed';;
esac
'''


class ValidationDriverTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp = tempfile.TemporaryDirectory(prefix="urpg validation ")
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        (self.root / "scripts").mkdir()
        (self.root / "project").mkdir()
        (self.root / "bin").mkdir()
        self.logs = self.root / "logs"
        self.logs.mkdir()
        shutil.copy2(DRIVER, self.root / "scripts/validate.sh")
        for name, text in (("dotnet", DOTNET), ("godot", GODOT)):
            path = self.root / "bin" / name
            path.write_text(text, encoding="utf-8")
            path.chmod(0o755)

    def run_case(self, scenario: str, success: bool = False, invalid_binary: bool = False) -> subprocess.CompletedProcess[str]:
        env = os.environ.copy()
        env.update({
            "PATH": str(self.root / "bin") + os.pathsep + env.get("PATH", ""),
            "GODOT_BIN": str(self.root / "bin" / ("missing-godot" if invalid_binary else "godot")),
            "URPG_VALIDATION_LOG_DIR": str(self.logs),
            "SCENARIO": scenario,
            "CALLS": str(self.root / "calls.txt"),
        })
        result = subprocess.run(
            ["bash", str(self.root / "scripts/validate.sh")], env=env,
            text=True, capture_output=True, timeout=15, check=False,
        )
        output = result.stdout + result.stderr
        if success:
            self.assertEqual(result.returncode, 0, output)
            self.assertIn("UniversalRPG validation passed.", output)
        else:
            self.assertNotEqual(result.returncode, 0, output)
            self.assertNotIn("UniversalRPG validation passed.", output)
        return result

    def test_completed_nonempty_suites_pass(self) -> None:
        self.run_case("normal", success=True)
        self.assertIn("All 10 tests passed", (self.logs / "08-godot-tests.log").read_text())

    def test_crlf_logs_are_accepted_and_normalized(self) -> None:
        self.run_case("crlf", success=True)
        self.assertNotIn(b"\r", (self.logs / "08-godot-tests.log").read_bytes())

    def test_sdk_build_failure_stops_before_godot(self) -> None:
        result = self.run_case("sdk_fail")
        self.assertEqual(result.returncode, 37)
        self.assertNotIn("godot ", (self.root / "calls.txt").read_text())

    def test_adapter_build_failure_is_not_hidden(self) -> None:
        self.assertEqual(self.run_case("adapter_fail").returncode, 38)

    def test_jint_without_completion_marker_fails(self) -> None:
        self.run_case("jint_missing")

    def test_zero_jint_checks_cannot_pass(self) -> None:
        self.run_case("jint_zero")

    def test_jint_failure_is_not_overridden_by_success_text(self) -> None:
        self.run_case("jint_failure")

    def test_standard_non_dotnet_godot_is_refused(self) -> None:
        self.run_case("standard_godot")

    def test_wrong_pinned_godot_version_is_refused(self) -> None:
        self.run_case("wrong_version")

    def test_explicit_invalid_binary_does_not_fall_back(self) -> None:
        result = self.run_case("normal", invalid_binary=True)
        self.assertEqual(result.returncode, 127)
        self.assertNotIn("godot ", (self.root / "calls.txt").read_text())

    def test_editor_error_with_zero_exit_is_refused(self) -> None:
        self.run_case("editor_error")
        self.assertFalse((self.logs / "08-godot-tests.log").exists())

    def test_godot_without_test_completion_is_refused(self) -> None:
        self.run_case("missing")

    def test_zero_godot_tests_cannot_pass(self) -> None:
        self.run_case("zero")

    def test_failure_count_wrapped_to_zero_exit_still_fails(self) -> None:
        self.run_case("wrapped_failure")

    def test_failure_marker_on_either_side_of_success_still_fails(self) -> None:
        for scenario in ("fail_then_pass", "pass_then_fail"):
            with self.subTest(scenario=scenario):
                self.run_case(scenario)

    def test_script_or_unhandled_error_overrides_success_marker(self) -> None:
        for scenario in ("script_error", "unhandled"):
            with self.subTest(scenario=scenario):
                self.run_case(scenario)

    def test_nonzero_exit_overrides_success_marker(self) -> None:
        self.assertEqual(self.run_case("pass_nonzero").returncode, 9)

    def test_stale_pass_log_is_truncated_before_new_run(self) -> None:
        (self.logs / "08-godot-tests.log").write_text("All 999 tests passed\n")
        self.run_case("stale")
        self.assertNotIn("999", (self.logs / "08-godot-tests.log").read_text())


if __name__ == "__main__":
    unittest.main(verbosity=2)

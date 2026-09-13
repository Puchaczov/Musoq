"""Synthetic tests for the full-gate runner.

These tests never invoke Musoq or a provider.  They exercise the gate's
provenance and counter checks with small, repository-independent fixtures.
"""
from __future__ import annotations

import os
import shutil
import tempfile
import unittest
from datetime import datetime, timedelta, timezone
from pathlib import Path

from full_gate import (
    GateFailure,
    ProcessResult,
    TestTarget,
    artifact_reference,
    discover_test_targets,
    validate_gate,
)


class FullGateTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.solution = self.root / "Musoq.sln"
        self.solution.write_text("synthetic solution", encoding="utf-8")
        self.project = self.root / "src" / "Musoq.Tests" / "Musoq.Tests.csproj"
        self.project.parent.mkdir(parents=True)
        self.project.write_text(
            """<Project Sdk=\"Microsoft.NET.Sdk\">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
  <ItemGroup><PackageReference Include=\"Microsoft.NET.Test.Sdk\" /></ItemGroup>
</Project>
""",
            encoding="utf-8",
        )
        self.source = self.root / "src" / "Musoq.Tests" / "Source.cs"
        self.source.write_text("source", encoding="utf-8")
        self.assembly = self.root / "src" / "Musoq.Tests" / "bin" / "Release" / "net10.0" / "Musoq.Tests.dll"
        self.assembly.parent.mkdir(parents=True)
        self.assembly.write_bytes(b"built test assembly")
        self.target = TestTarget(
            "Musoq.Tests/net10.0",
            "src/Musoq.Tests/Musoq.Tests.csproj",
            "net10.0",
            "src/Musoq.Tests/bin/Release/net10.0/Musoq.Tests.dll",
        )
        self.results = self.root / "results" / "trx"
        self.results.mkdir(parents=True)
        self.now = datetime.now(timezone.utc).replace(microsecond=0)
        self.started = self.now - timedelta(seconds=3)
        self.finished = self.now + timedelta(seconds=3)
        self.trx = self.results / "fresh.trx"
        self.write_trx()
        self.commands = self.make_commands()
        self.before = {
            "sha256": "a" * 64,
            "fileCount": 1,
            "files": [{"path": "src/Musoq.Tests/Source.cs", "sha256": "b" * 64}],
        }
        self.after = dict(self.before)
        self.build_artifacts = {
            self.target.target_id: {
                **artifact_reference(self.root, self.assembly),
                "modifiedAt": self.now.isoformat().replace("+00:00", "Z"),
            }
        }

    def tearDown(self) -> None:
        self.temp.cleanup()

    def write_trx(self, *, outcome: str = "Passed", timestamp: datetime | None = None, total: int = 1) -> None:
        point = timestamp or self.now
        stamp = point.isoformat().replace("+00:00", "Z")
        passed = 1 if outcome == "Passed" else 0
        skipped = 1 if outcome == "NotExecuted" else 0
        result = "" if total == 0 else (
            f'<UnitTestResult testId="test-1" testName="Works" outcome="{outcome}">'
            + ("<Output><ErrorInfo><Message>intentional fixture skip</Message></ErrorInfo></Output>" if skipped else "")
            + "</UnitTestResult>"
        )
        definition = "" if total == 0 else (
            f'<UnitTest id="test-1" name="Works" storage="{self.assembly}">'
            f'<TestMethod codeBase="{self.assembly}" className="Musoq.Tests.SampleTests" name="Works" />'
            "</UnitTest>"
        )
        executed = passed
        self.trx.write_text(
            f'''<?xml version="1.0" encoding="utf-8"?>
<TestRun id="run-1" xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
  <Times creation="{stamp}" start="{stamp}" finish="{stamp}" />
  <Results>{result}</Results>
  <TestDefinitions>{definition}</TestDefinitions>
  <ResultSummary outcome="Completed"><Counters total="{total}" executed="{executed}" passed="{passed}" failed="0" notExecuted="{skipped}" aborted="0" /></ResultSummary>
</TestRun>
''',
            encoding="utf-8",
        )

    def make_commands(self) -> list[dict]:
        commands = []
        for number, role in enumerate(("restore", "build", "test"), start=1):
            stdout = self.root / f"{role}.stdout.log"
            stderr = self.root / f"{role}.stderr.log"
            stdout.write_text(f"{role} output", encoding="utf-8")
            stderr.write_text("", encoding="utf-8")
            arguments = [role, "Musoq.sln", "-c", "Release"]
            if role == "build":
                arguments += ["--no-incremental"]
            if role == "test":
                arguments += ["--no-build", "--no-restore", "--logger", "trx", "--results-directory", "results/trx"]
            commands.append({
                "executable": "dotnet",
                "argv": arguments,
                "exitCode": 0,
                "startedAt": (self.started + timedelta(seconds=number - 1)).isoformat().replace("+00:00", "Z"),
                "finishedAt": (self.started + timedelta(seconds=number)).isoformat().replace("+00:00", "Z"),
                "stdoutArtifact": artifact_reference(self.root, stdout),
                "stderrArtifact": artifact_reference(self.root, stderr),
            })
        return commands

    def validate(self, **overrides):
        values = {
            "root": self.root,
            "scope_id": "REC-085",
            "run_id": "fixture-run",
            "solution": self.solution,
            "configuration": "Release",
            "targets": [self.target],
            "expected_inventory": [self.target.as_dict()],
            "commands": self.commands,
            "result_directory": self.results,
            "started_at": self.started.isoformat().replace("+00:00", "Z"),
            "finished_at": self.finished.isoformat().replace("+00:00", "Z"),
            "before_payload": self.before,
            "after_payload": self.after,
            "build_artifacts": self.build_artifacts,
            "skip_allowlist": [],
        }
        values.update(overrides)
        return validate_gate(**values)

    def test_valid_complete_gate_is_machine_readable(self) -> None:
        evidence = self.validate()
        self.assertEqual("passed", evidence["outcome"])
        self.assertEqual([self.target.target_id], evidence["expectedTargets"])
        self.assertEqual(1, evidence["results"][0]["passed"])

    def test_discovery_includes_all_test_projects_and_frameworks(self) -> None:
        example = self.root / "examples" / "Bench.Tests.csproj"
        example.parent.mkdir()
        example.write_text(
            '<Project><PropertyGroup><TargetFrameworks>net10.0;net9.0</TargetFrameworks></PropertyGroup>'
            '<ItemGroup><PackageReference Include="Microsoft.NET.Test.Sdk" /></ItemGroup></Project>',
            encoding="utf-8",
        )

        def fake_runner(argv, cwd):
            self.assertEqual(["dotnet", "sln", "Musoq.sln", "list"], argv)
            return ProcessResult(
                0,
                b"Project(s)\n----------\nsrc\\Musoq.Tests\\Musoq.Tests.csproj\nexamples\\Bench.Tests.csproj\n",
                b"",
                self.started.isoformat().replace("+00:00", "Z"),
                self.finished.isoformat().replace("+00:00", "Z"),
            )

        targets, inventory = discover_test_targets(self.root, self.solution, process_runner=fake_runner)
        self.assertEqual(
            ["Musoq.Tests/net10.0", "Bench.Tests/net10.0", "Bench.Tests/net9.0"],
            [target.target_id for target in targets],
        )
        self.assertEqual(2, len(inventory["solutionProjects"]))

    def assertGateFails(self, expected: str, **overrides) -> None:
        with self.assertRaises(GateFailure) as context:
            self.validate(**overrides)
        self.assertIn(expected, str(context.exception))

    def test_missing_project_inventory_fails(self) -> None:
        self.assertGateFails(
            "inventory shrinkage/mismatch",
            expected_inventory=[self.target.as_dict(), {"targetId": "Missing.Tests/net10.0"}],
        )

    def test_zero_test_filter_fails(self) -> None:
        commands = [dict(command) for command in self.commands]
        commands[2] = dict(commands[2])
        commands[2]["argv"] = [*commands[2]["argv"], "--filter", "NeverMatches"]
        self.assertGateFails("must not use a filter", commands=commands)

    def test_failed_child_process_fails(self) -> None:
        commands = [dict(command) for command in self.commands]
        commands[1] = dict(commands[1])
        commands[1]["exitCode"] = 1
        self.assertGateFails("Child process failed", commands=commands)

    def test_stale_result_fails(self) -> None:
        old = self.now - timedelta(hours=1)
        self.write_trx(timestamp=old)
        old_timestamp = old.timestamp()
        os.utime(self.trx, (old_timestamp, old_timestamp))
        self.assertGateFails("TRX result is stale")

    def test_duplicate_result_fails(self) -> None:
        shutil.copyfile(self.trx, self.results / "duplicate.trx")
        self.assertGateFails("Duplicate result for test target")

    def test_source_modified_after_build_fails(self) -> None:
        after = {
            "sha256": "c" * 64,
            "fileCount": 1,
            "files": [{"path": "src/Musoq.Tests/Source.cs", "sha256": "d" * 64}],
        }
        self.assertGateFails("Source payload changed after build", after_payload=after)

    def test_unexpected_skip_fails_and_allowlisted_skip_passes(self) -> None:
        self.write_trx(outcome="NotExecuted")
        self.assertGateFails("Unexpected skipped test")
        evidence = self.validate(skip_allowlist=[{
            "testId": "Musoq.Tests.SampleTests.Works",
            "reason": "intentional fixture skip",
        }])
        self.assertEqual(1, evidence["results"][0]["skipped"])

    def test_stale_binary_fails(self) -> None:
        old = self.now - timedelta(hours=1)
        old_timestamp = old.timestamp()
        os.utime(self.assembly, (old_timestamp, old_timestamp))
        self.assertGateFails("Build artifact Musoq.Tests/net10.0 is stale")


if __name__ == "__main__":
    unittest.main()

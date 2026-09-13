#!/usr/bin/env python3
"""Run and validate the Musoq full Release test gate.

This is a small evidence helper, not a second test orchestrator.  It discovers
test projects from the selected solution, runs the three repository gate
commands, binds each fresh TRX to a test assembly, and rejects incomplete or
stale evidence.  Raw command output belongs under an ignored artifact
directory; only the bounded JSON summary should be committed.

The validator is intentionally usable without running dotnet.  The companion
unit tests use synthetic TRX and process records to prove that a missing
project, empty result, failed process, stale result, duplicate result, or
source mutation cannot be reported as a passing gate.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import subprocess
import sys
import uuid
from dataclasses import dataclass
from datetime import datetime, timedelta, timezone
from pathlib import Path
from typing import Any, Callable, Iterable
from xml.etree import ElementTree as ET


DEFAULT_ROOT = Path(__file__).resolve().parents[4]
DEFAULT_PAYLOAD_EXCLUSIONS = (
    ".campaign-artifacts/",
    "TestResults/",
    "musoq-recovery-campaign.json",
    "docs/campaigns/recovery-v3/evidence/",
    "docs/campaigns/recovery-v3/reports/",
    "docs/campaigns/recovery-v3/PROGRESS.md",
    "docs/campaigns/recovery-v3/source-bindings.json",
)
OUTPUT_DIRECTORY_NAMES = {".campaign-artifacts", "bin", "obj", "TestResults", "__pycache__"}


class GateFailure(ValueError):
    """A gate validation failure with stable, testable messages."""

    def __init__(self, errors: Iterable[str]):
        self.errors = list(errors)
        super().__init__("; ".join(self.errors))


@dataclass(frozen=True)
class ProcessResult:
    returncode: int
    stdout: bytes
    stderr: bytes
    started_at: str
    finished_at: str


@dataclass(frozen=True)
class TestTarget:
    target_id: str
    project_path: str
    target_framework: str
    assembly_path: str

    def as_dict(self) -> dict[str, str]:
        return {
            "targetId": self.target_id,
            "projectPath": self.project_path,
            "targetFramework": self.target_framework,
            "assemblyPath": self.assembly_path,
        }


def utc_now() -> str:
    return datetime.now(timezone.utc).isoformat(timespec="milliseconds").replace("+00:00", "Z")


def parse_time(value: str) -> datetime:
    if not isinstance(value, str) or not value:
        raise ValueError("timestamp is missing")
    parsed = datetime.fromisoformat(value.replace("Z", "+00:00"))
    if parsed.tzinfo is None:
        raise ValueError("timestamp has no timezone")
    return parsed.astimezone(timezone.utc)


def sha256_bytes(value: bytes) -> str:
    return hashlib.sha256(value).hexdigest()


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def relative_path(root: Path, path: Path) -> str:
    return path.resolve().relative_to(root.resolve()).as_posix()


def contained(root: Path, value: str) -> Path:
    if not isinstance(value, str) or not value:
        raise ValueError("artifact path must be a nonempty relative path")
    normalized = value.replace("\\", "/")
    candidate = (root / Path(normalized)).resolve()
    candidate.relative_to(root.resolve())
    return candidate


def normalized_path(value: str | Path) -> str:
    text = str(value).replace("\\", "/")
    if text.startswith("file://"):
        text = text[7:]
    return text.rstrip("/").lower()


def run_process(argv: list[str], cwd: Path) -> ProcessResult:
    started = utc_now()
    try:
        completed = subprocess.run(
            argv,
            cwd=str(cwd),
            capture_output=True,
            check=False,
        )
        result = ProcessResult(
            completed.returncode,
            completed.stdout,
            completed.stderr,
            started,
            utc_now(),
        )
    except OSError as error:
        result = ProcessResult(127, b"", str(error).encode("utf-8", "replace"), started, utc_now())
    return result


def _local_name(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def _children(element: ET.Element, name: str) -> list[ET.Element]:
    return [child for child in element if _local_name(child.tag) == name]


def _descendants(element: ET.Element, name: str) -> list[ET.Element]:
    return [child for child in element.iter() if _local_name(child.tag) == name]


def _first_descendant(element: ET.Element, name: str) -> ET.Element | None:
    return next(iter(_descendants(element, name)), None)


def _property_text(project: ET.Element, name: str) -> list[str]:
    values: list[str] = []
    for element in _descendants(project, name):
        if element.text and element.text.strip():
            values.append(element.text.strip())
    return values


def _project_is_test(project: ET.Element) -> bool:
    for element in _descendants(project, "PackageReference"):
        include = element.attrib.get("Include", "").strip().lower()
        if include == "microsoft.net.test.sdk":
            return True
    return any(value.strip().lower() == "true" for value in _property_text(project, "IsTestProject"))


def _project_target_frameworks(project: ET.Element) -> list[str]:
    values: list[str] = []
    for name in ("TargetFrameworks", "TargetFramework"):
        for value in _property_text(project, name):
            values.extend(part.strip() for part in value.split(";") if part.strip())
    result = list(dict.fromkeys(values))
    if not result:
        raise GateFailure(["Test project has no target framework"])
    if any("$(" in value for value in result):
        unresolved = [value for value in result if "$(" in value]
        raise GateFailure([f"Target framework is unresolved: {value}" for value in unresolved])
    return result


def _assembly_name(project: ET.Element, project_path: Path) -> str:
    values = _property_text(project, "AssemblyName")
    return values[0] if values else project_path.stem


def solution_argument(root: Path, solution: Path) -> str:
    return relative_path(root, solution.resolve())


def discover_test_targets(
    root: Path,
    solution: Path,
    dotnet: str = "dotnet",
    configuration: str = "Release",
    process_runner: Callable[[list[str], Path], ProcessResult] = run_process,
) -> tuple[list[TestTarget], dict[str, Any]]:
    """Discover test projects/frameworks from ``dotnet sln ... list``."""
    root = root.resolve()
    solution = solution if solution.is_absolute() else root / solution
    solution = solution.resolve()
    if not solution.is_file():
        raise GateFailure([f"Solution is missing: {relative_path(root, solution)}"])

    result = process_runner([dotnet, "sln", solution_argument(root, solution), "list"], root)
    if result.returncode != 0:
        raise GateFailure([f"Solution inventory command failed with exit code {result.returncode}"])

    project_paths: list[Path] = []
    for raw_line in result.stdout.decode("utf-8", "replace").splitlines():
        line = raw_line.strip()
        if not line.lower().endswith(".csproj"):
            continue
        project = (solution.parent / Path(line.replace("\\", "/"))).resolve()
        try:
            project.relative_to(root)
        except ValueError as error:
            raise GateFailure([f"Solution project escapes repository root: {line}"]) from error
        if project not in project_paths:
            project_paths.append(project)

    if not project_paths:
        raise GateFailure(["Solution inventory contains no projects"])

    targets: list[TestTarget] = []
    for project_path in project_paths:
        if not project_path.is_file():
            raise GateFailure([f"Solution project is missing: {relative_path(root, project_path)}"])
        try:
            project_xml = ET.parse(project_path).getroot()
        except ET.ParseError as error:
            raise GateFailure([f"Test-project XML is invalid: {relative_path(root, project_path)}"]) from error
        if not _project_is_test(project_xml):
            continue
        assembly_name = _assembly_name(project_xml, project_path)
        project_relative = relative_path(root, project_path)
        for framework in _project_target_frameworks(project_xml):
            assembly_relative = (project_path.parent / "bin" / configuration / framework / f"{assembly_name}.dll")
            target_id = f"{project_path.stem}/{framework}"
            targets.append(TestTarget(target_id, project_relative, framework, relative_path(root, assembly_relative)))

    if not targets:
        raise GateFailure(["Solution inventory contains no test project targets"])
    ids = [target.target_id for target in targets]
    if len(ids) != len(set(ids)):
        raise GateFailure(["Solution inventory contains duplicate test targets"])
    inventory = {
        "command": {
            "executable": dotnet,
            "argv": ["sln", solution_argument(root, solution), "list"],
            "exitCode": result.returncode,
            "startedAt": result.started_at,
            "finishedAt": result.finished_at,
        },
        "solutionProjects": [relative_path(root, path) for path in project_paths],
        "testTargets": [target.as_dict() for target in targets],
        "stdoutSha256": sha256_bytes(result.stdout),
    }
    return targets, inventory


def _git_tracked_paths(root: Path) -> list[str]:
    result = subprocess.run(
        ["git", "-C", str(root), "ls-files", "-z"],
        capture_output=True,
        check=False,
    )
    if result.returncode != 0:
        raise GateFailure(["Cannot enumerate tracked source files"])
    return [item.decode("utf-8", "surrogateescape") for item in result.stdout.split(b"\0") if item]


def _payload_included(relative: str) -> bool:
    normalized = relative.replace("\\", "/")
    lower = normalized.lower()
    parts = {part.lower() for part in Path(normalized).parts}
    if parts.intersection({name.lower() for name in OUTPUT_DIRECTORY_NAMES}):
        return False
    if lower.endswith(".pyc"):
        return False
    for exclusion in DEFAULT_PAYLOAD_EXCLUSIONS:
        prefix = exclusion.lower().rstrip("/")
        if lower == prefix or lower.startswith(prefix + "/"):
            return False
    return True


def _candidate_untracked_paths(root: Path) -> Iterable[Path]:
    for directory in (root / "src", root / "scripts", root / "docs" / "campaigns" / "recovery-v3" / "tools"):
        if not directory.is_dir():
            continue
        for current, directories, files in os.walk(directory):
            directories[:] = [name for name in directories if name not in OUTPUT_DIRECTORY_NAMES]
            for name in files:
                yield Path(current) / name


def snapshot_payload(root: Path, explicit_paths: Iterable[Path] | None = None) -> dict[str, Any]:
    """Hash the current source payload; administrative/raw outputs are excluded."""
    root = root.resolve()
    if explicit_paths is None:
        candidates: set[Path] = set()
        for value in _git_tracked_paths(root):
            candidates.add((root / Path(value.replace("\\", "/"))).resolve())
        candidates.update(path.resolve() for path in _candidate_untracked_paths(root))
    else:
        candidates = {path.resolve() if path.is_absolute() else (root / path).resolve() for path in explicit_paths}

    entries: list[tuple[str, str]] = []
    for path in sorted(candidates, key=lambda item: relative_path(root, item)):
        try:
            relative = relative_path(root, path)
        except ValueError:
            continue
        if not _payload_included(relative):
            continue
        if not path.is_file():
            raise GateFailure([f"Source payload file disappeared: {relative}"])
        entries.append((relative, sha256_file(path)))
    if not entries:
        raise GateFailure(["Source payload is empty"])
    canonical = "".join(f"{path}\t{digest}\n" for path, digest in entries).encode("utf-8")
    return {
        "sha256": sha256_bytes(canonical),
        "fileCount": len(entries),
        "files": [{"path": path, "sha256": digest} for path, digest in entries],
        "exclusions": list(DEFAULT_PAYLOAD_EXCLUSIONS),
    }


def changed_payload_paths(before: dict[str, Any], after: dict[str, Any]) -> list[str]:
    old = {item["path"]: item["sha256"] for item in before.get("files", [])}
    new = {item["path"]: item["sha256"] for item in after.get("files", [])}
    return sorted(path for path in set(old) | set(new) if old.get(path) != new.get(path))


def artifact_reference(root: Path, path: Path) -> dict[str, str]:
    return {"path": relative_path(root, path), "sha256": sha256_file(path)}


def run_logged_command(root: Path, executable: str, arguments: list[str], stdout_path: Path, stderr_path: Path) -> dict[str, Any]:
    result = run_process([executable, *arguments], root)
    stdout_path.parent.mkdir(parents=True, exist_ok=True)
    stdout_path.write_bytes(result.stdout)
    stderr_path.write_bytes(result.stderr)
    return {
        "executable": executable,
        "argv": arguments,
        "exitCode": result.returncode,
        "startedAt": result.started_at,
        "finishedAt": result.finished_at,
        "stdoutArtifact": artifact_reference(root, stdout_path),
        "stderrArtifact": artifact_reference(root, stderr_path),
    }


def _option_value(arguments: list[str], names: set[str]) -> str | None:
    for index, argument in enumerate(arguments):
        if argument in names and index + 1 < len(arguments):
            return arguments[index + 1]
    return None


def _same_path(root: Path, left: str, right: Path) -> bool:
    left_normalized = normalized_path(left)
    right_normalized = normalized_path(right.resolve())
    relative_normalized = normalized_path(relative_path(root, right))
    if left_normalized in {right_normalized, relative_normalized}:
        return True
    # TRX can contain a drive-qualified path with a casing/separator variant.
    return left_normalized.endswith("/" + relative_normalized)


def _test_identity(definition: ET.Element | None, result: ET.Element) -> str:
    method = _first_descendant(definition, "TestMethod") if definition is not None else None
    class_name = method.attrib.get("className", "").strip() if method is not None else ""
    name = method.attrib.get("name", "").strip() if method is not None else result.attrib.get("testName", "").strip()
    return f"{class_name}.{name}" if class_name else name


def _test_codebase(definition: ET.Element | None) -> str | None:
    method = _first_descendant(definition, "TestMethod") if definition is not None else None
    if method is not None and method.attrib.get("codeBase"):
        return method.attrib["codeBase"]
    if definition is not None and definition.attrib.get("storage"):
        return definition.attrib["storage"]
    return None


def _skip_reason(result: ET.Element) -> str:
    message = _first_descendant(result, "Message")
    return " ".join("".join(message.itertext()).split()) if message is not None else "No reason recorded by test runner."


def parse_trx(path: Path) -> dict[str, Any]:
    try:
        root = ET.parse(path).getroot()
    except (ET.ParseError, OSError) as error:
        raise GateFailure([f"TRX cannot be parsed: {path.name}"]) from error
    summary = _first_descendant(root, "ResultSummary")
    counters = _first_descendant(summary, "Counters") if summary is not None else None
    if summary is None or counters is None:
        raise GateFailure([f"TRX lacks result counters: {path.name}"])

    def counter(name: str) -> int:
        value = counters.attrib.get(name)
        try:
            parsed = int(value) if value is not None else -1
        except ValueError:
            parsed = -1
        if parsed < 0:
            raise GateFailure([f"TRX counter is missing/invalid: {path.name}/{name}"])
        return parsed

    definitions = {
        element.attrib.get("id", ""): element
        for element in _descendants(root, "UnitTest")
        if element.attrib.get("id")
    }
    results_element = _first_descendant(root, "Results")
    results = _descendants(results_element if results_element is not None else root, "UnitTestResult")
    outcomes = {result.attrib.get("outcome", "").strip().lower() for result in results}
    passed = sum(outcome == "passed" for outcome in (result.attrib.get("outcome", "").strip().lower() for result in results))
    failed = sum(outcome in {"failed", "error"} for outcome in (result.attrib.get("outcome", "").strip().lower() for result in results))
    skipped = sum(outcome in {"notexecuted", "skipped", "inconclusive", "notrunnable", "pending"} for outcome in (result.attrib.get("outcome", "").strip().lower() for result in results))
    aborted = "aborted" in outcomes or counter("aborted") > 0 or summary.attrib.get("outcome") != "Completed"
    other = len(results) - passed - failed - skipped
    if other:
        raise GateFailure([f"TRX has unrecognized result outcomes: {path.name}"])

    skips: list[dict[str, str]] = []
    codebases: set[str] = set()
    for result in results:
        definition = definitions.get(result.attrib.get("testId", ""))
        codebase = _test_codebase(definition)
        if codebase:
            codebases.add(codebase)
        outcome = result.attrib.get("outcome", "").strip().lower()
        if outcome in {"notexecuted", "skipped", "inconclusive", "notrunnable", "pending"}:
            skips.append({"testId": _test_identity(definition, result), "reason": _skip_reason(result)})

    times = _first_descendant(root, "Times")
    if times is None:
        raise GateFailure([f"TRX lacks execution timestamps: {path.name}"])
    timestamps = {key: times.attrib.get(key) for key in ("creation", "start", "finish")}
    for key, value in timestamps.items():
        if not value:
            raise GateFailure([f"TRX lacks {key} timestamp: {path.name}"])

    return {
        "file": path,
        "runId": root.attrib.get("id"),
        "outcome": summary.attrib.get("outcome"),
        "timestamps": timestamps,
        "codeBases": sorted(codebases),
        "passed": passed,
        "failed": failed,
        "skipped": skipped,
        "total": counter("total"),
        "discovered": len(results),
        "executed": counter("executed"),
        "aborted": aborted,
        "skipTests": skips,
        "counters": {name: counter(name) for name in ("total", "executed", "passed", "failed", "notExecuted")},
    }


def _validate_artifact(root: Path, reference: dict[str, Any], label: str, errors: list[str], fresh_after: datetime | None = None) -> Path | None:
    if not isinstance(reference, dict) or not reference.get("path") or not reference.get("sha256"):
        errors.append(f"{label} lacks a path/digest")
        return None
    try:
        path = contained(root, reference["path"])
    except (ValueError, OSError):
        errors.append(f"{label} path escapes root")
        return None
    if not path.is_file():
        errors.append(f"{label} is missing")
        return None
    actual = sha256_file(path)
    if actual != reference["sha256"]:
        errors.append(f"{label} digest mismatch")
    if fresh_after is not None and path.stat().st_mtime < (fresh_after - timedelta(seconds=1)).timestamp():
        errors.append(f"{label} is stale")
    return path


def _load_expected_inventory(path: Path) -> list[dict[str, Any]]:
    document = json.loads(path.read_text(encoding="utf-8-sig"))
    values = document.get("targets", document) if isinstance(document, dict) else document
    if not isinstance(values, list):
        raise GateFailure(["Expected target inventory must be a JSON array or {targets: []}"])
    return values


def validate_gate(
    *,
    root: Path,
    scope_id: str,
    run_id: str,
    solution: Path,
    configuration: str,
    targets: list[TestTarget],
    expected_inventory: list[dict[str, Any]] | None,
    commands: list[dict[str, Any]],
    result_directory: Path,
    started_at: str,
    finished_at: str,
    before_payload: dict[str, Any],
    after_payload: dict[str, Any],
    build_artifacts: dict[str, dict[str, Any]],
    skip_allowlist: list[dict[str, str]],
    repository_identity: dict[str, Any] | None = None,
) -> dict[str, Any]:
    """Validate a complete gate and return the bounded machine-readable record."""
    root = root.resolve()
    solution = solution if solution.is_absolute() else root / solution
    solution = solution.resolve()
    started = parse_time(started_at)
    finished = parse_time(finished_at)
    errors: list[str] = []

    if finished < started:
        errors.append("Gate finished before it started")
    if len(commands) < 3:
        errors.append("Full gate is missing restore/build/test commands")
    else:
        roles = [str(command.get("argv", [""])[0]).lower() if command.get("argv") else "" for command in commands[:3]]
        if roles != ["restore", "build", "test"]:
            errors.append("Full gate command order must be restore, build, test")
    for command in commands:
        if not isinstance(command, dict):
            errors.append("Full gate command record is invalid")
            continue
        if command.get("exitCode") != 0:
            errors.append(f"Child process failed: {command.get('argv', ['?'])[0]}")
        for stream in ("stdoutArtifact", "stderrArtifact"):
            try:
                command_started = parse_time(command.get("startedAt", started_at))
            except ValueError:
                command_started = started
                errors.append("Command timestamp is invalid")
            _validate_artifact(root, command.get(stream), f"{command.get('argv', ['?'])[0]} {stream}", errors, command_started)

    test_command = commands[2] if len(commands) >= 3 else None
    test_arguments = list(test_command.get("argv", [])) if isinstance(test_command, dict) else []
    solution_arg = solution_argument(root, solution)
    if test_command is not None:
        if "--filter" in test_arguments or "-f" in test_arguments:
            errors.append("Test gate must not use a filter")
        if "--no-build" not in test_arguments:
            errors.append("Test gate must use --no-build after the recorded build")
        if "--no-restore" not in test_arguments:
            errors.append("Test gate must use --no-restore after the recorded restore")
        if not any(normalized_path(argument) == normalized_path(solution_arg) for argument in test_arguments):
            errors.append("Test gate is not bound to the selected solution")
        configuration_value = _option_value(test_arguments, {"-c", "--configuration"})
        if configuration_value != configuration:
            errors.append("Test gate configuration does not match the recorded build")
        result_argument = _option_value(test_arguments, {"--results-directory"})
        if result_argument is None or not _same_path(root, result_argument, result_directory):
            errors.append("Test gate result directory is not bound to the fresh output directory")

    changed = changed_payload_paths(before_payload, after_payload)
    if before_payload.get("sha256") != after_payload.get("sha256"):
        suffix = f": {', '.join(changed[:8])}" if changed else ""
        errors.append(f"Source payload changed after build{suffix}")

    build_command = commands[1] if len(commands) > 1 and isinstance(commands[1], dict) else {}
    build_arguments = list(build_command.get("argv", []))
    if "--no-incremental" not in build_arguments:
        errors.append("Build gate must use --no-incremental for fresh test artifacts")
    try:
        build_started = parse_time(build_command.get("startedAt", started_at))
    except ValueError:
        build_started = started
    for target in targets:
        reference = build_artifacts.get(target.target_id)
        if reference is None:
            errors.append(f"Missing build artifact: {target.target_id}")
            continue
        artifact_path = _validate_artifact(root, reference, f"Build artifact {target.target_id}", errors, build_started)
        if artifact_path is not None and normalized_path(relative_path(root, artifact_path)) != normalized_path(target.assembly_path):
            errors.append(f"Build artifact is not the discovered test assembly: {target.target_id}")

    discovered_ids = {target.target_id for target in targets}
    expected_ids = {str(item.get("targetId")) if isinstance(item, dict) else str(item) for item in (expected_inventory or [])}
    if expected_inventory is not None and expected_ids != discovered_ids:
        missing = sorted(expected_ids - discovered_ids)
        unexpected = sorted(discovered_ids - expected_ids)
        errors.append(f"Test inventory shrinkage/mismatch; missing={missing}, unexpected={unexpected}")

    allowlisted = {entry.get("testId") for entry in skip_allowlist if isinstance(entry, dict)}
    for entry in skip_allowlist:
        if not isinstance(entry, dict) or not entry.get("testId") or not entry.get("reason"):
            errors.append("Skip allowlist contains an incomplete identity/reason")

    result_directory = result_directory.resolve()
    trx_files = sorted(result_directory.rglob("*.trx")) if result_directory.is_dir() else []
    if not trx_files:
        errors.append("Fresh result directory contains no TRX files")

    results: list[dict[str, Any]] = []
    result_targets: set[str] = set()
    for trx_path in trx_files:
        try:
            parsed = parse_trx(trx_path)
        except GateFailure as failure:
            errors.extend(failure.errors)
            continue
        file_mtime = datetime.fromtimestamp(trx_path.stat().st_mtime, timezone.utc)
        if file_mtime < started - timedelta(seconds=1):
            errors.append(f"TRX result is stale: {trx_path.name}")
        try:
            trx_times = {key: parse_time(value) for key, value in parsed["timestamps"].items()}
            if trx_times["creation"] < started - timedelta(seconds=2) or trx_times["finish"] > finished + timedelta(seconds=2):
                errors.append(f"TRX timestamps are outside this invocation: {trx_path.name}")
        except ValueError:
            errors.append(f"TRX timestamp binding is invalid: {trx_path.name}")

        matching = [
            target for target in targets
            if any(_same_path(root, codebase, root / target.assembly_path) for codebase in parsed["codeBases"])
        ]
        if len(matching) != 1:
            errors.append(f"TRX result cannot be bound to exactly one test target: {trx_path.name}")
            continue
        target = matching[0]
        if target.target_id in result_targets:
            errors.append(f"Duplicate result for test target: {target.target_id}")
        result_targets.add(target.target_id)

        counters = parsed["counters"]
        if parsed["total"] <= 0:
            errors.append(f"Empty test result: {target.target_id}")
        if parsed["discovered"] != parsed["total"]:
            errors.append(f"Discovered/total mismatch: {target.target_id}")
        if parsed["passed"] + parsed["failed"] + parsed["skipped"] != parsed["total"]:
            errors.append(f"Unreconciled result counters: {target.target_id}")
        if parsed["executed"] != parsed["passed"] + parsed["failed"]:
            errors.append(f"Executed/result mismatch: {target.target_id}")
        if counters["passed"] != parsed["passed"] or counters["failed"] != parsed["failed"]:
            errors.append(f"TRX counter/result mismatch: {target.target_id}")
        if parsed["failed"] or parsed["aborted"] or parsed["outcome"] != "Completed":
            errors.append(f"Failed/aborted test target: {target.target_id}")

        for skipped in parsed["skipTests"]:
            if skipped["testId"] not in allowlisted:
                errors.append(f"Unexpected skipped test: {skipped['testId']}")
        results.append({
            "targetId": target.target_id,
            "projectPath": target.project_path,
            "targetFramework": target.target_framework,
            "passed": parsed["passed"],
            "failed": parsed["failed"],
            "skipped": parsed["skipped"],
            "total": parsed["total"],
            "discovered": parsed["discovered"],
            "executed": parsed["executed"],
            "runnerNotExecutedCounter": parsed["counters"]["notExecuted"],
            "skippedCountSource": "UnitTestResult outcome count; VSTest notExecuted counter is retained for comparison",
            "aborted": parsed["aborted"],
            "skippedTests": parsed["skipTests"],
            "artifact": {
                "path": relative_path(root, trx_path),
                "sha256": sha256_file(trx_path),
                "testAssembly": target.assembly_path,
                "testAssemblySha256": build_artifacts[target.target_id].get("sha256"),
            },
        })

    missing_results = sorted(discovered_ids - result_targets)
    if missing_results:
        errors.append(f"Missing test results: {', '.join(missing_results)}")
    if errors:
        raise GateFailure(errors)

    return {
        "formatVersion": 1,
        "scopeId": scope_id,
        "runId": run_id,
        "repositoryIdentity": repository_identity or {},
        "testedPayloadSha256": before_payload["sha256"],
        "testedPayloadFileCount": before_payload.get("fileCount"),
        "startedAt": started_at,
        "finishedAt": finished_at,
        "commands": commands,
        "expectedTargets": sorted(discovered_ids),
        "results": sorted(results, key=lambda item: item["targetId"]),
        "skipAllowlist": skip_allowlist,
        "outcome": "passed",
        "inventory": {
            "expected": sorted(expected_ids) if expected_inventory is not None else sorted(discovered_ids),
            "discovered": sorted(discovered_ids),
        },
        "buildArtifacts": build_artifacts,
        "payloadExclusions": list(DEFAULT_PAYLOAD_EXCLUSIONS),
    }


def capture_build_artifacts(root: Path, targets: list[TestTarget], configuration: str) -> dict[str, dict[str, Any]]:
    artifacts: dict[str, dict[str, Any]] = {}
    for target in targets:
        path = root / target.assembly_path
        if not path.is_file():
            raise GateFailure([f"Built test assembly is missing: {target.target_id}"])
        artifacts[target.target_id] = {
            **artifact_reference(root, path),
            "modifiedAt": datetime.fromtimestamp(path.stat().st_mtime, timezone.utc).isoformat().replace("+00:00", "Z"),
            "configuration": configuration,
            "targetFramework": target.target_framework,
        }
    return artifacts


def git_value(root: Path, *arguments: str) -> str:
    result = subprocess.run(["git", "-C", str(root), *arguments], capture_output=True, check=False)
    return result.stdout.decode("utf-8", "replace").strip() if result.returncode == 0 else ""


def run_gate(
    *,
    root: Path,
    scope_id: str,
    solution: Path,
    configuration: str,
    output: Path,
    evidence_path: Path,
    expected_inventory_path: Path,
    skip_allowlist_path: Path | None,
    dotnet: str,
) -> dict[str, Any]:
    root = root.resolve()
    solution = solution if solution.is_absolute() else root / solution
    solution = solution.resolve()
    output = output if output.is_absolute() else root / output
    evidence_path = evidence_path if evidence_path.is_absolute() else root / evidence_path
    expected_inventory_path = expected_inventory_path if expected_inventory_path.is_absolute() else root / expected_inventory_path
    if output.exists() and any(output.iterdir()):
        raise GateFailure([f"Gate output directory is not fresh: {relative_path(root, output)}"])
    output.mkdir(parents=True, exist_ok=True)
    expected_inventory = _load_expected_inventory(expected_inventory_path)
    skip_allowlist = []
    if skip_allowlist_path is not None:
        skip_allowlist_path = skip_allowlist_path if skip_allowlist_path.is_absolute() else root / skip_allowlist_path
        skip_allowlist = json.loads(skip_allowlist_path.read_text(encoding="utf-8-sig"))
    targets, discovery = discover_test_targets(root, solution, dotnet, configuration)
    (output / "discovery.stdout.log").write_text(json.dumps(discovery, indent=2), encoding="utf-8")

    before_payload = snapshot_payload(root)
    solution_arg = solution_argument(root, solution)
    commands: list[dict[str, Any]] = []
    commands.append(run_logged_command(
        root,
        dotnet,
        ["restore", solution_arg, "--nologo", "--verbosity", "quiet"],
        output / "restore.stdout.log",
        output / "restore.stderr.log",
    ))
    if commands[0]["exitCode"] != 0:
        raise GateFailure([f"Restore failed with exit code {commands[0]['exitCode']}"])
    commands.append(run_logged_command(
        root,
        dotnet,
        ["build", solution_arg, "-c", configuration, "--no-restore", "--no-incremental", "--nologo", "--verbosity", "quiet"],
        output / "build.stdout.log",
        output / "build.stderr.log",
    ))
    if commands[1]["exitCode"] != 0:
        raise GateFailure([f"Build failed with exit code {commands[1]['exitCode']}"])
    build_artifacts = capture_build_artifacts(root, targets, configuration)
    result_directory = output / "trx"
    result_directory.mkdir(parents=True, exist_ok=True)
    started_at = commands[0]["startedAt"]
    commands.append(run_logged_command(
        root,
        dotnet,
        [
            "test", solution_arg, "-c", configuration, "--no-build", "--no-restore", "--nologo",
            "--verbosity", "quiet", "--logger", "console;verbosity=minimal", "--logger", "trx",
            "--results-directory", relative_path(root, result_directory),
        ],
        output / "test.stdout.log",
        output / "test.stderr.log",
    ))
    after_payload = snapshot_payload(root)
    finished_at = commands[-1]["finishedAt"]
    evidence = validate_gate(
        root=root,
        scope_id=scope_id,
        run_id=f"{scope_id}-{uuid.uuid4().hex[:12]}",
        solution=solution,
        configuration=configuration,
        targets=targets,
        expected_inventory=expected_inventory,
        commands=commands,
        result_directory=result_directory,
        started_at=started_at,
        finished_at=finished_at,
        before_payload=before_payload,
        after_payload=after_payload,
        build_artifacts=build_artifacts,
        skip_allowlist=skip_allowlist,
        repository_identity={
            "root": str(root),
            "commit": git_value(root, "rev-parse", "HEAD"),
            "branch": git_value(root, "branch", "--show-current"),
            "configuration": configuration,
            "dotnet": dotnet,
            "sdkObserved": subprocess.run([dotnet, "--version"], capture_output=True, check=False).stdout.decode("utf-8", "replace").strip(),
        },
    )
    evidence["discovery"] = discovery
    evidence["discovery"]["stdoutArtifact"] = artifact_reference(root, output / "discovery.stdout.log")
    evidence["fullGateDefinition"] = {
        "solution": solution_arg,
        "configuration": configuration,
        "inventorySource": relative_path(root, expected_inventory_path),
        "resultDirectory": relative_path(root, result_directory),
    }
    evidence_path.parent.mkdir(parents=True, exist_ok=True)
    evidence_path.write_text(json.dumps(evidence, indent=2) + "\n", encoding="utf-8")
    return evidence


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=DEFAULT_ROOT)
    subparsers = parser.add_subparsers(dest="command", required=True)

    discover = subparsers.add_parser("discover", help="discover test projects and target frameworks")
    discover.add_argument("--solution", type=Path, required=True)
    discover.add_argument("--dotnet", default="dotnet")

    run = subparsers.add_parser("run", help="run and validate the full Release gate")
    run.add_argument("--scope-id", required=True)
    run.add_argument("--solution", type=Path, required=True)
    run.add_argument("--configuration", default="Release")
    run.add_argument("--output", type=Path, required=True)
    run.add_argument("--evidence", type=Path, required=True)
    run.add_argument("--expected-inventory", type=Path, required=True)
    run.add_argument("--skip-allowlist", type=Path)
    run.add_argument("--dotnet", default="dotnet")

    args = parser.parse_args(argv)
    try:
        root = args.root.resolve()
        if args.command == "discover":
            targets, inventory = discover_test_targets(root, args.solution, args.dotnet)
            print(json.dumps({"targets": [target.as_dict() for target in targets], "inventory": inventory}, indent=2))
        else:
            evidence = run_gate(
                root=root,
                scope_id=args.scope_id,
                solution=args.solution,
                configuration=args.configuration,
                output=args.output,
                evidence_path=args.evidence,
                expected_inventory_path=args.expected_inventory,
                skip_allowlist_path=args.skip_allowlist,
                dotnet=args.dotnet,
            )
            print(json.dumps({"scopeId": evidence["scopeId"], "outcome": evidence["outcome"], "targetCount": len(evidence["results"])}, indent=2))
        return 0
    except (GateFailure, OSError, ValueError, json.JSONDecodeError) as error:
        print(f"ERROR: {error}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())

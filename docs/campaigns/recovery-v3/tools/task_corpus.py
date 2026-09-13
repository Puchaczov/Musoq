#!/usr/bin/env python3
"""Validate task invariants and leakage-resistant recovery corpus splits.

The repair payload is intentionally not a gold-answer corpus.  It exposes the
task, malformed input, delivered diagnostic, fixture identity and a semantic
success invariant.  Private judge observations are supplied separately to
``evaluate_observation`` and are never loaded by ``load_payload``.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import re
from pathlib import Path
from typing import Any, Iterable


FAMILIES = {
    "lexical",
    "omissions",
    "names",
    "types",
    "grouping_windows",
    "enum",
    "table_couple",
    "binary_text",
}
PARTITIONS = {"development", "holdout_reserved"}
ALLOWED_OUTCOMES = {"executed", "clarification"}
HEX64 = re.compile(r"[0-9a-f]{64}\Z")
FORBIDDEN_PAYLOAD_KEYS = {
    "answer",
    "expectedRows",
    "expectedResult",
    "expectedQuery",
    "goldSql",
    "privateJudge",
    "repairedQuery",
}


class CorpusFailure(ValueError):
    """One or more corpus or judge contract checks failed."""

    def __init__(self, errors: Iterable[str]):
        self.errors = list(errors)
        super().__init__("; ".join(self.errors))


def canonical_bytes(value: Any) -> bytes:
    return json.dumps(
        value,
        ensure_ascii=False,
        sort_keys=True,
        separators=(",", ":"),
        allow_nan=False,
    ).encode("utf-8")


def digest(value: Any) -> str:
    return hashlib.sha256(canonical_bytes(value)).hexdigest()


def contained(root: Path, value: str) -> Path:
    if not isinstance(value, str) or not value.strip():
        raise CorpusFailure(["path must be a nonempty string"])
    candidate = (root / Path(value.replace("\\", "/"))).resolve()
    try:
        candidate.relative_to(root.resolve())
    except ValueError as error:
        raise CorpusFailure([f"path escapes corpus root: {value}"]) from error
    return candidate


def load_json(path: Path) -> Any:
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as error:
        raise CorpusFailure([f"cannot load JSON {path}: {error}"]) from error


def split_group(task: dict[str, Any]) -> str:
    return "|".join(
        (
            str(task["seedGroup"]),
            str(task["semanticFamily"]),
            str(task["providerFixtureId"]),
        )
    )


def partition_for_group(group: str, split_seed: str) -> str:
    value = hashlib.sha256(f"{split_seed}\0{group}".encode("utf-8")).digest()[0]
    return "holdout_reserved" if value % 4 == 0 else "development"


def _walk_forbidden(value: Any, path: str = "") -> list[str]:
    errors: list[str] = []
    if isinstance(value, dict):
        for key, child in value.items():
            child_path = f"{path}.{key}" if path else key
            if key in FORBIDDEN_PAYLOAD_KEYS:
                errors.append(f"{child_path} is private-answer data and is not allowed in the repair payload")
            errors.extend(_walk_forbidden(child, child_path))
    elif isinstance(value, list):
        for index, child in enumerate(value):
            errors.extend(_walk_forbidden(child, f"{path}[{index}]"))
    return errors


def _require_string(value: Any, field: str, errors: list[str]) -> None:
    if not isinstance(value, str) or not value.strip():
        errors.append(f"{field} must be a nonempty string")


def _validate_task(task: dict[str, Any], root: Path, split_seed: str) -> list[str]:
    errors: list[str] = []
    task_id = task.get("taskId", "<unknown>")
    prefix = f"{task_id}: "
    for field in (
        "taskId",
        "family",
        "semanticFamily",
        "seedGroup",
        "providerFixtureId",
        "partition",
        "taskPrompt",
        "malformedQuery",
    ):
        _require_string(task.get(field), prefix + field, errors)

    family = task.get("family")
    if family not in FAMILIES:
        errors.append(prefix + "family is outside the registered task families")
    partition = task.get("partition")
    if partition not in PARTITIONS:
        errors.append(prefix + "partition must be development or holdout_reserved")
    elif partition_for_group(split_group(task), split_seed) != partition:
        errors.append(prefix + "partition does not match the semantic split group")

    fixture = task.get("fixtureRef")
    if not isinstance(fixture, dict):
        errors.append(prefix + "fixtureRef must be an object")
    else:
        for field in ("id", "path", "testName", "proof"):
            _require_string(fixture.get(field), prefix + f"fixtureRef.{field}", errors)
        if isinstance(fixture.get("path"), str):
            try:
                fixture_path = contained(root, fixture["path"])
                if not fixture_path.is_file():
                    errors.append(prefix + "fixtureRef.path does not identify a file")
                elif isinstance(fixture.get("testName"), str):
                    try:
                        fixture_source = fixture_path.read_text(encoding="utf-8")
                    except (OSError, UnicodeError) as error:
                        errors.append(prefix + f"fixtureRef.path cannot be read: {error}")
                    else:
                        if fixture["testName"] not in fixture_source:
                            errors.append(prefix + "fixtureRef.testName is not present in fixtureRef.path")
            except CorpusFailure as error:
                errors.extend(prefix + item for item in error.errors)

    diagnostic = task.get("deliveredDiagnostic")
    if not isinstance(diagnostic, dict):
        errors.append(prefix + "deliveredDiagnostic must be an object")
    else:
        for field in ("code", "phase", "sourceKind"):
            _require_string(diagnostic.get(field), prefix + f"deliveredDiagnostic.{field}", errors)

    invariant = task.get("invariant")
    if not isinstance(invariant, dict):
        errors.append(prefix + "invariant must be an object")
    else:
        columns = invariant.get("columns")
        if not isinstance(columns, list) or not columns or not all(isinstance(column, str) and column.strip() for column in columns):
            errors.append(prefix + "invariant.columns must be a nonempty string array")
        minimum_rows = invariant.get("minimumRows")
        if type(minimum_rows) is not int or minimum_rows < 1:
            errors.append(prefix + "invariant.minimumRows must be a positive integer")
        if not isinstance(invariant.get("ordering"), str) or not invariant["ordering"].strip():
            errors.append(prefix + "invariant.ordering is required")
        if not isinstance(invariant.get("nullPolicy"), str) or not invariant["nullPolicy"].strip():
            errors.append(prefix + "invariant.nullPolicy is required")
        if not isinstance(invariant.get("failurePolicy"), str) or not invariant["failurePolicy"].strip():
            errors.append(prefix + "invariant.failurePolicy is required")
        clarification = invariant.get("clarification")
        if not isinstance(clarification, dict):
            errors.append(prefix + "invariant.clarification must be an object")
        elif not isinstance(clarification.get("acceptable"), bool):
            errors.append(prefix + "invariant.clarification.acceptable must be Boolean")

    allowed = task.get("allowedOutcomes")
    if not isinstance(allowed, list) or not allowed or not all(item in ALLOWED_OUTCOMES for item in allowed):
        errors.append(prefix + "allowedOutcomes must contain only executed or clarification")
    if task.get("mustExecute") is not True:
        errors.append(prefix + "mustExecute must be true; compile-only repair is not sufficient")
    if task.get("state") != "development_payload":
        errors.append(prefix + "state must be development_payload")
    return errors


def validate_payload(
    payload: dict[str, Any],
    root: Path,
    expected_scope: str = "REC-143",
    *,
    require_complete_families: bool = False,
) -> dict[str, Any]:
    errors: list[str] = []
    if not isinstance(payload, dict):
        raise CorpusFailure(["repair payload must be an object"])
    if payload.get("formatVersion") != 1:
        errors.append("formatVersion must be 1")
    if payload.get("scopeId") != expected_scope:
        errors.append(f"scopeId must be {expected_scope}")
    if payload.get("state") != "development_payload":
        errors.append("root state must be development_payload")
    errors.extend(_walk_forbidden(payload))

    split = payload.get("split")
    if not isinstance(split, dict):
        errors.append("split must be an object")
        split = {}
    split_seed = split.get("seed")
    _require_string(split_seed, "split.seed", errors)
    if split.get("algorithm") != "sha256-group-v1":
        errors.append("split.algorithm must be sha256-group-v1")
    if split.get("holdoutState") != "reserved_until_freeze":
        errors.append("split.holdoutState must be reserved_until_freeze")
    if split.get("finalHoldoutCreated") is not False:
        errors.append("split.finalHoldoutCreated must be false before freeze")

    reserved = split.get("reservedGroups")
    if not isinstance(reserved, list) or not reserved:
        errors.append("split.reservedGroups must contain explicit holdout_reserved groups")
        reserved = []

    tasks = payload.get("tasks")
    if not isinstance(tasks, list) or not tasks:
        errors.append("tasks must be a nonempty array")
        tasks = []
    task_ids: set[str] = set()
    groups: dict[str, str] = {}
    families: set[str] = set()
    for task in tasks:
        if not isinstance(task, dict):
            errors.append("every task must be an object")
            continue
        task_id = task.get("taskId")
        if task_id in task_ids:
            errors.append(f"duplicate taskId: {task_id}")
        task_ids.add(task_id)
        errors.extend(_validate_task(task, root, split_seed if isinstance(split_seed, str) else ""))
        family = task.get("family")
        if isinstance(family, str):
            families.add(family)
        if all(isinstance(task.get(field), str) for field in ("seedGroup", "semanticFamily", "providerFixtureId")):
            group = split_group(task)
            previous = groups.setdefault(group, task.get("partition"))
            if previous != task.get("partition"):
                errors.append(f"split leakage: group {group} crosses partitions")
            if task.get("partition") != "development":
                errors.append(f"repair payload task {task_id} must remain development data")

    if require_complete_families and families != FAMILIES:
        errors.append(f"task families must cover exactly {sorted(FAMILIES)}; got {sorted(families)}")
    reserved_groups: set[str] = set()
    for index, item in enumerate(reserved):
        if not isinstance(item, dict):
            errors.append(f"split.reservedGroups[{index}] must be an object")
            continue
        for field in ("seedGroup", "semanticFamily", "providerFixtureId"):
            _require_string(item.get(field), f"split.reservedGroups[{index}].{field}", errors)
        if item.get("partition") != "holdout_reserved":
            errors.append(f"split.reservedGroups[{index}].partition must be holdout_reserved")
        if all(isinstance(item.get(field), str) for field in ("seedGroup", "semanticFamily", "providerFixtureId")):
            group = "|".join(item[field] for field in ("seedGroup", "semanticFamily", "providerFixtureId"))
            reserved_groups.add(group)
            if group in groups:
                errors.append(f"reserved holdout group is present in repair payload: {group}")
            if isinstance(split_seed, str) and partition_for_group(group, split_seed) != "holdout_reserved":
                errors.append(f"reserved holdout group does not hash to holdout_reserved: {group}")

    if payload.get("taskCount") != len(tasks):
        errors.append("taskCount does not equal tasks length")
    if errors:
        raise CorpusFailure(errors)
    return {
        "scopeId": expected_scope,
        "taskCount": len(tasks),
        "familyCount": len(families),
        "developmentGroupCount": len(groups),
        "reservedHoldoutGroupCount": len(reserved_groups),
        "payloadSha256": digest(payload),
        "groups": sorted(groups),
        "reservedGroups": sorted(reserved_groups),
        "validated": True,
    }


def load_payload(path: Path, root: Path, expected_scope: str = "REC-143") -> tuple[dict[str, Any], dict[str, Any]]:
    payload = load_json(path)
    summary = validate_payload(payload, root, expected_scope, require_complete_families=True)
    return payload, summary


def build_private_judge(
    payload: dict[str, Any],
    expected: dict[str, dict[str, Any]],
    root: Path | None = None,
) -> dict[str, Any]:
    """Build judge-only observations without adding answers to the payload."""
    validation = validate_payload(
        payload,
        root or Path.cwd(),
        str(payload.get("scopeId", "REC-143")),
    )
    task_ids = {task["taskId"] for task in payload["tasks"]}
    if set(expected) != task_ids:
        raise CorpusFailure(["private judge task IDs must exactly match repair payload task IDs"])
    entries: list[dict[str, Any]] = []
    for task in payload["tasks"]:
        value = expected[task["taskId"]]
        if not isinstance(value, dict):
            raise CorpusFailure([f"judge entry must be an object: {task['taskId']}"])
        if value.get("outcome") not in ALLOWED_OUTCOMES:
            raise CorpusFailure([f"judge outcome is unsupported: {task['taskId']}"])
        if value.get("outcome") == "executed":
            rows = value.get("rowFingerprints")
            if not isinstance(rows, list) or not rows:
                raise CorpusFailure([f"judge execution needs nonempty rowFingerprints: {task['taskId']}"])
        entries.append({"taskId": task["taskId"], "partition": task["partition"], "expected": value})
    return {
        "formatVersion": 1,
        "scopeId": payload["scopeId"],
        "state": "private_judge",
        "sourcePayloadSha256": validation["payloadSha256"],
        "entries": entries,
    }


def evaluate_observation(
    task: dict[str, Any],
    observation: dict[str, Any],
    judge_entry: dict[str, Any] | None = None,
) -> dict[str, Any]:
    """Judge semantic task success, never a repaired SQL spelling."""
    if not isinstance(observation, dict):
        raise CorpusFailure([f"observation must be an object: {task.get('taskId', '<unknown>')}"])
    outcome = observation.get("outcome")
    if observation.get("compileOnly") is True or outcome in {"compile_only", "empty"}:
        raise CorpusFailure([f"compile-only or empty-result repair cannot pass: {task.get('taskId', '<unknown>')}"])
    allowed = set(task.get("allowedOutcomes", []))
    if outcome not in allowed:
        raise CorpusFailure([f"outcome {outcome!r} is not allowed for {task.get('taskId', '<unknown>')}"])
    if outcome == "clarification":
        requested = observation.get("requestedObservations")
        if not isinstance(requested, list) or not requested or not all(isinstance(item, str) and item.strip() for item in requested):
            raise CorpusFailure([f"clarification must request bounded observations: {task['taskId']}"])
        if not task["invariant"]["clarification"].get("acceptable"):
            raise CorpusFailure([f"clarification is not acceptable for {task['taskId']}"])
        return {"passed": True, "outcome": "clarification", "taskId": task["taskId"]}

    invariant = task["invariant"]
    if observation.get("columns") != invariant["columns"]:
        raise CorpusFailure([f"result shape mismatch: {task['taskId']}"])
    if type(observation.get("rowCount")) is not int or observation["rowCount"] < invariant["minimumRows"]:
        raise CorpusFailure([f"result multiplicity is below invariant: {task['taskId']}"])
    if observation.get("ordering") != invariant["ordering"]:
        raise CorpusFailure([f"result ordering mismatch: {task['taskId']}"])
    if judge_entry is not None:
        expected = judge_entry.get("expected", {})
        if expected.get("outcome") != "executed":
            raise CorpusFailure([f"judge expects a non-execution outcome: {task['taskId']}"])
        if observation.get("rowFingerprints") != expected.get("rowFingerprints"):
            raise CorpusFailure([f"private judge row observation mismatch: {task['taskId']}"])
    return {"passed": True, "outcome": "executed", "taskId": task["taskId"]}


def materialize_holdout(
    payload: dict[str, Any],
    compared_builds: list[str],
    repair_prompt_digest: str,
    root: Path | None = None,
) -> dict[str, Any]:
    validation = validate_payload(
        payload,
        root or Path.cwd(),
        str(payload.get("scopeId", "REC-143")),
    )
    if not isinstance(compared_builds, list) or not compared_builds or not all(isinstance(item, str) and item.strip() for item in compared_builds):
        raise CorpusFailure(["final holdout requires at least one compared build identity"])
    if not HEX64.fullmatch(repair_prompt_digest):
        raise CorpusFailure(["final holdout requires a lowercase SHA-256 repair prompt digest"])
    return {
        "formatVersion": 1,
        "scopeId": payload["scopeId"],
        "state": "holdout_materialized_after_freeze",
        "sourcePayloadSha256": validation["payloadSha256"],
        "comparedBuilds": compared_builds,
        "repairPromptSha256": repair_prompt_digest,
        "groups": validation["reservedGroups"],
        "answersExposedBeforeFreeze": False,
    }


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Validate REC-143 task invariants and split boundaries.")
    subparsers = parser.add_subparsers(dest="command", required=True)
    validate = subparsers.add_parser("validate")
    validate.add_argument("--root", default=".", type=Path)
    validate.add_argument("--payload", required=True, type=Path)
    holdout = subparsers.add_parser("materialize-holdout")
    holdout.add_argument("--root", default=".", type=Path)
    holdout.add_argument("--payload", required=True, type=Path)
    holdout.add_argument("--build", action="append", required=True)
    holdout.add_argument("--repair-prompt-sha256", required=True)
    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    try:
        payload, summary = load_payload(args.payload, args.root.resolve())
        if args.command == "materialize-holdout":
            result = materialize_holdout(payload, args.build, args.repair_prompt_sha256, args.root.resolve())
        else:
            result = summary
        print(json.dumps(result, ensure_ascii=False, indent=2))
        return 0
    except CorpusFailure as error:
        print("ERROR: " + str(error))
        return 2


if __name__ == "__main__":
    raise SystemExit(main())

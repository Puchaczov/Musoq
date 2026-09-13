#!/usr/bin/env python3
"""Freeze and verify pre-observation diagnostic case expectations.

The case file is an oracle declaration, not an execution result.  This helper
keeps that boundary explicit: a valid seed and its exact mutation are checked
before any candidate observation, raw authority files are hashed, and a frozen
manifest makes post-hoc expectation replacement observable.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from pathlib import Path
from typing import Any, Iterable


ALLOWED_CLASSIFICATIONS = {
    "invalid",
    "valid_suspicious",
    "valid_ordinary",
    "unspecified",
}
ALLOWED_REPAIRS = {
    "unique_safe_edit",
    "bounded_alternatives",
    "request_observation",
    "no_change",
    "no_safe_fix",
}
OBSERVATION_KEYS = {
    "observed",
    "observedAt",
    "observedClassification",
    "observedDiagnostics",
    "actualDiagnostics",
    "executionResult",
}
HEX64 = re.compile(r"[0-9a-f]{64}\Z")


class ContractFailure(ValueError):
    """One or more pre-observation contract checks failed."""

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


def raw_digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def contained(root: Path, value: str) -> Path:
    if not isinstance(value, str) or not value.strip():
        raise ValueError("path must be a nonempty string")
    candidate = (root / Path(value.replace("\\", "/"))).resolve()
    candidate.relative_to(root.resolve())
    return candidate


def load_json(path: Path) -> Any:
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as error:
        raise ContractFailure([f"Cannot load JSON {path}: {error}"]) from error


def load_cases(path: Path) -> list[dict[str, Any]]:
    document = load_json(path)
    cases = document.get("cases") if isinstance(document, dict) else document
    if not isinstance(cases, list) or not cases:
        raise ContractFailure(["Case document must contain a nonempty cases array"])
    if not all(isinstance(case, dict) for case in cases):
        raise ContractFailure(["Every case must be a JSON object"])
    return cases


def _require_string(value: Any, field: str, errors: list[str]) -> None:
    if not isinstance(value, str) or not value.strip():
        errors.append(f"{field} must be a nonempty string")


def mutated_query(case: dict[str, Any]) -> str:
    mutation = case["mutation"]
    seed = case["seedQuery"]
    before = mutation["before"]
    after = mutation["after"]
    if mutation["kind"] != "replace":
        raise ContractFailure([f"{case.get('caseId', '<unknown>')}: unsupported mutation kind"])
    if seed.count(before) != 1:
        raise ContractFailure(
            [f"{case.get('caseId', '<unknown>')}: mutation before text must occur exactly once in seedQuery"]
        )
    result = seed.replace(before, after, 1)
    if result != mutation.get("resultQuery"):
        raise ContractFailure([f"{case.get('caseId', '<unknown>')}: resultQuery is not the exact declared mutation"])
    return result


def fingerprint(case: dict[str, Any]) -> str:
    value = case["fingerprint"]
    return digest(
        {
            "domain": value["domain"],
            "rootCause": value["rootCause"],
            "context": value["context"],
        }
    )


def validate_minimization(case: dict[str, Any]) -> None:
    minimization = case.get("minimization")
    if not isinstance(minimization, dict):
        raise ContractFailure([f"{case.get('caseId', '<unknown>')}: minimization record is required"])

    status = minimization.get("status")
    if status == "pending_observation":
        if minimization.get("originalQuery") != case["mutation"].get("resultQuery"):
            raise ContractFailure([f"{case.get('caseId', '<unknown>')}: pending minimization must retain the original mutated query"])
        if minimization.get("minimizedQuery") is not None:
            raise ContractFailure([f"{case.get('caseId', '<unknown>')}: minimized query cannot be filled before observation"])
        if minimization.get("sameFault") is not None or minimization.get("sameObservation") is not None:
            raise ContractFailure([f"{case.get('caseId', '<unknown>')}: observation claims are not allowed before execution"])
        criteria = minimization.get("preservationCriteria")
        if not isinstance(criteria, list) or not criteria or not all(isinstance(item, str) and item.strip() for item in criteria):
            raise ContractFailure([f"{case.get('caseId', '<unknown>')}: minimization preservation criteria are required"])
        return

    if status == "completed":
        for field in ("originalQuery", "minimizedQuery"):
            if not isinstance(minimization.get(field), str) or not minimization[field].strip():
                raise ContractFailure([f"{case.get('caseId', '<unknown>')}: completed minimization requires {field}"])
        if minimization.get("sameFault") is not True or minimization.get("sameObservation") is not True:
            raise ContractFailure([f"{case.get('caseId', '<unknown>')}: completed minimization must prove the same fault and observation"])
        if not isinstance(minimization.get("evidence"), str) or not minimization["evidence"].strip():
            raise ContractFailure([f"{case.get('caseId', '<unknown>')}: completed minimization requires evidence"])
        return

    raise ContractFailure([f"{case.get('caseId', '<unknown>')}: minimization status must be pending_observation or completed"])


def validate_case(case: dict[str, Any], root: Path, expected_scope: str | None = None) -> list[dict[str, str]]:
    errors: list[str] = []
    case_id = case.get("caseId", "<unknown>")
    prefix = f"{case_id}: "
    required = (
        "formatVersion",
        "caseId",
        "scopeId",
        "task",
        "seedQuery",
        "fixtureRef",
        "mutation",
        "expectedClassification",
        "expectedRootCause",
        "expectedDiagnostics",
        "authority",
        "repairPolicy",
        "seedValidity",
        "preObservation",
        "fingerprint",
        "minimization",
    )
    for field in required:
        if field not in case:
            errors.append(prefix + f"missing {field}")

    if case.get("formatVersion") != 1:
        errors.append(prefix + "formatVersion must be 1")
    _require_string(case.get("caseId"), prefix + "caseId", errors)
    scope_id = case.get("scopeId")
    if not isinstance(scope_id, str) or not re.fullmatch(r"REC-[0-9]{3}", scope_id):
        errors.append(prefix + "scopeId must match REC-###")
    if expected_scope and scope_id != expected_scope:
        errors.append(prefix + f"scopeId does not match {expected_scope}")
    for field in ("task", "seedQuery", "expectedRootCause"):
        _require_string(case.get(field), prefix + field, errors)

    classification = case.get("expectedClassification")
    if classification not in ALLOWED_CLASSIFICATIONS:
        errors.append(prefix + "expectedClassification is outside the frozen outcome contract")
    if case.get("repairPolicy") not in ALLOWED_REPAIRS:
        errors.append(prefix + "repairPolicy is outside the frozen repair contract")
    if case.get("seedValidity") != "valid":
        errors.append(prefix + "seedValidity must be valid before a mutation is observed")

    fixture = case.get("fixtureRef")
    if not isinstance(fixture, dict):
        errors.append(prefix + "fixtureRef must be an object")
    else:
        for field in ("id", "path", "testName", "proof"):
            _require_string(fixture.get(field), prefix + f"fixtureRef.{field}", errors)
        if isinstance(fixture.get("path"), str):
            try:
                if not contained(root, fixture["path"]).is_file():
                    errors.append(prefix + "fixtureRef.path does not identify a file")
            except ValueError as error:
                errors.append(prefix + f"fixtureRef.path is unsafe: {error}")

    mutation = case.get("mutation")
    if not isinstance(mutation, dict):
        errors.append(prefix + "mutation must be an object")
    else:
        for field in ("kind", "description", "before", "after", "resultQuery"):
            _require_string(mutation.get(field), prefix + f"mutation.{field}", errors)
        if mutation.get("kind") != "replace":
            errors.append(prefix + "mutation.kind must be replace")

    diagnostics = case.get("expectedDiagnostics")
    if not isinstance(diagnostics, list):
        errors.append(prefix + "expectedDiagnostics must be an array")
    else:
        for index, item in enumerate(diagnostics):
            if not isinstance(item, dict):
                errors.append(prefix + f"expectedDiagnostics[{index}] must be an object")
                continue
            for field in ("code", "phase", "sourceDomain", "severity"):
                _require_string(item.get(field), prefix + f"expectedDiagnostics[{index}].{field}", errors)
            if isinstance(item.get("code"), str) and not re.match(r"^MQ[0-9]{4}_", item["code"]):
                errors.append(prefix + f"expectedDiagnostics[{index}].code must be an MQ code")

    authority = case.get("authority")
    authority_digests: list[dict[str, str]] = []
    if not isinstance(authority, list) or not authority:
        errors.append(prefix + "authority must be a nonempty array")
    else:
        for index, citation in enumerate(authority):
            label = prefix + f"authority[{index}]"
            if not isinstance(citation, dict):
                errors.append(label + " must be an object")
                continue
            for field in ("sourcePath", "section", "passage", "sourceSha256"):
                _require_string(citation.get(field), label + f".{field}", errors)
            source_path = citation.get("sourcePath")
            source_hash = citation.get("sourceSha256")
            if isinstance(source_hash, str) and not HEX64.fullmatch(source_hash):
                errors.append(label + ".sourceSha256 must be lowercase SHA-256")
            if isinstance(source_path, str) and isinstance(source_hash, str) and HEX64.fullmatch(source_hash):
                try:
                    source_file = contained(root, source_path)
                    if not source_file.is_file():
                        errors.append(label + " sourcePath does not identify a file")
                    elif raw_digest(source_file) != source_hash:
                        errors.append(label + " sourceSha256 does not match the raw source file")
                    authority_digests.append({"path": source_path.replace("\\", "/"), "sha256": source_hash})
                except (OSError, ValueError) as error:
                    errors.append(label + f" unsafe/unreadable sourcePath: {error}")

    pre_observation = case.get("preObservation")
    if not isinstance(pre_observation, dict):
        errors.append(prefix + "preObservation must be an object")
    else:
        if pre_observation.get("state") != "unobserved":
            errors.append(prefix + "preObservation.state must remain unobserved")
        if pre_observation.get("registeredBeforeCandidate") is not True:
            errors.append(prefix + "preObservation must assert registeredBeforeCandidate=true")
        _require_string(pre_observation.get("uncertainty"), prefix + "preObservation.uncertainty", errors)

    for key in OBSERVATION_KEYS:
        if key in case:
            errors.append(prefix + f"post-observation field {key} is not permitted in a frozen case")

    fingerprint_value = case.get("fingerprint")
    if not isinstance(fingerprint_value, dict):
        errors.append(prefix + "fingerprint must be an object")
    else:
        for field in ("domain", "rootCause", "context"):
            _require_string(fingerprint_value.get(field), prefix + f"fingerprint.{field}", errors)

    if not errors and mutation is not None:
        try:
            mutated_query(case)
        except ContractFailure as error:
            errors.extend(prefix + item for item in error.errors)

    if not errors:
        try:
            validate_minimization(case)
        except ContractFailure as error:
            errors.extend(error.errors)
    if errors:
        raise ContractFailure(errors)
    return authority_digests


def validate_cases(cases: list[dict[str, Any]], root: Path, expected_scope: str | None = None) -> dict[str, Any]:
    errors: list[str] = []
    case_ids: set[str] = set()
    fingerprints: dict[str, str] = {}
    records: list[dict[str, Any]] = []
    source_files: dict[str, str] = {}

    for case in cases:
        case_id = case.get("caseId", "<unknown>")
        if case_id in case_ids:
            errors.append(f"{case_id}: duplicate caseId")
        case_ids.add(case_id)
        authority_digests = validate_case(case, root, expected_scope)
        for source in authority_digests:
            previous = source_files.setdefault(source["path"], source["sha256"])
            if previous != source["sha256"]:
                errors.append(f"{case_id}: source has conflicting raw hashes: {source['path']}")
        if not any(case_id == record.get("caseId") for record in records):
            try:
                case_fingerprint = fingerprint(case)
            except (KeyError, TypeError):
                case_fingerprint = ""
            if case_fingerprint:
                previous_case = fingerprints.setdefault(case_fingerprint, case_id)
                if previous_case != case_id:
                    errors.append(
                        f"{case_id}: duplicate root-cause/context fingerprint with {previous_case}; identifier spelling is not novel coverage"
                    )
            records.append(
                {
                    "caseId": case_id,
                    "preObservationDigest": digest(case),
                    "rootCauseContextFingerprint": case_fingerprint,
                    "authorityDigests": sorted(authority_digests, key=lambda item: (item["path"], item["sha256"])),
                    "expectedClassification": case.get("expectedClassification"),
                }
            )

    if errors:
        raise ContractFailure(errors)
    source_records = [
        {"path": path, "sha256": source_hash, "byteLength": len(contained(root, path).read_bytes())}
        for path, source_hash in sorted(source_files.items())
    ]
    return {
        "records": records,
        "sourceFiles": source_records,
        "preObservationPayloadSha256": digest(cases),
    }


def make_manifest(cases: list[dict[str, Any]], root: Path, scope_id: str) -> dict[str, Any]:
    validation = validate_cases(cases, root, scope_id)
    return {
        "formatVersion": 1,
        "scopeId": scope_id,
        "state": "pre_observation",
        "caseCount": len(cases),
        "sourceFiles": validation["sourceFiles"],
        "preObservationPayloadSha256": validation["preObservationPayloadSha256"],
        "cases": validation["records"],
        "rules": {
            "expectationsFrozenBeforeObservation": True,
            "identifierSpellingAloneIsNotNovel": True,
            "sourceHashesAreRawBytes": True,
            "minimizationRequiresSameFaultAndObservation": True,
        },
    }


def freeze(cases_path: Path, manifest_path: Path, root: Path, scope_id: str) -> dict[str, Any]:
    if manifest_path.exists():
        raise ContractFailure([f"Refusing to replace existing frozen manifest: {manifest_path}"])
    manifest = make_manifest(load_cases(cases_path), root, scope_id)
    manifest_path.parent.mkdir(parents=True, exist_ok=True)
    manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    return manifest


def verify(cases_path: Path, manifest_path: Path, root: Path, scope_id: str) -> dict[str, Any]:
    manifest = load_json(manifest_path)
    if not isinstance(manifest, dict):
        raise ContractFailure(["Frozen manifest must be an object"])
    if manifest.get("scopeId") != scope_id:
        raise ContractFailure(["Frozen manifest scopeId does not match requested scope"])
    if manifest.get("state") != "pre_observation":
        raise ContractFailure(["Frozen manifest must remain pre_observation"])
    expected = make_manifest(load_cases(cases_path), root, scope_id)
    if manifest.get("caseCount") != expected["caseCount"]:
        raise ContractFailure(["Frozen case count differs from current cases"])
    if manifest.get("sourceFiles") != expected["sourceFiles"]:
        raise ContractFailure(["Frozen raw authority source hashes differ from current files"])
    if manifest.get("preObservationPayloadSha256") != expected["preObservationPayloadSha256"]:
        raise ContractFailure(["Pre-observation payload digest mismatch; expectation replacement is not permitted"])
    if manifest.get("cases") != expected["cases"]:
        raise ContractFailure(["Frozen case record mismatch; expectation, mutation, classification or fingerprint changed after freeze"])
    return {
        "scopeId": scope_id,
        "state": manifest["state"],
        "caseCount": manifest["caseCount"],
        "sourceFileCount": len(manifest.get("sourceFiles", [])),
        "preObservationPayloadSha256": manifest["preObservationPayloadSha256"],
        "verified": True,
    }


def _path_argument(value: str) -> Path:
    return Path(value)


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Freeze and verify diagnostic pre-observation case expectations.")
    subparsers = parser.add_subparsers(dest="command", required=True)
    for name in ("freeze", "verify"):
        command = subparsers.add_parser(name)
        command.add_argument("--root", default=".", type=_path_argument)
        command.add_argument("--scope-id", required=True)
        command.add_argument("--cases", required=True, type=_path_argument)
        command.add_argument("--manifest", required=True, type=_path_argument)
    validate = subparsers.add_parser("validate")
    validate.add_argument("--root", default=".", type=_path_argument)
    validate.add_argument("--scope-id", required=True)
    validate.add_argument("--cases", required=True, type=_path_argument)
    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    try:
        if args.command == "freeze":
            result = freeze(args.cases, args.manifest, args.root.resolve(), args.scope_id)
        elif args.command == "verify":
            result = verify(args.cases, args.manifest, args.root.resolve(), args.scope_id)
        else:
            cases = load_cases(args.cases)
            validation = validate_cases(cases, args.root.resolve(), args.scope_id)
            result = {
                "scopeId": args.scope_id,
                "caseCount": len(cases),
                "sourceFileCount": len(validation["sourceFiles"]),
                "preObservationPayloadSha256": validation["preObservationPayloadSha256"],
                "validated": True,
            }
        print(json.dumps(result, ensure_ascii=False, indent=2))
        return 0
    except ContractFailure as error:
        print("ERROR: " + str(error), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())

#!/usr/bin/env python3
"""Audit diagnostic catalog meaning without invoking the descriptor registry.

The active code set is derived from the current DiagnosticCode enum and the
JSON catalog.  The semantic expectations in the oracle are authored from the
language specification, so changing both a descriptor factory and its catalog
copy cannot make a root-cause assertion pass accidentally.

Python 3.10+; standard library only.
"""
from __future__ import annotations

import argparse
import copy
import hashlib
import json
import re
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


class CatalogAuditFailure(ValueError):
    """Raised when an independent catalog assertion fails."""

    def __init__(self, errors: list[str]):
        self.errors = errors
        super().__init__("; ".join(errors))


def _unique_object(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
    result: dict[str, Any] = {}
    for key, value in pairs:
        if key in result:
            raise ValueError(f"Duplicate JSON key: {key}")
        result[key] = value
    return result


def load_json(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8-sig"), object_pairs_hook=_unique_object)


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def parse_enum_codes(source: str) -> dict[str, int]:
    """Extract enum declarations, retaining the source-defined active set."""
    matches = re.findall(r"\b(MQ\d{4}_[A-Za-z0-9_]+)\s*=\s*(\d+)\b", source)
    if not matches:
        raise ValueError("DiagnosticCode.cs contained no diagnostic enum declarations")

    codes: dict[str, int] = {}
    numbers: dict[int, str] = {}
    errors: list[str] = []
    for name, raw_number in matches:
        number = int(raw_number)
        if name in codes:
            errors.append(f"duplicate enum member {name}")
        if number in numbers:
            errors.append(f"duplicate enum number {number}: {numbers[number]} and {name}")
        codes[name] = number
        numbers[number] = name
    if errors:
        raise CatalogAuditFailure(errors)
    return codes


def catalog_entries(document: Any) -> list[dict[str, Any]]:
    if not isinstance(document, dict) or not isinstance(document.get("diagnostics"), list):
        raise ValueError("diagnostic-catalog.json must contain a diagnostics array")
    entries = document["diagnostics"]
    if not all(isinstance(entry, dict) for entry in entries):
        raise ValueError("Every diagnostic catalog entry must be an object")
    return entries


def _index_entries(entries: list[dict[str, Any]]) -> tuple[dict[str, dict[str, Any]], list[str]]:
    by_code: dict[str, dict[str, Any]] = {}
    by_number: dict[int, str] = {}
    errors: list[str] = []
    for entry in entries:
        code = entry.get("code")
        number = entry.get("number")
        if not isinstance(code, str) or not isinstance(number, int):
            errors.append(f"catalog entry has invalid code/number: {entry!r}")
            continue
        if code in by_code:
            errors.append(f"duplicate catalog code {code}")
        if number in by_number:
            errors.append(f"duplicate catalog number {number}: {by_number[number]} and {code}")
        by_code[code] = entry
        by_number[number] = code
    return by_code, errors


def _lower(value: Any) -> str:
    return value.casefold() if isinstance(value, str) else ""


def _normalized_text(value: str) -> str:
    return " ".join(value.casefold().split())


def _require_phrases(errors: list[str], text: str, phrases: list[str], label: str) -> None:
    lowered = _normalized_text(text)
    for phrase in phrases:
        if _normalized_text(phrase) not in lowered:
            errors.append(f"{label} is missing authority phrase: {phrase}")


def _require_terms(errors: list[str], text: str, terms: list[str], label: str) -> None:
    lowered = text.casefold()
    for term in terms:
        if term.casefold() not in lowered:
            errors.append(f"{label} is missing independent meaning term: {term}")


def _audit_drift(
    spec_text: str,
    entries_by_code: dict[str, dict[str, Any]],
    oracle: dict[str, Any],
    errors: list[str],
) -> list[dict[str, Any]]:
    records: list[dict[str, Any]] = []
    for check in oracle.get("driftChecks", []):
        finding_id = check.get("findingId", "unidentified-drift")
        spec_phrases = check.get("specAllPhrases", [])
        missing_spec = [
            phrase for phrase in spec_phrases
            if _normalized_text(phrase) not in _normalized_text(spec_text)
        ]
        if missing_spec:
            errors.extend(
                f"{finding_id} authority passage is missing: {phrase}" for phrase in missing_spec
            )

        catalog_code = check.get("catalogCode")
        catalog_entry = entries_by_code.get(catalog_code) if catalog_code else None
        catalog_text = " ".join(
            str(catalog_entry.get(field, ""))
            for field in ("messageTemplate", "explanation", "suggestedFixes")
        ) if catalog_entry else ""
        catalog_terms = check.get("catalogAllTerms", [])
        conflict_observed = bool(catalog_entry) and all(
            term.casefold() in catalog_text.casefold() for term in catalog_terms
        )
        spec_conflict_phrases = check.get("specConflictAllPhrases", [])
        spec_conflict_observed = bool(spec_conflict_phrases) and all(
            _normalized_text(phrase) in _normalized_text(spec_text)
            for phrase in spec_conflict_phrases
        )

        compatibility: list[dict[str, Any]] = []
        for code in check.get("compatibilityCodes", []):
            entry = entries_by_code.get(code)
            compatibility.append(
                {
                    "code": code,
                    "present": entry is not None,
                    "explanation": entry.get("explanation") if entry else None,
                }
            )

        related: list[dict[str, Any]] = []
        for code in check.get("relatedCatalogCodes", []):
            entry = entries_by_code.get(code)
            related.append(
                {
                    "code": code,
                    "present": entry is not None,
                    "explanation": entry.get("explanation") if entry else None,
                }
            )

        records.append(
            {
                "findingId": finding_id,
                "state": "confirmed-current-drift"
                if conflict_observed or spec_conflict_observed
                else "resolved-or-changed",
                "disposition": "retain-explicit-finding",
                "authorityPhrasesFound": len(spec_phrases) - len(missing_spec),
                "authorityPhraseCount": len(spec_phrases),
                "catalogConflictObserved": conflict_observed,
                "specConflictObserved": spec_conflict_observed,
                "catalogCode": catalog_code,
                "relatedCatalogEntries": related,
                "compatibilityEntries": compatibility,
            }
        )
    return records


def collect_validation_errors(
    enum_source: str,
    catalog_document: Any,
    spec_text: str,
    oracle: dict[str, Any],
) -> tuple[list[str], dict[str, Any]]:
    errors: list[str] = []
    enum_codes = parse_enum_codes(enum_source)
    entries = catalog_entries(catalog_document)
    entries_by_code, catalog_errors = _index_entries(entries)
    errors.extend(catalog_errors)

    enum_pairs = set(enum_codes.items())
    catalog_pairs = {
        (entry.get("code"), entry.get("number"))
        for entry in entries
        if isinstance(entry.get("code"), str) and isinstance(entry.get("number"), int)
    }
    if enum_pairs != catalog_pairs:
        errors.append(
            "active enum/catalog identity mismatch: "
            f"enum-only={sorted(enum_pairs - catalog_pairs)!r}, "
            f"catalog-only={sorted(catalog_pairs - enum_pairs)!r}"
        )

    active_numbers = set(enum_codes.values())
    retired_inventory: list[dict[str, Any]] = []
    for retired in oracle.get("retiredNumericCodes", []):
        number = retired.get("number")
        if not isinstance(number, int):
            errors.append(f"retired inventory has invalid number: {retired!r}")
            continue
        enum_names = [name for name, value in enum_codes.items() if value == number]
        catalog_names = [
            entry.get("code") for entry in entries if entry.get("number") == number
        ]
        if enum_names:
            errors.append(f"retired numeric code {number} was repurposed in enum: {enum_names}")
        if catalog_names:
            errors.append(f"retired numeric code {number} remains active in catalog: {catalog_names}")
        retired_inventory.append(
            {
                "number": number,
                "replacement": retired.get("replacement"),
                "enumPresent": bool(enum_names),
                "catalogPresent": bool(catalog_names),
            }
        )

    compatibility_inventory: list[dict[str, Any]] = []
    for compatibility in oracle.get("compatibilityOnly", []):
        code = compatibility.get("code")
        number = compatibility.get("number")
        enum_present = enum_codes.get(code) == number
        catalog_entry = entries_by_code.get(code)
        catalog_present = catalog_entry is not None and catalog_entry.get("number") == number
        presence = compatibility.get("presence")
        if presence == "documentation-only" and (enum_present or catalog_present):
            errors.append(f"documentation-only compatibility code is active: {code}")
        if presence == "catalog-only" and not (enum_present and catalog_present):
            errors.append(f"catalog-only compatibility code is not synchronized: {code}")
        if presence not in ("documentation-only", "catalog-only"):
            errors.append(f"unknown compatibility presence for {code}: {presence}")
        if compatibility.get("specAllPhrases"):
            _require_phrases(errors, spec_text, compatibility["specAllPhrases"], code)
        if catalog_entry:
            catalog_text = " ".join(
                str(catalog_entry.get(field, ""))
                for field in ("messageTemplate", "explanation", "suggestedFixes")
            )
            _require_terms(
                errors,
                catalog_text,
                compatibility.get("catalogAllTerms", []),
                code,
            )
        compatibility_inventory.append(
            {
                "code": code,
                "number": number,
                "presence": presence,
                "enumPresent": enum_present,
                "catalogPresent": catalog_present,
                "explainableWithoutEmission": presence == "documentation-only"
                or bool(catalog_entry),
            }
        )

    high_risk_results: list[dict[str, Any]] = []
    for fact in oracle.get("highRiskRootCauses", []):
        code = fact.get("code")
        entry = entries_by_code.get(code)
        if entry is None:
            errors.append(f"high-risk catalog entry is missing: {code}")
            high_risk_results.append({"code": code, "passed": False})
            continue
        for field, expected in fact.get("requiredFields", {}).items():
            if entry.get(field) != expected:
                errors.append(
                    f"{code}.{field} differs from independent expectation: "
                    f"expected {expected!r}, observed {entry.get(field)!r}"
                )
        meaning_text = " ".join(
            str(entry.get(field, ""))
            for field in ("messageTemplate", "explanation", "suggestedFixes")
        )
        _require_terms(errors, meaning_text, fact.get("requiredExplanationTerms", []), code)
        _require_phrases(errors, spec_text, fact.get("requiredSpecPhrases", []), code)
        high_risk_results.append(
            {
                "code": code,
                "number": entry.get("number"),
                "phase": entry.get("phase"),
                "severity": entry.get("severity"),
                "explanation": entry.get("explanation"),
                "suggestedFixes": entry.get("suggestedFixes", []),
                "passed": True,
            }
        )

    drift_records = _audit_drift(spec_text, entries_by_code, oracle, errors)
    report = {
        "activeInventory": {
            "policy": "derived from current DiagnosticCode.cs and diagnostic-catalog.json; no fixed count",
            "enumCount": len(enum_codes),
            "catalogCount": len(entries),
            "codes": [
                {"code": code, "number": number}
                for code, number in sorted(enum_codes.items(), key=lambda item: (item[1], item[0]))
            ],
        },
        "retiredInventory": retired_inventory,
        "compatibilityInventory": compatibility_inventory,
        "highRiskRootCauses": high_risk_results,
        "driftRecords": drift_records,
    }
    return errors, report


def validate_catalog(
    enum_source: str,
    catalog_document: Any,
    spec_text: str,
    oracle: dict[str, Any],
) -> dict[str, Any]:
    errors, report = collect_validation_errors(enum_source, catalog_document, spec_text, oracle)
    if errors:
        raise CatalogAuditFailure(errors)
    return report


def run_audit(root: Path, oracle_path: Path | None = None) -> dict[str, Any]:
    root = root.resolve()
    enum_path = root / "src" / "dotnet" / "Musoq.Parser" / "Diagnostics" / "DiagnosticCode.cs"
    catalog_path = root / "specs" / "diagnostic-catalog.json"
    spec_path = root / "docs" / "campaigns" / "recovery-v3" / "authority" / "musoq-core-language-spec.md"
    if oracle_path is None:
        oracle_path = root / "docs" / "campaigns" / "recovery-v3" / "tools" / "rec089-independent-oracle.json"
    oracle_path = oracle_path.resolve()

    enum_source = enum_path.read_text(encoding="utf-8-sig")
    catalog_document = load_json(catalog_path)
    spec_text = spec_path.read_text(encoding="utf-8-sig")
    oracle = load_json(oracle_path)
    baseline = validate_catalog(enum_source, catalog_document, spec_text, oracle)

    control_document = copy.deepcopy(catalog_document)
    control_entries = catalog_entries(control_document)
    control_target = next(
        entry for entry in control_entries if entry.get("code") == "MQ3086_UnknownCallable"
    )
    control_target["explanation"] = "The schema is available and the requested source is valid."
    control_errors, _ = collect_validation_errors(enum_source, control_document, spec_text, oracle)
    if not control_errors:
        raise CatalogAuditFailure(
            ["negative control did not reject an intentionally wrong MQ3086 explanation"]
        )

    baseline["scopeId"] = "REC-089"
    baseline["outcome"] = "passed"
    baseline["independence"] = {
        "inputs": [
            "DiagnosticCode.cs enum declarations",
            "specs/diagnostic-catalog.json",
            "recovery-v3 authority language specification",
            "REC-089 independent oracle",
        ],
        "descriptorRegistryImported": False,
        "activeSetFrozen": False,
        "baselineEnumCatalogIdentityAgrees": True,
    }
    baseline["negativeControl"] = {
        "status": "killed",
        "mutatedCode": "MQ3086_UnknownCallable",
        "mutation": "replace explanation with a source-resolution claim",
        "observedIndependentFailures": control_errors,
    }
    baseline["sources"] = {
        "enum": {"path": "src/dotnet/Musoq.Parser/Diagnostics/DiagnosticCode.cs", "sha256": sha256(enum_path)},
        "catalog": {"path": "specs/diagnostic-catalog.json", "sha256": sha256(catalog_path)},
        "authority": {
            "path": "docs/campaigns/recovery-v3/authority/musoq-core-language-spec.md",
            "sha256": sha256(spec_path),
        },
        "oracle": {
            "path": "docs/campaigns/recovery-v3/tools/rec089-independent-oracle.json",
            "sha256": sha256(oracle_path),
        },
    }
    return baseline


def _default_root() -> Path:
    return Path(__file__).resolve().parents[4]


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=_default_root())
    parser.add_argument("--oracle", type=Path)
    parser.add_argument("--emit", type=Path)
    args = parser.parse_args(argv)
    try:
        report = run_audit(args.root, args.oracle)
        report["generatedAt"] = datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")
        if args.emit:
            emit = args.emit if args.emit.is_absolute() else args.root / args.emit
            emit.parent.mkdir(parents=True, exist_ok=True)
            emit.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
        print(json.dumps(report, indent=2))
        return 0
    except (CatalogAuditFailure, OSError, ValueError, json.JSONDecodeError) as error:
        print(json.dumps({"scopeId": "REC-089", "outcome": "failed", "error": str(error)}, indent=2), file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())

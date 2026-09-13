"""Synthetic tests for the pre-observation case contract.

These tests exercise the oracle boundary without invoking Musoq or a provider.
They deliberately mutate frozen declarations to prove that an observed result
cannot silently replace the expectation that was registered first.
"""
from __future__ import annotations

import copy
import hashlib
import json
import tempfile
import unittest
from pathlib import Path

from case_contract import (
    ContractFailure,
    freeze,
    validate_cases,
    validate_minimization,
    verify,
)


class CaseContractTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        (self.root / "specs").mkdir()
        (self.root / "fixtures").mkdir()
        self.source = self.root / "specs" / "core.md"
        self.source.write_text("# source\nvalid authority passage\n", encoding="utf-8")
        self.fixture = self.root / "fixtures" / "seed.cs"
        self.fixture.write_text("seed fixture\n", encoding="utf-8")
        self.cases_path = self.root / "cases.json"
        self.manifest_path = self.root / "manifest.json"

    def tearDown(self) -> None:
        self.temp.cleanup()

    def authority(self, section: str = "test section") -> list[dict[str, str]]:
        return [
            {
                "sourcePath": "specs/core.md",
                "section": section,
                "passage": "valid authority passage",
                "sourceSha256": hashlib.sha256(self.source.read_bytes()).hexdigest(),
            }
        ]

    def case(
        self,
        case_id: str = "REC-087-TEST-001",
        *,
        seed: str = "select 'seed' from system.dual()",
        before: str = "seed",
        after: str = "changed",
        root_cause: str = "controlled test root cause",
        context: str = "controlled test context",
        classification: str = "valid_ordinary",
    ) -> dict:
        result = seed.replace(before, after, 1)
        return {
            "formatVersion": 1,
            "caseId": case_id,
            "scopeId": "REC-087",
            "task": "Preserve the user's controlled query task.",
            "seedQuery": seed,
            "fixtureRef": {
                "id": "fixture-seed",
                "path": "fixtures/seed.cs",
                "testName": "SeedFixtureIsControlled",
                "proof": "fixture is repository-owned and deterministic",
            },
            "mutation": {
                "kind": "replace",
                "description": "Replace the nominated token exactly once.",
                "before": before,
                "after": after,
                "resultQuery": result,
            },
            "expectedClassification": classification,
            "expectedRootCause": root_cause,
            "expectedDiagnostics": [],
            "authority": self.authority(),
            "repairPolicy": "no_change",
            "seedValidity": "valid",
            "preObservation": {
                "state": "unobserved",
                "registeredBeforeCandidate": True,
                "uncertainty": "The mutation's validity is declared before candidate execution.",
            },
            "fingerprint": {
                "domain": "parser",
                "rootCause": root_cause,
                "context": context,
            },
            "minimization": {
                "status": "pending_observation",
                "originalQuery": result,
                "minimizedQuery": None,
                "sameFault": None,
                "sameObservation": None,
                "preservationCriteria": ["same source fault", "same expected diagnostic boundary"],
            },
        }

    def write_cases(self, cases: list[dict]) -> None:
        self.cases_path.write_text(json.dumps({"cases": cases}, indent=2) + "\n", encoding="utf-8")

    def test_valid_cases_freeze_and_verify_with_pre_observation_metadata(self) -> None:
        cases = [
            self.case(
                "REC-087-TEST-ALIAS",
                seed="select 1 as Value from system.dual()",
                before="1 as Value",
                after="1 Value",
                root_cause="implicit alias remains valid",
                context="select alias boundary",
            ),
            self.case(
                "REC-087-TEST-COMMA",
                seed="enum E : int { Ready = 1 };",
                before="Ready = 1 }",
                after="Ready = 1, }",
                root_cause="allowed enum trailing comma remains valid",
                context="enum member list boundary",
            ),
            self.case(
                "REC-087-TEST-ESCAPE",
                seed="select 'value' from system.dual()",
                before="value",
                after="\\q",
                root_cause="unknown ordinary escape remains literal",
                context="ordinary string escape boundary",
            ),
        ]
        self.write_cases(cases)
        result = freeze(self.cases_path, self.manifest_path, self.root, "REC-087")
        self.assertEqual(3, result["caseCount"])
        self.assertEqual(3, len(result["cases"]))
        verified = verify(self.cases_path, self.manifest_path, self.root, "REC-087")
        self.assertTrue(verified["verified"])
        self.assertEqual(1, verified["sourceFileCount"])

    def test_invalid_negative_seed_is_rejected(self) -> None:
        case = self.case()
        case["seedValidity"] = "invalid"
        with self.assertRaisesRegex(ContractFailure, "seedValidity must be valid"):
            validate_cases([case], self.root, "REC-087")

    def test_post_hoc_expected_diagnostic_replacement_is_rejected(self) -> None:
        case = self.case(
            seed="select '\\q' from system.dual()",
            before="\\q",
            after="\\u12",
            root_cause="malformed fixed-length escape",
            context="ordinary string escape boundary",
            classification="invalid",
        )
        case["expectedDiagnostics"] = [
            {
                "code": "MQ1004_InvalidEscapeSequence",
                "phase": "Parse",
                "sourceDomain": "Query",
                "severity": "Error",
            }
        ]
        self.write_cases([case])
        freeze(self.cases_path, self.manifest_path, self.root, "REC-087")

        changed = copy.deepcopy(case)
        changed["expectedDiagnostics"][0]["code"] = "MQ2001_UnexpectedToken"
        self.write_cases([changed])
        with self.assertRaisesRegex(ContractFailure, "(?i)pre-observation payload digest mismatch"):
            verify(self.cases_path, self.manifest_path, self.root, "REC-087")

    def test_identifier_spelling_alone_is_not_novel_fingerprint(self) -> None:
        first = self.case(
            "REC-087-TEST-NAME-A",
            seed="select Name from system.dual()",
            before="Name",
            after="NameA",
        )
        second = self.case(
            "REC-087-TEST-NAME-B",
            seed="select name from system.dual()",
            before="name",
            after="nameB",
        )
        with self.assertRaisesRegex(ContractFailure, "identifier spelling is not novel coverage"):
            validate_cases([first, second], self.root, "REC-087")

    def test_changed_raw_authority_source_is_rejected(self) -> None:
        self.write_cases([self.case()])
        freeze(self.cases_path, self.manifest_path, self.root, "REC-087")
        self.source.write_text("# source\nchanged authority passage\n", encoding="utf-8")
        with self.assertRaisesRegex(ContractFailure, "sourceSha256 does not match"):
            verify(self.cases_path, self.manifest_path, self.root, "REC-087")

    def test_observation_fields_cannot_be_added_to_a_pre_observation_case(self) -> None:
        case = self.case()
        case["observedDiagnostics"] = []
        with self.assertRaisesRegex(ContractFailure, "post-observation field observedDiagnostics"):
            validate_cases([case], self.root, "REC-087")

    def test_minimization_requires_same_fault_and_observation(self) -> None:
        case = self.case()
        validate_minimization(case)
        completed = copy.deepcopy(case)
        completed["minimization"] = {
            "status": "completed",
            "originalQuery": case["mutation"]["resultQuery"],
            "minimizedQuery": "select 'changed' from system.dual()",
            "sameFault": True,
            "sameObservation": True,
            "evidence": "controlled minimization replay",
        }
        validate_minimization(completed)
        completed["minimization"]["sameObservation"] = False
        with self.assertRaisesRegex(ContractFailure, "same fault and observation"):
            validate_minimization(completed)

    def test_existing_manifest_cannot_be_replaced(self) -> None:
        self.write_cases([self.case()])
        freeze(self.cases_path, self.manifest_path, self.root, "REC-087")
        with self.assertRaisesRegex(ContractFailure, "Refusing to replace existing frozen manifest"):
            freeze(self.cases_path, self.manifest_path, self.root, "REC-087")

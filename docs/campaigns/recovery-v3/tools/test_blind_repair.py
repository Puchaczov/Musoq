"""Adversarial tests for the simulated REC-144 repair boundary."""
from __future__ import annotations

import hashlib
import json
import unittest
from pathlib import Path

from blind_repair import (
    ALLOWED_DISCOVERY,
    PUBLIC_TASK_FIELDS,
    BlindRepairSession,
    BoundaryFailure,
    build_public_envelope,
    load_manifest,
    validate_actor_manifest,
)
from task_corpus import evaluate_observation


class BlindRepairTests(unittest.TestCase):
    def setUp(self) -> None:
        self.row_fingerprints = [hashlib.sha256(b"controlled-row").hexdigest()]
        self.task = {
            "taskId": "REC-144-TEST-001",
            "taskPrompt": "Repair the query and return the requested result.",
            "malformedQuery": "select Name from #fixture.rows()",
            "deliveredDiagnostic": {
                "code": "MQ2001_UnexpectedToken",
                "phase": "Parse",
                "sourceKind": "Query",
            },
            "invariant": {
                "columns": ["Name"],
                "minimumRows": 1,
                "ordering": "Name ascending",
                "nullPolicy": "Name is non-null",
                "failurePolicy": "A remaining failure is not success.",
                "clarification": {
                    "acceptable": True,
                    "requiredObservations": ["fixture row shape"],
                },
            },
            "allowedOutcomes": ["executed", "clarification"],
            "mustExecute": True,
            "seedGroup": "private-seed",
            "semanticFamily": "names.source",
            "providerFixtureId": "private-fixture",
            "partition": "development",
            "fixtureRef": {"path": "private-fixture.cs"},
            "goldSql": "select Name from #fixture.rows()",
            "privateJudge": {"rowFingerprints": self.row_fingerprints},
        }
        self.discovery = {
            "schema_shape": {"columns": ["Name"], "types": {"Name": "string"}},
            "source_methods": ["rows"],
        }

    def judge(self, task: dict, observation: dict) -> dict:
        return evaluate_observation(
            task,
            observation,
            {
                "expected": {
                    "outcome": "executed",
                    "rowFingerprints": self.row_fingerprints,
                }
            },
        )

    def session(self, **kwargs: object) -> BlindRepairSession:
        options = {
            "scope_id": "REC-144",
            "task": self.task,
            "discovery": self.discovery,
            "judge": self.judge,
            "actor_id": "fake-test-actor",
            "actor_kind": "fake",
            "instructions": "Use the public task contract and bounded observations only.",
            "capabilities": ["schema_shape", "source_methods", "submit"],
        }
        options.update(kwargs)
        return BlindRepairSession(**options)

    def answer(self, query: str = "SELECT Name FROM #fixture.rows()") -> dict:
        return {
            "outcome": "executed",
            "repairedQuery": query,
            "columns": ["Name"],
            "rowCount": 1,
            "ordering": "Name ascending",
            "rowFingerprints": self.row_fingerprints,
        }

    def test_public_envelope_strips_fixture_split_and_answer_data(self) -> None:
        public = build_public_envelope(self.task)
        self.assertEqual(sorted(PUBLIC_TASK_FIELDS), sorted(public))
        serialized = json.dumps(public)
        for private_key in ("goldSql", "privateJudge", "fixtureRef", "seedGroup", "semanticFamily", "providerFixtureId"):
            self.assertNotIn(private_key, serialized)

    def test_allowlisted_observation_and_semantic_result_are_recorded_without_query_spelling(self) -> None:
        captured: dict = {}

        def actor(context: dict, tools: object) -> None:
            captured.update(context)
            self.assertFalse(hasattr(tools, "read_file"))
            self.assertFalse(hasattr(tools, "git_log"))
            tools.observe("schema_shape")
            response = tools.submit(self.answer("select Name from #fixture.rows()"))
            self.assertTrue(response["passed"])

        result = self.session().run(actor)
        self.assertEqual("completed", result["terminal"])
        self.assertEqual("passed", result["score"]["status"])
        self.assertFalse(result["contamination"]["detected"])
        self.assertEqual("fake-test-actor", result["actor"]["id"])
        self.assertEqual("fake", result["actor"]["kind"])
        self.assertEqual(["schema_shape", "source_methods", "submit"], result["actor"]["capabilities"])
        self.assertTrue(result["instructions"]["providedToActor"])
        self.assertFalse(result["instructions"]["parentReasoningProvided"])
        self.assertEqual("allowed", result["observations"][0]["result"])
        self.assertNotIn("fixtureRef", captured)
        self.assertNotIn("goldSql", json.dumps(captured))
        self.assertTrue(result["submission"]["repairedQueryProvided"])
        self.assertNotIn("select Name from #fixture.rows()", json.dumps(result["submission"]))
        self.assertFalse(result["score"]["liveModelScore"])
        self.assertFalse(result["score"]["humanScore"])

    def test_filesystem_repository_history_and_judge_requests_are_denied_and_retained(self) -> None:
        denied = ["filesystem.read", "repository.read", "git.history", "judge.read"]

        def actor(_: dict, tools: object) -> None:
            for operation in denied:
                response = tools.request(operation, {"path": "outside-actor-cwd"})
                self.assertEqual("access_denied", response["error"])
            tools.submit(self.answer())

        result = self.session(working_directory="different-temporary-cwd").run(actor)
        self.assertTrue(result["capabilityBoundary"]["workingDirectoryIsNotIsolationBoundary"])
        self.assertTrue(result["contamination"]["detected"])
        self.assertTrue(result["contamination"]["retained"])
        self.assertTrue(result["contamination"]["excludedFromScore"])
        self.assertEqual("excluded_contaminated", result["score"]["status"])
        self.assertEqual(denied, [attempt["operation"] for attempt in result["contamination"]["attempts"]])

    def test_unallowlisted_observation_is_contamination_not_a_hidden_capability(self) -> None:
        def actor(_: dict, tools: object) -> None:
            response = tools.observe("filesystem_listing")
            self.assertEqual("access_denied", response["error"])
            tools.submit(self.answer())

        result = self.session().run(actor)
        self.assertTrue(result["contamination"]["detected"])
        self.assertEqual("excluded_contaminated", result["score"]["status"])
        self.assertEqual("filesystem_listing", result["contamination"]["attempts"][0]["name"])

    def test_turn_budget_excludes_over_budget_actor(self) -> None:
        def actor(_: dict, tools: object) -> None:
            tools.observe("schema_shape")
            tools.observe("source_methods")
            tools.submit(self.answer())

        result = self.session(max_turns=2).run(actor)
        self.assertEqual("budget_exceeded", result["terminal"])
        self.assertEqual("excluded_budget", result["score"]["status"])
        self.assertTrue(any(event.get("reason") == "turn_budget_exceeded" for event in result["observations"]))

    def test_malformed_answer_is_excluded_without_becoming_a_score(self) -> None:
        def actor(_: dict, tools: object) -> None:
            response = tools.submit({"outcome": "executed", "goldSql": "secret answer"})
            self.assertEqual("malformed_submission", response["error"])

        result = self.session().run(actor)
        self.assertEqual("malformed_answer", result["terminal"])
        self.assertEqual("excluded_malformed", result["score"]["status"])
        self.assertFalse(result["contamination"]["detected"])

    def test_unsafe_action_is_denied_even_when_actor_has_a_different_working_directory(self) -> None:
        def actor(_: dict, tools: object) -> None:
            response = tools.request("destructive.reset", {"profile": "default"})
            self.assertEqual("access_denied", response["error"])
            tools.submit(self.answer())

        result = self.session(working_directory="isolated-looking-cwd").run(actor)
        self.assertEqual("excluded_contaminated", result["score"]["status"])
        self.assertEqual("destructive.reset", result["contamination"]["attempts"][0]["operation"])

    def test_semantic_judge_failure_is_negative_evidence_without_expected_answer_leak(self) -> None:
        wrong = self.answer()
        wrong["columns"] = ["Wrong"]

        def actor(_: dict, tools: object) -> None:
            response = tools.submit(wrong)
            self.assertEqual("semantic_judge_failed", response["error"])

        result = self.session().run(actor)
        self.assertEqual("semantic_judge_failed", result["terminal"])
        self.assertEqual("failed", result["score"]["status"])
        self.assertFalse(result["score"]["excluded"])
        self.assertNotIn("rowFingerprints", json.dumps(result["observations"]))
        self.assertNotIn("Wrong", json.dumps(result["observations"]))

    def test_no_submission_is_retained_as_excluded(self) -> None:
        result = self.session().run(lambda _context, _tools: None)
        self.assertEqual("no_submission", result["terminal"])
        self.assertEqual("excluded_no_submission", result["score"]["status"])

    def test_actor_manifest_records_fake_identity_instructions_and_capabilities(self) -> None:
        path = Path(__file__).resolve().parent.parent / "cases" / "REC-144-simulated-actors.json"
        manifest, summary = load_manifest(path)
        self.assertTrue(summary["validated"])
        self.assertTrue(summary["simulated"])
        self.assertEqual(5, summary["actorCount"])
        self.assertEqual("fake-allowlisted-repairer", summary["actorIds"][0])
        self.assertNotIn("goldSql", json.dumps(manifest))
        self.assertEqual(ALLOWED_DISCOVERY, {"schema_shape", "source_methods", "runtime_settings_metadata"})

    def test_actor_manifest_rejects_private_context_and_unknown_capabilities(self) -> None:
        base = {
            "formatVersion": 1,
            "scopeId": "REC-144",
            "state": "simulated_construction",
            "simulated": True,
            "actors": [
                {
                    "actorId": "fake",
                    "kind": "fake",
                    "instructions": "bounded",
                    "capabilities": ["filesystem.read"],
                    "goldSql": "private",
                }
            ],
        }
        with self.assertRaisesRegex(BoundaryFailure, "unallowlisted capability"):
            validate_actor_manifest(base)
        base["actors"][0]["capabilities"] = ["submit"]
        with self.assertRaisesRegex(BoundaryFailure, "private-answer"):
            validate_actor_manifest(base)


if __name__ == "__main__":
    unittest.main()

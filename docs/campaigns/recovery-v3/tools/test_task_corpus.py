"""Contract and adversarial tests for the REC-143 task corpus harness."""
from __future__ import annotations

import copy
import hashlib
import json
import tempfile
import unittest
from pathlib import Path

from task_corpus import (
    CorpusFailure,
    build_private_judge,
    evaluate_observation,
    materialize_holdout,
    partition_for_group,
    split_group,
    validate_payload,
)


class TaskCorpusTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        fixture = self.root / "fixture.cs"
        fixture.write_text("repository-owned fixture\nControlledFixture\n", encoding="utf-8")
        self.fixture = fixture

    def tearDown(self) -> None:
        self.temp.cleanup()

    def task(
        self,
        task_id: str = "REC-143-TEST-001",
        *,
        seed_group: str = "seed-group-a",
        semantic_family: str = "lexical.escape",
        provider: str = "fixture-parser",
        partition: str | None = None,
    ) -> dict:
        task = {
            "formatVersion": 1,
            "taskId": task_id,
            "state": "development_payload",
            "family": semantic_family.split(".")[0],
            "semanticFamily": semantic_family,
            "seedGroup": seed_group,
            "providerFixtureId": provider,
            "partition": "development",
            "taskPrompt": "Repair the query and return the requested result or ask for bounded missing observations.",
            "malformedQuery": "select Name from #fixture.rows()",
            "fixtureRef": {
                "id": provider,
                "path": "fixture.cs",
                "testName": "ControlledFixture",
                "proof": "temporary repository-owned fixture",
            },
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
        }
        if partition is not None:
            task["partition"] = partition
        return task

    def payload(self, tasks: list[dict], reserved: list[dict] | None = None) -> dict:
        return {
            "formatVersion": 1,
            "scopeId": "REC-143",
            "state": "development_payload",
            "taskCount": len(tasks),
            "split": {
                "algorithm": "sha256-group-v1",
                "seed": "test-split-v1",
                "holdoutState": "reserved_until_freeze",
                "finalHoldoutCreated": False,
                "reservedGroups": reserved or [
                    {
                        "seedGroup": "seed-group-holdout-1",
                        "semanticFamily": "lexical.escape.holdout",
                        "providerFixtureId": "fixture-parser-holdout",
                        "partition": "holdout_reserved",
                    }
                ],
            },
            "tasks": tasks,
        }

    def development_task(self, **kwargs: str) -> dict:
        task = self.task(**kwargs)
        group = split_group(task)
        if partition_for_group(group, "test-split-v1") != "development":
            task["seedGroup"] = task["seedGroup"] + "-development"
        self.assertEqual("development", partition_for_group(split_group(task), "test-split-v1"))
        return task

    def test_valid_payload_covers_invariants_and_reserves_holdout(self) -> None:
        families = [
            "lexical.escape",
            "omissions.clause",
            "names.source",
            "types.overload",
            "grouping_windows.frame",
            "enum.intrinsic",
            "table_couple.settings",
            "binary_text.validation",
        ]
        tasks = [
            self.development_task(
                task_id=f"REC-143-TEST-{index:03}",
                seed_group=f"seed-{index}",
                semantic_family=family,
            )
            for index, family in enumerate(families, 1)
        ]
        result = validate_payload(self.payload(tasks), self.root)
        self.assertTrue(result["validated"])
        self.assertEqual(8, result["familyCount"])
        self.assertEqual(8, result["developmentGroupCount"])
        self.assertEqual(1, result["reservedHoldoutGroupCount"])

    def test_gold_sql_and_answer_rows_are_rejected_from_repair_payload(self) -> None:
        task = self.development_task()
        task["goldSql"] = "select Name from #fixture.rows()"
        with self.assertRaisesRegex(CorpusFailure, "goldSql.*private-answer"):
            validate_payload(self.payload([task]), self.root)

    def test_fixture_test_name_must_be_present_in_fixture_file(self) -> None:
        task = self.development_task()
        task["fixtureRef"]["testName"] = "MissingFixtureTest"
        with self.assertRaisesRegex(CorpusFailure, "fixtureRef.testName is not present"):
            validate_payload(self.payload([task]), self.root)

    def test_compile_only_and_empty_invariants_are_rejected(self) -> None:
        task = self.development_task()
        task["mustExecute"] = False
        with self.assertRaisesRegex(CorpusFailure, "mustExecute must be true"):
            validate_payload(self.payload([task]), self.root)

        valid = self.development_task()
        validate_payload(self.payload([valid]), self.root)
        with self.assertRaisesRegex(CorpusFailure, "compile-only or empty-result"):
            evaluate_observation(
                valid,
                {
                    "outcome": "empty",
                    "compileOnly": True,
                    "columns": ["Name"],
                    "rowCount": 0,
                },
            )

    def test_semantic_group_is_stable_across_superficial_token_spelling(self) -> None:
        first = self.development_task(
            task_id="REC-143-TEST-SPELLING-A",
            seed_group="same-semantic-seed",
            semantic_family="names.source",
        )
        second = copy.deepcopy(first)
        second["taskId"] = "REC-143-TEST-SPELLING-B"
        second["malformedQuery"] = "SELECT Name FROM #FIXTURE.ROWS()"
        self.assertEqual(split_group(first), split_group(second))
        self.assertEqual(
            partition_for_group(split_group(first), "test-split-v1"),
            partition_for_group(split_group(second), "test-split-v1"),
        )

    def test_cross_partition_group_leakage_is_rejected(self) -> None:
        first = self.development_task(seed_group="shared", semantic_family="names.source")
        second = copy.deepcopy(first)
        second["taskId"] = "REC-143-TEST-LEAK"
        second["partition"] = "holdout_reserved"
        with self.assertRaisesRegex(CorpusFailure, "partition does not match|crosses partitions"):
            validate_payload(self.payload([first, second]), self.root)

    def test_private_judge_is_separate_and_semantic_execution_is_accepted(self) -> None:
        task = self.development_task()
        payload = self.payload([task])
        validate_payload(payload, self.root)
        row_fingerprints = [hashlib.sha256(b"fixture-row").hexdigest()]
        judge = build_private_judge(
            payload,
            {task["taskId"]: {"outcome": "executed", "rowFingerprints": row_fingerprints}},
            self.root,
        )
        self.assertEqual("private_judge", judge["state"])
        self.assertNotIn("goldSql", json.dumps(payload))
        result = evaluate_observation(
            task,
            {
                "outcome": "executed",
                "columns": ["Name"],
                "rowCount": 1,
                "ordering": "Name ascending",
                "rowFingerprints": row_fingerprints,
            },
            judge["entries"][0],
        )
        self.assertTrue(result["passed"])

    def test_clarification_is_bounded_and_not_a_compile_only_escape_hatch(self) -> None:
        task = self.development_task()
        result = evaluate_observation(
            task,
            {"outcome": "clarification", "requestedObservations": ["source row shape"]},
        )
        self.assertTrue(result["passed"])
        with self.assertRaisesRegex(CorpusFailure, "bounded observations"):
            evaluate_observation(task, {"outcome": "clarification", "requestedObservations": []})

    def test_holdout_materialization_requires_build_and_prompt_freeze(self) -> None:
        task = self.development_task()
        payload = self.payload([task])
        with self.assertRaisesRegex(CorpusFailure, "compared build identity"):
            materialize_holdout(payload, [], "0" * 64, self.root)
        with self.assertRaisesRegex(CorpusFailure, "repair prompt digest"):
            materialize_holdout(payload, ["build-a"], "not-a-digest", self.root)

        result = materialize_holdout(payload, ["build-a", "build-b"], "a" * 64, self.root)
        self.assertEqual("holdout_materialized_after_freeze", result["state"])
        self.assertFalse(result["answersExposedBeforeFreeze"])

    def test_holdout_group_cannot_be_present_in_repair_payload(self) -> None:
        holdout = {
            "seedGroup": "reserved",
            "semanticFamily": "lexical.escape.holdout",
            "providerFixtureId": "fixture-parser-holdout",
            "partition": "holdout_reserved",
        }
        task = self.task(
            seed_group=holdout["seedGroup"],
            semantic_family=holdout["semanticFamily"],
            provider=holdout["providerFixtureId"],
        )
        with self.assertRaisesRegex(CorpusFailure, "partition does not match"):
            validate_payload(self.payload([task], reserved=[holdout]), self.root)

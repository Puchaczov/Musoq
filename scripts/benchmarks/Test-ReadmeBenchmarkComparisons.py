"""Check comparison evidence; pass --integration to also execute the SQL through Musoq."""

import copy
import sys
import unittest
from unittest.mock import patch

sys.dont_write_bytecode = True
from ReadmeBenchmarkComparisons import index_comparisons, prepare_comparisons

RUN_INTEGRATION = "--integration" in sys.argv


class ComparisonEvidenceTests(unittest.TestCase):
    def setUp(self):
        self.campaigns = [
            {
                "manifest": {"provenance": {"build": {"engineVersion": "1.0"}}},
                "measurements": {"query": {"meanNanoseconds": 100, "allocatedBytesPerOperation": 200}},
            },
            {
                "manifest": {"provenance": {"build": {"engineVersion": "2.0"}}},
                "measurements": {"query": {"meanNanoseconds": 150, "allocatedBytesPerOperation": 100}},
            },
        ]
        self.rows = [
            {"Sequence": 0, "VersionLabel": "Musoq 1.0", "CaseId": "query", "MeanNanoseconds": 100,
             "AllocatedBytes": 200, "TimeChangePercent": None, "AllocationChangePercent": None},
            {"Sequence": 1, "VersionLabel": "Musoq 2.0", "CaseId": "query", "MeanNanoseconds": 150,
             "AllocatedBytes": 100, "TimeChangePercent": 50, "AllocationChangePercent": -50},
        ]

    def test_unordered_rows_are_aligned_to_campaign_sequence(self):
        actual = index_comparisons(list(reversed(self.rows)), self.campaigns, ["query"])
        self.assertEqual(actual["query"], self.rows)

    def test_duplicate_version_cannot_hide_missing_version(self):
        with self.assertRaisesRegex(ValueError, "Duplicate Musoq comparison"):
            index_comparisons([self.rows[0], self.rows[0]], self.campaigns, ["query"])

    def test_missing_case_version_is_rejected(self):
        with self.assertRaisesRegex(ValueError, "exactly one comparison"):
            index_comparisons(self.rows[:1], self.campaigns, ["query"])

    def test_incorrect_version_label_is_rejected(self):
        rows = copy.deepcopy(self.rows)
        rows[1]["VersionLabel"] = "Musoq 3.0"
        with self.assertRaisesRegex(ValueError, "version does not match"):
            index_comparisons(rows, self.campaigns, ["query"])

    def test_altered_source_measurement_is_rejected(self):
        rows = copy.deepcopy(self.rows)
        rows[1]["MeanNanoseconds"] = 15
        with self.assertRaisesRegex(ValueError, "differs from retained evidence"):
            index_comparisons(rows, self.campaigns, ["query"])

    def test_null_percentage_cannot_hide_a_regression(self):
        rows = copy.deepcopy(self.rows)
        rows[1]["TimeChangePercent"] = None
        with self.assertRaisesRegex(ValueError, "Missing or invalid Musoq percentage"):
            index_comparisons(rows, self.campaigns, ["query"])

    def test_first_version_cannot_claim_an_improvement(self):
        rows = copy.deepcopy(self.rows)
        rows[0]["TimeChangePercent"] = -50
        with self.assertRaisesRegex(ValueError, "First version must have no"):
            index_comparisons(rows, self.campaigns, ["query"])

    def test_diagnostic_object_is_not_a_result_set(self):
        with self.assertRaisesRegex(ValueError, "exactly one comparison"):
            index_comparisons({"diagnostics": []}, self.campaigns, ["query"])

    def test_absent_sdk_has_no_silent_python_fallback(self):
        with patch("ReadmeBenchmarkComparisons.shutil.which", return_value=None):
            with self.assertRaisesRegex(FileNotFoundError, "The .NET SDK is required"):
                prepare_comparisons(self.campaigns, ["query"])


@unittest.skipUnless(RUN_INTEGRATION, "Pass --integration to build and execute the repository comparison runner")
class ComparisonQueryTests(unittest.TestCase):
    def test_preceding_version_changes_stay_with_their_case(self):
        campaigns = [
            {
                "manifest": {"provenance": {"build": {"engineVersion": "1.0"}}},
                "measurements": {
                    "a": {"meanNanoseconds": 100, "allocatedBytesPerOperation": 1000},
                    "b": {"meanNanoseconds": 40, "allocatedBytesPerOperation": 100},
                },
            },
            {
                "manifest": {"provenance": {"build": {"engineVersion": "2.0"}}},
                "measurements": {
                    "a": {"meanNanoseconds": 50, "allocatedBytesPerOperation": 2000},
                    "b": {"meanNanoseconds": 60, "allocatedBytesPerOperation": 100},
                },
            },
            {
                "manifest": {"provenance": {"build": {"engineVersion": "3.0"}}},
                "measurements": {
                    "a": {"meanNanoseconds": 75, "allocatedBytesPerOperation": 1000},
                    "b": {"meanNanoseconds": 30, "allocatedBytesPerOperation": 50},
                },
            },
        ]
        actual = prepare_comparisons(campaigns, ["b", "a"])
        self.assertEqual(
            [[(row["TimeChangePercent"], row["AllocationChangePercent"]) for row in actual[key]] for key in ("a", "b")],
            [[(None, None), (-50, 100), (50, -50)], [(None, None), (50, 0), (-50, -50)]],
        )


if __name__ == "__main__":
    if RUN_INTEGRATION:
        sys.argv.remove("--integration")
    unittest.main()

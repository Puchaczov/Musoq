"""Tests for the REC-089 independent catalog oracle."""
from __future__ import annotations

import copy
import json
import unittest
from pathlib import Path

from diagnostic_catalog_audit import (
    CatalogAuditFailure,
    catalog_entries,
    load_json,
    parse_enum_codes,
    run_audit,
    validate_catalog,
)


class DiagnosticCatalogAuditTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.root = Path(__file__).resolve().parents[4]
        cls.enum_path = cls.root / "src" / "dotnet" / "Musoq.Parser" / "Diagnostics" / "DiagnosticCode.cs"
        cls.catalog_path = cls.root / "specs" / "diagnostic-catalog.json"
        cls.spec_path = cls.root / "docs" / "campaigns" / "recovery-v3" / "authority" / "musoq-core-language-spec.md"
        cls.oracle_path = cls.root / "docs" / "campaigns" / "recovery-v3" / "tools" / "rec089-independent-oracle.json"
        cls.enum_source = cls.enum_path.read_text(encoding="utf-8-sig")
        cls.catalog = load_json(cls.catalog_path)
        cls.spec = cls.spec_path.read_text(encoding="utf-8-sig")
        cls.oracle = load_json(cls.oracle_path)

    def test_current_catalog_passes_without_descriptor_registry(self) -> None:
        report = run_audit(self.root, self.oracle_path)

        self.assertEqual("passed", report["outcome"])
        self.assertEqual("killed", report["negativeControl"]["status"])
        self.assertFalse(report["independence"]["descriptorRegistryImported"])
        self.assertFalse(report["independence"]["activeSetFrozen"])
        self.assertEqual(report["activeInventory"]["enumCount"], report["activeInventory"]["catalogCount"])
        drift = {item["findingId"]: item for item in report["driftRecords"]}
        self.assertEqual("confirmed-current-drift", drift["DRIFT-001"]["state"])
        self.assertEqual("confirmed-current-drift", drift["DRIFT-002"]["state"])

    def test_wrong_explanation_is_rejected_by_spec_oracle(self) -> None:
        mutated = copy.deepcopy(self.catalog)
        entry = next(item for item in catalog_entries(mutated) if item["code"] == "MQ3086_UnknownCallable")
        entry["explanation"] = "The schema is available and the requested source is valid."

        with self.assertRaises(CatalogAuditFailure) as context:
            validate_catalog(self.enum_source, mutated, self.spec, self.oracle)

        self.assertIn("MQ3086_UnknownCallable is missing independent meaning term", str(context.exception))

    def test_retired_numeric_code_cannot_be_repurposed(self) -> None:
        mutated_enum = self.enum_source + "\n    MQ5009_RetiredAliasBehavior = 5009,\n"

        with self.assertRaises(CatalogAuditFailure) as context:
            validate_catalog(mutated_enum, self.catalog, self.spec, self.oracle)

        self.assertIn("retired numeric code 5009 was repurposed", str(context.exception))

    def test_compatibility_identifiers_are_explainable_without_emission(self) -> None:
        report = validate_catalog(self.enum_source, self.catalog, self.spec, self.oracle)
        inventory = {item["code"]: item for item in report["compatibilityInventory"]}

        self.assertFalse(inventory["MQ5009_OrderByAliasBehavior"]["enumPresent"])
        self.assertFalse(inventory["MQ5009_OrderByAliasBehavior"]["catalogPresent"])
        self.assertTrue(inventory["MQ5020_SetOperationOrderByScope"]["catalogPresent"])
        self.assertTrue(inventory["MQ5026_SetOperationSliceScope"]["catalogPresent"])
        self.assertTrue(all(item["explainableWithoutEmission"] for item in inventory.values()))

    def test_active_inventory_is_derived_not_count_frozen(self) -> None:
        mutated_enum = self.enum_source.replace(
            "MQ9002_InternalExecutionError = 9002,",
            "MQ9002_InternalExecutionError = 9002,\n    MQ9010_IndependentInventoryProbe = 9010,",
        )
        mutated_catalog = copy.deepcopy(self.catalog)
        probe = copy.deepcopy(catalog_entries(mutated_catalog)[0])
        probe["code"] = "MQ9010_IndependentInventoryProbe"
        probe["number"] = 9010
        catalog_entries(mutated_catalog).append(probe)

        report = validate_catalog(mutated_enum, mutated_catalog, self.spec, self.oracle)

        self.assertEqual(211, report["activeInventory"]["enumCount"])
        self.assertIn(
            {"code": "MQ9010_IndependentInventoryProbe", "number": 9010},
            report["activeInventory"]["codes"],
        )

    def test_oracle_file_has_no_post_observation_fields(self) -> None:
        oracle = json.loads(self.oracle_path.read_text(encoding="utf-8"))
        self.assertNotIn("observedDiagnostics", oracle)
        self.assertNotIn("registryOutput", oracle)

    def test_enum_parser_rejects_duplicate_numbers(self) -> None:
        with self.assertRaises(CatalogAuditFailure):
            parse_enum_codes("MQ9001_First = 9001, MQ9002_Second = 9001,")


if __name__ == "__main__":
    unittest.main()

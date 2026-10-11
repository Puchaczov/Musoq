"""Checks for specification portability, references and balanced code fences."""
from pathlib import Path
import unittest

import verify_portability as verifier


class SpecificationPortabilityValidationTests(unittest.TestCase):
    def test_missing_local_reference_is_reported(self):
        errors = []
        verifier.check_links(Path(__file__), "[missing](unavailable-portable-spec.md)", errors)
        self.assertEqual(1, len(errors))

    def test_retained_explicit_anchor_is_recognized(self):
        self.assertIn("old-anchor", verifier.anchors('<a id="old-anchor"></a>\n## New heading'))

    def test_host_specific_language_is_reported_with_line_number(self):
        errors = []
        verifier.check_spec(Path("sample.md"), "# Contract\nValues require System.Int32.", errors)
        self.assertEqual(["sample.md:2: host-specific language: Values require System.Int32."], errors)

    def test_portable_language_is_accepted(self):
        errors = []
        verifier.check_spec(Path("sample.md"), "# Contract\nValues are signed 32-bit integers.", errors)
        self.assertEqual([], errors)

    def test_unclosed_code_fence_is_reported(self):
        errors = []
        verifier.check_spec(Path("sample.md"), "# Example\n```sql\nselect 1", errors)
        self.assertEqual(["sample.md: unclosed code fence"], errors)

    def test_balanced_code_fences_are_accepted(self):
        errors = []
        verifier.check_spec(Path("sample.md"), "# Example\n```sql\nselect 1\n```", errors)
        self.assertEqual([], errors)


if __name__ == "__main__":
    unittest.main()

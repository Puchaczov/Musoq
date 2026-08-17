using System.Collections.Generic;
using static Musoq.Parser.Diagnostics.ErrorMetadataCatalog;

namespace Musoq.Parser.Diagnostics;

internal static partial class WarningMetadataCatalog
{
    private static IEnumerable<ErrorMetadata> BuildOuterJoinMetadata()
    {
        yield return Entry(
            DiagnosticCode.MQ5018_AmbiguousOuterJoinNullCheck,
            "A nullable column from an optional outer-join side can be NULL both when its row is missing and when a present row contains NULL.",
            ["Use the table alias IS PRESENT or IS MISSING predicate to test row existence.", "Check a source column that is non-nullable when a present row is required."],
            "Core Spec - Outer Joins");

        yield return Entry(
            DiagnosticCode.MQ5019_NullRejectingOuterJoinFilter,
            "A WHERE predicate is false or UNKNOWN for the NULL-extended row of an outer join, so unmatched rows cannot survive.",
            ["Move the restriction into the JOIN ON clause when it is part of matching.", "Use an explicit row-presence predicate when removing unmatched rows is intentional."],
            "Core Spec - Outer Joins");
    }
}

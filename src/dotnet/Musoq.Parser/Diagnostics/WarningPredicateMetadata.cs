using System.Collections.Generic;
using static Musoq.Parser.Diagnostics.ErrorMetadataCatalog;

namespace Musoq.Parser.Diagnostics;

internal static partial class WarningMetadataCatalog
{
    private static IEnumerable<ErrorMetadata> BuildPredicateMetadata()
    {
        yield return Entry(
            DiagnosticCode.MQ5017_NullComparison,
            "SQL comparisons with NULL produce UNKNOWN rather than true or false. Use IS NULL, IS NOT NULL, or IS DISTINCT FROM when that is the intended test.",
            ["Replace the comparison with IS NULL or IS NOT NULL.", "Use IS DISTINCT FROM when a total comparison is required."],
            "Core Spec - Null Handling");

        yield return Entry(
            DiagnosticCode.MQ5024_NullSensitiveNotIn,
            "NOT IN returns UNKNOWN, rather than true, when its candidate list contains NULL and no value matches.",
            ["Remove NULL from the NOT IN list.", "Use an explicit NULL-safe predicate such as NOT EXISTS or add a non-null filter."],
            "Core Spec - Null Handling");
    }
}

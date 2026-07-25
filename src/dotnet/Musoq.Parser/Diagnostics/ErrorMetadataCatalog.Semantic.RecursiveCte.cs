using System.Collections.Generic;
using static Musoq.Parser.Diagnostics.ErrorMetadataCatalog;

namespace Musoq.Parser.Diagnostics;

internal static partial class SemanticErrorMetadataCatalog
{
    private static IEnumerable<ErrorMetadata> BuildRecursiveCteMetadata()
    {
        yield return Entry(
            DiagnosticCode.MQ3072_RecursiveCteRequiresKeyword,
            "A CTE references itself without declaring WITH RECURSIVE.",
            ["Add RECURSIVE after WITH."],
            "Core Spec - Recursive CTEs");

        yield return Entry(
            DiagnosticCode.MQ3073_InvalidRecursiveCteShape,
            "The recursive CTE does not have one anchor and one top-level recursive union member.",
            ["Use one anchor query followed by UNION, UNION ALL, or keyed UNION and one recursive member."],
            "Core Spec - Recursive CTEs");

        yield return Entry(
            DiagnosticCode.MQ3074_InvalidRecursiveCteReference,
            "The recursive CTE contains an invalid self, forward, or mutual reference.",
            ["Reference the recursive CTE exactly once in the recursive member and only depend on earlier CTEs."],
            "Core Spec - Recursive CTEs");

        yield return Entry(
            DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator,
            "The recursive member uses an operator outside the v1 recursive CTE subset.",
            ["Move the operator to the outer query or rewrite the recursive member using projection, filtering, supported joins, or APPLY."],
            "Core Spec - Recursive CTEs");

        yield return Entry(
            DiagnosticCode.MQ3076_RecursiveCteOutputMismatch,
            "The recursive member output does not match the anchor-derived recursive relation shape.",
            ["Match the anchor column count and types; add an explicit postfix cast to the anchor when necessary."],
            "Core Spec - Recursive CTEs");

        yield return Entry(
            DiagnosticCode.MQ3077_CteColumnListCountMismatch,
            "The number of exported CTE column names differs from the number of projected columns.",
            ["Add or remove names so the CTE column list matches the projection positionally."],
            "Core Spec - Common Table Expressions");

        yield return Entry(
            DiagnosticCode.MQ3078_DuplicateCteColumnName,
            "A CTE column list exports the same case-insensitive identifier more than once.",
            ["Rename one of the duplicate exported columns."],
            "Core Spec - Common Table Expressions");
    }
}

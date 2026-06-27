using System.Collections.Generic;
using static Musoq.Parser.Diagnostics.ErrorMetadataCatalog;

namespace Musoq.Parser.Diagnostics;

internal static partial class WarningMetadataCatalog
{
    public static IEnumerable<ErrorMetadata> Build()
    {
        yield return Entry(
            DiagnosticCode.MQ5001_UnusedAlias,
            "An alias was defined but is never referenced in the query.",
            ["Remove the unused alias or reference it in the query."],
            "Core Spec - Aliasing");

        yield return Entry(
            DiagnosticCode.MQ5002_SelectStar,
            "SELECT * was used, which can make result shapes fragile when source schemas evolve.",
            [
                "List the required columns explicitly.",
                "Use star modifiers only when broad projection is intentional."
            ],
            "Core Spec - SELECT Clause");

        yield return Entry(
            DiagnosticCode.MQ5003_ImplicitTypeConversion,
            "An implicit type conversion is occurring and may cause unexpected results.",
            ["Use an explicit conversion function to make the intent clear."],
            "Core Spec - Type System");

        yield return Entry(
            DiagnosticCode.MQ5004_PotentialNullReference,
            "An expression may dereference a null value at runtime.",
            [
                "Add a null guard with CASE WHEN.",
                "Use nullable-aware query logic when nulls are expected."
            ],
            "Core Spec - Null Handling");

        yield return Entry(
            DiagnosticCode.MQ5005_RedundantParentheses,
            "An expression contains parentheses that do not change evaluation order.",
            [
                "Remove redundant parentheses for readability.",
                "Keep parentheses when they clarify intent in complex expressions."
            ],
            "Core Spec - Expressions");

        yield return Entry(
            DiagnosticCode.MQ5006_DeprecatedSyntax,
            "The query uses syntax that is deprecated and may be removed in a future version.",
            [
                "Rewrite the query using the current syntax.",
                "Check release notes for the recommended replacement."
            ],
            "Core Spec - Compatibility");

        yield return Entry(
            DiagnosticCode.MQ5007_PerformanceWarning,
            "The query uses a pattern that may be expensive for large inputs.",
            [
                "Add filters earlier in the query when possible.",
                "Project only columns needed by later operations."
            ],
            "Core Spec - Query Performance");

        yield return Entry(
            DiagnosticCode.MQ5008_UnreachableCode,
            "A branch or expression cannot be reached based on preceding query logic.",
            [
                "Remove the unreachable branch.",
                "Check whether a condition was inverted accidentally."
            ],
            "Core Spec - Expressions");

        yield return Entry(
            DiagnosticCode.MQ5009_OrderByAliasBehavior,
            "ORDER BY alias may not resolve to the computed expression in this version.",
            ["Repeat the expression explicitly in ORDER BY."],
            "Core Spec - ORDER BY Clause");

        yield return Entry(
            DiagnosticCode.MQ5010_TautologicalCondition,
            "A condition always evaluates to true and does not filter rows.",
            [
                "Remove the redundant condition.",
                "Check whether one side of the comparison should reference a different value."
            ],
            "Core Spec - WHERE Clause");

        yield return Entry(
            DiagnosticCode.MQ5011_ContradictoryCondition,
            "A condition always evaluates to false, so no rows can satisfy it.",
            [
                "Remove or correct the contradictory condition.",
                "Check whether AND should be OR in the predicate."
            ],
            "Core Spec - WHERE Clause");

        yield return Entry(
            DiagnosticCode.MQ5012_OptimizationFallback,
            "An optimization was requested or attempted, but Musoq had to keep residual runtime work.",
            [
                "Inspect the planning, logical, and physical plan text for the affected source or operator.",
                "Review whether the source can accept the requested predicate, ordering, or slicing capability."
            ],
            "Core Spec - Query Performance");

        foreach (var metadata in BuildSourceContractMetadata()) yield return metadata;
    }
}

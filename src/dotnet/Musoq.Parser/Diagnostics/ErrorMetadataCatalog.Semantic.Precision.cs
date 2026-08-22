using System.Collections.Generic;
using static Musoq.Parser.Diagnostics.ErrorMetadataCatalog;

namespace Musoq.Parser.Diagnostics;

internal static partial class SemanticErrorMetadataCatalog
{
    private static IEnumerable<ErrorMetadata> BuildPrecisionMetadata()
    {
        yield return Entry(
            DiagnosticCode.MQ3090_UnsupportedCastTarget,
            "The postfix cast target is not one of the CLR types or C# aliases supported by Musoq.",
            ["Use a supported CLR type name such as Int32, Decimal, DateTime, or String.", "Use an explicit conversion function when a custom target is required."],
            "Core Spec - Type System");

        yield return Entry(
            DiagnosticCode.MQ3091_InvalidConstantCast,
            "A constant value is known at bind time and cannot be converted to the requested target type.",
            ["Correct the literal value or use a compatible target type.", "Use a guarded conversion function when the value is not guaranteed to be valid."],
            "Core Spec - Type System");

        yield return Entry(
            DiagnosticCode.MQ3092_AggregateInGroupBy,
            "An aggregate expression is not valid inside GROUP BY.",
            ["Group by the input expression instead.", "Move the aggregate expression to SELECT or HAVING."],
            "Core Spec - GROUP BY and Aggregation");

        yield return Entry(
            DiagnosticCode.MQ3093_OrderByOrdinalUnsupported,
            "This engine does not interpret numeric ORDER BY expressions as projection ordinals.",
            ["Order by a column name or projection alias instead of a numeric position."],
            "Core Spec - ORDER BY");

        yield return Entry(
            DiagnosticCode.MQ3094_InvalidConstantRegex,
            "A constant RLIKE pattern can be checked during binding and is not valid for the runtime regex engine.",
            ["Fix the regex syntax or escape the intended metacharacters.", "Use a raw literal when backslashes should be preserved."],
            "Core Spec - Pattern Predicates");

        yield return Entry(
            DiagnosticCode.MQ3095_ScalarSubqueryCardinality,
            "A scalar subquery must be provably single-row before its value can be used as a scalar.",
            ["Add an aggregate, TAKE 1, or another predicate that guarantees one row.", "Use IN or EXISTS when multiple rows are intended."],
            "Core Spec - Subqueries");

        yield return Entry(
            DiagnosticCode.MQ3096_UnsupportedVariableKeyAccess,
            "Bracket key access on a script variable is not supported by the current binding and code-generation pipeline.",
            ["Bind the key explicitly or use a typed object/property access.", "Use a source column or dynamic object that supports key access."],
            "Core Spec - Script Variables");

        yield return Entry(
            DiagnosticCode.MQ3097_UnsupportedAggregateProjection,
            "The selected aggregate and non-aggregate expressions cannot be lowered without a grouping boundary.",
            ["Add GROUP BY for the non-aggregate projection.", "Aggregate every selected expression that is not a grouping key."],
            "Core Spec - GROUP BY and Aggregation");

        yield return Entry(
            DiagnosticCode.MQ3098_InvalidRangeFrameOrderKey,
            "A RANGE frame with a PRECEDING or FOLLOWING offset requires exactly one numeric ORDER BY key.",
            ["Use one numeric ORDER BY expression for a bounded RANGE frame.", "Use CURRENT ROW boundaries for peer-aware RANGE frames with composite or nonnumeric ordering."],
            "Core Spec - Window Frames");
    }
}

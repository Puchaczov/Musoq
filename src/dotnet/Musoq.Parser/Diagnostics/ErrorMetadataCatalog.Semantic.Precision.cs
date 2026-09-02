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

        yield return Entry(
            DiagnosticCode.MQ3099_WindowOrderByRequired,
            "This ranking or offset window function cannot determine its row order without an ORDER BY clause.",
            [
                "Add ORDER BY <expression> inside OVER (...).",
                "If the calculation is partition-wide, use an aggregate window function that does not require row order."
            ],
            "Core Spec - Window Functions");

        yield return Entry(
            DiagnosticCode.MQ3100_NestedWindowFunction,
            "A window function argument contains another window function. Window evaluation has one boundary per query level.",
            [
                "Move the inner window expression into a CTE or derived query.",
                "Apply the outer window function in the next query level over the materialized result."
            ],
            "Core Spec - Window Functions");

        yield return Entry(
            DiagnosticCode.MQ3101_WindowFunctionInFilter,
            "Window functions are evaluated after WHERE and HAVING, so they cannot be referenced by those filters.",
            [
                "Move the window predicate to QUALIFY.",
                "Compute the window value in an inner query and filter it from an outer query."
            ],
            "Core Spec - Window Functions");

        yield return Entry(
            DiagnosticCode.MQ3102_InvalidStatementOrder,
            "TABLE and COUPLE declarations form the query preamble and must precede CTEs and executable statements.",
            [
                "Move all TABLE definitions before COUPLE statements.",
                "Move TABLE and COUPLE declarations before the first CTE or query."
            ],
            "TABLE/COUPLE Spec - Statement Order");

        yield return Entry(
            DiagnosticCode.MQ3103_InvalidWindowFunctionArgument,
            "A constant window-function argument has a valid CLR type but violates the function's value domain.",
            [
                "Use a positive integer for the NTILE bucket count.",
                "Use a one-based positive position for NTH_VALUE."
            ],
            "Core Spec - Window Functions");

        yield return Entry(
            DiagnosticCode.MQ3104_UnknownNamedWindow,
            "A window function references a named WINDOW specification that is not defined in the current query.",
            [
                "Declare the referenced name with WINDOW name AS (...).",
                "Use an inline OVER (...) specification instead."
            ],
            "Core Spec - Window Functions");

        yield return Entry(
            DiagnosticCode.MQ3105_DuplicateNamedWindow,
            "A query cannot declare two named WINDOW specifications with the same name.",
            [
                "Remove the duplicate WINDOW definition.",
                "Rename one of the definitions so every window name is unique."
            ],
            "Core Spec - Window Functions");

    }
}

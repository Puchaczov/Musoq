using System.Collections.Generic;
using static Musoq.Parser.Diagnostics.ErrorMetadataCatalog;

namespace Musoq.Parser.Diagnostics;

internal static partial class WarningMetadataCatalog
{
    private static IEnumerable<ErrorMetadata> BuildCoreWarningMetadata()
    {
        yield return Entry(
            DiagnosticCode.MQ5003_ImplicitTypeConversion,
            "Ambiguous numeric date text is being implicitly converted to a temporal type; its day/month interpretation can depend on the shape of the input.",
            [
                "Use ISO year-first text such as 2026-02-01.",
                "Use ToDateTimeWithFormat or ToDateTimeOffsetWithFormat with an explicit format."
            ],
            "Core Spec - Type System");

        yield return Entry(
            DiagnosticCode.MQ5008_UnreachableCode,
            "A branch or expression cannot be reached based on preceding query logic.",
            [
                "Remove the unreachable branch.",
                "Check whether a condition was inverted accidentally."
            ],
            "Core Spec - Expressions");

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
            DiagnosticCode.MQ5025_ImpossibleImplicitConversion,
            "A constant selected for an implicit temporal conversion cannot be parsed by the target type, so the comparison will never match.",
            ["Use a valid value for the target type.", "Use an explicit conversion or format when the text has a non-default representation."],
            "Core Spec - Type System");

    }
}

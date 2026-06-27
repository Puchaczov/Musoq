using System.Collections.Generic;
using static Musoq.Parser.Diagnostics.ErrorMetadataCatalog;

namespace Musoq.Parser.Diagnostics;

internal static partial class SemanticErrorMetadataCatalog
{
    private static IEnumerable<ErrorMetadata> BuildAsOfJoinMetadata()
    {
        yield return Entry(
            DiagnosticCode.MQ3036_AsOfJoinMissingInequality,
            "An ASOF JOIN requires exactly one inequality condition to identify the nearest match.",
            [
                "Add one inequality predicate between the left and right sources.",
                "Keep equality predicates for partitioning and one inequality for the as-of ordering."
            ],
            "Core Spec - ASOF JOIN");

        yield return Entry(
            DiagnosticCode.MQ3037_AsOfJoinMultipleInequalities,
            "An ASOF JOIN contains more than one inequality condition.",
            [
                "Keep only the inequality that defines the as-of ordering.",
                "Move additional range checks to WHERE when they are row filters."
            ],
            "Core Spec - ASOF JOIN");

        yield return Entry(
            DiagnosticCode.MQ3038_AsOfJoinOrNotSupported,
            "The ASOF JOIN ON clause does not support OR conditions.",
            [
                "Rewrite the join condition using AND predicates.",
                "Split OR alternatives into separate queries when needed."
            ],
            "Core Spec - ASOF JOIN");

        yield return Entry(
            DiagnosticCode.MQ3039_AsOfJoinInequalityMustReferenceBothSides,
            "The ASOF JOIN inequality must compare values from the left and right sources.",
            [
                "Reference one column from each side of the join in the inequality.",
                "Use source aliases to make each side explicit."
            ],
            "Core Spec - ASOF JOIN");

        yield return Entry(
            DiagnosticCode.MQ3040_AsOfJoinInequalityColumnNotOrderable,
            "The ASOF JOIN inequality uses a column type that cannot be ordered.",
            [
                "Use a numeric, date, or other comparable column for the as-of inequality.",
                "Convert the column to an orderable type before joining."
            ],
            "Core Spec - ASOF JOIN");
    }
}

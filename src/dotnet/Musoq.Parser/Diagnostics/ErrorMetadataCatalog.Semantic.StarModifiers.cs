using System.Collections.Generic;
using static Musoq.Parser.Diagnostics.ErrorMetadataCatalog;

namespace Musoq.Parser.Diagnostics;

internal static partial class SemanticErrorMetadataCatalog
{
    private static IEnumerable<ErrorMetadata> BuildStarModifierMetadata()
    {
        yield return Entry(
            DiagnosticCode.MQ3041_StarExcludeColumnNotFound,
            "A star EXCLUDE modifier references a column that is not present in the source shape.",
            [
                "Check the excluded column name for typos.",
                "Remove columns from EXCLUDE that are not returned by the source."
            ],
            "Core Spec - Star Modifiers");

        yield return Entry(
            DiagnosticCode.MQ3042_StarReplaceColumnNotFound,
            "A star REPLACE modifier targets a column that is not present in the source shape.",
            [
                "Check the replacement target column name for typos.",
                "Use REPLACE only for columns produced by the star expansion."
            ],
            "Core Spec - Star Modifiers");

        yield return Entry(
            DiagnosticCode.MQ3043_StarExcludeRemovesAllColumns,
            "A star EXCLUDE or LIKE modifier would remove every column from the projection.",
            [
                "Leave at least one column in the star expansion.",
                "Use explicit projections when the intended shape is small."
            ],
            "Core Spec - Star Modifiers");

        yield return Entry(
            DiagnosticCode.MQ3044_StarColumnInBothExcludeAndReplace,
            "The same star-expanded column appears in both EXCLUDE and REPLACE modifiers.",
            [
                "Remove the column from EXCLUDE if it should be replaced.",
                "Remove the replacement if the column should be excluded."
            ],
            "Core Spec - Star Modifiers");

        yield return Entry(
            DiagnosticCode.MQ3045_StarLikeMatchedNoColumns,
            "A star LIKE pattern did not match any columns in the source shape.",
            [
                "Check the LIKE pattern spelling.",
                "Use explicit column names if no wildcard match is intended."
            ],
            "Core Spec - Star Modifiers");

        yield return Entry(
            DiagnosticCode.MQ3046_StarExcludeDuplicateColumn,
            "The same column appears more than once in a star EXCLUDE list.",
            [
                "Remove the duplicate EXCLUDE entry.",
                "Keep each excluded column name only once."
            ],
            "Core Spec - Star Modifiers");

        yield return Entry(
            DiagnosticCode.MQ3047_StarReplaceDuplicateColumn,
            "The same column appears more than once in a star REPLACE list.",
            [
                "Remove the duplicate replacement.",
                "Combine the replacement logic into a single REPLACE expression."
            ],
            "Core Spec - Star Modifiers");

        yield return Entry(
            DiagnosticCode.MQ3048_StarReplaceTargetsRemovedColumn,
            "A star REPLACE modifier targets a column already removed by EXCLUDE or LIKE filtering.",
            [
                "Remove the column from EXCLUDE or adjust the LIKE pattern if it should be replaced.",
                "Remove the replacement if the column should stay removed."
            ],
            "Core Spec - Star Modifiers");

    }
}

using System.Collections.Generic;
using static Musoq.Parser.Diagnostics.ErrorMetadataCatalog;

namespace Musoq.Parser.Diagnostics;

internal static partial class SemanticErrorMetadataCatalog
{
    private static IEnumerable<ErrorMetadata> BuildStarRenameMetadata()
    {
        yield return Entry(
            DiagnosticCode.MQ3068_StarRenameDuplicateSource,
            "The same output column appears more than once as a star RENAME source.",
            [
                "Remove the duplicate RENAME entry.",
                "Keep each renamed output column name only once."
            ],
            "Core Spec - Star Modifiers");

        yield return Entry(
            DiagnosticCode.MQ3069_StarRenameDuplicateTarget,
            "A star RENAME modifier would produce duplicate output column names.",
            [
                "Choose unique RENAME target names.",
                "Avoid renaming a column to the name of another surviving star output."
            ],
            "Core Spec - Star Modifiers");

        yield return Entry(
            DiagnosticCode.MQ3070_StarRenameColumnNotFound,
            "A star RENAME modifier references a column that is not present after earlier star modifiers.",
            [
                "Check the RENAME source column name for typos.",
                "Rename only columns that remain after LIKE, EXCLUDE, and REPLACE."
            ],
            "Core Spec - Star Modifiers");
    }
}

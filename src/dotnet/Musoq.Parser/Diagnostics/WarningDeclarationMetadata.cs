using System.Collections.Generic;
using static Musoq.Parser.Diagnostics.ErrorMetadataCatalog;

namespace Musoq.Parser.Diagnostics;

internal static partial class WarningMetadataCatalog
{
    private static IEnumerable<ErrorMetadata> BuildDeclarationMetadata()
    {
        yield return Entry(
            DiagnosticCode.MQ5022_UnusedCte,
            "A user-declared CTE is not reachable from the outer query, so its work will not contribute to the result.",
            ["Remove the unused CTE.", "Reference the CTE from the outer query or from another live CTE."],
            "Core Spec - Common Table Expressions");

        yield return Entry(
            DiagnosticCode.MQ5023_UnusedScriptVariable,
            "A let variable is not used by an executable statement or by a live dependency chain.",
            ["Remove the unused variable.", "Reference it from a live query or variable initializer."],
            "Core Spec - Script Variables");
    }
}

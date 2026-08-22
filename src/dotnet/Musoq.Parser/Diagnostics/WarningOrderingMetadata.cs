using System.Collections.Generic;
using static Musoq.Parser.Diagnostics.ErrorMetadataCatalog;

namespace Musoq.Parser.Diagnostics;

internal static partial class WarningMetadataCatalog
{
    private static IEnumerable<ErrorMetadata> BuildOrderingMetadata()
    {
        yield return Entry(
            DiagnosticCode.MQ5020_SetOperationOrderByScope,
            "Compatibility metadata for the completed migration to result-level ordering after set operations. The current language version does not emit this advisory.",
            [
                "Use trailing ORDER BY for combined-result ordering.",
                "Wrap an intentionally ordered operand in a derived table or CTE."
            ],
            "Core Spec - Set Operations and ORDER BY");

        yield return Entry(
            DiagnosticCode.MQ5021_UnorderedSkip,
            "SKIP without ORDER BY selects an unspecified offset, so repeated executions may skip different rows.",
            [
                "Add ORDER BY on the same query before SKIP.",
                "Remove SKIP when row order is not part of the query contract."
            ],
            "Core Spec - ORDER BY and SKIP");

        yield return Entry(
            DiagnosticCode.MQ5026_SetOperationSliceScope,
            "Compatibility metadata for the completed migration to result-level slicing after set operations. The current language version does not emit this advisory.",
            [
                "Use trailing SKIP or TAKE for combined-result slicing.",
                "Wrap an intentionally sliced operand in a derived table or CTE."
            ],
            "Core Spec - Set Operations and SKIP/TAKE");
    }
}

using System.Collections.Generic;
using static Musoq.Parser.Diagnostics.ErrorMetadataCatalog;

namespace Musoq.Parser.Diagnostics;

internal static partial class WarningMetadataCatalog
{
    private static IEnumerable<ErrorMetadata> BuildOrderingMetadata()
    {
        yield return Entry(
            DiagnosticCode.MQ5020_SetOperationOrderByScope,
            "An ORDER BY attached to the rightmost operand of a set operation orders that operand before combination, not the combined result.",
            [
                "Move ORDER BY to a query that consumes the combined set.",
                "Keep branch-local ORDER BY only when it is paired with TAKE or positive SKIP for intentional slicing."
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
    }
}

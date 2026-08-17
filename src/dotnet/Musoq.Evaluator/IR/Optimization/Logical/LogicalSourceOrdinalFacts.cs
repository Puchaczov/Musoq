using System.Collections.Generic;
using System.Globalization;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Optimization.Logical;

internal static class LogicalSourceOrdinalFacts
{
    public static IEnumerable<int> CollectSchemaSourceOrdinals(LogicalNode node)
    {
        if (node is SchemaScanNode scan && TryParseSourceContextOrdinal(scan.SourceContextId, out var ordinal))
            yield return ordinal;

        foreach (var child in node.Children)
        {
            foreach (var childOrdinal in CollectSchemaSourceOrdinals(child))
                yield return childOrdinal;
        }
    }

    public static bool TryParseSourceContextOrdinal(string? sourceContextId, out int ordinal)
    {
        ordinal = 0;
        if (string.IsNullOrWhiteSpace(sourceContextId))
            return false;

        var separatorIndex = sourceContextId.LastIndexOf(':');
        var ordinalText = separatorIndex >= 0
            ? sourceContextId[(separatorIndex + 1)..]
            : sourceContextId;

        return int.TryParse(
            ordinalText,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out ordinal);
    }
}


using System.Collections.Generic;
using System.Linq;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static ExecutionAppendRow NormalizeLazyContextSegments(ExecutionAppendRow appendRow)
    {
        return TryExpandSingleContextArray(
            appendRow.Contexts,
            appendRow.ContextLayout,
            out var contexts,
            out var contextLayout)
            ? appendRow with { Contexts = contexts, ContextLayout = contextLayout }
            : appendRow;
    }

    private static ExecutionCreateGeneratedRow NormalizeLazyContextSegments(ExecutionCreateGeneratedRow createRow)
    {
        return TryExpandSingleContextArray(
            createRow.Contexts,
            createRow.ContextLayout,
            out var contexts,
            out var contextLayout)
            ? createRow with { Contexts = contexts, ContextLayout = contextLayout }
            : createRow;
    }

    private static bool TryExpandSingleContextArray(
        IReadOnlyList<ExecutionExpression> contexts,
        ExecutionContextLayout? contextLayout,
        out ExecutionExpression[] expandedContexts,
        out ExecutionContextLayout expandedLayout)
    {
        expandedContexts = [];
        expandedLayout = new ExecutionContextLayout([]);

        if (contexts is not [ExecutionContextArray contextArray] ||
            contextLayout?.Segments is not [var contextArraySegment] ||
            contextArraySegment.Kind != ExecutionContextSegmentKind.Array ||
            !Equals(contextArraySegment.Value, contextArray) ||
            contextArray.Segments.Any(static segment =>
                segment is not { Kind: ExecutionContextSegmentKind.Single, Count: 1 }))
        {
            return false;
        }

        expandedContexts = contextArray.Segments.Select(static segment => segment.Value).ToArray();
        expandedLayout = new ExecutionContextLayout(contextArray.Segments);
        return true;
    }
}

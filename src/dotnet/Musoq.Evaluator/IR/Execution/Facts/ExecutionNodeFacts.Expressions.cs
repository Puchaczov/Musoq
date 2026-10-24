using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution.Facts;

internal static partial class ExecutionNodeFacts
{
    internal static IEnumerable<ExecutionExpression> GetContextLayoutExpressions(ExecutionContextLayout? contextLayout)
    {
        return contextLayout == null
            ? []
            : contextLayout.Segments.Select(static segment => segment.Value);
    }

    internal static IEnumerable<ExecutionExpression> GetLocalExpressions(ExecutionNode node) => node switch
    {
        ExecutionSourceScan scan => scan.Binding.Arguments,
        ExecutionInterpretSource interpret => interpret.Arguments,
        ExecutionGetOrAddValueTupleAggregateGroup getOrAdd => getOrAdd.Keys,
        ExecutionReturnDesc description => description.Arguments,
        _ => MaterializeSmallLocalExpressions(node)
    };

    private static IEnumerable<ExecutionExpression> MaterializeSmallLocalExpressions(ExecutionNode node)
    {
        var first = GetLocalExpressionAt(node, 0);
        if (first is null)
            return [];
        var second = GetLocalExpressionAt(node, 1);
        if (second is null)
            return [first];
        var third = GetLocalExpressionAt(node, 2);
        if (third is null)
            return [first, second];
        if (GetLocalExpressionAt(node, 3) is null)
            return [first, second, third];
        return EnumerateLocalExpressions(node);
    }

    private static IEnumerable<ExecutionExpression> EnumerateLocalExpressions(ExecutionNode node)
    {
        var cursor = new ExecutionLocalExpressionCursor(node);
        while (cursor.MoveNext())
            yield return cursor.Current;
    }
}

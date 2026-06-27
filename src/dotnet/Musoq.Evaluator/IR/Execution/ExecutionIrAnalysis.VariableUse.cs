using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Execution.Facts;

namespace Musoq.Evaluator.IR.Execution;

internal static partial class ExecutionIrAnalysis
{
    internal static bool IsVariableUsedAfter(
        IReadOnlyList<ExecutionNode> nodes,
        int index,
        string variableName)
    {
        for (var nextIndex = index + 1; nextIndex < nodes.Count; nextIndex++)
        {
            if (ContainsVariableUse(nodes[nextIndex], variableName))
                return true;
        }

        return false;
    }

    internal static bool ContainsVariableUse(ExecutionBlock block, string variableName)
    {
        return block.Nodes.Any(node => ContainsVariableUse(node, variableName));
    }

    internal static bool ContainsVariableUse(ExecutionNode node, string variableName)
    {
        return ExecutionNodeFacts.GetDirectVariableReferences(node).Any(variable => HasName(variable, variableName)) ||
               ContainsVariableUse(GetNodeExpressions(node), variableName) ||
               GetChildBlocks(node).Any(block => ContainsVariableUse(block, variableName));
    }

    internal static bool ContainsVariableUse(IEnumerable<ExecutionExpression> expressions, string variableName)
    {
        return FlattenExpressions(expressions)
            .Any(expression => ExpressionUsesVariable(expression, variableName));
    }

    internal static bool ContainsVariableUse(ExecutionExpression? expression, string variableName)
    {
        return FlattenExpressions(expression)
            .Any(current => ExpressionUsesVariable(current, variableName));
    }

    private static bool ExpressionUsesVariable(ExecutionExpression expression, string variableName)
    {
        return expression switch
        {
            ExecutionVariableRead variableRead => HasName(variableRead.Variable, variableName),
            ExecutionRowStream rows => HasName(rows.Variable, variableName),
            ExecutionScalarRowStream rows => HasName(rows.Variable, variableName),
            ExecutionMethodCall methodCall => methodCall.Target != null && HasName(methodCall.Target, variableName) ||
                                              methodCall.Cache != null && HasName(methodCall.Cache, variableName),
            ExecutionMethodTargetReuseCandidate candidate => ExpressionUsesVariable(candidate.MethodCall, variableName),
            ExecutionStrictCast strictCast => strictCast.Target != null && HasName(strictCast.Target, variableName),
            ExecutionIndexedHashRowCreate indexedCreate =>
                HasName(indexedCreate.Row, variableName) ||
                HasName(indexedCreate.Index, variableName),
            ExecutionIndexedHashRowRowRead rowRead => HasName(rowRead.IndexedRow, variableName),
            ExecutionIndexedHashRowIndexRead indexRead => HasName(indexRead.IndexedRow, variableName),
            ExecutionRowContextsRead rowContexts => HasName(rowContexts.Row, variableName),
            ExecutionWindowValueRead windowValue =>
                HasName(windowValue.Results, variableName) ||
                HasName(windowValue.Index, variableName),
            ExecutionAggregateCall aggregateCall => HasName(aggregateCall.Group, variableName),
            ExecutionGroupKeyRead groupKey => HasName(groupKey.Group, variableName),
            ExecutionAggregateCapturedValueRead capturedValue => HasName(capturedValue.Group, variableName),
            _ => false
        };
    }

    private static bool HasName(ExecutionVariable variable, string variableName)
    {
        return string.Equals(variable.Name, variableName, StringComparison.Ordinal);
    }
}

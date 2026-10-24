using System.Collections.Generic;
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
        var visitor = new VariableUseNodeVisitor(variableName);
        return !ExecutionIrTraversal.VisitNodes(block, ref visitor);
    }

    internal static bool ContainsVariableUse(ExecutionNode node, string variableName)
    {
        var visitor = new VariableUseNodeVisitor(variableName);
        return !ExecutionIrTraversal.VisitNode(node, ref visitor);
    }

    internal static bool ContainsVariableUse(IEnumerable<ExecutionExpression> expressions, string variableName)
    {
        foreach (var expression in expressions)
            if (ContainsVariableUse(expression, variableName))
                return true;
        return false;
    }

    internal static bool ContainsVariableUse(ExecutionExpression? expression, string variableName)
    {
        var visitor = new VariableUseExpressionVisitor(variableName);
        return !ExecutionIrTraversal.VisitExpressions(expression, ref visitor);
    }

    private readonly struct VariableUseExpressionVisitor(string name) : IExecutionExpressionVisitor
    {
        public bool Visit(ExecutionExpression expression) => !ExpressionUsesVariable(expression, name);
    }

    private readonly struct VariableUseNodeVisitor(string name) : IExecutionNodeVisitor
    {
        public bool Visit(ExecutionNode node)
        {
            if (ExecutionNodeFacts.HasDirectVariableUse(node, name))
                return false;
            var expressions = new ExecutionLocalExpressionCursor(node);
            while (expressions.MoveNext())
                if (ContainsVariableUse(expressions.Current, name))
                    return false;
            return true;
        }
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

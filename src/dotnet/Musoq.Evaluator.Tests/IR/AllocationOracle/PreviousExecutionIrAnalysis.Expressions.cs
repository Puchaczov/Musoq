using System.Collections.Generic;
using Musoq.Evaluator.IR.Execution.Facts;

namespace Musoq.Evaluator.IR.Execution;

internal static partial class PreviousExecutionIrAnalysis
{

    internal static IEnumerable<ExecutionExpression> FlattenExpressions(ExecutionBlock block)
    {
        foreach (var node in FlattenNodes(block))
        {
            foreach (var expression in GetNodeExpressions(node))
            {
                foreach (var childExpression in FlattenExpressions(expression))
                    yield return childExpression;
            }
        }
    }

    internal static IEnumerable<ExecutionExpression> FlattenExpressions(IEnumerable<ExecutionExpression> expressions)
    {
        foreach (var expression in expressions)
        {
            foreach (var childExpression in FlattenExpressions(expression))
                yield return childExpression;
        }
    }

    internal static IEnumerable<ExecutionExpression> FlattenExpressions(ExecutionExpression? expression)
    {
        if (expression == null)
            yield break;

        yield return expression;

        foreach (var child in GetChildExpressions(expression))
        {
            foreach (var childExpression in FlattenExpressions(child))
                yield return childExpression;
        }
    }

    internal static IEnumerable<ExecutionExpression> GetNodeExpressions(ExecutionNode node)
    {
        return PreviousExecutionNodeFacts.GetLocalExpressions(node);
    }

    internal static IEnumerable<ExecutionExpression> GetChildExpressions(ExecutionExpression expression) =>
        PreviousExpressionChildren.Get(expression);
}

using System.Collections.Generic;
using Musoq.Evaluator.IR.Execution.Facts;

namespace Musoq.Evaluator.IR.Execution;
internal static partial class ExecutionIrAnalysis
{
    internal static IEnumerable<ExecutionExpression> GetContextLayoutExpressions(ExecutionContextLayout? contextLayout) =>
        ExecutionNodeFacts.GetContextLayoutExpressions(contextLayout);

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

    internal static IEnumerable<TExpression> CollectExpressions<TExpression>(ExecutionBlock block)
        where TExpression : ExecutionExpression
    {
        foreach (var expression in FlattenExpressions(block))
        {
            if (expression is TExpression match)
                yield return match;
        }
    }

    internal static IEnumerable<ExecutionExpression> GetNodeExpressions(ExecutionNode node)
    {
        return ExecutionNodeFacts.GetLocalExpressions(node);
    }

    internal static IEnumerable<ExecutionExpression> GetChildExpressions(ExecutionExpression expression) =>
        ExecutionExpressionChildren.Get(expression);
}

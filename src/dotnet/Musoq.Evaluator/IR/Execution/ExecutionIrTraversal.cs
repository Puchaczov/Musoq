using System.Threading;
using Musoq.Evaluator.IR.Execution.Facts;

namespace Musoq.Evaluator.IR.Execution;

internal interface IExecutionNodeVisitor
{
    bool Visit(ExecutionNode node);
}

internal interface IExecutionExpressionVisitor
{
    bool Visit(ExecutionExpression expression);
}

/// <summary>Preorder traversal with borrowed child cursors and unboxed, caller-owned visitor state.</summary>
internal static partial class ExecutionIrTraversal
{
    internal static bool VisitNodes<TVisitor>(ExecutionBlock block, ref TVisitor visitor,
        CancellationToken cancellationToken = default) where TVisitor : struct, IExecutionNodeVisitor
    {
        cancellationToken.ThrowIfCancellationRequested();
        for (var index = 0; index < block.Nodes.Count; index++)
            if (!VisitNode(block.Nodes[index], ref visitor, cancellationToken))
                return false;
        return true;
    }

    internal static bool VisitNode<TVisitor>(ExecutionNode node, ref TVisitor visitor,
        CancellationToken cancellationToken = default) where TVisitor : struct, IExecutionNodeVisitor
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!visitor.Visit(node))
            return false;
        var children = new ExecutionNodeChildCursor(node);
        while (children.MoveNext())
            if (!VisitNode(children.Current, ref visitor, cancellationToken))
                return false;
        return true;
    }

    internal static bool VisitExpressions<TVisitor>(ExecutionExpression? expression, ref TVisitor visitor,
        CancellationToken cancellationToken = default) where TVisitor : struct, IExecutionExpressionVisitor
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (expression is null)
            return true;
        if (!visitor.Visit(expression))
            return false;
        var children = new ExecutionExpressionChildCursor(expression);
        while (children.MoveNext())
            if (!VisitExpressions(children.Current, ref visitor, cancellationToken))
                return false;
        return true;
    }

    internal static bool VisitExpressions<TVisitor>(ExecutionBlock block, ref TVisitor visitor,
        CancellationToken cancellationToken = default) where TVisitor : struct, IExecutionExpressionVisitor
    {
        cancellationToken.ThrowIfCancellationRequested();
        for (var index = 0; index < block.Nodes.Count; index++)
            if (!VisitNodeExpressions(block.Nodes[index], ref visitor, cancellationToken))
                return false;
        return true;
    }

    private static bool VisitNodeExpressions<TVisitor>(ExecutionNode node, ref TVisitor visitor,
        CancellationToken cancellationToken) where TVisitor : struct, IExecutionExpressionVisitor
    {
        cancellationToken.ThrowIfCancellationRequested();
        var expressions = new ExecutionLocalExpressionCursor(node);
        while (expressions.MoveNext())
            if (!VisitExpressions(expressions.Current, ref visitor, cancellationToken))
                return false;
        var children = new ExecutionNodeChildCursor(node);
        while (children.MoveNext())
            if (!VisitNodeExpressions(children.Current, ref visitor, cancellationToken))
                return false;
        return true;
    }
}

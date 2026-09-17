using System.Collections.Generic;
using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers;

/// <summary>
///     Helper class for handling comparison operations in the RewriteQueryVisitor.
///     Provides common implementation for comparison operations that follow the pattern:
///     pop right, pop left, push new comparison node.
/// </summary>
public static class ComparisonOperationVisitorHelper
{
    /// <summary>Processes an Equality operation.</summary>
    public static void ProcessEqualityOperation(Stack<Node> nodes, TextSpan span = default) =>
        ProcessComparisonOperation(nodes, (left, right) => new EqualityNode(left, right), span);

    /// <summary>Processes a GreaterOrEqual operation.</summary>
    public static void ProcessGreaterOrEqualOperation(Stack<Node> nodes, TextSpan span = default) =>
        ProcessComparisonOperation(nodes, (left, right) => new GreaterOrEqualNode(left, right), span);

    /// <summary>Processes a LessOrEqual operation.</summary>
    public static void ProcessLessOrEqualOperation(Stack<Node> nodes, TextSpan span = default) =>
        ProcessComparisonOperation(nodes, (left, right) => new LessOrEqualNode(left, right), span);

    /// <summary>Processes a Greater operation.</summary>
    public static void ProcessGreaterOperation(Stack<Node> nodes, TextSpan span = default) =>
        ProcessComparisonOperation(nodes, (left, right) => new GreaterNode(left, right), span);

    /// <summary>Processes a Less operation.</summary>
    public static void ProcessLessOperation(Stack<Node> nodes, TextSpan span = default) =>
        ProcessComparisonOperation(nodes, (left, right) => new LessNode(left, right), span);

    /// <summary>Processes a Diff operation.</summary>
    public static void ProcessDiffOperation(Stack<Node> nodes, TextSpan span = default) =>
        ProcessComparisonOperation(nodes, (left, right) => new DiffNode(left, right), span);

    /// <summary>Processes a Like operation.</summary>
    public static void ProcessLikeOperation(Stack<Node> nodes, TextSpan span = default) =>
        ProcessComparisonOperation(nodes, (left, right) => new LikeNode(left, right), span);

    /// <summary>Processes an RLike operation.</summary>
    public static void ProcessRLikeOperation(Stack<Node> nodes, TextSpan span = default) =>
        ProcessComparisonOperation(nodes, (left, right) => new RLikeNode(left, right), span);

    private static void ProcessComparisonOperation(Stack<Node> nodes, Func<Node, Node, Node> nodeFactory, TextSpan span)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        if (nodes.Count < 2)
            throw new InvalidOperationException("Stack must contain at least 2 nodes for binary operation");

        var right = nodes.Pop();
        var left = nodes.Pop();

        if (left == null)
            throw new ArgumentException("Left operand cannot be null");
        if (right == null)
            throw new ArgumentException("Right operand cannot be null");

        nodes.Push(nodeFactory(left, right).WithSpan(span));
    }
}

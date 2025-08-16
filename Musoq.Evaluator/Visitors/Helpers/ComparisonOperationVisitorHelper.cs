using System.Collections.Generic;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers;

/// <summary>
/// Helper class for handling comparison operations in the RewriteQueryVisitor.
/// Provides common implementation for comparison operations that follow the pattern:
/// pop right, pop left, push new comparison node.
/// </summary>
public static class ComparisonOperationVisitorHelper
{
    /// <summary>
    /// Processes an Equality operation.
    /// </summary>
    /// <param name="nodes">The node stack.</param>
    public static void ProcessEqualityOperation(Stack<Node> nodes)
    {
        var right = nodes.Pop();
        var left = nodes.Pop();
        nodes.Push(new EqualityNode(left, right));
    }

    /// <summary>
    /// Processes a GreaterOrEqual operation.
    /// </summary>
    /// <param name="nodes">The node stack.</param>
    public static void ProcessGreaterOrEqualOperation(Stack<Node> nodes)
    {
        var right = nodes.Pop();
        var left = nodes.Pop();
        nodes.Push(new GreaterOrEqualNode(left, right));
    }

    /// <summary>
    /// Processes a LessOrEqual operation.
    /// </summary>
    /// <param name="nodes">The node stack.</param>
    public static void ProcessLessOrEqualOperation(Stack<Node> nodes)
    {
        var right = nodes.Pop();
        var left = nodes.Pop();
        nodes.Push(new LessOrEqualNode(left, right));
    }

    /// <summary>
    /// Processes a Greater operation.
    /// </summary>
    /// <param name="nodes">The node stack.</param>
    public static void ProcessGreaterOperation(Stack<Node> nodes)
    {
        var right = nodes.Pop();
        var left = nodes.Pop();
        nodes.Push(new GreaterNode(left, right));
    }

    /// <summary>
    /// Processes a Less operation.
    /// </summary>
    /// <param name="nodes">The node stack.</param>
    public static void ProcessLessOperation(Stack<Node> nodes)
    {
        var right = nodes.Pop();
        var left = nodes.Pop();
        nodes.Push(new LessNode(left, right));
    }

    /// <summary>
    /// Processes a Diff operation.
    /// </summary>
    /// <param name="nodes">The node stack.</param>
    public static void ProcessDiffOperation(Stack<Node> nodes)
    {
        var right = nodes.Pop();
        var left = nodes.Pop();
        nodes.Push(new DiffNode(left, right));
    }

    /// <summary>
    /// Processes a Like operation.
    /// </summary>
    /// <param name="nodes">The node stack.</param>
    public static void ProcessLikeOperation(Stack<Node> nodes)
    {
        var right = nodes.Pop();
        var left = nodes.Pop();
        nodes.Push(new LikeNode(left, right));
    }

    /// <summary>
    /// Processes an RLike operation.
    /// </summary>
    /// <param name="nodes">The node stack.</param>
    public static void ProcessRLikeOperation(Stack<Node> nodes)
    {
        var right = nodes.Pop();
        var left = nodes.Pop();
        nodes.Push(new RLikeNode(left, right));
    }
}
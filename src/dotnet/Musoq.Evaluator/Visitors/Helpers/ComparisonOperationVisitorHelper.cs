using System;
using System.Collections.Generic;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers;

/// <summary>
///     Helper class for handling comparison operations in the RewriteQueryVisitor.
///     Provides common implementation for comparison operations that follow the pattern:
///     pop right, pop left, push new comparison node.
/// </summary>
public static class ComparisonOperationVisitorHelper
{
    /// <summary>
    ///     Processes an Equality operation.
    /// </summary>
    /// <param name="nodes">The node stack.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when stack has insufficient nodes.</exception>
    /// <exception cref="ArgumentException">Thrown when popped nodes are null.</exception>
    public static void ProcessEqualityOperation(Stack<Node> nodes)
    {
        ValidateBinaryOperation(nodes);
        var right = nodes.Pop();
        var left = nodes.Pop();
        ValidateOperands(left, right);
        nodes.Push(new EqualityNode(left, right));
    }

    /// <summary>
    ///     Processes a GreaterOrEqual operation.
    /// </summary>
    /// <param name="nodes">The node stack.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when stack has insufficient nodes.</exception>
    /// <exception cref="ArgumentException">Thrown when popped nodes are null.</exception>
    public static void ProcessGreaterOrEqualOperation(Stack<Node> nodes)
    {
        ValidateBinaryOperation(nodes);
        var right = nodes.Pop();
        var left = nodes.Pop();
        ValidateOperands(left, right);
        nodes.Push(new GreaterOrEqualNode(left, right));
    }

    /// <summary>
    ///     Processes a LessOrEqual operation.
    /// </summary>
    /// <param name="nodes">The node stack.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when stack has insufficient nodes.</exception>
    /// <exception cref="ArgumentException">Thrown when popped nodes are null.</exception>
    public static void ProcessLessOrEqualOperation(Stack<Node> nodes)
    {
        ValidateBinaryOperation(nodes);
        var right = nodes.Pop();
        var left = nodes.Pop();
        ValidateOperands(left, right);
        nodes.Push(new LessOrEqualNode(left, right));
    }

    /// <summary>
    ///     Processes a Greater operation.
    /// </summary>
    /// <param name="nodes">The node stack.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when stack has insufficient nodes.</exception>
    /// <exception cref="ArgumentException">Thrown when popped nodes are null.</exception>
    public static void ProcessGreaterOperation(Stack<Node> nodes)
    {
        ValidateBinaryOperation(nodes);
        var right = nodes.Pop();
        var left = nodes.Pop();
        ValidateOperands(left, right);
        nodes.Push(new GreaterNode(left, right));
    }

    /// <summary>
    ///     Processes a Less operation.
    /// </summary>
    /// <param name="nodes">The node stack.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when stack has insufficient nodes.</exception>
    /// <exception cref="ArgumentException">Thrown when popped nodes are null.</exception>
    public static void ProcessLessOperation(Stack<Node> nodes)
    {
        ValidateBinaryOperation(nodes);
        var right = nodes.Pop();
        var left = nodes.Pop();
        ValidateOperands(left, right);
        nodes.Push(new LessNode(left, right));
    }

    /// <summary>
    ///     Processes a Diff operation.
    /// </summary>
    /// <param name="nodes">The node stack.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when stack has insufficient nodes.</exception>
    /// <exception cref="ArgumentException">Thrown when popped nodes are null.</exception>
    public static void ProcessDiffOperation(Stack<Node> nodes)
    {
        ValidateBinaryOperation(nodes);
        var right = nodes.Pop();
        var left = nodes.Pop();
        ValidateOperands(left, right);
        nodes.Push(new DiffNode(left, right));
    }

    /// <summary>
    ///     Processes a Like operation.
    /// </summary>
    /// <param name="nodes">The node stack.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when stack has insufficient nodes.</exception>
    /// <exception cref="ArgumentException">Thrown when popped nodes are null.</exception>
    public static void ProcessLikeOperation(Stack<Node> nodes)
    {
        ValidateBinaryOperation(nodes);
        var right = nodes.Pop();
        var left = nodes.Pop();
        ValidateOperands(left, right);
        nodes.Push(new LikeNode(left, right));
    }

    /// <summary>
    ///     Processes an RLike operation.
    /// </summary>
    /// <param name="nodes">The node stack.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when stack has insufficient nodes.</exception>
    /// <exception cref="ArgumentException">Thrown when popped nodes are null.</exception>
    public static void ProcessRLikeOperation(Stack<Node> nodes)
    {
        ValidateBinaryOperation(nodes);
        var right = nodes.Pop();
        var left = nodes.Pop();
        ValidateOperands(left, right);
        nodes.Push(new RLikeNode(left, right));
    }

    private static void ValidateBinaryOperation(Stack<Node> nodes)
    {
        if (nodes == null)
            throw new ArgumentNullException(nameof(nodes));

        if (nodes.Count < 2)
            throw new InvalidOperationException("Stack must contain at least 2 nodes for binary operation");
    }

    private static void ValidateOperands(Node left, Node right)
    {
        if (left == null)
            throw new ArgumentException("Left operand cannot be null");
        if (right == null)
            throw new ArgumentException("Right operand cannot be null");
    }
}

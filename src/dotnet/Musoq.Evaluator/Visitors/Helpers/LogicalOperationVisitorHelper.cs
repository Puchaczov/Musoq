using System.Collections.Generic;
using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers;

/// <summary>
///     Helper class for handling logical operations in the RewriteQueryVisitor.
///     Provides common implementation for logical operations with nullable boolean expression rewriting.
/// </summary>
public static class LogicalOperationVisitorHelper
{
    /// <summary>Processes an And operation with nullable boolean expression rewriting.</summary>
    public static void ProcessAndOperation(Stack<Node> nodes, Func<Node, Node> rewriteNullableBoolExpressions,
        TextSpan span = default)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(rewriteNullableBoolExpressions);
        ProcessLogicalBinaryOperation(nodes, rewriteNullableBoolExpressions, (left, right) => new AndNode(left, right), span);
    }

    /// <summary>Processes an Or operation with nullable boolean expression rewriting.</summary>
    public static void ProcessOrOperation(Stack<Node> nodes, Func<Node, Node> rewriteNullableBoolExpressions,
        TextSpan span = default)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(rewriteNullableBoolExpressions);
        ProcessLogicalBinaryOperation(nodes, rewriteNullableBoolExpressions, (left, right) => new OrNode(left, right), span);
    }

    private static void ProcessLogicalBinaryOperation(
        Stack<Node> nodes,
        Func<Node, Node> rewriteNullableBoolExpressions,
        Func<Node, Node, Node> nodeFactory,
        TextSpan span)
    {
        ValidateBinaryOperation(nodes);
        ArgumentNullException.ThrowIfNull(rewriteNullableBoolExpressions);

        var rightRaw = nodes.Pop();
        var leftRaw = nodes.Pop();

        ValidateOperands(leftRaw, rightRaw);

        var right = rewriteNullableBoolExpressions(rightRaw);
        var left = rewriteNullableBoolExpressions(leftRaw);
        nodes.Push(nodeFactory(left, right).WithSpan(span));
    }

    /// <summary>
    ///     Processes a Not operation.
    /// </summary>
    /// <param name="nodes">The node stack.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when stack is empty.</exception>
    /// <exception cref="ArgumentException">Thrown when popped node is null.</exception>
    public static void ProcessNotOperation(Stack<Node> nodes, TextSpan span = default)
    {
        ValidateUnaryOperation(nodes);
        var operand = nodes.Pop();

        if (operand == null)
            throw new ArgumentException("Operand cannot be null");

        nodes.Push(new NotNode(operand).WithSpan(span));
    }

    /// <summary>
    ///     Processes a Contains operation.
    /// </summary>
    /// <param name="nodes">The node stack.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when stack has insufficient nodes.</exception>
    /// <exception cref="ArgumentException">Thrown when popped nodes are null or right operand is not ArgsListNode.</exception>
    public static void ProcessContainsOperation(Stack<Node> nodes, TextSpan span = default)
    {
        ValidateBinaryOperation(nodes);
        var right = nodes.Pop();
        var left = nodes.Pop();

        ValidateOperands(left, right);

        if (!(right is ArgsListNode argsListNode))
            throw new ArgumentException("Right operand must be an ArgsListNode for Contains operation");

        nodes.Push(new ContainsNode(left, argsListNode).WithSpan(span));
    }

    /// <summary>
    ///     Processes an IsNull operation.
    /// </summary>
    /// <param name="nodes">The node stack.</param>
    /// <param name="isNegated">Whether the IsNull operation is negated.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when stack is empty.</exception>
    /// <exception cref="ArgumentException">Thrown when popped node is null.</exception>
    public static void ProcessIsNullOperation(Stack<Node> nodes, bool isNegated, TextSpan span = default)
    {
        ValidateUnaryOperation(nodes);
        var operand = nodes.Pop();

        if (operand == null)
            throw new ArgumentException("Operand cannot be null");

        nodes.Push(new IsNullNode(operand, isNegated).WithSpan(span));
    }

    internal const int ContainsThreshold = 5;

    /// <summary>
    ///     Retains SQL membership as a distinct operation regardless of list size.
    /// </summary>
    /// <param name="nodes">The node stack.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when stack has insufficient nodes.</exception>
    /// <exception cref="ArgumentException">Thrown when popped nodes are null or right operand is not ArgsListNode.</exception>
    public static void ProcessInOperation(Stack<Node> nodes, TextSpan span = default)
    {
        ValidateBinaryOperation(nodes);
        var rightRaw = nodes.Pop();
        var left = nodes.Pop();

        ValidateOperands(left, rightRaw);

        if (rightRaw is not ArgsListNode right)
            throw new ArgumentException("Right operand must be an ArgsListNode for In operation");

        if (right.Args == null)
            throw new ArgumentException("ArgsListNode arguments cannot be null");

        nodes.Push(new InNode(left, right).WithSpan(span));
    }

    private static void ValidateBinaryOperation(Stack<Node> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        if (nodes.Count < 2)
            throw new InvalidOperationException("Stack must contain at least 2 nodes for binary operation");
    }

    private static void ValidateUnaryOperation(Stack<Node> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        if (nodes.Count < 1)
            throw new InvalidOperationException("Stack must contain at least 1 node for unary operation");
    }

    private static void ValidateOperands(Node left, Node right)
    {
        if (left == null)
            throw new ArgumentException("Left operand cannot be null");
        if (right == null)
            throw new ArgumentException("Right operand cannot be null");
    }
}

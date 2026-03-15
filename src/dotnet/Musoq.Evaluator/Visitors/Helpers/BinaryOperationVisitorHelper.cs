using System;
using System.Collections.Generic;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers;

/// <summary>
///     Helper class for handling binary arithmetic operations in the RewriteQueryVisitor.
///     Provides common implementation for operations that follow the pattern:
///     pop right, pop left, push new operation node.
/// </summary>
public static class BinaryOperationVisitorHelper
{
    /// <summary>
    ///     Processes a binary operation by popping two nodes, validating, and pushing a new node.
    /// </summary>
    /// <param name="nodes">The node stack.</param>
    /// <param name="nodeFactory">Factory to create the resulting node from left and right operands.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when stack has insufficient nodes.</exception>
    /// <exception cref="ArgumentException">Thrown when popped nodes are null.</exception>
    public static void ProcessBinaryOperation(Stack<Node> nodes, Func<Node, Node, Node> nodeFactory)
    {
        ValidateBinaryOperation(nodes);
        var right = nodes.Pop();
        var left = nodes.Pop();
        ValidateOperands(left, right);
        nodes.Push(nodeFactory(left, right));
    }

    /// <summary>Processes a Star operation.</summary>
    public static void ProcessStarOperation(Stack<Node> nodes) =>
        ProcessBinaryOperation(nodes, (left, right) => new StarNode(left, right));

    /// <summary>Processes a FSlash operation.</summary>
    public static void ProcessFSlashOperation(Stack<Node> nodes) =>
        ProcessBinaryOperation(nodes, (left, right) => new FSlashNode(left, right));

    /// <summary>Processes a Modulo operation.</summary>
    public static void ProcessModuloOperation(Stack<Node> nodes) =>
        ProcessBinaryOperation(nodes, (left, right) => new ModuloNode(left, right));

    /// <summary>Processes an Add operation.</summary>
    public static void ProcessAddOperation(Stack<Node> nodes) =>
        ProcessBinaryOperation(nodes, (left, right) => new AddNode(left, right));

    /// <summary>Processes a Hyphen operation.</summary>
    public static void ProcessHyphenOperation(Stack<Node> nodes) =>
        ProcessBinaryOperation(nodes, (left, right) => new HyphenNode(left, right));

    /// <summary>Processes a BitwiseAnd operation.</summary>
    public static void ProcessBitwiseAndOperation(Stack<Node> nodes) =>
        ProcessBinaryOperation(nodes, (left, right) => new BitwiseAndNode(left, right));

    /// <summary>Processes a BitwiseOr operation.</summary>
    public static void ProcessBitwiseOrOperation(Stack<Node> nodes) =>
        ProcessBinaryOperation(nodes, (left, right) => new BitwiseOrNode(left, right));

    /// <summary>Processes a BitwiseXor operation.</summary>
    public static void ProcessBitwiseXorOperation(Stack<Node> nodes) =>
        ProcessBinaryOperation(nodes, (left, right) => new BitwiseXorNode(left, right));

    /// <summary>Processes a LeftShift operation.</summary>
    public static void ProcessLeftShiftOperation(Stack<Node> nodes) =>
        ProcessBinaryOperation(nodes, (left, right) => new LeftShiftNode(left, right));

    /// <summary>Processes a RightShift operation.</summary>
    public static void ProcessRightShiftOperation(Stack<Node> nodes) =>
        ProcessBinaryOperation(nodes, (left, right) => new RightShiftNode(left, right));

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

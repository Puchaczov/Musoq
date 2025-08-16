using System;
using System.Collections.Generic;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers;

/// <summary>
/// Helper class for handling binary arithmetic operations in the RewriteQueryVisitor.
/// Provides common implementation for operations that follow the pattern:
/// pop right, pop left, push new operation node.
/// </summary>
public static class BinaryOperationVisitorHelper
{
    /// <summary>
    /// Processes a Star operation.
    /// </summary>
    /// <param name="nodes">The node stack.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when stack has insufficient nodes.</exception>
    /// <exception cref="ArgumentException">Thrown when popped nodes are null.</exception>
    public static void ProcessStarOperation(Stack<Node> nodes)
    {
        ValidateBinaryOperation(nodes);
        var right = nodes.Pop();
        var left = nodes.Pop();
        ValidateOperands(left, right);
        nodes.Push(new StarNode(left, right));
    }

    /// <summary>
    /// Processes a FSlash operation.
    /// </summary>
    /// <param name="nodes">The node stack.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when stack has insufficient nodes.</exception>
    /// <exception cref="ArgumentException">Thrown when popped nodes are null.</exception>
    public static void ProcessFSlashOperation(Stack<Node> nodes)
    {
        ValidateBinaryOperation(nodes);
        var right = nodes.Pop();
        var left = nodes.Pop();
        ValidateOperands(left, right);
        nodes.Push(new FSlashNode(left, right));
    }

    /// <summary>
    /// Processes a Modulo operation.
    /// </summary>
    /// <param name="nodes">The node stack.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when stack has insufficient nodes.</exception>
    /// <exception cref="ArgumentException">Thrown when popped nodes are null.</exception>
    public static void ProcessModuloOperation(Stack<Node> nodes)
    {
        ValidateBinaryOperation(nodes);
        var right = nodes.Pop();
        var left = nodes.Pop();
        ValidateOperands(left, right);
        nodes.Push(new ModuloNode(left, right));
    }

    /// <summary>
    /// Processes an Add operation.
    /// </summary>
    /// <param name="nodes">The node stack.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when stack has insufficient nodes.</exception>
    /// <exception cref="ArgumentException">Thrown when popped nodes are null.</exception>
    public static void ProcessAddOperation(Stack<Node> nodes)
    {
        ValidateBinaryOperation(nodes);
        var right = nodes.Pop();
        var left = nodes.Pop();
        ValidateOperands(left, right);
        nodes.Push(new AddNode(left, right));
    }

    /// <summary>
    /// Processes a Hyphen operation.
    /// </summary>
    /// <param name="nodes">The node stack.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when stack has insufficient nodes.</exception>
    /// <exception cref="ArgumentException">Thrown when popped nodes are null.</exception>
    public static void ProcessHyphenOperation(Stack<Node> nodes)
    {
        ValidateBinaryOperation(nodes);
        var right = nodes.Pop();
        var left = nodes.Pop();
        ValidateOperands(left, right);
        nodes.Push(new HyphenNode(left, right));
    }

    /// <summary>
    /// Validates that the stack is not null and has at least 2 nodes for binary operations.
    /// </summary>
    /// <param name="nodes">The node stack to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when stack has insufficient nodes.</exception>
    private static void ValidateBinaryOperation(Stack<Node> nodes)
    {
        if (nodes == null)
            throw new ArgumentNullException(nameof(nodes));
            
        if (nodes.Count < 2)
            throw new InvalidOperationException("Stack must contain at least 2 nodes for binary operation");
    }

    /// <summary>
    /// Validates that both operands are not null.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <exception cref="ArgumentException">Thrown when either operand is null.</exception>
    private static void ValidateOperands(Node left, Node right)
    {
        if (left == null)
            throw new ArgumentException("Left operand cannot be null");
        if (right == null)
            throw new ArgumentException("Right operand cannot be null");
    }
}
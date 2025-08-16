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
    public static void ProcessStarOperation(Stack<Node> nodes)
    {
        if (nodes == null)
            throw new ArgumentNullException(nameof(nodes));
            
        if (nodes.Count < 2)
            throw new InvalidOperationException("Stack must contain at least 2 nodes for binary operation");
            
        var right = nodes.Pop();
        var left = nodes.Pop();
        nodes.Push(new StarNode(left, right));
    }

    /// <summary>
    /// Processes a FSlash operation.
    /// </summary>
    /// <param name="nodes">The node stack.</param>
    public static void ProcessFSlashOperation(Stack<Node> nodes)
    {
        var right = nodes.Pop();
        var left = nodes.Pop();
        nodes.Push(new FSlashNode(left, right));
    }

    /// <summary>
    /// Processes a Modulo operation.
    /// </summary>
    /// <param name="nodes">The node stack.</param>
    public static void ProcessModuloOperation(Stack<Node> nodes)
    {
        var right = nodes.Pop();
        var left = nodes.Pop();
        nodes.Push(new ModuloNode(left, right));
    }

    /// <summary>
    /// Processes an Add operation.
    /// </summary>
    /// <param name="nodes">The node stack.</param>
    public static void ProcessAddOperation(Stack<Node> nodes)
    {
        var right = nodes.Pop();
        var left = nodes.Pop();
        nodes.Push(new AddNode(left, right));
    }

    /// <summary>
    /// Processes a Hyphen operation.
    /// </summary>
    /// <param name="nodes">The node stack.</param>
    public static void ProcessHyphenOperation(Stack<Node> nodes)
    {
        var right = nodes.Pop();
        var left = nodes.Pop();
        nodes.Push(new HyphenNode(left, right));
    }
}
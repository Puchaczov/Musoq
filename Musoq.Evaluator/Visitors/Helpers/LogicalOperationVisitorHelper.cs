using System;
using System.Collections.Generic;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers;

/// <summary>
/// Helper class for handling logical operations in the RewriteQueryVisitor.
/// Provides common implementation for logical operations with nullable boolean expression rewriting.
/// </summary>
public static class LogicalOperationVisitorHelper
{
    /// <summary>
    /// Processes an And operation with nullable boolean expression rewriting.
    /// </summary>
    /// <param name="nodes">The node stack.</param>
    /// <param name="rewriteNullableBoolExpressions">Function to rewrite nullable boolean expressions.</param>
    public static void ProcessAndOperation(Stack<Node> nodes, Func<Node, Node> rewriteNullableBoolExpressions)
    {
        var right = rewriteNullableBoolExpressions(nodes.Pop());
        var left = rewriteNullableBoolExpressions(nodes.Pop());
        nodes.Push(new AndNode(left, right));
    }

    /// <summary>
    /// Processes an Or operation with nullable boolean expression rewriting.
    /// </summary>
    /// <param name="nodes">The node stack.</param>
    /// <param name="rewriteNullableBoolExpressions">Function to rewrite nullable boolean expressions.</param>
    public static void ProcessOrOperation(Stack<Node> nodes, Func<Node, Node> rewriteNullableBoolExpressions)
    {
        var right = rewriteNullableBoolExpressions(nodes.Pop());
        var left = rewriteNullableBoolExpressions(nodes.Pop());
        nodes.Push(new OrNode(left, right));
    }

    /// <summary>
    /// Processes a Not operation.
    /// </summary>
    /// <param name="nodes">The node stack.</param>
    public static void ProcessNotOperation(Stack<Node> nodes)
    {
        nodes.Push(new NotNode(nodes.Pop()));
    }

    /// <summary>
    /// Processes a Contains operation.
    /// </summary>
    /// <param name="nodes">The node stack.</param>
    public static void ProcessContainsOperation(Stack<Node> nodes)
    {
        var right = nodes.Pop();
        var left = nodes.Pop();
        nodes.Push(new ContainsNode(left, right as ArgsListNode));
    }

    /// <summary>
    /// Processes an IsNull operation.
    /// </summary>
    /// <param name="nodes">The node stack.</param>
    /// <param name="isNegated">Whether the IsNull operation is negated.</param>
    public static void ProcessIsNullOperation(Stack<Node> nodes, bool isNegated)
    {
        nodes.Push(new IsNullNode(nodes.Pop(), isNegated));
    }

    /// <summary>
    /// Processes an In operation by converting it to a series of equality checks.
    /// </summary>
    /// <param name="nodes">The node stack.</param>
    public static void ProcessInOperation(Stack<Node> nodes)
    {
        var right = (ArgsListNode)nodes.Pop();
        var left = nodes.Pop();

        if (right.Args.Length == 0)
        {
            nodes.Push(new BooleanNode(false));
            return;
        }

        Node exp = new EqualityNode(left, right.Args[0]);

        for (var i = 1; i < right.Args.Length; i++)
        {
            exp = new OrNode(exp, new EqualityNode(left, right.Args[i]));
        }

        nodes.Push(exp);
    }
}
using System;
using System.Diagnostics;

namespace Musoq.Parser.Nodes;

public abstract class Node
{
    /// <summary>
    ///     Gets the source location span for this node.
    ///     May be empty (default) if span tracking was not enabled during parsing.
    /// </summary>
    public TextSpan Span { get; protected set; }

    /// <summary>
    ///     Gets the full span of this node including any leading/trailing trivia.
    ///     Defaults to the same as Span if not explicitly set.
    /// </summary>
    public TextSpan FullSpan { get; protected set; }

    /// <summary>
    ///     Gets a value indicating whether this node has a valid span.
    /// </summary>
    public bool HasSpan => !Span.IsEmpty;

    public abstract Type ReturnType { get; }

    public abstract string Id { get; }

    [DebuggerStepThrough]
    public abstract void Accept(IExpressionVisitor visitor);

    public new abstract string ToString();

    /// <summary>
    ///     Creates a copy of this node with the specified span.
    /// </summary>
    /// <param name="span">The new span.</param>
    /// <returns>This node (for chaining).</returns>
    public Node WithSpan(TextSpan span)
    {
        Span = span;
        if (FullSpan.IsEmpty)
            FullSpan = span;
        return this;
    }

    /// <summary>
    ///     Creates a copy of this node with the specified full span.
    /// </summary>
    /// <param name="fullSpan">The new full span.</param>
    /// <returns>This node (for chaining).</returns>
    public Node WithFullSpan(TextSpan fullSpan)
    {
        FullSpan = fullSpan;
        return this;
    }

    /// <summary>
    ///     Computes the combined span from start of first node to end of last node.
    /// </summary>
    protected static TextSpan ComputeSpan(Node first, Node last)
    {
        if (first == null || last == null)
            return default;
        if (!first.HasSpan || !last.HasSpan)
            return default;
        return first.Span.Through(last.Span);
    }

    /// <summary>
    ///     Computes the combined span from a collection of nodes.
    /// </summary>
    protected static TextSpan ComputeSpan(params Node[] nodes)
    {
        if (nodes == null || nodes.Length == 0)
            return default;

        var first = nodes[0];
        var last = nodes[^1];

        for (var i = 0; i < nodes.Length; i++)
            if (nodes[i]?.HasSpan == true)
            {
                first = nodes[i];
                break;
            }

        for (var i = nodes.Length - 1; i >= 0; i--)
            if (nodes[i]?.HasSpan == true)
            {
                last = nodes[i];
                break;
            }

        return ComputeSpan(first, last);
    }
}

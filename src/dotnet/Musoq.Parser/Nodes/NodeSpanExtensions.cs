namespace Musoq.Parser.Nodes;

/// <summary>
///     Extension helpers for reading source spans from parser nodes.
/// </summary>
public static class NodeSpanExtensions
{
    /// <summary>
    ///     Returns the node's source span, or <see cref="TextSpan.Empty" /> when the node is null
    ///     or has no span. Centralizes the span-or-empty fallback used throughout diagnostics.
    /// </summary>
    public static TextSpan SpanOrEmpty(this Node? node)
    {
        return node?.HasSpan == true ? node.Span : TextSpan.Empty;
    }
}

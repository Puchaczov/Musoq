using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Diagnostics;

namespace Musoq.Parser.Syntax;

internal static class SourceComposition
{
    internal static IEnumerable<SyntaxNode> Attach(IEnumerable<SyntaxNode> nodes,
        IReadOnlyDictionary<int, SyntaxParseResult> embedded, SourceText source) =>
        nodes.Select(node => AttachNode(node, embedded, source));

    private static SyntaxNode AttachNode(SyntaxNode node, IReadOnlyDictionary<int, SyntaxParseResult> embedded, SourceText source)
    {
        var children = Attach(node.Children, embedded, source);
        if (node.Kind == SyntaxKind.Subquery && embedded.TryGetValue(node.Span.Start, out var syntax) && syntax.Root != null)
            children = children.Concat(syntax.Root.Children.Select(child => Shift(child, node.Span.Start, source)));
        return new SyntaxNode(node.Kind, node.Span, source, children);
    }

    private static SyntaxNode Shift(SyntaxNode node, int offset, SourceText source) =>
        new(node.Kind, new TextSpan(node.Span.Start + offset, node.Span.Length), source,
            node.Children.Select(child => Shift(child, offset, source)));
}

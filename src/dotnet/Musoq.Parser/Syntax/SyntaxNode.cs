using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Diagnostics;

namespace Musoq.Parser.Syntax;

/// <summary>A source-level grammar region. Children retain parsing order before AST normalization.</summary>
public sealed class SyntaxNode
{
    private readonly SourceText _source;

    internal SyntaxNode(SyntaxKind kind, TextSpan span, SourceText source, IEnumerable<SyntaxNode> children)
    {
        Kind = kind;
        Span = span;
        _source = source;
        Children = Array.AsReadOnly(children.ToArray());
    }

    public SyntaxKind Kind { get; }
    public TextSpan Span { get; }
    public IReadOnlyList<SyntaxNode> Children { get; }
    public string ToFullString() => _source.GetText(Span);
}

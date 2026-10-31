using System;
using Musoq.Formatter.Syntax;
using Musoq.Parser.Syntax;

namespace Musoq.Formatter.Layout;

internal sealed class CommentPlacement(string source, TokenDocuments tokens)
{
    internal Document Build(LayoutElement element, LayoutElement? previous)
    {
        var leading = Document.Text(string.Empty);
        if (previous != null && IsStandalone(previous, element))
            leading = Document.Line;
        var content = tokens.Of(element.Token);
        if (element.Token.Text.StartsWith("--", StringComparison.Ordinal) || element.Token.Text.Contains('\n'))
            return Document.Concat(leading, content, Document.Line);
        return Document.Concat(leading, content);
    }

    internal bool IsStandalone(LayoutElement previous, LayoutElement comment) => HasNewline(previous.End, comment.Start);

    internal bool IsTrailingLineComment(SyntaxToken previous, SyntaxToken comment) =>
        comment.Role == SyntaxTokenRole.Comment && comment.Text.StartsWith("--", StringComparison.Ordinal) &&
        !HasNewline(previous.Span.End, comment.Span.Start);

    private bool HasNewline(int start, int end) => source.AsSpan(start, Math.Max(0, end - start)).ContainsAny('\r', '\n');
}

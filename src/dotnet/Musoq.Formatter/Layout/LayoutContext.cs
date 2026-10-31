using Musoq.Formatter.Syntax;

namespace Musoq.Formatter.Layout;

internal sealed class LayoutContext(SyntaxIndex syntax, TokenDocuments tokens, string source)
{
    internal SyntaxIndex Syntax { get; } = syntax;
    internal TokenDocuments Tokens { get; } = tokens;
    internal CommentPlacement Comments { get; } = new(source, tokens);
}

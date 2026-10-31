using System.Collections.Generic;
using Musoq.Parser.Syntax;

namespace Musoq.Formatter.Syntax;

internal sealed record LayoutElement(SyntaxToken Token, IReadOnlyList<LayoutElement> Children, SyntaxToken? Close)
{
    internal int Start => Token.Span.Start;
    internal int End => Close?.Span.End ?? Token.Span.End;

    internal bool IsGroup => Close != null;
    internal bool IsKeyword => Token.Role == SyntaxTokenRole.Keyword;
}

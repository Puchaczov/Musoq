using System.Collections.Generic;
using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Syntax;
using Musoq.Parser.Tokens;

namespace Musoq.Formatter.Syntax;

internal static class LayoutTree
{
    internal static IReadOnlyList<LayoutElement> Build(IReadOnlyList<SyntaxToken> tokens)
    {
        var groups = new Stack<OpenGroup>();
        var current = new List<LayoutElement>();
        foreach (var token in tokens.SelectMany(ExpandClosers))
        {
            if (token.Role == SyntaxTokenRole.Whitespace)
                continue;
            if (IsOpening(token))
            {
                groups.Push(new OpenGroup(token, current));
                current = [];
                continue;
            }
            if (IsClosing(token) && groups.TryPop(out var opening))
            {
                var group = new LayoutElement(opening.Token, current.AsReadOnly(), token);
                current = opening.Parent;
                current.Add(group);
                continue;
            }
            current.Add(new LayoutElement(token, [], null));
        }
        if (groups.Count != 0)
            throw new InvalidOperationException("A successful syntax parse contained unbalanced source delimiters.");
        return current.AsReadOnly();
    }

    private static bool IsOpening(SyntaxToken token) => token.Role == SyntaxTokenRole.Punctuation && token.TokenType == TokenType.Less || token.TokenType is
        TokenType.LeftParenthesis or TokenType.LeftSquareBracket or TokenType.LBracket;

    private static bool IsClosing(SyntaxToken token) => token.Role == SyntaxTokenRole.Punctuation && token.TokenType == TokenType.Greater || token.TokenType is
        TokenType.RightParenthesis or TokenType.RightSquareBracket or TokenType.RBracket;

    private static IEnumerable<SyntaxToken> ExpandClosers(SyntaxToken token)
    {
        if (token.Role != SyntaxTokenRole.Punctuation || token.TokenType != TokenType.RightShift)
        {
            yield return token;
            yield break;
        }
        yield return token with { TokenType = TokenType.Greater, Span = new TextSpan(token.Span.Start, 1), Text = ">" };
        yield return token with { TokenType = TokenType.Greater, Span = new TextSpan(token.Span.Start + 1, 1), Text = ">" };
    }

    private sealed record OpenGroup(SyntaxToken Token, List<LayoutElement> Parent);
}

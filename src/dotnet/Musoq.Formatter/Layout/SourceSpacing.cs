using Musoq.Formatter.Syntax;
using Musoq.Parser.Tokens;

namespace Musoq.Formatter.Layout;

internal static class SourceSpacing
{
    internal static bool NeedsSpace(LayoutElement? previous, LayoutElement current, bool previousWasUnary)
    {
        if (previous == null)
            return false;
        if (previous.Token.Role == Parser.Syntax.SyntaxTokenRole.Comment &&
            (previous.Token.Text.StartsWith("--", StringComparison.Ordinal) || previous.Token.Text.Contains('\n')))
            return false;
        var left = previous.Close?.TokenType ?? previous.Token.TokenType;
        var right = current.Token.TokenType;
        if (right is TokenType.Comma or TokenType.Semicolon or TokenType.Dot or TokenType.Colon
            or TokenType.DoubleColon or TokenType.QuestionMark)
            return false;
        if (left is TokenType.Dot or TokenType.DoubleColon)
            return false;
        if (previousWasUnary)
        {
            if (current.Token.Text.StartsWith("-", StringComparison.Ordinal))
                return true;
            if (current.Token.Role == Parser.Syntax.SyntaxTokenRole.Literal && previous.Token.Span.End < current.Token.Span.Start)
                return true;
            return false;
        }
        if (current.IsGroup && right is TokenType.LeftSquareBracket or TokenType.Less)
            return false;
        if (current.IsGroup && right == TokenType.LeftParenthesis && !previous.IsGroup)
        {
            var parameterHeader = previous.IsKeyword && previous.Token.Text.Equals("param", StringComparison.OrdinalIgnoreCase) ||
                                  previous.IsKeyword && previous.Token.Text.Equals("params", StringComparison.OrdinalIgnoreCase);
            if (parameterHeader || !previous.IsKeyword && previous.Token.TokenType is
                TokenType.Function)
                return false;
        }
        return true;
    }

    internal static bool IsUnary(LayoutElement current, LayoutElement? previous)
    {
        if (current.Token.TokenType is not (TokenType.Hyphen or TokenType.Plus))
            return false;
        return previous == null || previous.Token.Role == Parser.Syntax.SyntaxTokenRole.Operator ||
               previous.Token.TokenType is TokenType.Comma or TokenType.Then or TokenType.Else;
    }
}

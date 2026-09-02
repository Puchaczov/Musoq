using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private static short GetArithmeticPrecedence(TokenType tokenType)
    {
        return tokenType switch
        {
            TokenType.Pipe or TokenType.Caret or TokenType.Ampersand or TokenType.NullCoalescing => 0,
            TokenType.LeftShift or TokenType.RightShift => 1,
            TokenType.Plus or TokenType.Hyphen => 2,
            TokenType.Star or TokenType.FSlash or TokenType.Mod => 3,
            TokenType.Dot => 4,
            _ => -1
        };
    }

    private static bool IsEqualityOperator(Token currentToken)
    {
        return currentToken.TokenType is TokenType.Greater or TokenType.GreaterEqual or TokenType.Less
            or TokenType.LessEqual or TokenType.Equality or TokenType.Diff or TokenType.Like
            or TokenType.NotLike or TokenType.Contains or TokenType.Is or TokenType.In or TokenType.NotIn
            or TokenType.RLike or TokenType.NotRLike or TokenType.Between;
    }

    private static bool IsNumericToken(Token current)
    {
        return current.TokenType is TokenType.Decimal or TokenType.Integer or TokenType.HexadecimalInteger
            or TokenType.BinaryInteger or TokenType.OctalInteger;
    }

    private void ThrowIfMissingRightOperand(string operatorText) => ParserDiagnosticFacts.ThrowIfMissingRightOperand(Current, operatorText, _lexer.AlreadyResolvedQueryPart);
}

using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private static bool IsArithmeticBinaryOperator(Token currentToken)
    {
        return currentToken.TokenType is TokenType.Star or TokenType.FSlash or TokenType.Mod or TokenType.Plus
            or TokenType.Hyphen or TokenType.Dot or TokenType.Ampersand or TokenType.Pipe or TokenType.Caret
            or TokenType.LeftShift or TokenType.RightShift or TokenType.NullCoalescing;
    }


    private static bool IsEqualityOperator(Token currentToken)
    {
        return currentToken.TokenType is TokenType.Greater or TokenType.GreaterEqual or TokenType.Less
            or TokenType.LessEqual or TokenType.Equality or TokenType.Not or TokenType.Diff or TokenType.Like
            or TokenType.NotLike or TokenType.Contains or TokenType.Is or TokenType.In or TokenType.NotIn
            or TokenType.RLike or TokenType.NotRLike or TokenType.Between;
    }


    private static bool IsQueryOperator(Token currentToken)
    {
        return currentToken.TokenType is TokenType.And or TokenType.Or;
    }


    private static bool IsNumericToken(Token current)
    {
        return current.TokenType is TokenType.Decimal or TokenType.Integer or TokenType.HexadecimalInteger
            or TokenType.BinaryInteger or TokenType.OctalInteger;
    }

}

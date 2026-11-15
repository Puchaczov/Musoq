using Musoq.Parser.Tokens;

namespace Musoq.Parser.Lexing;

public sealed partial class Lexer
{
    private bool IsExpressionIndexStart(int start) =>
        _significantTokenType is TokenType.ParameterReference or TokenType.RightSquareBracket
        || start > 0
            && !char.IsWhiteSpace(Input[start - 1])
            && _significantTokenType
                is TokenType.Identifier
                    or TokenType.Property
                    or TokenType.NumericAccess
                    or TokenType.KeyAccess
                    or TokenType.RightParenthesis;
}

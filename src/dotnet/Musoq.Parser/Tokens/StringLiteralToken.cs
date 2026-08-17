namespace Musoq.Parser.Tokens;

/// <summary>
///     Represents an ordinary or raw single-quoted string literal token.
/// </summary>
public class StringLiteralToken(string value, TextSpan span) : Token(value, TokenType.StringLiteral, span)
{
    public const string TokenText = "string_literal";
}

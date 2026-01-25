namespace Musoq.Parser.Tokens;

/// <summary>
///     Represents a single-quoted string literal token.
/// </summary>
public class StringLiteralToken : Token
{
    public const string TokenText = "string_literal";

    public StringLiteralToken(string value, TextSpan span)
        : base(value, TokenType.StringLiteral, span)
    {
    }
}

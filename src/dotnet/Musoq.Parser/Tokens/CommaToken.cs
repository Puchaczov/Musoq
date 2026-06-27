namespace Musoq.Parser.Tokens;

public class CommaToken(TextSpan span) : Token(TokenText, TokenType.Comma, span)
{
    public const string TokenText = ",";
}

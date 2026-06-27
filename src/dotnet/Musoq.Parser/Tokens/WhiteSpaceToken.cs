namespace Musoq.Parser.Tokens;

public class WhiteSpaceToken(TextSpan span) : Token(TokenText, TokenType.WhiteSpace, span)
{
    public const string TokenText = " ";
}

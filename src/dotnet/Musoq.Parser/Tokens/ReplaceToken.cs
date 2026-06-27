namespace Musoq.Parser.Tokens;

public class ReplaceToken(TextSpan span) : Token(TokenText, TokenType.Replace, span)
{
    public const string TokenText = "replace";
}

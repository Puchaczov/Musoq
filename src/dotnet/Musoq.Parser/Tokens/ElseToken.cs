namespace Musoq.Parser.Tokens;

public class ElseToken(TextSpan span) : Token(TokenText, TokenType.Else, span)
{
    public const string TokenText = "else";
}

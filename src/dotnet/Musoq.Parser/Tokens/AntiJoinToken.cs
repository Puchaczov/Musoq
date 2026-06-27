namespace Musoq.Parser.Tokens;

public class AntiJoinToken(TextSpan span) : Token(TokenText, TokenType.AntiJoin, span)
{
    public const string TokenText = "anti join";
}
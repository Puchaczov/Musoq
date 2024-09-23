namespace Musoq.Parser.Tokens;

public class TakeToken : Token
{
    public const string TokenText = "take";

    public TakeToken(string value, TextSpan span) : base(value, TokenType.Take, span)
    {
    }
}
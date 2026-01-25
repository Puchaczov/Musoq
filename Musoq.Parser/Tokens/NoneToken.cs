namespace Musoq.Parser.Tokens;

public class NoneToken : Token
{
    public const string TokenText = "none";

    public NoneToken()
        : base(string.Empty, TokenType.None, new TextSpan(0, 0))
    {
    }
}

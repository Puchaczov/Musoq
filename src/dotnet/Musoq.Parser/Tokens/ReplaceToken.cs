namespace Musoq.Parser.Tokens;

public class ReplaceToken : Token
{
    public const string TokenText = "replace";

    public ReplaceToken(TextSpan span)
        : base(TokenText, TokenType.Replace, span)
    {
    }
}

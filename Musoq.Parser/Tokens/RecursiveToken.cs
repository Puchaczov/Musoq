namespace Musoq.Parser.Tokens;

public class RecursiveToken : Token
{
    public const string TokenText = "recursive";

    public RecursiveToken(TextSpan span)
        : base(TokenText, TokenType.Recursive, span)
    {
    }
}
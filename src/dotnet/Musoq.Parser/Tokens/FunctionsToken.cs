namespace Musoq.Parser.Tokens;

public class FunctionsToken : Token
{
    public const string TokenText = "functions";

    public FunctionsToken(TextSpan span)
        : base(TokenText, TokenType.Functions, span)
    {
    }
}

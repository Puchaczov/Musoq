namespace Musoq.Parser.Tokens;

public class MethodsToken : Token
{
    public const string TokenText = "methods";

    public MethodsToken(TextSpan span)
        : base(TokenText, TokenType.Methods, span)
    {
    }
}

namespace Musoq.Parser.Tokens;

public class FunctionsToken(TextSpan span) : Token(TokenText, TokenType.Functions, span)
{
    public const string TokenText = "functions";
}

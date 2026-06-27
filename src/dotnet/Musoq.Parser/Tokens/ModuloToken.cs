namespace Musoq.Parser.Tokens;

public class ModuloToken(TextSpan span) : Token(TokenText, TokenType.Mod, span)
{
    public const string TokenText = "%";
}

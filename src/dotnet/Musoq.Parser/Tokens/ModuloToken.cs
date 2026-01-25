namespace Musoq.Parser.Tokens;

public class ModuloToken : Token
{
    public const string TokenText = "%";

    public ModuloToken(TextSpan span)
        : base(TokenText, TokenType.Mod, span)
    {
    }
}

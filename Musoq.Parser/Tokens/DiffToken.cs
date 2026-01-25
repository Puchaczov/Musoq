namespace Musoq.Parser.Tokens;

public class DiffToken : Token
{
    public const string TokenText = "<>";

    public DiffToken(TextSpan span)
        : base(TokenText, TokenType.Diff, span)
    {
    }
}

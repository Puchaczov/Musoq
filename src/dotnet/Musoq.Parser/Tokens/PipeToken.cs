namespace Musoq.Parser.Tokens;

public class PipeToken : Token
{
    public const string TokenText = "|";

    public PipeToken(TextSpan span)
        : base(TokenText, TokenType.Pipe, span)
    {
    }
}

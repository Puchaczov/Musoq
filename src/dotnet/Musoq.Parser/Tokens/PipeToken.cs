namespace Musoq.Parser.Tokens;

public class PipeToken(TextSpan span) : Token(TokenText, TokenType.Pipe, span)
{
    public const string TokenText = "|";
}

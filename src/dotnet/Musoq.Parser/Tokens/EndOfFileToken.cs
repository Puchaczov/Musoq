namespace Musoq.Parser.Tokens;

public class EndOfFileToken(TextSpan span) : Token(string.Empty, TokenType.EndOfFile, span)
{
    public const string TokenText = "eof";
}

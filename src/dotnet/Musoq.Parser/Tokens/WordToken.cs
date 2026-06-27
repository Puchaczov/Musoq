namespace Musoq.Parser.Tokens;

public class WordToken(string value, TextSpan span) : Token(value, TokenType.Word, span)
{
    public const string TokenText = "word";

    public const string EmptyTokenText = "''";
}

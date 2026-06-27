namespace Musoq.Parser.Tokens;

public class PartitionByToken(TextSpan span) : Token(TokenText, TokenType.PartitionBy, span)
{
    public const string TokenText = "partition by";
}

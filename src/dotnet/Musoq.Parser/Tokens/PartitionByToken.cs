namespace Musoq.Parser.Tokens;

public class PartitionByToken : Token
{
    public const string TokenText = "partition by";

    public PartitionByToken(TextSpan span)
        : base(TokenText, TokenType.PartitionBy, span)
    {
    }
}

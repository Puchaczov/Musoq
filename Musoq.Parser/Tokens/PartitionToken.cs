namespace Musoq.Parser.Tokens;

public class PartitionToken : Token
{
    public const string TokenText = "PARTITION";

    public PartitionToken(TextSpan span)
        : base(TokenText, TokenType.Partition, span)
    {
    }
}
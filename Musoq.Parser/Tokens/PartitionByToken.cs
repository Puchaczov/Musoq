namespace Musoq.Parser.Tokens;

public class PartitionByToken : Token
{
    public static string TokenText = "partition by";

    public PartitionByToken(TextSpan span)
        : base(TokenText, TokenType.PartitionBy, span)
    {
    }
}
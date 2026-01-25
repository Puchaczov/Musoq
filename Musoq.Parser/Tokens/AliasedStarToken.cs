using Musoq.Parser;
using Musoq.Parser.Tokens;

public class AliasedStarToken : Token
{
    public AliasedStarToken(string value, TextSpan span)
        : base(value, TokenType.AliasedStar, span)
    {
    }
}

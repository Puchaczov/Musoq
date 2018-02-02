namespace Musoq.Parser.Tokens
{
    public class LikeToken : Token
    {
        public const string TokenText = "like";

        public LikeToken(TextSpan span)
            : base(TokenText, TokenType.Like, span)
        {
        }
    }

    public class NotLikeToken : Token
    {
        public const string TokenText = "not like";

        public NotLikeToken(TextSpan span)
            : base(TokenText, TokenType.NotLike, span)
        {
        }
    }
}
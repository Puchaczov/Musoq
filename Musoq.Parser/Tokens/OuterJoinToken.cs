namespace Musoq.Parser.Tokens
{
    public class OuterJoinToken : Token
    {
        public const string TokenText = "outer join";

        public OuterJoinToken(TextSpan span)
            : base(TokenText, TokenType.OuterJoin, span)
        {
        }
    }
}
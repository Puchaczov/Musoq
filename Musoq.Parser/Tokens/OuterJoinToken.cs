namespace Musoq.Parser.Tokens
{
    public class OuterJoinToken : Token
    {
        public const string TokenText = "outer join";

        public OuterJoinToken(OuterJoinType type, TextSpan span)
            : base(TokenText, TokenType.OuterJoin, span)
        {
            Type = type;
        }

        public OuterJoinType Type { get; }
    }
}
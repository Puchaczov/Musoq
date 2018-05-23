using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tokens
{
    public class OuterJoinToken : Token
    {
        public const string TokenText = "outer join";

        public OuterJoinToken(OuterJoinNode.OuterJoinType type, TextSpan span)
            : base(TokenText, TokenType.OuterJoin, span)
        {
            Type = type;
        }

        public OuterJoinNode.OuterJoinType Type { get; }
    }
}
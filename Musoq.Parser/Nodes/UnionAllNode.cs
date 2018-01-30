using Musoq.Parser.Tokens;

namespace Musoq.Parser.Nodes
{
    public class UnionAllNode : SetOperatorNode
    {
        public UnionAllNode(string tableName, string[] keys, Node left, Node right, bool isNested, bool isTheLastOne)
            : base(TokenType.UnionAll, keys, left, right, isNested, isTheLastOne)
        {
            ResultTableName = tableName;
        }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"{Left} union all {Right}";
        }
    }
}
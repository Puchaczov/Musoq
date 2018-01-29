using FQL.Parser.Tokens;

namespace FQL.Parser.Nodes
{
    public class ExceptNode : SetOperatorNode
    {
        public ExceptNode(string tableName, string[] keys, Node left, Node right, bool isNested, bool isTheLastOne)
            : base(TokenType.Except, keys, left, right, isNested, isTheLastOne)
        {
            ResultTableName = tableName;
        }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"{Left} except {Right}";
        }
    }
}
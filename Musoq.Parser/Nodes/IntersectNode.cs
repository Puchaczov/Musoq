using System.Linq;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Nodes;

public class IntersectNode : SetOperatorNode
{
    public IntersectNode(string tableName, string[] keys, Node left, Node right, bool isNested, bool isTheLastOne)
        : base(TokenType.Intersect, keys, left, right, isNested, isTheLastOne)
    {
        ResultTableName = tableName;
    }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var keys = Keys.Length == 0 ? string.Empty : Keys.Aggregate((a, b) => a + "," + b);
        return $"{Left.ToString()} intersect ({keys}) {Right.ToString()}";
    }
}
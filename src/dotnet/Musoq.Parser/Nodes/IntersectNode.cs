using Musoq.Parser.Tokens;

namespace Musoq.Parser.Nodes;

public class IntersectNode : SetOperatorNode
{
    public IntersectNode(string tableName, string[] keys, Node left, Node right, bool isNested, bool isTheLastOne)
        : base(TokenType.Intersect, keys, left, right, isNested, isTheLastOne)
    {
        ResultTableName = tableName;
    }

    public IntersectNode(
        string tableName,
        string[] keys,
        Node left,
        Node right,
        bool isNested,
        bool isTheLastOne,
        OrderByNode? resultOrderBy,
        SkipNode? resultSkip,
        TakeNode? resultTake)
        : base(TokenType.Intersect, keys, left, right, isNested, isTheLastOne, resultOrderBy, resultSkip, resultTake)
    {
        ResultTableName = tableName;
    }

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var keys = Keys.Length == 0 ? string.Empty : string.Join(",", Keys);
        return $"{Left.ToString()} intersect ({keys}) {Right.ToString()}{FormatResultModifiers()}";
    }
}

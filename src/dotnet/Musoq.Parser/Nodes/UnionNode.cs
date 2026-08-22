using Musoq.Parser.Tokens;

namespace Musoq.Parser.Nodes;

public class UnionNode : SetOperatorNode
{
    public UnionNode(string tableName, string[] keys, Node left, Node right, bool isNested, bool isTheLastOne)
        : base(TokenType.Union, keys, left, right, isNested, isTheLastOne)
    {
        ResultTableName = tableName;
    }

    public UnionNode(
        string tableName,
        string[] keys,
        Node left,
        Node right,
        bool isNested,
        bool isTheLastOne,
        OrderByNode? resultOrderBy,
        SkipNode? resultSkip,
        TakeNode? resultTake)
        : base(TokenType.Union, keys, left, right, isNested, isTheLastOne, resultOrderBy, resultSkip, resultTake)
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
        return $"{Left.ToString()} union ({keys}) {Right.ToString()}{FormatResultModifiers()}";
    }
}

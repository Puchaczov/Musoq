using Musoq.Parser.Tokens;

namespace Musoq.Parser.Nodes;

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
        var keys = Keys.Length == 0 ? string.Empty : string.Join(",", Keys);
        return $"{Left.ToString()} except ({keys}) {Right.ToString()}";
    }
}

using System;

namespace Musoq.Parser.Nodes;

public class StarReplaceItemNode(Node expression, string columnName) : Node
{
    public Node Expression { get; } = expression;

    public string ColumnName { get; } = columnName;

    public override Type ReturnType => Expression.ReturnType;

    public override string Id => $"{nameof(StarReplaceItemNode)}{Expression.Id}{ColumnName}";

    public override void Accept(IExpressionVisitor visitor)
    {
        Expression.Accept(visitor);
    }

    public override string ToString()
    {
        return $"{Expression} as {ColumnName}";
    }
}

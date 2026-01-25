using System;

namespace Musoq.Parser.Nodes;

public class QueryScope : Node
{
    public QueryScope(Node[] statements)
    {
        Statements = statements;
    }

    public Node[] Statements { get; }

    public override Type ReturnType { get; }

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return null;
    }
}

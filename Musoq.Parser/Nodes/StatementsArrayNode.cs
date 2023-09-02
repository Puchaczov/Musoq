using System;
using System.Linq;

namespace Musoq.Parser.Nodes;

public class StatementsArrayNode : Node
{
    public StatementsArrayNode(StatementNode[] nodes)
    {
        ReturnType = typeof(void);
        Statements = nodes;
        Id = null;
    }

    public StatementNode[] Statements { get; }

    public override Type ReturnType { get; }

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return Statements.Select(f => f.ToString())
            .Aggregate((a, b) => $"{a.ToString()}{Environment.NewLine}{b.ToString()}");
    }
}
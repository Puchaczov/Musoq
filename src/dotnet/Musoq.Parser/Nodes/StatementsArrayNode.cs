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
        if (Statements.Length == 0)
            return string.Empty;

        if (Statements.Length == 1)
            return Statements[0].ToString();

        return string.Join(Environment.NewLine, Statements.Select(f => f.ToString()));
    }
}

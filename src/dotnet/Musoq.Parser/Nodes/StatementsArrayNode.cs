using System.Linq;

namespace Musoq.Parser.Nodes;

public class StatementsArrayNode(StatementNode[] nodes) : Node
{
    public StatementNode[] Statements { get; } = nodes;

    public override Type ReturnType { get; } = typeof(void);

    public override string Id { get; } = $"{nameof(StatementsArrayNode)}{string.Concat(nodes.Select(node => node.Id))}";

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
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

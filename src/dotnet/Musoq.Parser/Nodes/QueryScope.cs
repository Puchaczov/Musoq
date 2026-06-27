using System.Linq;

namespace Musoq.Parser.Nodes;

public class QueryScope(Node[] statements) : Node
{
    public Node[] Statements { get; } = statements;

    public override Type? ReturnType => null;

    public override string Id { get; } = $"{nameof(QueryScope)}{string.Concat(statements.Select(statement => statement.Id))}";

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return string.Join(Environment.NewLine, Statements.Select(statement => statement.ToString()));
    }
}

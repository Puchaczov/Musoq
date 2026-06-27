namespace Musoq.Parser.Nodes;

public class ExistsQueryNode(Node subquery) : Node
{
    public Node Subquery { get; } = subquery;

    public override Type ReturnType => typeof(bool);

    public override string Id { get; } = $"{nameof(ExistsQueryNode)}{subquery.Id}";

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"exists ({Subquery.ToString()})";
    }
}

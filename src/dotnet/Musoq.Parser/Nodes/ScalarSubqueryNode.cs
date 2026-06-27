namespace Musoq.Parser.Nodes;

public class ScalarSubqueryNode(Node subquery) : Node
{
    public Node Subquery { get; } = subquery;

    public override Type ReturnType => typeof(object);

    public override string Id { get; } = $"{nameof(ScalarSubqueryNode)}{subquery.Id}";

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"({Subquery.ToString()})";
    }
}

namespace Musoq.Parser.Nodes;

public class InQueryNode(Node left, Node subquery) : Node
{
    public Node Left { get; } = left;

    public Node Subquery { get; } = subquery;

    public override Type ReturnType => typeof(bool);

    public override string Id { get; } = $"{nameof(InQueryNode)}{left.Id}{subquery.Id}";

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"{Left.ToString()} in ({Subquery.ToString()})";
    }
}

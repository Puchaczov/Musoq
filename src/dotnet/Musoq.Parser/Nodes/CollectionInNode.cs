namespace Musoq.Parser.Nodes;

public class CollectionInNode : BinaryNode
{
    public CollectionInNode(Node left, Node collection)
        : base(left, collection)
    {
        Collection = collection;
        Id = CalculateId(this);
    }

    public Node Collection { get; }

    public override Type ReturnType => typeof(bool);

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"{Left} in {Collection}";
    }
}

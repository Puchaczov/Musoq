namespace Musoq.Parser.Nodes;

public class IsDistinctFromNode : BinaryNode
{
    public IsDistinctFromNode(Node left, Node right, bool isNegated) : base(left, right)
    {
        IsNegated = isNegated;
        Id = $"{CalculateId(this)}{isNegated}";
    }

    public bool IsNegated { get; }

    public override string Id { get; }

    public override Type ReturnType => typeof(bool);

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return IsNegated
            ? $"{Left.ToString()} is not distinct from {Right.ToString()}"
            : $"{Left.ToString()} is distinct from {Right.ToString()}";
    }
}

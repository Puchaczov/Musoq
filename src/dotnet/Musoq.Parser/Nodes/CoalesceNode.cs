namespace Musoq.Parser.Nodes;

public class CoalesceNode : BinaryNode
{
    private readonly Type? _returnType;

    public CoalesceNode(Node left, Node right, Type? returnType = null)
        : base(left, right)
    {
        _returnType = returnType;
        Id = $"{nameof(CoalesceNode)}{left.Id}{right.Id}{returnType?.Name ?? string.Empty}";
    }

    public override Type ReturnType => _returnType!;

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"{Left.ToString()} ?? {Right.ToString()}";
    }
}
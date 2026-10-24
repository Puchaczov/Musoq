namespace Musoq.Parser.Nodes;

/// <summary>SQL string concatenation with NULL propagation and string-only operands.</summary>
public sealed class StringConcatenationNode : BinaryNode
{
    public StringConcatenationNode(Node left, Node right) : base(left, right)
    {
        Id = CalculateId(this);
    }
    public override Type ReturnType => typeof(string);
    public override string Id { get; }
    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }
    public override string ToString() => $"{Left} || {Right}";
}

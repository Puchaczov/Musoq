namespace Musoq.Parser.Nodes;

public class WindowFrameBoundNode(WindowFrameBoundType boundType, int offset = 0) : Node
{
    public WindowFrameBoundType BoundType { get; } = boundType;

    public int Offset { get; } = offset;

    public override Type ReturnType => typeof(void);

    public override string Id { get; } = $"{nameof(WindowFrameBoundNode)}{boundType}{offset}";

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return BoundType switch
        {
            WindowFrameBoundType.UnboundedPreceding => "unbounded preceding",
            WindowFrameBoundType.UnboundedFollowing => "unbounded following",
            WindowFrameBoundType.CurrentRow => "current row",
            WindowFrameBoundType.OffsetPreceding => $"{Offset} preceding",
            WindowFrameBoundType.OffsetFollowing => $"{Offset} following",
            _ => throw new InvalidOperationException($"Unknown bound type: {BoundType}")
        };
    }
}

namespace Musoq.Parser.Nodes;

public class WindowFrameNode(WindowFrameType frameType, WindowFrameBoundNode start, WindowFrameBoundNode end)
    : Node
{
    public WindowFrameType FrameType { get; } = frameType;

    public WindowFrameBoundNode Start { get; } = start;

    public WindowFrameBoundNode End { get; } = end;

    public override Type ReturnType => typeof(void);

    public override string Id { get; } = $"{nameof(WindowFrameNode)}{frameType}{start.Id}{end.Id}";

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var frameKind = FrameType == WindowFrameType.Rows ? "rows" : "range";
        return $"{frameKind} between {Start} and {End}";
    }
}

namespace Musoq.Parser.Nodes;

public class RowPresenceNode(Node expression, bool isPresent) : Node
{
    public Node Expression { get; } = expression;

    public bool IsPresent { get; } = isPresent;

    public override Type ReturnType => typeof(bool);

    public override string Id { get; } = $"{nameof(RowPresenceNode)}{expression.Id}{isPresent}";

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return IsPresent ? $"{Expression} is present" : $"{Expression} is missing";
    }
}

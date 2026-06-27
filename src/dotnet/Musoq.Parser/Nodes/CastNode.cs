namespace Musoq.Parser.Nodes;

public class CastNode(Node expression, string targetTypeName, Type? returnType = null) : Node
{
    public Node Expression { get; } = expression ?? throw new ArgumentNullException(nameof(expression));

    public string TargetTypeName { get; } = !string.IsNullOrWhiteSpace(targetTypeName)
        ? targetTypeName
        : throw new ArgumentException("Cast target type name cannot be empty.", nameof(targetTypeName));

    public override Type? ReturnType { get; } = returnType;

    public override string Id => $"{nameof(CastNode)}{Expression.Id}{TargetTypeName}{ReturnType?.Name}";

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var expression = Expression is BinaryNode ? $"({Expression.ToString()})" : Expression.ToString();
        return $"{expression}::{TargetTypeName}";
    }
}

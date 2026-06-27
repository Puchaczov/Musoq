namespace Musoq.Parser.Nodes;

/// <summary>
///     Represents an array indexer access expression, e.g., Records[-1].
/// </summary>
public class ArrayIndexNode(Node array, Node index) : Node
{
    /// <summary>The array expression being indexed.</summary>
    public Node Array { get; } = array ?? throw new ArgumentNullException(nameof(array));

    /// <summary>The index expression.</summary>
    public Node Index { get; } = index ?? throw new ArgumentNullException(nameof(index));

    public override Type ReturnType => typeof(object);

    public override string Id { get; } = $"{nameof(ArrayIndexNode)}{array}{index}";

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"{Array}[{Index}]";
    }
}

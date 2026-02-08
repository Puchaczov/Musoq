using System;

namespace Musoq.Parser.Nodes;

/// <summary>
///     Represents an array indexer access expression, e.g., Records[-1].
/// </summary>
public class ArrayIndexNode : Node
{
    public ArrayIndexNode(Node array, Node index)
    {
        Array = array ?? throw new ArgumentNullException(nameof(array));
        Index = index ?? throw new ArgumentNullException(nameof(index));
        Id = $"{nameof(ArrayIndexNode)}{array}{index}";
    }

    /// <summary>The array expression being indexed.</summary>
    public Node Array { get; }

    /// <summary>The index expression.</summary>
    public Node Index { get; }

    public override Type ReturnType => typeof(object);

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"{Array}[{Index}]";
    }
}

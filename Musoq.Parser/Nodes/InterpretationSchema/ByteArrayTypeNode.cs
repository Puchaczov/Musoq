using System;

namespace Musoq.Parser.Nodes.InterpretationSchema;

/// <summary>
///     Represents a byte array type annotation: byte[size].
///     Size can be a literal, field reference, or computed expression.
/// </summary>
public class ByteArrayTypeNode : TypeAnnotationNode
{
    /// <summary>
    ///     Creates a new byte array type annotation.
    /// </summary>
    /// <param name="sizeExpression">The expression determining array size.</param>
    public ByteArrayTypeNode(Node sizeExpression)
    {
        SizeExpression = sizeExpression ?? throw new ArgumentNullException(nameof(sizeExpression));
        Id = $"{nameof(ByteArrayTypeNode)}{sizeExpression.Id}";
    }

    /// <summary>
    ///     Gets the expression that determines the array size.
    ///     Can be an integer literal, field reference, or computed expression.
    /// </summary>
    public Node SizeExpression { get; }

    /// <inheritdoc />
    public override Type ClrType => typeof(byte[]);

    /// <inheritdoc />
    public override bool IsFixedSize => SizeExpression is IntegerNode;

    /// <inheritdoc />
    public override int? FixedSizeBytes => SizeExpression is IntegerNode intNode
        ? int.Parse(intNode.ObjValue.ToString()!)
        : null;

    /// <inheritdoc />
    public override Type ReturnType => ClrType;

    /// <inheritdoc />
    public override string Id { get; }

    /// <inheritdoc />
    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"byte[{SizeExpression.ToString()}]";
    }
}

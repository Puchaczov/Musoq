using System;

namespace Musoq.Parser.Nodes.InterpretationSchema;

/// <summary>
///     Represents an array of a type: Type[size].
///     Used for arrays of primitives, strings, or schema references.
/// </summary>
public class ArrayTypeNode : TypeAnnotationNode
{
    /// <summary>
    ///     Creates a new array type annotation.
    /// </summary>
    /// <param name="elementType">The type of array elements.</param>
    /// <param name="sizeExpression">The expression determining array length.</param>
    public ArrayTypeNode(TypeAnnotationNode elementType, Node sizeExpression)
    {
        ElementType = elementType ?? throw new ArgumentNullException(nameof(elementType));
        SizeExpression = sizeExpression ?? throw new ArgumentNullException(nameof(sizeExpression));
        Id = $"{nameof(ArrayTypeNode)}{elementType.Id}{sizeExpression.Id}";
    }

    /// <summary>
    ///     Gets the element type of the array.
    /// </summary>
    public TypeAnnotationNode ElementType { get; }

    /// <summary>
    ///     Gets the expression that determines the array length.
    /// </summary>
    public Node SizeExpression { get; }

    /// <inheritdoc />
    public override Type ClrType => ElementType.ClrType.MakeArrayType();

    /// <inheritdoc />
    public override bool IsFixedSize => SizeExpression is IntegerNode && ElementType.IsFixedSize;

    /// <inheritdoc />
    public override int? FixedSizeBytes
    {
        get
        {
            if (!IsFixedSize) return null;

            var count = int.Parse(((IntegerNode)SizeExpression).ObjValue.ToString()!);
            var elementSize = ElementType.FixedSizeBytes;

            return elementSize.HasValue ? count * elementSize.Value : null;
        }
    }

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
        return $"{ElementType.ToString()}[{SizeExpression.ToString()}]";
    }
}

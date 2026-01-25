using System;

namespace Musoq.Parser.Nodes.InterpretationSchema;

/// <summary>
///     Represents a repeat until type annotation for binary schemas.
///     Parses elements of a type repeatedly until a condition becomes true.
/// </summary>
/// <remarks>
///     Syntax: Type repeat until Expression
///     Example: TlvRecord repeat until Records[-1].Type = 0x00
///     The field name is used with [-1] indexer to refer to the most recently parsed element.
///     At least one element is always attempted (do-while semantics).
/// </remarks>
public class RepeatUntilTypeNode : TypeAnnotationNode
{
    /// <summary>
    ///     Creates a new repeat until type annotation.
    /// </summary>
    /// <param name="elementType">The type of elements to parse repeatedly.</param>
    /// <param name="condition">The condition expression that stops iteration when true.</param>
    /// <param name="fieldName">The field name, used for [-1] indexer in condition.</param>
    public RepeatUntilTypeNode(TypeAnnotationNode elementType, Node condition, string fieldName)
    {
        ElementType = elementType ?? throw new ArgumentNullException(nameof(elementType));
        Condition = condition ?? throw new ArgumentNullException(nameof(condition));
        FieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
        Id = $"{nameof(RepeatUntilTypeNode)}{elementType.Id}{condition.Id}";
    }

    /// <summary>
    ///     Gets the element type to parse repeatedly.
    /// </summary>
    public TypeAnnotationNode ElementType { get; }

    /// <summary>
    ///     Gets the condition expression that determines when to stop.
    ///     Evaluated after each element is parsed; stops when true.
    /// </summary>
    public Node Condition { get; }

    /// <summary>
    ///     Gets the field name this repeat is assigned to.
    ///     Used to resolve FieldName[-1] references in the condition.
    /// </summary>
    public string FieldName { get; }

    /// <inheritdoc />
    public override Type ClrType => ElementType.ClrType.MakeArrayType();

    /// <inheritdoc />
    public override bool IsFixedSize => false;

    /// <inheritdoc />
    public override int? FixedSizeBytes => null;

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
        return $"{ElementType} repeat until {Condition}";
    }
}

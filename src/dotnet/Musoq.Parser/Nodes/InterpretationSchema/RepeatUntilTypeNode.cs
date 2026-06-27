namespace Musoq.Parser.Nodes.InterpretationSchema;

/// <summary>
///     Represents a repeat until type annotation for binary schemas.
///     Parses elements of a type repeatedly until a stopping point is reached.
/// </summary>
/// <remarks>
///     Syntax: Type repeat until (Expression | eof)
///     Condition example: TlvRecord repeat until Records[-1].Type = 0x00
///     EOF example: Entry repeat until eof
///     The field name is used with [-1] indexer to refer to the most recently parsed element.
///     Condition repeats attempt at least one element (do-while semantics); EOF repeats are
///     zero-or-more and bounded by the current interpreter input span.
/// </remarks>
public class RepeatUntilTypeNode : TypeAnnotationNode
{
    /// <summary>
    ///     Creates a condition-based repeat until type annotation.
    /// </summary>
    /// <param name="elementType">The type of elements to parse repeatedly.</param>
    /// <param name="condition">The condition expression that stops iteration when true.</param>
    /// <param name="fieldName">The field name, used for [-1] indexer in condition.</param>
    public RepeatUntilTypeNode(TypeAnnotationNode elementType, Node condition, string fieldName)
    {
        ElementType = elementType ?? throw new ArgumentNullException(nameof(elementType));
        Condition = condition ?? throw new ArgumentNullException(nameof(condition));
        FieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
        StopKind = RepeatUntilStopKind.Condition;
        Id = $"{nameof(RepeatUntilTypeNode)}{elementType.Id}{condition.Id}";
    }

    private RepeatUntilTypeNode(TypeAnnotationNode elementType, string fieldName)
    {
        ElementType = elementType ?? throw new ArgumentNullException(nameof(elementType));
        FieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
        Condition = null;
        StopKind = RepeatUntilStopKind.EndOfInput;
        Id = $"{nameof(RepeatUntilTypeNode)}{elementType.Id}eof";
    }

    /// <summary>
    ///     Creates an end-of-input repeat until type annotation (zero-or-more).
    /// </summary>
    /// <param name="elementType">The type of elements to parse repeatedly.</param>
    /// <param name="fieldName">The field name this repeat is assigned to.</param>
    public static RepeatUntilTypeNode EndOfInput(TypeAnnotationNode elementType, string fieldName)
    {
        return new RepeatUntilTypeNode(elementType, fieldName);
    }

    /// <summary>
    ///     Gets the element type to parse repeatedly.
    /// </summary>
    public TypeAnnotationNode ElementType { get; }

    /// <summary>
    ///     Gets the condition expression that determines when to stop.
    ///     Evaluated after each element is parsed; stops when true.
    ///     Null when <see cref="StopKind" /> is <see cref="RepeatUntilStopKind.EndOfInput" />.
    /// </summary>
    public Node? Condition { get; }

    /// <summary>
    ///     Gets the field name this repeat is assigned to.
    ///     Used to resolve FieldName[-1] references in the condition.
    /// </summary>
    public string FieldName { get; }

    /// <summary>
    ///     Gets how this repeat decides when to stop reading elements.
    /// </summary>
    public RepeatUntilStopKind StopKind { get; }

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
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return StopKind == RepeatUntilStopKind.EndOfInput
            ? $"{ElementType} repeat until eof"
            : $"{ElementType} repeat until {Condition}";
    }
}

using System.Text;

namespace Musoq.Parser.Nodes.InterpretationSchema;

/// <summary>
///     Represents a parsed field definition within a binary or text schema.
///     A parsed field has a name, type annotation, and optional modifiers.
///     Parsed fields consume bytes from the input stream.
/// </summary>
public class FieldDefinitionNode : SchemaFieldNode
{
    /// <summary>
    ///     Creates a new field definition.
    /// </summary>
    /// <param name="name">The field name.</param>
    /// <param name="typeAnnotation">The type specification.</param>
    /// <param name="constraint">Optional check constraint.</param>
    /// <param name="atOffset">Optional fixed offset position.</param>
    /// <param name="whenCondition">Optional conditional parsing expression.</param>
    public FieldDefinitionNode(
        string name,
        TypeAnnotationNode typeAnnotation,
        FieldConstraintNode? constraint = null,
        Node? atOffset = null,
        Node? whenCondition = null)
        : this(name, typeAnnotation, constraint, atOffset, whenCondition, null)
    {
    }

    /// <summary>
    ///     Creates a new field definition with an optional value validation.
    /// </summary>
    /// <param name="name">The field name.</param>
    /// <param name="typeAnnotation">The type specification.</param>
    /// <param name="constraint">Optional check constraint.</param>
    /// <param name="atOffset">Optional fixed offset position.</param>
    /// <param name="whenCondition">Optional conditional parsing expression.</param>
    /// <param name="valueValidation">Optional <c>const</c>/<c>magic</c>/<c>oneOf</c> value validation.</param>
    public FieldDefinitionNode(
        string name,
        TypeAnnotationNode typeAnnotation,
        FieldConstraintNode? constraint,
        Node? atOffset,
        Node? whenCondition,
        FieldValueValidationNode? valueValidation)
        : base(name)
    {
        TypeAnnotation = typeAnnotation ?? throw new ArgumentNullException(nameof(typeAnnotation));
        Constraint = constraint;
        AtOffset = atOffset;
        WhenCondition = whenCondition;
        ValueValidation = valueValidation;

        var constraintId = constraint?.Id ?? string.Empty;
        var atId = atOffset?.Id ?? string.Empty;
        var whenId = whenCondition?.Id ?? string.Empty;
        var validationId = valueValidation?.Id ?? string.Empty;
        Id = $"{nameof(FieldDefinitionNode)}{Name}{typeAnnotation.Id}{constraintId}{atId}{whenId}{validationId}";
    }

    /// <summary>
    ///     Gets the type annotation specifying the field's type.
    /// </summary>
    public TypeAnnotationNode TypeAnnotation { get; }

    /// <summary>
    ///     Gets the optional check constraint for this field.
    /// </summary>
    public FieldConstraintNode? Constraint { get; }

    /// <summary>
    ///     Gets the optional fixed offset position (at clause).
    ///     When specified, cursor jumps to this position before reading.
    /// </summary>
    public Node? AtOffset { get; }

    /// <summary>
    ///     Gets the optional conditional parsing expression.
    ///     When condition evaluates to false, field is not parsed, cursor doesn't advance, and field value is null.
    /// </summary>
    public Node? WhenCondition { get; }

    /// <summary>
    ///     Gets the optional <c>const</c>/<c>magic</c>/<c>oneOf</c> value validation for this field.
    /// </summary>
    public FieldValueValidationNode? ValueValidation { get; }

    /// <inheritdoc />
    public override Type ReturnType => TypeAnnotation.ClrType;

    /// <inheritdoc />
    /// <remarks>
    ///     Parsed fields are not computed (they consume bytes from input).
    /// </remarks>
    public override bool IsComputed => false;

    /// <inheritdoc />
    /// <remarks>
    ///     Conditional fields may be null when condition evaluates to false.
    /// </remarks>
    public override bool IsConditional => WhenCondition != null;

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
        var builder = new StringBuilder();
        builder.Append(Name);
        builder.Append(": ");
        builder.Append(TypeAnnotation.ToString());

        if (ValueValidation != null) builder.Append(System.Globalization.CultureInfo.InvariantCulture, $" {ValueValidation.ToString()}");

        if (AtOffset != null) builder.Append(System.Globalization.CultureInfo.InvariantCulture, $" at {AtOffset.ToString()}");

        if (Constraint != null) builder.Append(System.Globalization.CultureInfo.InvariantCulture, $" {Constraint.ToString()}");

        if (WhenCondition != null) builder.Append(System.Globalization.CultureInfo.InvariantCulture, $" when {WhenCondition.ToString()}");

        return builder.ToString();
    }
}

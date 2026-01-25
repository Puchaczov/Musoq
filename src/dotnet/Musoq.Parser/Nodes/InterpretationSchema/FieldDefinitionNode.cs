#nullable enable
using System;
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
        : base(name)
    {
        TypeAnnotation = typeAnnotation ?? throw new ArgumentNullException(nameof(typeAnnotation));
        Constraint = constraint;
        AtOffset = atOffset;
        WhenCondition = whenCondition;

        var constraintId = constraint?.Id ?? string.Empty;
        var atId = atOffset?.Id ?? string.Empty;
        var whenId = whenCondition?.Id ?? string.Empty;
        Id = $"{nameof(FieldDefinitionNode)}{Name}{typeAnnotation.Id}{constraintId}{atId}{whenId}";
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
        visitor.Visit(this);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append(Name);
        builder.Append(": ");
        builder.Append(TypeAnnotation.ToString());

        if (AtOffset != null) builder.Append($" at {AtOffset.ToString()}");

        if (Constraint != null) builder.Append($" {Constraint.ToString()}");

        if (WhenCondition != null) builder.Append($" when {WhenCondition.ToString()}");

        return builder.ToString();
    }
}

using System.Collections.Generic;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

/// <summary>
///     Resolved binding for a single binary schema field, describing the property
///     shape and parse-time identity ahead of C# rendering.
/// </summary>
public sealed record BoundBinaryField
{
    /// <summary>Gets the originating field AST node.</summary>
    public required SchemaFieldNode Source { get; init; }

    /// <summary>Gets the field name as declared in the schema.</summary>
    public required string Name { get; init; }

    /// <summary>Gets whether the field is parsed from bytes or computed.</summary>
    public required BoundBinaryFieldKind Kind { get; init; }

    /// <summary>Gets the local variable name used while reading the field.</summary>
    public required string LocalVariableName { get; init; }

    /// <summary>Gets whether the field is an anonymous discard ("_").</summary>
    public required bool IsDiscard { get; init; }

    /// <summary>Gets whether the field is an alignment directive rather than a value.</summary>
    public required bool IsAlignment { get; init; }

    /// <summary>Gets whether the field is parsed only when a condition holds.</summary>
    public required bool IsConditional { get; init; }

    /// <summary>Gets the emitted property name, or null when the field emits no property.</summary>
    public string? PropertyName { get; init; }

    /// <summary>Gets the emitted property CLR type, or null when the field emits no property.</summary>
    public string? PropertyClrType { get; init; }

    /// <summary>Gets whether the emitted property is a nullable value type.</summary>
    public bool IsNullableProperty { get; init; }

    /// <summary>Gets the resolved switch binding when the field is a tagged union, otherwise null.</summary>
    public BoundBinarySwitch? Switch { get; init; }

    /// <summary>Gets whether the field contributes a property to the generated class.</summary>
    public bool EmitsProperty => PropertyName is not null;
}

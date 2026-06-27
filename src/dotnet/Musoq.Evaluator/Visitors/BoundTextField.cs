using System.Collections.Generic;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

/// <summary>
///     Resolved binding for a single text schema field, describing the property
///     shape and parse-time identity ahead of C# rendering.
/// </summary>
public sealed record BoundTextField
{
    /// <summary>Gets the originating field AST node.</summary>
    public required TextFieldDefinitionNode Source { get; init; }

    /// <summary>Gets the field name as declared in the schema.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the local variable name used while reading the field.</summary>
    public required string LocalVariableName { get; init; }

    /// <summary>Gets whether the field is an anonymous discard ("_").</summary>
    public required bool IsDiscard { get; init; }

    /// <summary>Gets the emitted property name, or null when the field emits no property.</summary>
    public string? PropertyName { get; init; }

    /// <summary>Gets the emitted property CLR type, or null when the field emits no property.</summary>
    public string? PropertyClrType { get; init; }

    /// <summary>Gets whether the property is a pattern capture result with named groups.</summary>
    public bool IsCaptureResult { get; init; }

    /// <summary>Gets the named capture groups when the field is a capture-result pattern.</summary>
    public IReadOnlyList<string> CaptureGroups { get; init; } = [];

    /// <summary>Gets whether the field contributes a property to the generated class.</summary>
    public bool EmitsProperty => PropertyName is not null;
}

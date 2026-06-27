using System.Collections.Generic;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

/// <summary>
///     Immutable, resolved description of a binary interpretation schema.
///     Captures the inheritance-flattened field list together with the property-shape
///     decisions (CLR type, nullability, emission) that the C# renderer consumes.
/// </summary>
public sealed record BoundBinaryInterpretationPlan
{
    /// <summary>Gets the schema name.</summary>
    public required string SchemaName { get; init; }

    /// <summary>Gets whether the schema declares generic type parameters.</summary>
    public required bool IsGeneric { get; init; }

    /// <summary>Gets the declared generic type parameters, empty when non-generic.</summary>
    public required IReadOnlyList<string> TypeParameters { get; init; }

    /// <summary>Gets the base schema name when the schema inherits, otherwise null.</summary>
    public string? Extends { get; init; }

    /// <summary>Gets the inheritance-flattened fields in declaration order.</summary>
    public required IReadOnlyList<BoundBinaryField> Fields { get; init; }
}

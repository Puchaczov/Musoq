using System.Collections.Generic;

namespace Musoq.Evaluator.Visitors;

/// <summary>
///     Immutable, resolved description of a text interpretation schema.
///     Captures the ordered field list together with the property-shape decisions
///     (CLR type, capture-result emission) that the C# renderer consumes.
/// </summary>
public sealed record BoundTextInterpretationPlan
{
    /// <summary>Gets the schema name.</summary>
    public required string SchemaName { get; init; }

    /// <summary>Gets the base schema name when the schema inherits, otherwise null.</summary>
    public string? Extends { get; init; }

    /// <summary>Gets the fields in declaration order.</summary>
    public required IReadOnlyList<BoundTextField> Fields { get; init; }
}

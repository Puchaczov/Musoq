using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

/// <summary>One authored field expression in a structural record.</summary>
public sealed record ExecutionStructuralField(
    string Name,
    ExecutionExpression Value,
    bool IsPresent = true)
{
    public string Name { get; } = string.IsNullOrWhiteSpace(Name)
        ? throw new ArgumentException("Structural field name cannot be empty.", nameof(Name))
        : Name;

    public ExecutionExpression Value { get; } = Value ?? throw new ArgumentNullException(nameof(Value));
}

/// <summary>Typed structural record expression with authored field order and presence.</summary>
public sealed record ExecutionStructuralRecord : ExecutionExpression
{
    public ExecutionStructuralRecord(
        ExecutionTypeRef returnType,
        IEnumerable<ExecutionStructuralField> fields,
        ExecutionStructuralConstructionPlan? constructionPlan = null)
        : base(returnType ?? throw new ArgumentNullException(nameof(returnType)))
    {
        ConstructionPlan = constructionPlan;
        Fields = Array.AsReadOnly((fields ?? throw new ArgumentNullException(nameof(fields))).ToArray());
        if (Fields.Any(static field => field is null))
            throw new ArgumentException("Structural record fields cannot be null.", nameof(fields));
        if (Fields.Select(static field => field.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Fields.Count)
            throw new ArgumentException("Structural record field names must be unique.", nameof(fields));
    }

    public IReadOnlyList<ExecutionStructuralField> Fields { get; }

    /// <summary>Gets the indexed constructor plan when this record targets a CLR receiver.</summary>
    public ExecutionStructuralConstructionPlan? ConstructionPlan { get; }

    /// <summary>Gets receiver-limit metadata when this value is demanded by a source.</summary>
    public ExecutionStructuralLimitBinding? LimitBinding { get; init; }
}

/// <summary>Typed ordered structural collection expression.</summary>
public sealed record ExecutionStructuralArray : ExecutionExpression
{
    public ExecutionStructuralArray(
        ExecutionTypeRef returnType,
        ExecutionTypeRef elementType,
        IEnumerable<ExecutionExpression> elements)
        : base(returnType ?? throw new ArgumentNullException(nameof(returnType)))
    {
        ElementType = elementType ?? throw new ArgumentNullException(nameof(elementType));
        Elements = Array.AsReadOnly((elements ?? throw new ArgumentNullException(nameof(elements))).ToArray());
        if (Elements.Any(static element => element is null))
            throw new ArgumentException("Structural collection elements cannot be null.", nameof(elements));
    }

    public ExecutionTypeRef ElementType { get; }

    public IReadOnlyList<ExecutionExpression> Elements { get; }

    /// <summary>Gets receiver-limit metadata when this value is demanded by a source.</summary>
    public ExecutionStructuralLimitBinding? LimitBinding { get; init; }
}


/// <summary>Converts an immutable Core structural value into a receiver-specific typed value.</summary>
public sealed record ExecutionStructuralConversion(
    ExecutionExpression Input,
    ExecutionTypeRef TargetType) : ExecutionExpression(TargetType ?? throw new ArgumentNullException(nameof(TargetType)))
{
    public ExecutionExpression Input { get; } = Input ?? throw new ArgumentNullException(nameof(Input));

    public ExecutionTypeRef TargetType { get; } = TargetType;

    /// <summary>Gets receiver-limit metadata when this conversion is the source root.</summary>
    public ExecutionStructuralLimitBinding? LimitBinding { get; init; }
}

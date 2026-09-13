using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

/// <summary>
/// Adapts a complete stored CTE relation to a datasource collection argument.
/// The relation is retained as a child expression so normal scope and rewrite
/// infrastructure can see the dependency, while the receiver-specific
/// construction plan keeps conversion indexed and deterministic.
/// </summary>
public sealed record ExecutionCteCollectionInput : ExecutionExpression
{
    public ExecutionCteCollectionInput(
        string cteName,
        ExecutionExpression rows,
        ExecutionTypeRef returnType,
        ExecutionTypeRef elementType,
        IEnumerable<ExecutionCteCollectionField> fields,
        ExecutionStructuralConstructionPlan? constructionPlan = null)
        : base(returnType ?? throw new ArgumentNullException(nameof(returnType)))
    {
        if (string.IsNullOrWhiteSpace(cteName))
            throw new ArgumentException("A CTE name is required.", nameof(cteName));

        CteName = cteName;
        Rows = rows ?? throw new ArgumentNullException(nameof(rows));
        ElementType = elementType ?? throw new ArgumentNullException(nameof(elementType));
        Fields = new ReadOnlyCollection<ExecutionCteCollectionField>(
            (fields ?? throw new ArgumentNullException(nameof(fields))).ToArray());
        if (Fields.Count == 0)
            throw new ArgumentException("A CTE relation must expose at least one field.", nameof(fields));
        if (Fields.Any(static field => field is null))
            throw new ArgumentException("CTE relation fields cannot be null.", nameof(fields));
        if (Fields.Select(static field => field.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Fields.Count)
            throw new ArgumentException("CTE relation field names must be unique.", nameof(fields));
        ConstructionPlan = constructionPlan;
    }

    public string CteName { get; }

    public ExecutionExpression Rows { get; init; }

    public ExecutionTypeRef ElementType { get; }

    public IReadOnlyList<ExecutionCteCollectionField> Fields { get; }

    public ExecutionStructuralConstructionPlan? ConstructionPlan { get; }

    /// <summary>Gets the receiver limit enforced at the CTE demand boundary.</summary>
    public ExecutionStructuralLimitBinding? LimitBinding { get; init; }

    /// <summary>Gets the planner-selected ownership strategy for the adapted collection.</summary>
    public ExecutionStructuralOwnershipMode Ownership { get; init; } = ExecutionStructuralOwnershipMode.ConstructFresh;

    /// <summary>Gets the planner-selected lifetime for the adapted collection.</summary>
    public ExecutionStructuralPreparationLifetime Lifetime { get; init; } = ExecutionStructuralPreparationLifetime.Execution;

    /// <summary>Gets the planner-selected metrics strategy for the adapted collection.</summary>
    public ExecutionStructuralMetricsStrategy MetricsStrategy { get; init; } = ExecutionStructuralMetricsStrategy.TypedPrepass;
}

/// <summary>One indexed output column available to a CTE collection adapter.</summary>
public sealed record ExecutionCteCollectionField(string Name, int Index, ExecutionTypeRef Type)
{
    public string Name { get; } = string.IsNullOrWhiteSpace(Name)
        ? throw new ArgumentException("CTE field name cannot be empty.", nameof(Name))
        : Name;

    public int Index { get; } = Index < 0
        ? throw new ArgumentOutOfRangeException(nameof(Index))
        : Index;

    public ExecutionTypeRef Type { get; } = Type ?? throw new ArgumentNullException(nameof(Type));
}

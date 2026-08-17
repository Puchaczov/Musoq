namespace Musoq.Evaluator.IR.Planning;

internal sealed record PredicatePlacementPlan(
    string PredicateId,
    PredicatePlacementOrigin Origin,
    string PredicateText,
    string[] Aliases,
    PredicateEarliestPlacement EarliestPlacement,
    PlanningConfidence Confidence,
    string Reason)
{
    public string ConjunctGroupId { get; init; } = string.Empty;

    public string[] AliasOwners { get; init; } = [];

    public bool IsDeterministic { get; init; } = true;

    public PredicateNullSensitivity NullSensitivity { get; init; } = PredicateNullSensitivity.NullInsensitive;

    public string[] BlockedReasons { get; init; } = [];
}

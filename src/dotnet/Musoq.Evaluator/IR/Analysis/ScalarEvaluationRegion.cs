namespace Musoq.Evaluator.IR.Analysis;

/// <summary>
/// Describes the control-flow and ownership region in which a scalar is
/// evaluated. Rewrites must not move a scalar out of a conditional-only or
/// materialization region unless its stability contract explicitly permits it.
/// </summary>
internal sealed record ScalarEvaluationRegion(
    string OwnerScope,
    ScalarEvaluationRegionKind Kind,
    bool IsUnconditional,
    bool AllowsHoisting,
    ScalarEvaluationRegion? Parent = null)
{
    public static ScalarEvaluationRegion Root(string ownerScope) =>
        new(ownerScope, ScalarEvaluationRegionKind.Unconditional, true, true);

    public bool IsDescendantOf(ScalarEvaluationRegion region)
    {
        ArgumentNullException.ThrowIfNull(region);

        for (var current = Parent; current is not null; current = current.Parent)
        {
            if (ReferenceEquals(current, region) || current == region)
                return true;
        }

        return false;
    }
}

internal enum ScalarEvaluationRegionKind
{
    Unconditional,
    ShortCircuit,
    Case,
    Coalesce,
    NullGuard,
    MatchedRow,
    UnmatchedRow,
    Helper,
    Worker,
    Materialization,
    Recursive
}

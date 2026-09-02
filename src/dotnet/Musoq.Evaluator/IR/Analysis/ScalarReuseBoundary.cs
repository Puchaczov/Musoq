namespace Musoq.Evaluator.IR.Analysis;

/// <summary>
/// Identifies a boundary at which a scalar may be carried without changing
/// when its producer is observed.
/// </summary>
internal enum ScalarReuseBoundaryKind
{
    Predicate,
    Having,
    Qualify,
    Projection,
    Ordering,
    Paging,
    Materialization,
    FinalOutput
}

internal sealed record ScalarReuseBoundary(
    ScalarReuseBoundaryKind Kind,
    string OwnerScope,
    bool AcceptsStableValues,
    bool RetainsOriginalRow,
    bool IsConditional)
{
    public bool CanCarry(ScalarReuseCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return AcceptsStableValues &&
               candidate.IsStable &&
               !IsConditional &&
               candidate.Region.AllowsHoisting &&
               string.Equals(candidate.OwnerScope, OwnerScope, StringComparison.Ordinal);
    }
}

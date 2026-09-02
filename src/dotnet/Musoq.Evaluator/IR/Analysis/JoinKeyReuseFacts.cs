using Musoq.Schema;

namespace Musoq.Evaluator.IR.Analysis;

internal enum SpecializedJoinKeyKind
{
    NestedResidual,
    SortMerge,
    AsOfProbe,
    AsOfCandidate,
    AsOfEquality,
    AsOfTieBreak,
    RangePartition,
    RangeProbe
}

/// <summary>
/// Stability facts for a specialized join key. A key can be shared only for
/// the side/row that owns it; inner-dependent and volatile keys stay at their
/// original probe or candidate evaluation site.
/// </summary>
internal sealed record JoinKeyReuseFact(
    SpecializedJoinKeyKind Kind,
    string OwnerScope,
    ColumnStability Stability,
    bool DependsOnInnerRow,
    bool IsNullable,
    string Fingerprint)
{
    public bool CanReuse =>
        Stability == ColumnStability.Stable &&
        !DependsOnInnerRow &&
        !string.IsNullOrWhiteSpace(Fingerprint);
}

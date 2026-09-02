using System.Collections.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Analysis;

/// <summary>
/// Compile-time description of a PIVOT discriminator dispatch. It is
/// intentionally data-only: the renderer still emits SQL null-correct
/// comparisons and keeps overlapping predicates separate.
/// </summary>
internal sealed record PivotPredicateDispatch(
    string DiscriminatorFingerprint,
    ColumnStability Stability,
    IReadOnlyList<string> Categories,
    bool HasOverlappingPredicates)
{
    public bool CanShareDiscriminator =>
        Stability == ColumnStability.Stable &&
        !string.IsNullOrWhiteSpace(DiscriminatorFingerprint) &&
        Categories.Count > 0;

    public bool RetainsIndependentPredicates => HasOverlappingPredicates;
}

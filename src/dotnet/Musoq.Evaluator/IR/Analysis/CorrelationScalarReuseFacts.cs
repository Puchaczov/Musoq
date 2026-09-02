using Musoq.Schema;

namespace Musoq.Evaluator.IR.Analysis;

/// <summary>
/// Describes an outer-row scalar carried into a correlated CTE/helper probe.
/// Materialization is explicit so a volatile producer cannot be frozen by a
/// probe cache or sidecar index.
/// </summary>
internal sealed record CorrelationScalarReuseFact(
    string CorrelationKey,
    string OwnerScope,
    ColumnStability Stability,
    bool IsMaterialized,
    bool UsedByMultipleProbes,
    bool CrossesHelperBoundary)
{
    public bool CanCarry =>
        Stability == ColumnStability.Stable &&
        (IsMaterialized || !CrossesHelperBoundary) &&
        UsedByMultipleProbes;

    public bool MustEvaluatePerProducedRow => Stability == ColumnStability.Volatile && IsMaterialized;
}

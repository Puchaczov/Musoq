using System.Collections.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Analysis;

/// <summary>
/// Target-neutral facts for one scalar that may be reused by a static rewrite.
/// </summary>
internal sealed record ScalarReuseCandidate(
    string Fingerprint,
    Type ReturnType,
    ColumnStability Stability,
    IReadOnlyList<string> Dependencies,
    string OwnerScope,
    ScalarEvaluationRegion Region,
    int UseCount,
    int EstimatedRepeatCount,
    int EstimatedPayloadBytes,
    bool IsVariableOnly = false)
{
    public bool IsStable => Stability == ColumnStability.Stable;

    public bool IsRepeated => UseCount > 1 || EstimatedRepeatCount > 1;

    public bool CanMoveTo(ScalarEvaluationRegion targetRegion)
    {
        ArgumentNullException.ThrowIfNull(targetRegion);

        return IsStable &&
               Region.AllowsHoisting &&
               targetRegion.AllowsHoisting &&
               targetRegion.IsDescendantOf(Region);
    }
}

using System.Collections.Generic;
using System.Linq;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Analysis;

/// <summary>
/// Static plan for values shared by every UNPIVOT entry for one source row.
/// Entry-specific and volatile values deliberately remain in their entry
/// blocks; no stream or expansion is cached here.
/// </summary>
internal sealed record UnpivotScalarReusePlan(
    IReadOnlyList<string> StableKeepFingerprints,
    IReadOnlyList<string> EntrySpecificFingerprints,
    bool HasVolatileKeepValues)
{
    public bool CanHoistKeep(string fingerprint, ColumnStability stability)
    {
        return stability == ColumnStability.Stable &&
               StableKeepFingerprints.Contains(fingerprint, StringComparer.Ordinal);
    }

    public bool KeepsEntryEvaluationLocal(string fingerprint) =>
        EntrySpecificFingerprints.Contains(fingerprint, StringComparer.Ordinal);
}

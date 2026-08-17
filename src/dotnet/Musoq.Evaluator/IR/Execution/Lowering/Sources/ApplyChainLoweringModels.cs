using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical;

namespace Musoq.Evaluator.IR.Execution.Lowering.Sources;

internal sealed record ApplyChainSource(
    IReadOnlyList<JoinSource> Sources,
    IReadOnlyDictionary<string, RowShape> SourceLookup,
    IReadOnlyList<RowShape> Shapes);

internal sealed record ApplyChainPhysicalSource(
    PhysicalNode Source,
    bool WithOrdinality);

internal sealed record ApplyChainBuildResult(
    bool IsBuilt,
    ApplyChainSource Chain,
    string UnsupportedReason)
{
    public static ApplyChainBuildResult Success(ApplyChainSource chain)
    {
        return new ApplyChainBuildResult(true, chain, string.Empty);
    }

    public static ApplyChainBuildResult Unsupported(string reason)
    {
        return new ApplyChainBuildResult(
            false,
            new ApplyChainSource(
                [],
                new Dictionary<string, RowShape>(StringComparer.OrdinalIgnoreCase),
                []),
            reason);
    }
}

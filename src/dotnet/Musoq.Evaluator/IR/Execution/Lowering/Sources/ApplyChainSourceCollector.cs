using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution.Lowering.Sources;

internal sealed class ApplyChainSourceCollector
{
    public bool TryCollectCrossApplySources(
        PhysicalNode source,
        out IReadOnlyList<ApplyChainPhysicalSource> sources)
    {
        var collected = new List<ApplyChainPhysicalSource>();
        if (!CollectCrossApplySources(source, collected))
        {
            sources = [];
            return false;
        }

        sources = collected;
        return true;
    }

    private static bool CollectCrossApplySources(
        PhysicalNode source,
        List<ApplyChainPhysicalSource> sources)
    {
        if (source is not PhysicalNestedLoopApplyNode apply)
        {
            sources.Add(new ApplyChainPhysicalSource(source, false));
            return true;
        }

        if (apply.Kind != ApplyKind.Cross)
            return false;

        if (!CollectCrossApplySources(apply.Left, sources))
            return false;

        sources.Add(new ApplyChainPhysicalSource(apply.Right, apply.WithOrdinality));
        return true;
    }
}

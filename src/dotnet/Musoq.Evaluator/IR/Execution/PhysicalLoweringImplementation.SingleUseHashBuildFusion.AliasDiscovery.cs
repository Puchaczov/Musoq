using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static bool TryFindHashBuildCteRef(
        PhysicalNode node,
        string cteName,
        out PhysicalCteRefNode cteRef)
    {
        if (node is PhysicalHashJoinNode hashJoin)
        {
            if (TryGetHashBuildCteRef(hashJoin.Left, hashJoin, cteName, out cteRef) ||
                TryGetHashBuildCteRef(hashJoin.Right, hashJoin, cteName, out cteRef))
            {
                return true;
            }
        }

        foreach (var child in node.Children)
        {
            if (TryFindHashBuildCteRef(child, cteName, out cteRef))
                return true;
        }

        cteRef = null!;
        return false;
    }

    private static bool TryGetHashBuildCteRef(
        PhysicalNode side,
        PhysicalHashJoinNode hashJoin,
        string cteName,
        out PhysicalCteRefNode cteRef)
    {
        if (side is PhysicalCteRefNode candidate &&
            string.Equals(candidate.CteName, cteName, StringComparison.OrdinalIgnoreCase) &&
            HashBuildAliasUsage.BuildKeysReferenceAlias(hashJoin, candidate.Alias))
        {
            cteRef = candidate;
            return true;
        }

        cteRef = null!;
        return false;
    }
}

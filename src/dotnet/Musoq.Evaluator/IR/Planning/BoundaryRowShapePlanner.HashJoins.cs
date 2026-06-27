using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class BoundaryRowShapePlanner
{
    private sealed partial class BoundaryRowShapePlanningState
    {
        private void VisitHashJoin(PhysicalHashJoinNode hashJoin, IReadOnlyList<string> requiredAfter)
        {
            var buildSide = ResolveBuildSide(hashJoin);
            var probeSide = ReferenceEquals(buildSide, hashJoin.Left) ? hashJoin.Right : hashJoin.Left;
            var buildColumns = CollectColumns(hashJoin.BuildKeys);
            var probeColumns = CollectColumns(hashJoin.ProbeKeys);
            var residualColumns = hashJoin.Residual != null ? CollectColumns(hashJoin.Residual) : [];
            var buildResidualColumns = FilterColumnsProducedBy(buildSide, residualColumns);
            var probeResidualColumns = FilterColumnsProducedBy(probeSide, residualColumns);
            var buildRequiredColumns = Merge(buildColumns, buildResidualColumns);
            var probeRequiredColumns = Merge(probeColumns, probeResidualColumns);
            var buildNeededAfter = Merge(ResolveNeededAfter(requiredAfter), buildResidualColumns);
            var probeNeededAfter = Merge(FilterColumnsProducedBy(probeSide, ResolveNeededAfter(requiredAfter)), probeResidualColumns);

            AddPlan(
                BoundaryRowShapeKind.HashJoinBuild,
                buildSide,
                buildNeededAfter,
                buildColumns,
                buildResidualColumns.Length == 0
                    ? "Hash join build boundary uses build-key columns while preserving the current row shape; no physical pruning was applied."
                    : "Hash join build boundary retains build-key and residual predicate columns while preserving the current row shape; no physical pruning was applied.");
            AddPlan(
                BoundaryRowShapeKind.HashJoinProbe,
                probeSide,
                probeNeededAfter,
                probeColumns,
                probeResidualColumns.Length == 0
                    ? "Hash join probe boundary uses probe-key columns while preserving the current row shape; no physical pruning was applied."
                    : "Hash join probe boundary retains probe-key and residual predicate columns while preserving the current row shape; no physical pruning was applied.");

            if (ReferenceEquals(buildSide, hashJoin.Left))
            {
                Visit(hashJoin.Left, Merge(requiredAfter, buildRequiredColumns));
                Visit(hashJoin.Right, Merge(requiredAfter, probeRequiredColumns));
                return;
            }

            Visit(hashJoin.Left, Merge(requiredAfter, probeRequiredColumns));
            Visit(hashJoin.Right, Merge(requiredAfter, buildRequiredColumns));
        }
    }
}

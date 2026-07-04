using System;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using ColumnUsage = Musoq.Evaluator.IR.Optimization.Physical.PhysicalColumnUsageFacts;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.IR.Optimization.Physical;

internal static class PhysicalProjectionBoundaryClassifier
{
    public static bool TryGetPrunableJoin(
        PhysicalNode node,
        out JoinKind kind,
        out PhysicalNode left,
        out PhysicalNode right,
        out ColumnRef[] joinRefs,
        out Func<PhysicalNode, PhysicalNode, PhysicalNode> createJoin)
    {
        switch (node)
        {
            case PhysicalJoinCandidateNode join
                when join.Kind is JoinKind.Inner or JoinKind.LeftSemi or JoinKind.LeftAntiSemi:
                kind = join.Kind;
                left = join.Left;
                right = join.Right;
                joinRefs = ColumnUsage.CollectReferencedColumns(join.OnPredicate)
                    .Concat(join.TieBreak != null ? ColumnUsage.CollectReferencedColumns(join.TieBreak.Expression) : [])
                    .Concat(ColumnUsage.CollectReferencedColumns(join.LeftMovedPredicates))
                    .Concat(ColumnUsage.CollectReferencedColumns(join.RightMovedPredicates))
                    .ToArray();
                createJoin = (rewrittenLeft, rewrittenRight) => new PhysicalJoinCandidateNode(
                    join.Kind,
                    join.OnPredicate,
                    rewrittenLeft,
                    rewrittenRight,
                    join.LeftMovedPredicates,
                    join.RightMovedPredicates,
                    join.TieBreak);
                return true;
            case PhysicalHashJoinNode join
                when join.Kind is JoinKind.Inner or JoinKind.LeftSemi or JoinKind.LeftAntiSemi:
                kind = join.Kind;
                left = join.Left;
                right = join.Right;
                joinRefs = ColumnUsage.CollectReferencedColumns(join.BuildKeys)
                    .Concat(ColumnUsage.CollectReferencedColumns(join.ProbeKeys))
                    .Concat(join.Residual != null ? ColumnUsage.CollectReferencedColumns(join.Residual) : [])
                    .ToArray();
                createJoin = (rewrittenLeft, rewrittenRight) => new PhysicalHashJoinNode(
                    join.Kind,
                    join.BuildKeys,
                    join.ProbeKeys,
                    join.Residual,
                    rewrittenLeft,
                    rewrittenRight);
                return true;
            default:
                kind = default;
                left = null!;
                right = null!;
                joinRefs = [];
                createJoin = static (rewrittenLeft, _) => rewrittenLeft;
                return false;
        }
    }
}


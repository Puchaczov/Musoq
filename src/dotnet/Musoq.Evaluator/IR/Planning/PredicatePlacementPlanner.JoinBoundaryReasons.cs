using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class PredicatePlacementPlanner
{
    private static string CreateJoinBoundaryReason(JoinKind joinKind) =>
        joinKind switch
        {
            JoinKind.AsofInner or JoinKind.AsofLeft => $"{joinKind} join predicate remains at the post-join boundary to preserve ASOF ordering/probe semantics.",
            JoinKind.LeftOuter or JoinKind.RightOuter or JoinKind.FullOuter => $"{joinKind} join predicate remains at the post-join boundary to preserve outer join row semantics.",
            JoinKind.LeftSingle => $"{joinKind} join predicate remains at the post-join boundary to preserve scalar cardinality semantics.",
            JoinKind.LeftMark => $"{joinKind} join predicate remains at the post-join boundary to preserve predicate truth semantics.",
            JoinKind.LeftSemi or JoinKind.LeftAntiSemi => $"{joinKind} join predicate remains at the post-join boundary to preserve semi-join row semantics.",
            _ => $"{joinKind} join predicate remains at the post-join boundary to preserve join semantics."
        };
}

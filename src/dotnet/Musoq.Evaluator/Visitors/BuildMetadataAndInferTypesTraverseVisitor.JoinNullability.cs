using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesTraverseVisitor
{
    private static void EnsureSupportedJoinType(JoinType joinType)
    {
        if (joinType is not (JoinType.Inner
            or JoinType.AsOf
            or JoinType.AsOfLeft
            or JoinType.Cross
            or JoinType.LeftSemi
            or JoinType.LeftAntiSemi
            or JoinType.OuterLeft
            or JoinType.OuterRight
            or JoinType.OuterFull))
        {
            throw new InvalidOperationException($"Unsupported join type '{joinType}'.");
        }
    }

    private static bool MakesLeftSideNullable(JoinType joinType)
    {
        return joinType is JoinType.OuterRight or JoinType.OuterFull;
    }

    private static bool MakesRightSideNullable(JoinType joinType)
    {
        return joinType is JoinType.OuterLeft or JoinType.AsOfLeft or JoinType.OuterFull;
    }
}

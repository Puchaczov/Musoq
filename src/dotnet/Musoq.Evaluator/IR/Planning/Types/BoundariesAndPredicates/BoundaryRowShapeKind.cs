namespace Musoq.Evaluator.IR.Planning;

internal enum BoundaryRowShapeKind
{
    Sort,
    TopN,
    TopOffset,
    Aggregate,
    Distinct,
    SetOperation,
    Window,
    HashJoinBuild,
    HashJoinProbe,
    CteMaterialization
}

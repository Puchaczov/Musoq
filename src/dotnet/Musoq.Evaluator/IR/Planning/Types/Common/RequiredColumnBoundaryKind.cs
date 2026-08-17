namespace Musoq.Evaluator.IR.Planning;

internal enum RequiredColumnBoundaryKind
{
    Aggregate,
    Window,
    SetOperation,
    HashJoinBuild,
    CteMaterialization,
    JoinLeftEdge,
    JoinRightEdge
}

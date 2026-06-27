using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Logical.Nodes;

public enum JoinKind
{
    Inner,
    LeftOuter,
    RightOuter,
    FullOuter,
    AsofInner,
    AsofLeft,
    Cross,
    LeftSemi,
    LeftAntiSemi
}

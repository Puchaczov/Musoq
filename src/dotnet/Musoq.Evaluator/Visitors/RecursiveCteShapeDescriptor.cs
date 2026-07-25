using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public sealed record RecursiveCteShapeDescriptor(
    CteInnerExpressionNode Definition,
    QueryNode Anchor,
    QueryNode RecursiveMember,
    SetOperatorNode Boundary,
    RecursiveCteUnionKind UnionKind,
    string[] Keys);

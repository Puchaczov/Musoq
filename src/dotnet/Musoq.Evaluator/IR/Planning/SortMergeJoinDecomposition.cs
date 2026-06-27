using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record SortMergeJoinDecomposition(
    IrExpression LeftKey,
    IrExpression RightKey,
    BinaryOpKind ComparisonKind,
    IrExpression Residual);

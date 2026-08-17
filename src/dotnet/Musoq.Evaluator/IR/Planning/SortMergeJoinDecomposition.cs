using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record SortMergeJoinDecomposition(
    IrExpression LeftKey,
    IrExpression RightKey,
    BinaryOpKind ComparisonKind,
    IrExpression Residual,
    IrExpression[] LeftPartitionKeys,
    IrExpression[] RightPartitionKeys);

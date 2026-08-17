using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Planning;

internal readonly record struct NormalizedSortMergeKey(
    IrExpression LeftKey,
    IrExpression RightKey,
    BinaryOpKind ComparisonKind);

using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal readonly record struct NormalizedSortMergeKey(
    IrExpression LeftKey,
    IrExpression RightKey,
    BinaryOpKind ComparisonKind);

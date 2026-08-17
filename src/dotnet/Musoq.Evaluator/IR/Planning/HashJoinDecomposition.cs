using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record HashJoinDecomposition(
    IrExpression[] BuildKeys,
    IrExpression[] ProbeKeys,
    IrExpression? Residual,
    string BuildSideReason);

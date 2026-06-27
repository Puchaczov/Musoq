using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record HashJoinDecomposition(
    IrExpression[] BuildKeys,
    IrExpression[] ProbeKeys,
    IrExpression? Residual,
    string BuildSideReason);

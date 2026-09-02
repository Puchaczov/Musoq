using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Expressions;

public sealed record InCheck(
    IrExpression Expression,
    IReadOnlyList<IrExpression> Values,
    Type ReturnType,
    bool IsNegated = false)
    : IrExpression(ReturnType);

using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Expressions;

public sealed record InCheck(IrExpression Expression, IReadOnlyList<IrExpression> Values, Type ReturnType)
    : IrExpression(ReturnType);

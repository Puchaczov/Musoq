using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Expressions;

public sealed record MethodCall(
    MethodInfo Method,
    IReadOnlyList<IrExpression> Arguments,
    string? Alias,
    Type ReturnType) : IrExpression(ReturnType);

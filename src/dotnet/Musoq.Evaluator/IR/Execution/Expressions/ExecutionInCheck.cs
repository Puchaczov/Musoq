using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionInCheck(
    ExecutionExpression Expression,
    IReadOnlyList<ExecutionExpression> Values,
    Type ReturnType,
    ExecutionConstantInSet? ConstantSet = null) : ExecutionExpression(ReturnType);

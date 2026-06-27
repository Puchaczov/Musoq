using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionIsNullCheck(
    ExecutionExpression Expression,
    bool IsNegated,
    Type ReturnType) : ExecutionExpression(ReturnType);

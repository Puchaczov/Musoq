using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionBetween(
    ExecutionExpression Expression,
    ExecutionExpression Low,
    ExecutionExpression High,
    Type ReturnType) : ExecutionExpression(ReturnType);

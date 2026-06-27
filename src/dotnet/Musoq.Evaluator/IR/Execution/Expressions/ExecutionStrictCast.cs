using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionStrictCast(
    ExecutionExpression Expression,
    string TargetTypeName,
    Type ReturnType,
    ExecutionVariable? Target = null) : ExecutionExpression(ReturnType);

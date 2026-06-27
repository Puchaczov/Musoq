using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionAggregateCapturedValueRead(
    ExecutionVariable Group,
    string ValueName,
    Type ReturnType,
    AggregateCapturedField CapturedField) : ExecutionExpression(ReturnType);

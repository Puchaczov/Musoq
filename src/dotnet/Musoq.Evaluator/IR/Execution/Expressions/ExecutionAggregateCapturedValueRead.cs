using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionAggregateCapturedValueRead(
    ExecutionVariable Group,
    string ValueName,
    ExecutionTypeRef ReturnType,
    AggregateCapturedField CapturedField) : ExecutionExpression(ReturnType)
{
    internal ExecutionAggregateCapturedValueRead(
        ExecutionVariable group,
        string valueName,
        Type returnType,
        AggregateCapturedField capturedField)
        : this(group, valueName, ExecutionClrBindingFactory.FromClr(returnType), capturedField)
    {
    }
}

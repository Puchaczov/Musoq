using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionAggregateCapturedValueSet(
    ExecutionVariable Group,
    string ValueName,
    ExecutionExpression Value,
    ExecutionTypeRef ValueType,
    AggregateCapturedField CapturedField) : ExecutionNode
{
    internal ExecutionAggregateCapturedValueSet(
        ExecutionVariable group,
        string valueName,
        ExecutionExpression value,
        Type valueType,
        AggregateCapturedField capturedField)
        : this(group, valueName, value, ExecutionTypeRef.FromClr(valueType), capturedField)
    {
    }
}

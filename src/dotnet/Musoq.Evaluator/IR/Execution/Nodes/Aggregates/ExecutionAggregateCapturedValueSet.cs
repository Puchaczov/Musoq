using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionAggregateCapturedValueSet(
    ExecutionVariable Group,
    string ValueName,
    ExecutionExpression Value,
    Type ValueType,
    AggregateCapturedField CapturedField) : ExecutionNode;

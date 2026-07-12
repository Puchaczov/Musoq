using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionRangeProbe(
    ExecutionVariable Match,
    ExecutionVariable Index,
    ExecutionExpression ProbeKey,
    ExecutionTypeRef KeyType,
    ExecutionBlock Body) : ExecutionNode
{
    internal ExecutionRangeProbe(
        ExecutionVariable match,
        ExecutionVariable index,
        ExecutionExpression probeKey,
        Type keyType,
        ExecutionBlock body)
        : this(match, index, probeKey, ExecutionTypeRef.FromClr(keyType), body)
    {
    }
}

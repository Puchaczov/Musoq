using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionWindowAggregateKernelDescriptor(
    ExecutionWindowAggregateFunction Function,
    ExecutionWindowAggregateMode Mode,
    ExecutionTypeRef InputType,
    ExecutionTypeRef ResultType,
    ExecutionTypeRef AccumulatorType)
{
    internal ExecutionWindowAggregateKernelDescriptor(
        ExecutionWindowAggregateFunction function,
        ExecutionWindowAggregateMode mode,
        Type inputType,
        Type resultType,
        Type accumulatorType)
        : this(
            function,
            mode,
            ExecutionTypeRef.FromClr(inputType),
            ExecutionTypeRef.FromClr(resultType),
            ExecutionTypeRef.FromClr(accumulatorType))
    {
    }
}

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
            ExecutionClrBindingFactory.FromClr(inputType),
            ExecutionClrBindingFactory.FromClr(resultType),
            ExecutionClrBindingFactory.FromClr(accumulatorType))
    {
    }
}

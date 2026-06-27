using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionWindowAggregateKernelDescriptor(
    ExecutionWindowAggregateFunction Function,
    ExecutionWindowAggregateMode Mode,
    Type InputType,
    Type ResultType,
    Type AccumulatorType);

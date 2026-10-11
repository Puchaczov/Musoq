namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionWindowAggregateKernelDescriptor(
    ExecutionWindowAggregateFunction Function,
    ExecutionWindowAggregateMode Mode,
    ExecutionTypeRef InputType,
    ExecutionTypeRef ResultType,
    ExecutionTypeRef AccumulatorType)
{
    public ExecutionAggregateKernelOperations? LibraryKernel { get; init; }
    /// <summary>Gets an optional validated typed batch operation with a serial fallback.</summary>
    public ExecutionCallableRef? LibraryBatch { get; init; }
    /// <summary>Gets the planner-owned input capture decision.</summary>
    public bool CaptureArguments { get; init; }
    /// <summary>Gets the selected incremental RANGE peer evaluation strategy.</summary>
    public bool UseRunningPeers { get; init; }

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

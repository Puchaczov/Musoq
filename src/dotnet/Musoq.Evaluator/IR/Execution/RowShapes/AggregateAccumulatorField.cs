using Musoq.Plugins;

namespace Musoq.Evaluator.IR.Execution;

public sealed record AggregateAccumulatorField(
    string Identifier,
    string FieldName,
    AggregateKernelDescriptor Kernel,
    int ParentDepth = 0,
    int OwnerPrefixLength = 0,
    string? OwnerFieldName = null)
{
    public ExecutionTypeRef InputType => ExecutionClrBindingFactory.FromClr(Kernel.InputShape.InputType);

    public ExecutionTypeRef ResultType => ExecutionClrBindingFactory.FromClr(Kernel.ResultType);

    public ExecutionTypeRef AccumulatorType => ExecutionClrBindingFactory.FromClr(Kernel.StateType);

    public bool CanMerge => Kernel.SupportsMerge;
}

using System.Collections.Generic;
using Musoq.Plugins;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

public sealed record AggregateAccumulatorField(
    string Identifier,
    string FieldName,
    AggregateKernelDescriptor Kernel,
    int ParentDepth = 0,
    int OwnerPrefixLength = 0,
    string? OwnerFieldName = null)
{
    public ExecutionTypeRef InputType => ExecutionTypeRef.FromClr(Kernel.InputShape.InputType);

    public ExecutionTypeRef ResultType => ExecutionTypeRef.FromClr(Kernel.ResultType);

    public ExecutionTypeRef AccumulatorType => ExecutionTypeRef.FromClr(Kernel.StateType);

    public bool CanMerge => Kernel.SupportsMerge;
}

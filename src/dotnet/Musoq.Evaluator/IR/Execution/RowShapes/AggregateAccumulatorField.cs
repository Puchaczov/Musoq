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
    public Type InputType => Kernel.InputShape.InputType;

    public Type ResultType => Kernel.ResultType;

    public Type AccumulatorType => Kernel.StateType;

    public bool CanMerge => Kernel.SupportsMerge;
}

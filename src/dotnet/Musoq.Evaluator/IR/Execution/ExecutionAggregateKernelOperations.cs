using System.Collections.Generic;
using System.Linq;
using Musoq.Plugins;

namespace Musoq.Evaluator.IR.Execution;

/// <summary>Immutable concretized kernel operations carried across the target boundary.</summary>
public sealed record ExecutionAggregateKernelOperations
{
    internal ExecutionAggregateKernelOperations(AggregateKernelDescriptor descriptor)
    {
        FunctionName = descriptor.FunctionName;
        StateType = ExecutionClrBindingFactory.FromClr(descriptor.StateType);
        KernelType = ExecutionClrBindingFactory.FromClr(descriptor.KernelType);
        ResultType = ExecutionClrBindingFactory.FromClr(descriptor.ResultType);
        ArgumentTypes = ExecutionIrCollections.Freeze(descriptor.InputShape.ArgumentTypes
            .Select(ExecutionClrBindingFactory.FromClr).ToArray());
        ResetMethod = ExecutionClrBindingFactory.FromClr(descriptor.ResetMethod);
        SetMethod = ExecutionClrBindingFactory.FromClr(descriptor.SetMethod);
        GetMethod = ExecutionClrBindingFactory.FromClr(descriptor.GetMethod);
        MergeMethod = descriptor.MergeMethod is null ? null : ExecutionClrBindingFactory.FromClr(descriptor.MergeMethod);
        ExecutionPermissions = descriptor.ExecutionPermissions;
    }

    public string FunctionName { get; }
    public ExecutionTypeRef StateType { get; }
    public ExecutionTypeRef KernelType { get; }
    public ExecutionTypeRef ResultType { get; }
    public IReadOnlyList<ExecutionTypeRef> ArgumentTypes { get; }
    public ExecutionCallableRef ResetMethod { get; }
    public ExecutionCallableRef SetMethod { get; }
    public ExecutionCallableRef GetMethod { get; }
    public ExecutionCallableRef? MergeMethod { get; }
    public FunctionExecutionPermissions ExecutionPermissions { get; }
}

using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionWindowKernelPlan : ExecutionNode
{
    public ExecutionWindowKernelPlan(
        string signature,
        ExecutionWindowKernelPlanStrategy strategy,
        IReadOnlyList<ExecutionNode> kernels)
    {
        Signature = signature;
        Strategy = strategy;
        Kernels = ExecutionIrCollections.Freeze(kernels);
    }

    public string Signature { get; init; }

    public ExecutionWindowKernelPlanStrategy Strategy { get; init; }

    public IReadOnlyList<ExecutionNode> Kernels { get; init; }
}

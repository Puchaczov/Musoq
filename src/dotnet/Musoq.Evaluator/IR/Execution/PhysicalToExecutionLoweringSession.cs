using System.Collections.Generic;
using ExecutionStrategyPlan = Musoq.Evaluator.IR.Planning.ExecutionStrategyPlan;

namespace Musoq.Evaluator.IR.Execution;

internal sealed class PhysicalToExecutionLoweringSession
{
    public PhysicalToExecutionLoweringSession(
        ExecutionStrategyPlan executionStrategies,
        IReadOnlyDictionary<string, FusedCteHashBuildSource>? fusedCteHashBuildSources = null,
        Dictionary<int, HashPayloadShape>? cteSidecarHashPayloadsBySlot = null)
    {
        ExecutionStrategies = executionStrategies;
        FusedCteHashBuildSources = fusedCteHashBuildSources;
        CteSidecarHashPayloadsBySlot = cteSidecarHashPayloadsBySlot ?? [];
    }

    public ExecutionStrategyPlan ExecutionStrategies { get; }
    public IReadOnlyDictionary<string, FusedCteHashBuildSource>? FusedCteHashBuildSources { get; }
    public Dictionary<int, HashPayloadShape> CteSidecarHashPayloadsBySlot { get; }

    public PhysicalToExecutionLoweringSession WithFusedCteHashBuildSources(
        IReadOnlyDictionary<string, FusedCteHashBuildSource>? fusedCteHashBuildSources)
    {
        return new PhysicalToExecutionLoweringSession(
            ExecutionStrategies,
            fusedCteHashBuildSources,
            CteSidecarHashPayloadsBySlot);
    }
}

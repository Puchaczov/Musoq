using System.Collections.Generic;
using ExecutionStrategyPlan = Musoq.Evaluator.IR.Planning.ExecutionStrategyPlan;

namespace Musoq.Evaluator.IR.Execution;

internal sealed class PhysicalToExecutionLoweringSession
{
    public PhysicalToExecutionLoweringSession(
        ExecutionStrategyPlan executionStrategies,
        IReadOnlyDictionary<string, FusedCteHashBuildSource>? fusedCteHashBuildSources = null,
        Dictionary<int, HashPayloadShape>? cteSidecarHashPayloadsBySlot = null,
        bool suppressSidecarJoinPipeline = false,
        IReadOnlyDictionary<string, ScalarSubqueryEmptyResultSpec>? scalarSubqueryEmptyResults = null)
    {
        ExecutionStrategies = executionStrategies;
        FusedCteHashBuildSources = fusedCteHashBuildSources;
        CteSidecarHashPayloadsBySlot = cteSidecarHashPayloadsBySlot ?? [];
        SuppressSidecarJoinPipeline = suppressSidecarJoinPipeline;
        ScalarSubqueryEmptyResults = scalarSubqueryEmptyResults ??
            new Dictionary<string, ScalarSubqueryEmptyResultSpec>(StringComparer.OrdinalIgnoreCase);
    }

    public ExecutionStrategyPlan ExecutionStrategies { get; }
    public IReadOnlyDictionary<string, FusedCteHashBuildSource>? FusedCteHashBuildSources { get; }
    public Dictionary<int, HashPayloadShape> CteSidecarHashPayloadsBySlot { get; }
    public bool SuppressSidecarJoinPipeline { get; }
    public IReadOnlyDictionary<string, ScalarSubqueryEmptyResultSpec> ScalarSubqueryEmptyResults { get; }

    public PhysicalToExecutionLoweringSession WithFusedCteHashBuildSources(
        IReadOnlyDictionary<string, FusedCteHashBuildSource>? fusedCteHashBuildSources)
    {
        return new PhysicalToExecutionLoweringSession(
            ExecutionStrategies,
            fusedCteHashBuildSources,
            CteSidecarHashPayloadsBySlot,
            SuppressSidecarJoinPipeline,
            ScalarSubqueryEmptyResults);
    }

    public PhysicalToExecutionLoweringSession WithSidecarJoinPipelineSuppressed()
    {
        return new PhysicalToExecutionLoweringSession(
            ExecutionStrategies,
            FusedCteHashBuildSources,
            CteSidecarHashPayloadsBySlot,
            suppressSidecarJoinPipeline: true,
            scalarSubqueryEmptyResults: ScalarSubqueryEmptyResults);
    }

    public PhysicalToExecutionLoweringSession WithScalarSubqueryEmptyResults(
        IReadOnlyDictionary<string, ScalarSubqueryEmptyResultSpec> scalarSubqueryEmptyResults)
    {
        return new PhysicalToExecutionLoweringSession(
            ExecutionStrategies,
            FusedCteHashBuildSources,
            CteSidecarHashPayloadsBySlot,
            SuppressSidecarJoinPipeline,
            scalarSubqueryEmptyResults);
    }
}

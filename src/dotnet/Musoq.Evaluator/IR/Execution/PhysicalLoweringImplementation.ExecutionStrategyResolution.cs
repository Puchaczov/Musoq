using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;
using ExecutionStrategyPlan = Musoq.Evaluator.IR.Planning.ExecutionStrategyPlan;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    internal PhysicalLoweringImplementation(
        ExecutionShapeResolver shapeResolver,
        ExecutionPlanningArtifacts executionArtifacts,
        SchemaRegistry? schemaRegistry = null,
        CompilationOptions? compilationOptions = null,
        CteExecutionPlan? cteExecutionPlan = null)
        : this(shapeResolver, schemaRegistry, compilationOptions, cteExecutionPlan, executionArtifacts)
    {
    }

    internal PhysicalLoweringImplementation(
        ExecutionShapeResolver shapeResolver,
        SchemaRegistry? schemaRegistry,
        CompilationOptions? compilationOptions,
        CteExecutionPlan? cteExecutionPlan,
        ExecutionPlanningArtifacts executionArtifacts)
    {
        _facts = new PhysicalLoweringFacts(
            shapeResolver,
            schemaRegistry,
            compilationOptions ?? new CompilationOptions(),
            cteExecutionPlan,
            executionArtifacts);
        _aggregatePlanLowerer = CreateAggregatePlanLowerer();
        _windowPlanLowerer = CreateWindowPlanLowerer();
        _applyLoweringService = new ApplyLoweringService(this);
        _joinPlanLowerer = CreateJoinPlanLowerer();
        _ctePlanLowerer = CreateCtePlanLowerer();
        _pipelinePlanLowerer = CreatePipelinePlanLowerer();
        _multiStatementPlanLowerer = new MultiStatementPlanLowerer(new MultiStatementLoweringService(this));
        _descPlanLowerer = new DescPlanLowerer(new DescLoweringService(this));
        _physicalLoweringFacade = CreatePhysicalLoweringDispatchFacade();
    }

    private ExecutionStrategyPlan ResolveExecutionStrategies()
    {
        return _facts.ExecutionStrategies;
    }
}

using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Physical;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation :
    IJoinLoweringOperations,
    IPipelineLoweringOperations,
    ICteLoweringOperations,
    IMultiStatementLoweringOperations,
    IDescLoweringOperations,
    IAggregateLoweringOperations,
    IWindowLoweringOperations,
    IApplyLoweringOperations
{
    private const int DefaultSchemaFromIndex = 0;
    private const int SourceInstanceOrdinal = 1;
    private const int ParallelAggregateRowThreshold = 4096;
    private const int ParallelAggregateCardinalitySampleSize = 8192;
    private const int ParallelAggregateMaxDistinctSample = 6144;
    private const int ParallelFilterProjectRowThreshold = 4096;
    private const string DefaultAggregateScopeName = "result";
    private const ExecutionAppendMode SerialAppendMode = ExecutionAppendMode.Direct;
    private readonly PhysicalLoweringFacts _facts;
    private ExecutionShapeResolver _shapeResolver => _facts.ShapeResolver;
    private SchemaRegistry? _schemaRegistry => _facts.SchemaRegistry;
    private CompilationOptions _compilationOptions => _facts.CompilationOptions;
    private IReadOnlyDictionary<string, SourceInteractionPlan> _sourceInteractionPlans =>
        _facts.SourceInteractionPlansBySourceId;
    private readonly AggregatePlanLowerer _aggregatePlanLowerer;
    private readonly WindowPlanLowerer _windowPlanLowerer;
    private readonly ApplyLoweringService _applyLoweringService;
    private readonly JoinPlanLowerer _joinPlanLowerer;
    private readonly CtePlanLowerer _ctePlanLowerer;
    private readonly PipelinePlanLowerer _pipelinePlanLowerer;
    private readonly MultiStatementPlanLowerer _multiStatementPlanLowerer;
    private readonly DescPlanLowerer _descPlanLowerer;
    private readonly PhysicalLoweringDispatchFacade _physicalLoweringFacade;

    public ExecutionPlanBuildResult Build(PhysicalNode physicalPlan, string identifier = "compiled")
    {
        ArgumentNullException.ThrowIfNull(physicalPlan);
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        var scope = _physicalLoweringFacade.CreateScope(_facts).WithGeneratedTypePrefix(_facts.GeneratedTypePrefix)
            .WithSubqueryCtes(_shapeResolver.Subqueries.Get(identifier));
        return BuildWithScope(physicalPlan, identifier, scope);
    }

    private ExecutionPlanBuildResult BuildWithScope(
        PhysicalNode physicalPlan,
        string identifier,
        LoweringScope scope)
    {
        var unwrapped = UnwrapSingleStatement(physicalPlan);
        _shapeResolver.Subqueries.Register(physicalPlan, new Dictionary<string, int>(),
            new Dictionary<string, GeneratedRowShape>(), scope.SubqueryCtes);
        var loweringContext = new PhysicalToExecutionLoweringContext(unwrapped, identifier, scope);

        var result = _physicalLoweringFacade.BuildPlan(loweringContext, CreateUnsupported);
        return result is { Supported: true, ExecutionPlan: { } executionPlan }
            ? result with
            {
                ExecutionPlan = ExecutionPlanRepresentationPlanner.Plan(
                    new NativeScriptGenericBinding(_shapeResolver.ScriptValues).RewritePlan(
                        PagingCountExecution.AddValidation(unwrapped, ExecutionPhaseBoundaryPlanner.AddRootBoundaries(unwrapped, executionPlan)))) with
                {
                    StructuralCarriers = ExecutionIrCollections.Freeze(_shapeResolver.InlineRecords.Carriers)
                }
            }
            : result;
    }

    private ExecutionStrategyPlan ExecutionStrategies => _facts.ExecutionStrategies;

    private IReadOnlyDictionary<string, SourceInteractionPlan> SourceInteractionPlans => _sourceInteractionPlans;

    private ExecutionPlanBuildResult BuildPipeline(
        CteSupportedPipeline pipeline,
        string identifier,
        LoweringScope scope)
    {
        var cteIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var result = BuildTable(
            pipeline,
            "result",
            scope.GeneratedTypePrefix + "ResultRow0",
            cteIndexes,
            cteShapesByName: scope.SubqueryCtes.ToDictionary(static binding => binding.Name, static binding => binding.Shape, StringComparer.OrdinalIgnoreCase),
            schemaFromIndex: DefaultSchemaFromIndex,
            scope: scope);

        if (!result.IsBuilt)
            return ExecutionPlanBuildResult.CreateUnsupported(result.UnsupportedReason);

        return ExecutionPlanBuildResult.CreateSupported(CreateTableResultPlan(identifier, result));
    }
}

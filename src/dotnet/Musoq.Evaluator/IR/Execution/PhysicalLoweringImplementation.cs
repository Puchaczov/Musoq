using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;
using Musoq.Evaluator.IR.Execution.Lowering;
using Musoq.Evaluator.IR.Execution.Lowering.Coordinators;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;

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
    private static readonly MethodInfo CreateNullableHashJoinKeyMethod = typeof(EvaluationHelper)
        .GetMethod(nameof(EvaluationHelper.CreateNullableHashJoinKey))!;

    private readonly PhysicalLoweringFacts _facts;
    private ExecutionShapeResolver _shapeResolver => _facts.ShapeResolver;
    private SchemaRegistry? _schemaRegistry => _facts.SchemaRegistry;
    private CompilationOptions _compilationOptions => _facts.CompilationOptions;
    private CteExecutionPlan? _cteExecutionPlan => _facts.CteExecutionPlan;
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

        var scope = _physicalLoweringFacade.CreateScope(_facts);
        return BuildWithScope(physicalPlan, identifier, scope);
    }

    private ExecutionPlanBuildResult BuildWithScope(
        PhysicalNode physicalPlan,
        string identifier,
        LoweringScope scope)
    {
        var unwrapped = UnwrapSingleStatement(physicalPlan);
        var loweringContext = new PhysicalToExecutionLoweringContext(unwrapped, identifier, scope);

        return _physicalLoweringFacade.BuildPlan(loweringContext, CreateUnsupported);
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
            "ResultRow0",
            cteIndexes,
            cteShapesByName: null,
            schemaFromIndex: DefaultSchemaFromIndex,
            scope: scope);

        if (!result.IsBuilt)
            return ExecutionPlanBuildResult.CreateUnsupported(result.UnsupportedReason);

        return ExecutionPlanBuildResult.CreateSupported(CreateTableResultPlan(identifier, result));
    }
}

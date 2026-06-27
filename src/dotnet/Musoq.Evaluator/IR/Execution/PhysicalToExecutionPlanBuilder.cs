using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;
using ExecutionStrategyPlan = Musoq.Evaluator.IR.Planning.ExecutionStrategyPlan;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
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

    private readonly ExecutionShapeResolver _shapeResolver;
    private readonly SchemaRegistry? _schemaRegistry;
    private readonly CompilationOptions _compilationOptions;
    private readonly CteExecutionPlan? _cteExecutionPlan;
    private readonly ExecutionPlanningArtifacts _executionPlanningArtifacts;
    private readonly IReadOnlyDictionary<string, SourceInteractionPlan> _sourceInteractionPlans;
    private ExecutionStrategyPlan? _executionStrategies;
    private IReadOnlyDictionary<string, FusedCteHashBuildSource>? _fusedCteHashBuildSources;
    private Dictionary<int, HashPayloadShape> _cteSidecarHashPayloadsBySlot = [];
    public ExecutionPlanBuildResult Build(PhysicalNode physicalPlan, string identifier = "compiled")
    {
        ArgumentNullException.ThrowIfNull(physicalPlan);
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        var previousExecutionStrategies = _executionStrategies;
        var previousCteSidecarHashPayloadsBySlot = _cteSidecarHashPayloadsBySlot;
        _executionStrategies = ResolveExecutionStrategies();
        _cteSidecarHashPayloadsBySlot = [];

        try
        {
            return BuildWithExecutionStrategies(physicalPlan, identifier);
        }
        finally
        {
            _executionStrategies = previousExecutionStrategies;
            _cteSidecarHashPayloadsBySlot = previousCteSidecarHashPayloadsBySlot;
        }
    }

    private ExecutionPlanBuildResult BuildWithExecutionStrategies(PhysicalNode physicalPlan, string identifier)
    {
        var unwrapped = UnwrapSingleStatement(physicalPlan);
        var loweringContext = new PhysicalToExecutionLoweringContext(unwrapped, identifier);

        if (unwrapped is PhysicalMultiStatementNode multiStatement)
            return BuildMultiStatement(multiStatement, identifier);

        if (new CteLoweringCoordinator(this).TryBuild(loweringContext, out var cteResult))
            return cteResult;

        if (unwrapped is PhysicalDescNode desc)
            return BuildDesc(desc, identifier);

        var setOperationPipeline = DecomposeSetOperationPipeline(unwrapped);
        if (setOperationPipeline != null)
            return BuildSetOperation(setOperationPipeline, identifier);

        if (new AggregateLoweringCoordinator(this).TryBuildPlan(loweringContext, out var aggregateResult))
            return aggregateResult;

        if (new WindowLoweringCoordinator(this).TryBuildPlan(loweringContext, out var windowResult))
            return windowResult;

        var pipeline = DecomposeSupportedPipeline(unwrapped);
        if (pipeline != null)
            return BuildPipeline(pipeline, identifier);

        return CreateUnsupported(unwrapped);
    }

    private ExecutionStrategyPlan ExecutionStrategies => _executionStrategies
        ?? throw new InvalidOperationException("Execution strategies must be resolved before lowering a physical plan.");

    private IReadOnlyDictionary<string, SourceInteractionPlan> SourceInteractionPlans => _sourceInteractionPlans;

    private ExecutionPlanBuildResult BuildPipeline(SupportedPipeline pipeline, string identifier)
    {
        var cteIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var result = BuildTable(pipeline, "result", "ResultRow0", cteIndexes);

        if (!result.Supported)
            return ExecutionPlanBuildResult.CreateUnsupported(result.UnsupportedReason);

        return ExecutionPlanBuildResult.CreateSupported(CreateTableResultPlan(identifier, result));
    }

}

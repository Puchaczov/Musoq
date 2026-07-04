using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;

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

    public ExecutionPlanBuildResult Build(PhysicalNode physicalPlan, string identifier = "compiled")
    {
        ArgumentNullException.ThrowIfNull(physicalPlan);
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        var session = new PhysicalToExecutionLoweringSession(ResolveExecutionStrategies());
        return BuildWithSession(physicalPlan, identifier, session);
    }

    private ExecutionPlanBuildResult BuildWithSession(
        PhysicalNode physicalPlan,
        string identifier,
        PhysicalToExecutionLoweringSession session)
    {
        var unwrapped = UnwrapSingleStatement(physicalPlan);
        var loweringContext = new PhysicalToExecutionLoweringContext(unwrapped, identifier, session);

        if (unwrapped is PhysicalMultiStatementNode multiStatement)
            return BuildMultiStatement(multiStatement, identifier, session);

        if (new CteLoweringCoordinator(BuildCte).TryBuild(loweringContext, out var cteResult))
            return cteResult;

        if (unwrapped is PhysicalDescNode desc)
            return BuildDesc(desc, identifier);

        var setOperationPipeline = DecomposeSetOperationPipeline(unwrapped);
        if (setOperationPipeline != null)
            return BuildSetOperation(setOperationPipeline, identifier, session);

        if (CreateAggregateLoweringCoordinator().TryBuildPlan(loweringContext, out var aggregateResult))
            return aggregateResult;

        if (CreateWindowLoweringCoordinator().TryBuildPlan(loweringContext, out var windowResult))
            return windowResult;

        var pipeline = DecomposeSupportedPipeline(unwrapped);
        if (pipeline != null)
            return BuildPipeline(pipeline, identifier, session);

        return CreateUnsupported(unwrapped);
    }

    private ExecutionStrategyPlan ExecutionStrategies => _executionPlanningArtifacts.ExecutionStrategies;

    private IReadOnlyDictionary<string, SourceInteractionPlan> SourceInteractionPlans => _sourceInteractionPlans;

    private ExecutionPlanBuildResult BuildPipeline(
        SupportedPipeline pipeline,
        string identifier,
        PhysicalToExecutionLoweringSession session)
    {
        var cteIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var result = BuildTable(pipeline, "result", "ResultRow0", cteIndexes, session: session);

        if (!result.Supported)
            return ExecutionPlanBuildResult.CreateUnsupported(result.UnsupportedReason);

        return ExecutionPlanBuildResult.CreateSupported(CreateTableResultPlan(identifier, result));
    }

}

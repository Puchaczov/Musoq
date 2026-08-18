global using Musoq.Evaluator.IR.Execution.Lowering;
global using Musoq.Evaluator.IR.Execution.Lowering.Aggregates;
global using Musoq.Evaluator.IR.Execution.Lowering.Common;
global using Musoq.Evaluator.IR.Execution.Lowering.Coordinators;
global using Musoq.Evaluator.IR.Execution.Lowering.Ctes;
global using Musoq.Evaluator.IR.Execution.Lowering.PostOperations;
global using Musoq.Evaluator.IR.Execution.Lowering.ProjectionAndApply;
global using Musoq.Evaluator.IR.Execution.Lowering.SetOperations;
global using Musoq.Evaluator.IR.Execution.Lowering.Sources;
global using Musoq.Evaluator.IR.Execution.Lowering.Tables;
global using Musoq.Evaluator.IR.Execution.Lowering.Windows;
global using Musoq.Evaluator.IR.Planning;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;
using ExecutionStrategyPlan = Musoq.Evaluator.IR.Planning.ExecutionStrategyPlan;

namespace Musoq.Evaluator.IR.Execution;

internal sealed class PhysicalLoweringFacts
{
    public PhysicalLoweringFacts(
        ExecutionShapeResolver shapeResolver,
        SchemaRegistry? schemaRegistry,
        CompilationOptions compilationOptions,
        CteExecutionPlan? cteExecutionPlan,
        ExecutionPlanningArtifacts executionArtifacts)
    {
        ShapeResolver = shapeResolver ?? throw new ArgumentNullException(nameof(shapeResolver));
        SchemaRegistry = schemaRegistry;
        CompilationOptions = compilationOptions ?? throw new ArgumentNullException(nameof(compilationOptions));
        CteExecutionPlan = cteExecutionPlan;
        ArgumentNullException.ThrowIfNull(executionArtifacts);
        ExecutionStrategies = executionArtifacts.ExecutionStrategies ??
            throw new ArgumentNullException(nameof(executionArtifacts.ExecutionStrategies));
        SourceInteractionPlansBySourceId = new ReadOnlyDictionary<string, SourceInteractionPlan>(
            new Dictionary<string, SourceInteractionPlan>(
                executionArtifacts.SourceInteractionPlansBySourceId ??
                new Dictionary<string, SourceInteractionPlan>(StringComparer.Ordinal),
                StringComparer.Ordinal));
        SourceTransferPlansBySourceId = new ReadOnlyDictionary<string, SourceTransferStrategyPlan>(
            new Dictionary<string, SourceTransferStrategyPlan>(
                executionArtifacts.SourceTransferPlansBySourceId ??
                new Dictionary<string, SourceTransferStrategyPlan>(StringComparer.Ordinal),
                StringComparer.Ordinal));
    }

    public ExecutionShapeResolver ShapeResolver { get; }

    public SchemaRegistry? SchemaRegistry { get; }

    public CompilationOptions CompilationOptions { get; }

    public CteExecutionPlan? CteExecutionPlan { get; }

    public ExecutionStrategyPlan ExecutionStrategies { get; }

    public IReadOnlyDictionary<string, SourceInteractionPlan> SourceInteractionPlansBySourceId { get; }

    public IReadOnlyDictionary<string, SourceTransferStrategyPlan> SourceTransferPlansBySourceId { get; }
}

internal sealed record RecursiveCteLoweringContext(
    RecursiveCteTableSink? Sink,
    IReadOnlyDictionary<string, RecursiveCteInvariantInput> InvariantInputs)
{
    public static RecursiveCteLoweringContext Empty => new(
        null,
        new Dictionary<string, RecursiveCteInvariantInput>(StringComparer.Ordinal));
}

internal sealed record DirectTableLoweringContext(IDirectTableSink? Sink)
{
    public static DirectTableLoweringContext Empty { get; } =
        new DirectTableLoweringContext((IDirectTableSink?)null);
}

internal sealed record CteLoweringContext(
    IReadOnlyDictionary<string, FusedCteHashBuildSource>? FusedHashBuildSources,
    CteSidecarHashPayloadState SidecarHashPayloads,
    bool SuppressSidecarJoinPipeline,
    IReadOnlyDictionary<string, ScalarSubqueryEmptyResultSpec> ScalarSubqueryEmptyResults,
    RecursiveCteLoweringContext RecursiveCte)
{
    public static CteLoweringContext Empty => new(
        null,
        new CteSidecarHashPayloadState(),
        false,
        new Dictionary<string, ScalarSubqueryEmptyResultSpec>(StringComparer.OrdinalIgnoreCase),
        RecursiveCteLoweringContext.Empty);
}

internal sealed class CteSidecarHashPayloadState
{
    private readonly IReadOnlyDictionary<int, HashPayloadShape> _payloadsBySlot;

    public CteSidecarHashPayloadState()
        : this(new Dictionary<int, HashPayloadShape>())
    {
    }

    private CteSidecarHashPayloadState(IReadOnlyDictionary<int, HashPayloadShape> payloadsBySlot)
    {
        _payloadsBySlot = new ReadOnlyDictionary<int, HashPayloadShape>(
            new Dictionary<int, HashPayloadShape>(payloadsBySlot));
    }

    public IReadOnlyDictionary<int, HashPayloadShape> Snapshot => _payloadsBySlot;

    public CteSidecarHashPayloadState WithPayload(int indexSlot, HashPayloadShape payloadShape)
    {
        ArgumentNullException.ThrowIfNull(payloadShape);
        var payloads = new Dictionary<int, HashPayloadShape>(_payloadsBySlot)
        {
            [indexSlot] = payloadShape
        };
        return new CteSidecarHashPayloadState(payloads);
    }

    public bool TryGet(int indexSlot, out HashPayloadShape payloadShape) =>
        _payloadsBySlot.TryGetValue(indexSlot, out payloadShape!);
}

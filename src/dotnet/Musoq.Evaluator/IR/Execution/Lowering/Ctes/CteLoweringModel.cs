using System;
using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Execution;

internal sealed record MultiStatementIndexes(
    IReadOnlyDictionary<string, int> CteIndexes,
    IReadOnlyDictionary<string, int> ProducerIndexByName,
    Dictionary<string, GeneratedRowShape> CteShapesByName,
    string? StatementNamePrefix);

internal sealed record ParallelCteLevel(
    int Level,
    IReadOnlyList<PhysicalCteDefinition> Definitions);

internal sealed record CteDefinitionPrefixBuildResult(
    bool Supported,
    IReadOnlyList<RowShape> Shapes,
    IReadOnlyList<ExecutionNode> Nodes,
    string UnsupportedReason)
{
    public static CteDefinitionPrefixBuildResult Success(
        IReadOnlyList<RowShape> shapes,
        IReadOnlyList<ExecutionNode> nodes)
    {
        return new CteDefinitionPrefixBuildResult(true, shapes, nodes, string.Empty);
    }

    public static CteDefinitionPrefixBuildResult Unsupported(string reason)
    {
        return new CteDefinitionPrefixBuildResult(false, [], [], reason);
    }
}

internal sealed record CteDefinitionPruningPlan(
    IReadOnlyDictionary<string, IReadOnlySet<string>> RequiredColumnsByName,
    IReadOnlySet<string> ContextFreeDefinitions)
{
    public static CteDefinitionPruningPlan Empty { get; } = new(
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase),
        new HashSet<string>(StringComparer.OrdinalIgnoreCase));

    public bool TryGetRequiredColumns(string definitionName, out IReadOnlySet<string> columns)
    {
        return RequiredColumnsByName.TryGetValue(definitionName, out columns!);
    }

    public bool CanDropContexts(string definitionName)
    {
        return ContextFreeDefinitions.Contains(definitionName);
    }
}

internal sealed record CteSidecarIndexBuild(
    CteSidecarIndexSpec Spec,
    ExecutionVariable Index,
    HashPayloadShape? PayloadShape);

internal sealed record CteSidecarAppendTransformResult(
    ExecutionBlock Block,
    int AppendCount,
    ExecutionCapacityHint? CapacityHint);

internal sealed record CteSidecarAppendNodeTransformResult(
    ExecutionNode Node,
    int AppendCount,
    ExecutionCapacityHint? CapacityHint);

internal sealed record FusedSiblingCteBuildResult(
    int DefinitionCount,
    IReadOnlyList<RowShape> Shapes,
    ExecutionFusedCteProducer Producer,
    IReadOnlyDictionary<string, GeneratedRowShape> RowShapesByName);

internal sealed record FusedSiblingCteCandidate(
    string DefinitionName,
    int TableIndex,
    TableBuildResult Result,
    CteSidecarStorageDecision Storage,
    IReadOnlyList<ExecutionNode> SetupNodes,
    ExecutionForEach Loop,
    IReadOnlyList<ExecutionNode> StoreIndexNodes);

internal sealed record ReadOnceCteProjectionFusion(
    int RootDefinitionIndex,
    SupportedPipeline Pipeline,
    int[] FusedDefinitionIndexes);

internal sealed record ReadOnceCteProjectionStep(
    int DefinitionIndex,
    SupportedPipeline Pipeline);

internal sealed record SidecarJoinPipelineStage(
    SupportedPipeline Pipeline,
    string? ExpectedInputCteName,
    string? OutputCteName);

internal enum SidecarJoinPipelineStageKind
{
    Projection,
    IndexedHashJoin,
    IndexedKeySetJoin,
    StandardJoin,
    AsOfJoin,
    Apply,
    CrossJoin
}

internal sealed record SidecarJoinPipelineStageAnalysis(
    SidecarJoinPipelineStageKind Kind,
    string Description);

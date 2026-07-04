using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

internal static class ExecutionNodeRegistry
{
    private static readonly IReadOnlyList<ExecutionBlock> EmptyBlocks = [];
    private static readonly IReadOnlyDictionary<Type, ExecutionNodeDescriptor> DescriptorsByType = CreateDescriptors()
        .ToDictionary(static descriptor => descriptor.NodeType);

    public static IReadOnlyCollection<ExecutionNodeDescriptor> Descriptors => DescriptorsByType.Values.ToArray();

    public static bool TryGetDescriptor(ExecutionNode node, out ExecutionNodeDescriptor descriptor)
    {
        return DescriptorsByType.TryGetValue(node.GetType(), out descriptor!);
    }

    public static ExecutionRendererNodeFamily GetRendererFamily(ExecutionNode node)
    {
        return TryGetDescriptor(node, out var descriptor)
            ? descriptor.RendererFamily
            : ExecutionRendererNodeFamily.Unsupported;
    }

    public static IReadOnlyList<ExecutionBlock> GetChildBlocks(ExecutionNode node)
    {
        return TryGetDescriptor(node, out var descriptor)
            ? descriptor.GetChildBlocks(node)
            : EmptyBlocks;
    }

    private static IEnumerable<ExecutionNodeDescriptor> CreateDescriptors()
    {
        yield return Descriptor<ExecutionSourceScan>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionInterpretSource>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionEnumerableSource>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionCreateTable>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionCreateValuesRows>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionCreateRecordList>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionCreateBoundedRecordList>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionEnsureTableCapacity>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionForEach>(ExecutionRendererNodeFamily.TableControlFlow, ExecutionNodeChildBlockShape.Single, static node => [node.Body]);
        yield return Descriptor<ExecutionForEachWithOrdinality>(ExecutionRendererNodeFamily.TableControlFlow, ExecutionNodeChildBlockShape.Single, static node => [node.Body]);
        yield return Descriptor<ExecutionScopedBlock>(ExecutionRendererNodeFamily.TableControlFlow, ExecutionNodeChildBlockShape.Single, static node => [node.Body]);
        yield return Descriptor<ExecutionForEachIndexed>(ExecutionRendererNodeFamily.TableControlFlow, ExecutionNodeChildBlockShape.Single, static node => [node.Body]);
        yield return Descriptor<ExecutionParallelBlock>(ExecutionRendererNodeFamily.TableControlFlow, ExecutionNodeChildBlockShape.Multiple, static node => [.. node.Tasks.Select(static task => task.Body), node.Merge.Body]);
        yield return Descriptor<ExecutionFusedCteProducer>(ExecutionRendererNodeFamily.TableControlFlow, ExecutionNodeChildBlockShape.Single, static node => [node.Body]);
        yield return Descriptor<ExecutionLet>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionHoistCandidateLet>(ExecutionRendererNodeFamily.Unsupported);
        yield return Descriptor<ExecutionAssign>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionCreateBooleanArray>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionArrayAssign>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionContinue>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionContinueIf>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionBreak>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionAdaptExpando>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionCreateObject>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionMethodTargetDeclarationCandidate>(ExecutionRendererNodeFamily.Unsupported);
        yield return Descriptor<ExecutionIf>(ExecutionRendererNodeFamily.TableControlFlow, ExecutionNodeChildBlockShape.Single, static node => [node.Body]);
        yield return Descriptor<ExecutionCreateGeneratedRow>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionAppendRow>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionAppendExistingRow>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionAppendRecord>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionMaterializeList>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionMaterializeFilteredList>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionMaterializeExpandoList>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionSetOperation>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionDistinctTable>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionSortTable>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionTopNTable>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionTopOffsetTable>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionSkipTable>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionTakeTable>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionSliceTable>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionProjectTable>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionOrderRecordList>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionMaterializeRecordListToTable>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionStoreTable>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionRelatedCtePhase>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionReturnDesc>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionReturnTable>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionCreateAggregateLibrary>(ExecutionRendererNodeFamily.Aggregate);
        yield return Descriptor<ExecutionCreateAggregateContext>(ExecutionRendererNodeFamily.Aggregate);
        yield return Descriptor<ExecutionEnsureAggregateGroup>(ExecutionRendererNodeFamily.Aggregate);
        yield return Descriptor<ExecutionCreateSingleKeyAggregateContext>(ExecutionRendererNodeFamily.Aggregate);
        yield return Descriptor<ExecutionGetOrAddSingleKeyAggregateGroup>(ExecutionRendererNodeFamily.Aggregate);
        yield return Descriptor<ExecutionParallelSingleKeyAggregateLoop>(ExecutionRendererNodeFamily.Aggregate, ExecutionNodeChildBlockShape.Single, static node => [node.AggregateBody]);
        yield return Descriptor<ExecutionCreateValueTupleAggregateContext>(ExecutionRendererNodeFamily.Aggregate);
        yield return Descriptor<ExecutionGetOrAddValueTupleAggregateGroup>(ExecutionRendererNodeFamily.Aggregate);
        yield return Descriptor<ExecutionAggregateSet>(ExecutionRendererNodeFamily.Aggregate);
        yield return Descriptor<ExecutionAggregateCapturedValueSet>(ExecutionRendererNodeFamily.Aggregate);
        yield return Descriptor<ExecutionParallelFilterProjectLoop>(ExecutionRendererNodeFamily.Join, ExecutionNodeChildBlockShape.Single, static node => [node.ProjectionBody]);
        yield return Descriptor<ExecutionCreateHashPayload>(ExecutionRendererNodeFamily.Join);
        yield return Descriptor<ExecutionWindowKernelPlan>(ExecutionRendererNodeFamily.Window, ExecutionNodeChildBlockShape.Multiple, static node => [new ExecutionBlock(node.Kernels)]);
        yield return Descriptor<ExecutionComputeRankingWindow>(ExecutionRendererNodeFamily.Window);
        yield return Descriptor<ExecutionComputeOffsetWindow>(ExecutionRendererNodeFamily.Window);
        yield return Descriptor<ExecutionComputePluginWindow>(ExecutionRendererNodeFamily.Window);
        yield return Descriptor<ExecutionWindowAggregateKernel>(ExecutionRendererNodeFamily.Window);
        yield return Descriptor<ExecutionCreateHash>(ExecutionRendererNodeFamily.Index);
        yield return Descriptor<ExecutionHashAdd>(ExecutionRendererNodeFamily.Index);
        yield return Descriptor<ExecutionHashProbe>(ExecutionRendererNodeFamily.Index, ExecutionNodeChildBlockShape.Multiple, static node => AppendOptionalBlock(node.Body, node.NoMatchBody));
        yield return Descriptor<ExecutionCreateKeySet>(ExecutionRendererNodeFamily.Index);
        yield return Descriptor<ExecutionKeySetAdd>(ExecutionRendererNodeFamily.Index);
        yield return Descriptor<ExecutionKeySetProbe>(ExecutionRendererNodeFamily.Index, ExecutionNodeChildBlockShape.Multiple, static node => AppendOptionalBlock(node.Body, node.NoMatchBody));
        yield return Descriptor<ExecutionStoreCteIndex>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionLoadCteIndex>(ExecutionRendererNodeFamily.TableControlFlow);
        yield return Descriptor<ExecutionCreateAsOfIndex>(ExecutionRendererNodeFamily.Index);
        yield return Descriptor<ExecutionAsOfProbe>(ExecutionRendererNodeFamily.Index, ExecutionNodeChildBlockShape.Multiple, static node => AppendOptionalBlock(node.Body, node.NoMatchBody));
        yield return Descriptor<ExecutionCreateRangeIndex>(ExecutionRendererNodeFamily.Index);
        yield return Descriptor<ExecutionRangeProbe>(ExecutionRendererNodeFamily.Index, ExecutionNodeChildBlockShape.Single, static node => [node.Body]);
        yield return Descriptor<ExecutionSingleUsePipelineFusionCandidate>(ExecutionRendererNodeFamily.Unsupported, ExecutionNodeChildBlockShape.Single, static node => [node.Body]);
        yield return Descriptor<ExecutionCteReadOnceFusionCandidate>(ExecutionRendererNodeFamily.Unsupported, ExecutionNodeChildBlockShape.Single, static node => [node.Body]);
        yield return Descriptor<ExecutionCteSidecarIndexStoreCandidate>(ExecutionRendererNodeFamily.Unsupported);
        yield return Descriptor<ExecutionCteSidecarIndexLoadCandidate>(ExecutionRendererNodeFamily.Unsupported);
        yield return Descriptor<ExecutionCteSidecarIndexBuildCandidate>(ExecutionRendererNodeFamily.Unsupported);
        yield return Descriptor<ExecutionCteSidecarAppendRewriteCandidate>(ExecutionRendererNodeFamily.Unsupported);
        yield return Descriptor<ExecutionCteFusedProducerCandidate>(ExecutionRendererNodeFamily.Unsupported, ExecutionNodeChildBlockShape.Single, static node => [node.Body]);
        yield return Descriptor<ExecutionCteIndexOnlyStorageCandidate>(ExecutionRendererNodeFamily.Unsupported);
    }

    private static ExecutionNodeDescriptor Descriptor<TNode>(
        ExecutionRendererNodeFamily rendererFamily,
        ExecutionNodeChildBlockShape childBlockShape = ExecutionNodeChildBlockShape.None,
        Func<TNode, IReadOnlyList<ExecutionBlock>>? childBlocks = null)
        where TNode : ExecutionNode
    {
        return new ExecutionNodeDescriptor(
            typeof(TNode),
            rendererFamily,
            childBlockShape,
            node => childBlocks == null ? EmptyBlocks : childBlocks((TNode)node));
    }

    private static IReadOnlyList<ExecutionBlock> AppendOptionalBlock(ExecutionBlock body, ExecutionBlock? optional)
    {
        return optional == null ? [body] : [body, optional];
    }
}

internal sealed record ExecutionNodeDescriptor(
    Type NodeType,
    ExecutionRendererNodeFamily RendererFamily,
    ExecutionNodeChildBlockShape ChildBlockShape,
    Func<ExecutionNode, IReadOnlyList<ExecutionBlock>> GetChildBlocks);

internal enum ExecutionNodeChildBlockShape
{
    None,
    Single,
    Multiple
}

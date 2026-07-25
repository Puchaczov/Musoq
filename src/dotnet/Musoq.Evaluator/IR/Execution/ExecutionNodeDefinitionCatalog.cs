using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Musoq.Targets.Abstractions;

namespace Musoq.Evaluator.IR.Execution;

internal static class ExecutionNodeDefinitionCatalog
{
    private static readonly IReadOnlyList<ExecutionNodeDefinition> DefinitionList = ValidateDefinitions(CreateDefinitions().ToArray());

    public static IReadOnlyList<ExecutionNodeDefinition> Definitions => DefinitionList;

    private static IEnumerable<ExecutionNodeDefinition> CreateDefinitions()
    {
        yield return Definition<ExecutionSourceScan>("source.scan", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionInterpretSource>("source.interpret", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionEnumerableSource>("source.enumerable", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionCreateTable>("table.create", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionCreateValuesRows>("table.values.create", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionCreateRecordList>("record-list.create", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionCreateBoundedRecordList>("record-list.bounded.create", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionEnsureTableCapacity>("table.capacity.ensure", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionForEach>("control.foreach", ExecutionRendererNodeFamily.TableControlFlow, ExecutionNodeChildBlockShape.Single, static node => [node.Body]);
        yield return Definition<ExecutionForEachWithOrdinality>("control.foreach.ordinality", ExecutionRendererNodeFamily.TableControlFlow, ExecutionNodeChildBlockShape.Single, static node => [node.Body]);
        yield return Definition<ExecutionScopedBlock>("control.scope", ExecutionRendererNodeFamily.TableControlFlow, ExecutionNodeChildBlockShape.Single, static node => [node.Body]);
        yield return Definition<ExecutionForEachIndexed>("control.foreach.indexed", ExecutionRendererNodeFamily.TableControlFlow, ExecutionNodeChildBlockShape.Single, static node => [node.Body]);
        yield return Definition<ExecutionParallelBlock>("control.parallel", ExecutionRendererNodeFamily.TableControlFlow, ExecutionNodeChildBlockShape.Multiple, static node => [.. node.Tasks.Select(static task => task.Body), node.Merge.Body]);
        yield return Definition<ExecutionFusedCteProducer>("cte.producer.fused", ExecutionRendererNodeFamily.TableControlFlow, ExecutionNodeChildBlockShape.Single, static node => [node.Body]);
        yield return Definition<ExecutionLet>("variable.let", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionHoistCandidateLet>("optimizer.hoist-candidate", ExecutionRendererNodeFamily.Unsupported);
        yield return Definition<ExecutionAssign>("variable.assign", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionCreateBooleanArray>("array.boolean.create", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionArrayAssign>("array.assign", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionContinue>("control.continue", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionContinueIf>("control.continue-if", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionBreak>("control.break", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionAdaptExpando>("row.adapt-expando", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionCreateObject>("object.create", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionMethodTargetDeclarationCandidate>("optimizer.method-target-declaration", ExecutionRendererNodeFamily.Unsupported);
        yield return Definition<ExecutionIf>("control.if", ExecutionRendererNodeFamily.TableControlFlow, ExecutionNodeChildBlockShape.Single, static node => [node.Body]);
        yield return Definition<ExecutionCreateGeneratedRow>("row.generated.create", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionRecursiveCte>("cte.recursive", ExecutionRendererNodeFamily.TableControlFlow, ExecutionNodeChildBlockShape.Multiple, static node => [node.Anchor, node.InvariantSetup, node.RecursiveMember]);
        yield return Definition<ExecutionRecursiveCteAppend>("cte.recursive.append", ExecutionRendererNodeFamily.TableControlFlow, ExecutionNodeChildBlockShape.Single, static node => [new ExecutionBlock([node.AppendRow])]);
        yield return Definition<ExecutionRecursiveCteSnapshotRowGuard>("cte.recursive.snapshot-guard", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionAppendRow>("table.row.append", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionAppendExistingRow>("table.row.append-existing", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionAppendRecord>("record-list.append", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionMaterializeList>("collection.materialize", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionMaterializeFilteredList>("collection.materialize-filtered", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionMaterializeExpandoList>("collection.materialize-expando", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionSetOperation>("set.operation", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionDistinctTable>("table.distinct", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionSortTable>("table.sort", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionTopNTable>("table.top", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionTopOffsetTable>("table.top-offset", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionSkipTable>("table.skip", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionTakeTable>("table.take", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionSliceTable>("table.slice", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionProjectTable>("table.project", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionOrderRecordList>("record-list.order", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionMaterializeRecordListToTable>("record-list.to-table", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionStoreTable>("cte.table.store", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionRelatedCtePhase>("cte.phase.related", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionReturnDesc>("return.desc", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionReturnTable>("return.table", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionCreateAggregateLibrary>("aggregate.library.create", ExecutionRendererNodeFamily.Aggregate);
        yield return Definition<ExecutionCreateAggregateContext>("aggregate.context.create", ExecutionRendererNodeFamily.Aggregate);
        yield return Definition<ExecutionEnsureAggregateGroup>("aggregate.group.ensure", ExecutionRendererNodeFamily.Aggregate);
        yield return Definition<ExecutionCreateSingleKeyAggregateContext>("aggregate.single-key.context.create", ExecutionRendererNodeFamily.Aggregate);
        yield return Definition<ExecutionGetOrAddSingleKeyAggregateGroup>("aggregate.single-key.group.get-or-add", ExecutionRendererNodeFamily.Aggregate);
        yield return Definition<ExecutionParallelSingleKeyAggregateLoop>("aggregate.single-key.parallel-loop", ExecutionRendererNodeFamily.Aggregate, ExecutionNodeChildBlockShape.Single, static node => [node.AggregateBody]);
        yield return Definition<ExecutionCreateValueTupleAggregateContext>("aggregate.tuple.context.create", ExecutionRendererNodeFamily.Aggregate);
        yield return Definition<ExecutionGetOrAddValueTupleAggregateGroup>("aggregate.tuple.group.get-or-add", ExecutionRendererNodeFamily.Aggregate);
        yield return Definition<ExecutionAggregateSet>("aggregate.accumulator.set", ExecutionRendererNodeFamily.Aggregate);
        yield return Definition<ExecutionAggregateCapturedValueSet>("aggregate.capture.set", ExecutionRendererNodeFamily.Aggregate);
        yield return Definition<ExecutionParallelFilterProjectLoop>("join.parallel-filter-project", ExecutionRendererNodeFamily.Join, ExecutionNodeChildBlockShape.Single, static node => [node.ProjectionBody]);
        yield return Definition<ExecutionCreateHashPayload>("hash.payload.create", ExecutionRendererNodeFamily.Join);
        yield return Definition<ExecutionWindowKernelPlan>("window.kernel-plan", ExecutionRendererNodeFamily.Window, ExecutionNodeChildBlockShape.Multiple, static node => [new ExecutionBlock(node.Kernels)]);
        yield return Definition<ExecutionComputeRankingWindow>("window.ranking", ExecutionRendererNodeFamily.Window);
        yield return Definition<ExecutionComputeOffsetWindow>("window.offset", ExecutionRendererNodeFamily.Window);
        yield return Definition<ExecutionComputePluginWindow>("window.plugin", ExecutionRendererNodeFamily.Window);
        yield return Definition<ExecutionWindowAggregateKernel>("window.aggregate", ExecutionRendererNodeFamily.Window);
        yield return Definition<ExecutionCreateHash>("hash.create", ExecutionRendererNodeFamily.Index);
        yield return Definition<ExecutionHashAdd>("hash.add", ExecutionRendererNodeFamily.Index);
        yield return Definition<ExecutionHashProbe>("hash.probe", ExecutionRendererNodeFamily.Index, ExecutionNodeChildBlockShape.Multiple, static node => AppendOptionalBlock(node.Body, node.NoMatchBody));
        yield return Definition<ExecutionCreateKeySet>("keyset.create", ExecutionRendererNodeFamily.Index);
        yield return Definition<ExecutionKeySetAdd>("keyset.add", ExecutionRendererNodeFamily.Index);
        yield return Definition<ExecutionKeySetProbe>("keyset.probe", ExecutionRendererNodeFamily.Index, ExecutionNodeChildBlockShape.Multiple, static node => AppendOptionalBlock(node.Body, node.NoMatchBody));
        yield return Definition<ExecutionStoreCteIndex>("cte.index.store", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionLoadCteIndex>("cte.index.load", ExecutionRendererNodeFamily.TableControlFlow);
        yield return Definition<ExecutionCreateAsOfIndex>("asof.index.create", ExecutionRendererNodeFamily.Index);
        yield return Definition<ExecutionAsOfProbe>("asof.probe", ExecutionRendererNodeFamily.Index, ExecutionNodeChildBlockShape.Multiple, static node => AppendOptionalBlock(node.Body, node.NoMatchBody));
        yield return Definition<ExecutionCreateRangeIndex>("range.index.create", ExecutionRendererNodeFamily.Index);
        yield return Definition<ExecutionRangeProbe>("range.probe", ExecutionRendererNodeFamily.Index, ExecutionNodeChildBlockShape.Multiple, static node => AppendOptionalBlock(node.Body, node.NoMatchBody));
        yield return Definition<ExecutionSingleUsePipelineFusionCandidate>("optimizer.pipeline-fusion-candidate", ExecutionRendererNodeFamily.Unsupported, ExecutionNodeChildBlockShape.Single, static node => [node.Body]);
        yield return Definition<ExecutionCteReadOnceFusionCandidate>("optimizer.cte-read-once-candidate", ExecutionRendererNodeFamily.Unsupported, ExecutionNodeChildBlockShape.Single, static node => [node.Body]);
        yield return Definition<ExecutionCteSidecarIndexStoreCandidate>("optimizer.cte-sidecar-store-candidate", ExecutionRendererNodeFamily.Unsupported);
        yield return Definition<ExecutionCteSidecarIndexLoadCandidate>("optimizer.cte-sidecar-load-candidate", ExecutionRendererNodeFamily.Unsupported);
        yield return Definition<ExecutionCteSidecarIndexBuildCandidate>("optimizer.cte-sidecar-build-candidate", ExecutionRendererNodeFamily.Unsupported);
        yield return Definition<ExecutionCteSidecarAppendRewriteCandidate>("optimizer.cte-sidecar-append-candidate", ExecutionRendererNodeFamily.Unsupported);
        yield return Definition<ExecutionCteFusedProducerCandidate>("optimizer.cte-fused-producer-candidate", ExecutionRendererNodeFamily.Unsupported, ExecutionNodeChildBlockShape.Single, static node => [node.Body]);
        yield return Definition<ExecutionCteIndexOnlyStorageCandidate>("optimizer.cte-index-only-candidate", ExecutionRendererNodeFamily.Unsupported);
    }

    private static ExecutionNodeDefinition Definition<TNode>(
        string operationId,
        ExecutionRendererNodeFamily rendererFamily,
        ExecutionNodeChildBlockShape childBlockShape = ExecutionNodeChildBlockShape.None,
        Func<TNode, IReadOnlyList<ExecutionBlock>>? childBlocks = null)
        where TNode : ExecutionNode
    {
        return new ExecutionNodeDefinition(
            typeof(TNode),
            new ExecutionOperationId(operationId),
            rendererFamily,
            childBlockShape,
            childBlocks == null
                ? static _ => []
                : node => childBlocks((TNode)node),
            new ExecutionNodeBehaviorDefinition(
                static (builder, node, indentation) =>
                    ExecutionPlanPrinter.AppendNodeLegacy(builder, node, indentation),
                static (rewriter, node) => rewriter.RewriteNodeLegacy(node),
                rendererFamily == ExecutionRendererNodeFamily.Unsupported
                    ? ExecutionNodeTargetCapability.Unsupported
                    : ExecutionNodeTargetCapability.Supported));
    }

    private static IReadOnlyList<ExecutionNodeDefinition> ValidateDefinitions(
        IReadOnlyList<ExecutionNodeDefinition> definitions)
    {
        if (definitions.Any(static definition =>
                definition.Behavior.Printer is null ||
                definition.Behavior.Rewriter is null))
        {
            throw new InvalidOperationException(
                "Every execution node definition must register printer and rewriter behavior.");
        }

        return definitions;
    }

    private static IReadOnlyList<ExecutionBlock> AppendOptionalBlock(ExecutionBlock body, ExecutionBlock? optional) =>
        optional == null ? [body] : [body, optional];
}

internal sealed record ExecutionNodeDefinition(
    Type NodeType,
    ExecutionOperationId OperationId,
    ExecutionRendererNodeFamily RendererFamily,
    ExecutionNodeChildBlockShape ChildBlockShape,
    Func<ExecutionNode, IReadOnlyList<ExecutionBlock>> GetChildBlocks,
    ExecutionNodeBehaviorDefinition Behavior);

internal sealed record ExecutionNodeBehaviorDefinition(
    ExecutionNodePrinterBehavior Printer,
    ExecutionNodeRewriterBehavior Rewriter,
    ExecutionNodeTargetCapability TargetCapability);

internal delegate void ExecutionNodePrinterBehavior(
    StringBuilder builder,
    ExecutionNode node,
    int indentation);

internal delegate ExecutionNode ExecutionNodeRewriterBehavior(
    ExecutionIrRewriter rewriter,
    ExecutionNode node);

internal enum ExecutionNodeTargetCapability
{
    Supported,
    Unsupported
}

internal enum ExecutionNodeChildBlockShape
{
    None,
    Single,
    Multiple
}

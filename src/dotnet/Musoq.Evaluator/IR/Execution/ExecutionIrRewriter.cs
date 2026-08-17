namespace Musoq.Evaluator.IR.Execution;

internal abstract partial class ExecutionIrRewriter
{
    public virtual ExecutionPlan RewritePlan(ExecutionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var body = RewriteBlock(plan.Body);
        return ReferenceEquals(body, plan.Body)
            ? plan
            : plan with { Body = body };
    }

    public virtual ExecutionBlock RewriteBlock(ExecutionBlock block)
    {
        ArgumentNullException.ThrowIfNull(block);

        var nodes = RewriteList(block.Nodes, RewriteNode);
        return ReferenceEquals(nodes, block.Nodes)
            ? block
            : block with { Nodes = nodes };
    }

    public virtual ExecutionNode RewriteNode(ExecutionNode node) {
        ArgumentNullException.ThrowIfNull(node);

        return ExecutionNodeRegistry.TryGetDescriptor(node, out var descriptor)
            ? descriptor.Behavior.Rewriter(this, node)
            : RewriteNodeLegacy(node);
    }

    internal ExecutionNode RewriteNodeLegacy(ExecutionNode node) {
        ArgumentNullException.ThrowIfNull(node);

        return node switch
        {
            ExecutionSourceScan sourceScan => RewriteSourceScan(sourceScan),
            ExecutionInterpretSource interpret => RewriteInterpretSource(interpret),
            ExecutionEnumerableSource enumerable => RewriteEnumerableSource(enumerable),
            ExecutionCreateTable createTable => RewriteCreateTable(createTable),
            ExecutionCreateRecordList createList => RewriteCreateRecordList(createList),
            ExecutionCreateValuesRows createValuesRows => RewriteCreateValuesRows(createValuesRows),
            ExecutionCreateBoundedRecordList createList => RewriteCreateBoundedRecordList(createList),
            ExecutionForEach forEach => RewriteForEach(forEach),
            ExecutionForEachWithOrdinality forEach => RewriteForEachWithOrdinality(forEach),
            ExecutionScopedBlock scopedBlock => RewriteBlockOwner(scopedBlock, scopedBlock.Body, body => scopedBlock with { Body = body }),
            ExecutionForEachIndexed forEachIndexed => RewriteForEachIndexed(forEachIndexed),
            ExecutionParallelBlock parallel => RewriteParallelBlock(parallel),
            ExecutionParallelFilterProjectLoop parallelProject => RewriteParallelFilterProjectLoop(parallelProject),
            ExecutionParallelSingleKeyAggregateLoop parallelAggregate => RewriteParallelSingleKeyAggregateLoop(parallelAggregate),
            ExecutionLet let => RewriteLet(let),
            ExecutionHoistCandidateLet candidate => RewriteHoistCandidateLet(candidate),
            ExecutionAssign assign => RewriteAssign(assign),
            ExecutionCreateBooleanArray createArray => RewriteCreateBooleanArray(createArray),
            ExecutionArrayAssign arrayAssign => RewriteArrayAssign(arrayAssign),
            ExecutionContinue continueNode => RewriteContinue(continueNode),
            ExecutionContinueIf continueIf => RewriteContinueIf(continueIf),
            ExecutionBreak breakNode => RewriteBreak(breakNode),
            ExecutionAdaptExpando adapt => RewriteAdaptExpando(adapt),
            ExecutionCreateObject createObject => RewriteCreateObject(createObject),
            ExecutionMethodTargetDeclarationCandidate candidate => RewriteMethodTargetDeclarationCandidate(candidate),
            ExecutionIf ifNode => RewriteIf(ifNode),
            ExecutionCreateGeneratedRow createRow => RewriteCreateGeneratedRow(createRow),
            ExecutionRecursiveCte recursiveCte => RewriteRecursiveCte(recursiveCte),
            ExecutionRecursiveCteAppend recursiveAppend => RewriteRecursiveCteAppend(recursiveAppend),
            ExecutionRecursiveCteSnapshotRowGuard snapshotGuard => RewriteRecursiveCteSnapshotRowGuard(snapshotGuard),
            ExecutionCreateHashPayload createPayload => RewriteCreateHashPayload(createPayload),
            ExecutionAppendRow appendRow => RewriteAppendRow(appendRow),
            ExecutionAppendExistingRow appendExistingRow => RewriteAppendExistingRow(appendExistingRow),
            ExecutionAppendRecord appendRecord => RewriteAppendRecord(appendRecord),
            ExecutionMaterializeList materialize => RewriteMaterializeList(materialize),
            ExecutionMaterializeFilteredList materialize => RewriteMaterializeFilteredList(materialize),
            ExecutionMaterializeExpandoList materialize => RewriteMaterializeExpandoList(materialize),
            ExecutionWindowKernelPlan plan => RewriteWindowKernelPlan(plan),
            ExecutionComputeRankingWindow ranking => RewriteComputeRankingWindow(ranking),
            ExecutionComputeOffsetWindow offset => RewriteComputeOffsetWindow(offset),
            ExecutionComputePluginWindow plugin => RewriteComputePluginWindow(plugin),
            ExecutionWindowAggregateKernel kernel => RewriteWindowAggregateKernel(kernel),
            ExecutionCreateHash createHash => RewriteCreateHash(createHash),
            ExecutionHashAdd hashAdd => RewriteHashAdd(hashAdd),
            ExecutionHashProbe hashProbe => RewriteHashProbe(hashProbe),
            ExecutionCreateKeySet createKeySet => RewriteCreateKeySet(createKeySet),
            ExecutionKeySetAdd keySetAdd => RewriteKeySetAdd(keySetAdd),
            ExecutionKeySetProbe keySetProbe => RewriteKeySetProbe(keySetProbe),
            ExecutionStoreCteIndex storeCteIndex => RewriteStoreCteIndex(storeCteIndex),
            ExecutionLoadCteIndex loadCteIndex => RewriteLoadCteIndex(loadCteIndex),
            ExecutionCreateAsOfIndex createIndex => RewriteCreateAsOfIndex(createIndex),
            ExecutionAsOfProbe asOfProbe => RewriteAsOfProbe(asOfProbe),
            ExecutionCreateRangeIndex createIndex => RewriteCreateRangeIndex(createIndex),
            ExecutionRangeProbe rangeProbe => RewriteRangeProbe(rangeProbe),
            ExecutionCreateAggregateLibrary library => RewriteCreateAggregateLibrary(library),
            ExecutionCreateAggregateContext context => RewriteCreateAggregateContext(context),
            ExecutionEnsureAggregateGroup ensure => RewriteEnsureAggregateGroup(ensure),
            ExecutionCreateSingleKeyAggregateContext context => RewriteCreateSingleKeyAggregateContext(context),
            ExecutionGetOrAddSingleKeyAggregateGroup getOrAdd => RewriteGetOrAddSingleKeyAggregateGroup(getOrAdd),
            ExecutionCreateValueTupleAggregateContext context => RewriteCreateValueTupleAggregateContext(context),
            ExecutionGetOrAddValueTupleAggregateGroup getOrAdd => RewriteGetOrAddValueTupleAggregateGroup(getOrAdd),
            ExecutionAggregateSet aggregateSet => RewriteAggregateSet(aggregateSet),
            ExecutionAggregateCapturedValueSet capturedValueSet => RewriteAggregateCapturedValueSet(capturedValueSet),
            ExecutionSetOperation setOperation => RewriteSetOperation(setOperation),
            ExecutionDistinctTable distinct => RewriteDistinctTable(distinct),
            ExecutionSortTable sort => RewriteSortTable(sort),
            ExecutionTopNTable topN => RewriteTopNTable(topN),
            ExecutionTopOffsetTable topOffset => RewriteTopOffsetTable(topOffset),
            ExecutionSkipTable skip => RewriteSkipTable(skip),
            ExecutionTakeTable take => RewriteTakeTable(take),
            ExecutionSliceTable slice => RewriteSliceTable(slice),
            ExecutionProjectTable project => RewriteProjectTable(project),
            ExecutionOrderRecordList orderList => RewriteOrderRecordList(orderList),
            ExecutionMaterializeRecordListToTable materialize => RewriteMaterializeRecordListToTable(materialize),
            ExecutionStoreTable storeTable => RewriteStoreTable(storeTable),
            ExecutionRelatedCtePhase relatedPhase => RewriteRelatedCtePhase(relatedPhase),
            ExecutionFusedCteProducer fusedProducer => RewriteFusedCteProducer(fusedProducer),
            ExecutionSingleUsePipelineFusionCandidate candidate => RewriteSingleUsePipelineFusionCandidate(candidate),
            ExecutionCteReadOnceFusionCandidate candidate => RewriteCteReadOnceFusionCandidate(candidate),
            ExecutionCteSidecarIndexStoreCandidate candidate => RewriteCteSidecarIndexStoreCandidate(candidate),
            ExecutionCteSidecarIndexLoadCandidate candidate => RewriteCteSidecarIndexLoadCandidate(candidate),
            ExecutionCteSidecarIndexBuildCandidate candidate => RewriteCteSidecarIndexBuildCandidate(candidate),
            ExecutionCteSidecarAppendRewriteCandidate candidate => RewriteCteSidecarAppendRewriteCandidate(candidate),
            ExecutionCteFusedProducerCandidate candidate => RewriteCteFusedProducerCandidate(candidate),
            ExecutionCteIndexOnlyStorageCandidate candidate => RewriteCteIndexOnlyStorageCandidate(candidate),
            ExecutionEnsureTableCapacity ensureCapacity => RewriteEnsureTableCapacity(ensureCapacity),
            ExecutionReturnDesc returnDesc => RewriteReturnDesc(returnDesc),
            ExecutionReturnTable returnTable => RewriteReturnTable(returnTable),
            _ => node
        };
    }
}

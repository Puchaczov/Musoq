namespace Musoq.Evaluator.IR.Execution.Facts;

internal static partial class ExecutionNodeFacts
{
    internal static bool HasDirectVariableUse(ExecutionNode node, string name)
    {
        for (var index = 0; PagingCountExecution.ReadAt(node, index) is { } read; index++)
            if (read is ExecutionVariableRead variableRead && MatchesVariable(variableRead.Variable, name))
                return true;
        if (TryGetTablePostOperation(node, out var postOperation))
            return MatchesVariable(postOperation.Source, name) || MatchesVariable(postOperation.Target, name) ||
                   MatchesVariable(CapacityVariable(postOperation.CapacityHint), name);
        var indexOfReference = 0;
        while (DirectVariableAt(node, indexOfReference++) is { } variable)
            if (MatchesVariable(variable, name))
                return true;
        return false;
    }

    private static ExecutionVariable? DirectVariableAt(ExecutionNode node, int index) => node switch
    {
        ExecutionCreateTable createTable => index switch { 0 => CapacityVariable(createTable.CapacityHint), _ => null },
        ExecutionCreateValuesRows valuesRows => index switch { 0 => valuesRows.Rows, _ => null },
        ExecutionCreateRecordList createList => index switch { 0 => createList.List, 1 => CapacityVariable(createList.CapacityHint), _ => null },
        ExecutionCreateBoundedRecordList createList => index switch { 0 => createList.List, _ => null },
        ExecutionEnsureTableCapacity ensureCapacity => index switch { 0 => ensureCapacity.Table, 1 => CapacityVariable(ensureCapacity.CapacityHint), _ => null },
        ExecutionForEachIndexed forEachIndexed => index switch { 0 => forEachIndexed.Source, _ => null },
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate => index switch { 0 => parallelAggregate.Source, _ => null },
        ExecutionParallelFilterProjectLoop parallelProject => index switch { 0 => parallelProject.Source, 1 => parallelProject.AppendRow.Table, _ => null },
        ExecutionParallelBlock parallel => (uint)index < (uint)parallel.Tasks.Count ? parallel.Tasks[index].Output : null,
        ExecutionLet let => index switch { 0 => let.Variable, _ => null },
        ExecutionHoistCandidateLet candidate => index switch { 0 => candidate.Variable, _ => null },
        ExecutionAssign assign => index switch { 0 => assign.Variable, _ => null },
        ExecutionCreateBooleanArray createArray => index switch { 0 => createArray.Array, 1 => createArray.LengthSource, _ => null },
        ExecutionArrayAssign arrayAssign => index switch { 0 => arrayAssign.Array, _ => null },
        ExecutionCreateObject createObject => index switch { 0 => createObject.Target, _ => null },
        ExecutionInitializeAggregateState initialize => index switch { 0 => initialize.Target, _ => null },
        ExecutionMethodTargetDeclarationCandidate candidate => index switch { 0 => candidate.Target, _ => null },
        ExecutionCreateGeneratedRow createRow => index switch { 0 => createRow.Row, _ => null },
        ExecutionRecursiveCte recursiveCte => index switch { 0 => recursiveCte.Result, 1 => recursiveCte.CurrentFrontier, 2 => recursiveCte.NextFrontier, 3 => recursiveCte.SnapshotRows, 4 => recursiveCte.Seen, _ => null },
        ExecutionRecursiveCteSnapshotRowGuard guard => index switch { 0 => guard.Counter, _ => null },
        ExecutionRecursiveCteAppend append => index switch { 0 => append.Result, 1 => append.Frontier, 2 => append.AppendRow.Table, 3 => append.Seen, _ => null },
        ExecutionCreateHashPayload createPayload => index switch { 0 => createPayload.Payload, _ => null },
        ExecutionAppendRow appendRow => index switch { 0 => appendRow.Table, _ => null },
        ExecutionAppendExistingRow appendRow => index switch { 0 => appendRow.Table, 1 => appendRow.Row, _ => null },
        ExecutionAppendRecord appendRecord => index switch { 0 => appendRecord.List, _ => null },
        ExecutionMaterializeList materialize => index switch { 0 => materialize.Buffer, _ => null },
        ExecutionMaterializeFilteredList materialize => index switch { 0 => materialize.Buffer, 1 => materialize.Item, _ => null },
        ExecutionHashAdd hashAdd => index switch { 0 => hashAdd.Hash, 1 => hashAdd.Row, _ => null },
        ExecutionHashProbe hashProbe => index switch { 0 => hashProbe.Hash, 1 => hashProbe.Matches, 2 => hashProbe.MatchFound, _ => null },
        ExecutionCreateKeySet createKeySet => index switch { 0 => createKeySet.Set, 1 => CapacityVariable(createKeySet.CapacityHint), _ => null },
        ExecutionKeySetAdd keySetAdd => index switch { 0 => keySetAdd.Set, _ => null },
        ExecutionKeySetProbe keySetProbe => index switch { 0 => keySetProbe.Set, 1 => keySetProbe.MatchFound, _ => null },
        ExecutionStoreCteIndex storeCteIndex => index switch { 0 => storeCteIndex.Index, _ => null },
        ExecutionCteSidecarIndexStoreCandidate candidate => index switch { 0 => candidate.Index, _ => null },
        ExecutionAsOfProbe asOfProbe => index switch { 0 => asOfProbe.Match, 1 => asOfProbe.Index, _ => null },
        ExecutionRangeProbe rangeProbe => index switch { 0 => rangeProbe.Match, 1 => rangeProbe.Index, 2 => rangeProbe.MatchFound, _ => null },
        ExecutionCreateAggregateContext aggregateContext => index switch { 0 => aggregateContext.RootGroup, 1 => aggregateContext.CurrentGroup, 2 => aggregateContext.Groups, _ => null },
        ExecutionEnsureAggregateGroup ensureGroup => index switch { 0 => ensureGroup.RootGroup, 1 => ensureGroup.CurrentGroup, 2 => ensureGroup.Groups, _ => null },
        ExecutionCreateSingleKeyAggregateContext singleKeyContext => index switch { 0 => singleKeyContext.RootGroup, 1 => singleKeyContext.Groups, 2 => singleKeyContext.GroupsToFinalize, 3 => singleKeyContext.NullGroup, _ => null },
        ExecutionGetOrAddSingleKeyAggregateGroup getOrAdd => index switch { 0 => getOrAdd.RootGroup, 1 => getOrAdd.Groups, 2 => getOrAdd.GroupsToFinalize, 3 => getOrAdd.Group, 4 => getOrAdd.NullGroup, _ => null },
        ExecutionCreateValueTupleAggregateContext valueTupleContext => index switch { 0 => valueTupleContext.RootGroup, 1 => valueTupleContext.GroupsToFinalize, _ => (uint)(index - 2) < (uint)valueTupleContext.GroupDictionaries.Count ? valueTupleContext.GroupDictionaries[index - 2].Variable : null },
        ExecutionGetOrAddValueTupleAggregateGroup getOrAdd => index switch { 0 => getOrAdd.RootGroup, 1 => getOrAdd.GroupsToFinalize, 2 => getOrAdd.Group, _ => (uint)(index - 3) < (uint)getOrAdd.GroupDictionaries.Count ? getOrAdd.GroupDictionaries[index - 3].Variable : null },
        ExecutionAggregateSet aggregateSet => index switch { 0 => aggregateSet.Group, _ => null },
        ExecutionAggregateCapturedValueSet capturedValueSet => index switch { 0 => capturedValueSet.Group, _ => null },
        ExecutionSetOperation setOperation => index switch { 0 => setOperation.Target, 1 => setOperation.Left, 2 => setOperation.Right, _ => null },
        ExecutionOrderRecordList orderRecords => index switch { 0 => orderRecords.Source, _ => null },
        ExecutionStoreTable store => index switch { 0 => store.Table, _ => null },
        ExecutionReturnTable returnTable => index switch { 0 => returnTable.Table, _ => null },
        _ => null
    };

    private static bool MatchesVariable(ExecutionVariable? variable, string name) =>
        variable is not null && string.Equals(variable.Name, name, StringComparison.Ordinal);

    private static ExecutionVariable? CapacityVariable(ExecutionCapacityHint? hint) => hint switch
    {
        ExecutionCollectionCountCapacityHint collection => collection.Collection,
        ExecutionTryGetNonEnumeratedCountCapacityHint tryCount => tryCount.Collection,
        ExecutionTakeCapacityHint take => take.Collection,
        ExecutionSkipCapacityHint skip => skip.Collection,
        ExecutionSkipTakeCapacityHint skipTake => skipTake.Collection,
        ExecutionCollectionCountCapacityHintCandidate collection => collection.Collection,
        ExecutionTakeCapacityHintCandidate take => take.Collection,
        ExecutionSkipCapacityHintCandidate skip => skip.Collection,
        ExecutionSkipTakeCapacityHintCandidate skipTake => skipTake.Collection,
        ExecutionRowsCapacityHintCandidate { Rows: ExecutionRowStream rows } => rows.Variable,
        ExecutionRowsCapacityHintCandidate { Rows: ExecutionScalarRowStream rows } => rows.Variable,
        _ => null
    };
}

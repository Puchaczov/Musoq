using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution.Facts;

internal static partial class ExecutionNodeFacts
{
    internal static IEnumerable<ExecutionVariable> GetDirectVariableReferences(ExecutionNode node)
    {
        if (TryGetTablePostOperation(node, out var tablePostOperation))
        {
            yield return tablePostOperation.Source;
            yield return tablePostOperation.Target;
            foreach (var variable in GetCapacityHintVariables(tablePostOperation.CapacityHint))
                yield return variable;
            yield break;
        }

        switch (node)
        {
            case ExecutionCreateTable createTable:
                foreach (var variable in GetCapacityHintVariables(createTable.CapacityHint))
                    yield return variable;
                break;
            case ExecutionCreateValuesRows valuesRows:
                yield return valuesRows.Rows;
                break;
            case ExecutionCreateRecordList createList:
                yield return createList.List;
                foreach (var variable in GetCapacityHintVariables(createList.CapacityHint))
                    yield return variable;
                break;
            case ExecutionCreateBoundedRecordList createList:
                yield return createList.List;
                break;
            case ExecutionEnsureTableCapacity ensureCapacity:
                yield return ensureCapacity.Table;
                foreach (var variable in GetCapacityHintVariables(ensureCapacity.CapacityHint))
                    yield return variable;
                break;
            case ExecutionForEachIndexed forEachIndexed:
                yield return forEachIndexed.Source;
                break;
            case ExecutionParallelSingleKeyAggregateLoop parallelAggregate:
                yield return parallelAggregate.Source;
                break;
            case ExecutionParallelFilterProjectLoop parallelProject:
                yield return parallelProject.Source;
                yield return parallelProject.AppendRow.Table;
                break;
            case ExecutionParallelBlock parallel:
                foreach (var task in parallel.Tasks)
                    yield return task.Output;
                break;
            case ExecutionLet let:
                yield return let.Variable;
                break;
            case ExecutionHoistCandidateLet candidate:
                yield return candidate.Variable;
                break;
            case ExecutionAssign assign:
                yield return assign.Variable;
                break;
            case ExecutionCreateBooleanArray createArray:
                yield return createArray.Array;
                yield return createArray.LengthSource;
                break;
            case ExecutionArrayAssign arrayAssign:
                yield return arrayAssign.Array;
                break;
            case ExecutionAdaptExpando adapt:
                yield return adapt.Target;
                yield return adapt.Source;
                break;
            case ExecutionCreateObject createObject:
                yield return createObject.Target;
                break;
            case ExecutionMethodTargetDeclarationCandidate candidate:
                yield return candidate.Target;
                break;
            case ExecutionCreateGeneratedRow createRow:
                yield return createRow.Row;
                break;
            case ExecutionCreateHashPayload createPayload:
                yield return createPayload.Payload;
                break;
            case ExecutionAppendRow appendRow:
                yield return appendRow.Table;
                break;
            case ExecutionAppendExistingRow appendRow:
                yield return appendRow.Table;
                yield return appendRow.Row;
                break;
            case ExecutionAppendRecord appendRecord:
                yield return appendRecord.List;
                break;
            case ExecutionMaterializeList materialize:
                yield return materialize.Buffer;
                break;
            case ExecutionMaterializeFilteredList materialize:
                yield return materialize.Buffer;
                yield return materialize.Item;
                break;
            case ExecutionMaterializeExpandoList materialize:
                yield return materialize.Buffer;
                break;
            case ExecutionHashAdd hashAdd:
                yield return hashAdd.Hash;
                yield return hashAdd.Row;
                break;
            case ExecutionHashProbe hashProbe:
                yield return hashProbe.Hash;
                yield return hashProbe.Matches;
                if (hashProbe.MatchFound != null)
                    yield return hashProbe.MatchFound;
                break;
            case ExecutionCreateKeySet createKeySet:
                yield return createKeySet.Set;
                foreach (var variable in GetCapacityHintVariables(createKeySet.CapacityHint))
                    yield return variable;
                break;
            case ExecutionKeySetAdd keySetAdd:
                yield return keySetAdd.Set;
                break;
            case ExecutionKeySetProbe keySetProbe:
                yield return keySetProbe.Set;
                if (keySetProbe.MatchFound != null)
                    yield return keySetProbe.MatchFound;
                break;
            case ExecutionStoreCteIndex storeCteIndex:
                yield return storeCteIndex.Index;
                break;
            case ExecutionCteSidecarIndexStoreCandidate candidate:
                yield return candidate.Index;
                break;
            case ExecutionAsOfProbe asOfProbe:
                yield return asOfProbe.Match;
                if (asOfProbe.Index != null)
                    yield return asOfProbe.Index;
                break;
            case ExecutionRangeProbe rangeProbe:
                yield return rangeProbe.Match;
                yield return rangeProbe.Index;
                if (rangeProbe.MatchFound is not null)
                    yield return rangeProbe.MatchFound;
                break;
            case ExecutionCreateAggregateContext aggregateContext:
                yield return aggregateContext.RootGroup;
                yield return aggregateContext.CurrentGroup;
                yield return aggregateContext.Groups;
                break;
            case ExecutionEnsureAggregateGroup ensureGroup:
                yield return ensureGroup.RootGroup;
                yield return ensureGroup.CurrentGroup;
                yield return ensureGroup.Groups;
                break;
            case ExecutionCreateSingleKeyAggregateContext singleKeyContext:
                yield return singleKeyContext.RootGroup;
                yield return singleKeyContext.Groups;
                yield return singleKeyContext.GroupsToFinalize;
                if (singleKeyContext.NullGroup != null)
                    yield return singleKeyContext.NullGroup;
                break;
            case ExecutionGetOrAddSingleKeyAggregateGroup getOrAdd:
                yield return getOrAdd.RootGroup;
                yield return getOrAdd.Groups;
                yield return getOrAdd.GroupsToFinalize;
                yield return getOrAdd.Group;
                if (getOrAdd.NullGroup != null)
                    yield return getOrAdd.NullGroup;
                break;
            case ExecutionCreateValueTupleAggregateContext valueTupleContext:
                yield return valueTupleContext.RootGroup;
                yield return valueTupleContext.GroupsToFinalize;
                foreach (var dictionary in valueTupleContext.GroupDictionaries)
                    yield return dictionary.Variable;
                break;
            case ExecutionGetOrAddValueTupleAggregateGroup getOrAdd:
                yield return getOrAdd.RootGroup;
                yield return getOrAdd.GroupsToFinalize;
                yield return getOrAdd.Group;
                foreach (var dictionary in getOrAdd.GroupDictionaries)
                    yield return dictionary.Variable;
                break;
            case ExecutionAggregateSet aggregateSet:
                yield return aggregateSet.Group;
                break;
            case ExecutionAggregateCapturedValueSet capturedValueSet:
                yield return capturedValueSet.Group;
                break;
            case ExecutionSetOperation setOperation:
                yield return setOperation.Target;
                yield return setOperation.Left;
                yield return setOperation.Right;
                break;
            case ExecutionOrderRecordList orderRecords:
                yield return orderRecords.Source;
                break;
            case ExecutionStoreTable store:
                yield return store.Table;
                break;
            case ExecutionReturnTable returnTable:
                yield return returnTable.Table;
                break;
        }
    }

    internal static IEnumerable<ExecutionVariable> GetCapacityHintVariables(ExecutionCapacityHint? capacityHint)
    {
        switch (capacityHint)
        {
            case ExecutionCollectionCountCapacityHint collection:
                yield return collection.Collection;
                break;
            case ExecutionTryGetNonEnumeratedCountCapacityHint tryCount:
                yield return tryCount.Collection;
                break;
            case ExecutionTakeCapacityHint take:
                yield return take.Collection;
                break;
            case ExecutionSkipCapacityHint skip:
                yield return skip.Collection;
                break;
            case ExecutionSkipTakeCapacityHint skipTake:
                yield return skipTake.Collection;
                break;
            case ExecutionRowsCapacityHintCandidate { Rows: ExecutionRowStream rows }:
                yield return rows.Variable;
                break;
            case ExecutionRowsCapacityHintCandidate { Rows: ExecutionScalarRowStream rows }:
                yield return rows.Variable;
                break;
            case ExecutionCollectionCountCapacityHintCandidate collection:
                yield return collection.Collection;
                break;
            case ExecutionTakeCapacityHintCandidate take:
                yield return take.Collection;
                break;
            case ExecutionSkipCapacityHintCandidate skip:
                yield return skip.Collection;
                break;
            case ExecutionSkipTakeCapacityHintCandidate skipTake:
                yield return skipTake.Collection;
                break;
        }
    }
}

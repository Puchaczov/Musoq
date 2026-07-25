using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution.Facts;

internal static partial class ExecutionNodeFacts
{
    internal static IEnumerable<ExecutionVariable> GetDeclaredVariables(ExecutionNode node)
    {
        if (TryGetWindowComputation(node, out var window))
        {
            foreach (var variable in GetWindowDeclaredVariables(window))
                yield return variable;
            yield break;
        }

        if (TryGetTablePostOperation(node, out var tablePostOperation))
        {
            yield return tablePostOperation.Target;
            yield break;
        }

        switch (node)
        {
            case ExecutionSourceScan sourceScan:
                yield return sourceScan.Rows;
                break;
            case ExecutionInterpretSource interpret:
                yield return interpret.Rows;
                break;
            case ExecutionEnumerableSource enumerable:
                yield return enumerable.Rows;
                break;
            case ExecutionForEach forEach:
                yield return forEach.Item;
                break;
            case ExecutionForEachWithOrdinality forEach:
                yield return forEach.Item;
                yield return forEach.Ordinal;
                break;
            case ExecutionForEachIndexed forEachIndexed:
                yield return forEachIndexed.Item;
                yield return forEachIndexed.Index;
                break;
            case ExecutionParallelFilterProjectLoop parallelProject:
                yield return parallelProject.Source;
                break;
            case ExecutionCreateTable createTable:
                yield return createTable.Table;
                break;
            case ExecutionCreateRecordList createList:
                yield return createList.List;
                break;
            case ExecutionCreateValuesRows createValuesRows:
                yield return createValuesRows.Rows;
                break;
            case ExecutionCreateBoundedRecordList createList:
                yield return createList.List;
                break;
            case ExecutionAdaptExpando adapt:
                yield return adapt.Target;
                break;
            case ExecutionCreateObject createObject:
                yield return createObject.Target;
                break;
            case ExecutionCreateGeneratedRow createRow:
                yield return createRow.Row;
                break;
            case ExecutionRecursiveCte recursiveCte:
                yield return recursiveCte.Result;
                yield return recursiveCte.CurrentFrontier;
                yield return recursiveCte.NextFrontier;
                yield return recursiveCte.SnapshotRows;
                if (recursiveCte.Seen != null)
                    yield return recursiveCte.Seen;
                break;
            case ExecutionMethodTargetDeclarationCandidate candidate:
                yield return candidate.Target;
                break;
            case ExecutionHoistCandidateLet candidate:
                yield return candidate.Variable;
                break;
            case ExecutionCreateBooleanArray createArray:
                yield return createArray.Array;
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
            case ExecutionCreateHash createHash:
                yield return createHash.Hash;
                break;
            case ExecutionLoadCteIndex loadCteIndex:
                yield return loadCteIndex.Index;
                break;
            case ExecutionCteSidecarIndexLoadCandidate candidate:
                yield return candidate.Index;
                break;
            case ExecutionHashProbe hashProbe:
                yield return hashProbe.Matches;
                if (hashProbe.MatchFound is not null)
                    yield return hashProbe.MatchFound;
                break;
            case ExecutionCreateKeySet createKeySet:
                yield return createKeySet.Set;
                break;
            case ExecutionKeySetProbe keySetProbe:
                if (keySetProbe.MatchFound is not null)
                    yield return keySetProbe.MatchFound;
                break;
            case ExecutionCreateAsOfIndex createIndex:
                yield return createIndex.Index;
                break;
            case ExecutionAsOfProbe asOfProbe:
                yield return asOfProbe.Match;
                break;
            case ExecutionCreateRangeIndex createIndex:
                yield return createIndex.Index;
                break;
            case ExecutionRangeProbe rangeProbe:
                yield return rangeProbe.Match;
                if (rangeProbe.MatchFound is not null)
                    yield return rangeProbe.MatchFound;
                break;
            case ExecutionCreateAggregateLibrary library:
                yield return library.Library;
                break;
            case ExecutionCreateAggregateContext context:
                yield return context.RootGroup;
                yield return context.CurrentGroup;
                yield return context.Groups;
                break;
            case ExecutionCreateSingleKeyAggregateContext context:
                yield return context.RootGroup;
                yield return context.Groups;
                yield return context.GroupsToFinalize;
                if (context.NullGroup is not null)
                    yield return context.NullGroup;
                break;
            case ExecutionGetOrAddSingleKeyAggregateGroup getOrAdd:
                yield return getOrAdd.Group;
                break;
            case ExecutionParallelSingleKeyAggregateLoop parallelAggregate:
                yield return parallelAggregate.Source;
                yield return parallelAggregate.Group;
                break;
            case ExecutionCreateValueTupleAggregateContext context:
                yield return context.RootGroup;
                yield return context.GroupsToFinalize;
                foreach (var dictionary in context.GroupDictionaries)
                    yield return dictionary.Variable;
                break;
            case ExecutionGetOrAddValueTupleAggregateGroup getOrAdd:
                yield return getOrAdd.Group;
                break;
            case ExecutionLet let:
                yield return let.Variable;
                break;
            case ExecutionSetOperation setOperation:
                yield return setOperation.Target;
                break;
        }
    }

    private static IEnumerable<ExecutionVariable> GetWindowDeclaredVariables(
        ExecutionWindowComputationMetadata metadata)
    {
        yield return metadata.Results;

        if (metadata.PartitionKeyArray is not null)
            yield return metadata.PartitionKeyArray.Variable;
        if (metadata.OrderKeyArray is not null)
            yield return metadata.OrderKeyArray.Variable;
        if (metadata.Partitions is not null)
            yield return metadata.Partitions.Variable;
        if (metadata.SortedPartitions is not null)
            yield return metadata.SortedPartitions.Variable;
    }
}

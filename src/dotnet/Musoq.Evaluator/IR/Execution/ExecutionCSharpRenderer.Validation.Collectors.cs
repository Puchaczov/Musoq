using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private static IEnumerable<string> CreateRelatedPhaseQueryIdentifiers(ExecutionBlock block, string queryIdentifier)
    {
        if (ContainsNode<ExecutionSetOperation>(block))
        {
            yield return $"{queryIdentifier}:left";
            yield return $"{queryIdentifier}:right";
        }

        var taskScopedTableIndexes = CollectTaskScopedStoredTableIndexes(block).ToHashSet();
        foreach (var tableIndex in CollectStoredTableIndexes(block)
                     .Where(index => !taskScopedTableIndexes.Contains(index))
                     .Distinct()
                     .OrderBy(static index => index))
        {
            yield return CreateRelatedCtePhaseQueryIdentifier(queryIdentifier, tableIndex);
        }
    }

    private static IEnumerable<int> CollectStoredTableIndexes(ExecutionBlock block)
    {
        foreach (var node in block.Nodes)
        {
            if (node is ExecutionStoreTable store)
                yield return store.TableIndex;

            if (node is ExecutionFusedCteProducer fusedCte)
            {
                foreach (var output in fusedCte.Outputs)
                    yield return output.TableIndex;
            }

            if (node is ExecutionRelatedCtePhase phase)
                yield return phase.TableIndex;

            foreach (var tableIndex in CollectNestedValues(node, CollectStoredTableIndexes))
                yield return tableIndex;
        }
    }

    private static IEnumerable<int> CollectTaskScopedStoredTableIndexes(ExecutionBlock block)
    {
        foreach (var node in block.Nodes)
        {
            if (node is ExecutionParallelBlock parallel)
            {
                foreach (var task in parallel.Tasks)
                {
                    if (task.RelatedTableIndex is { } tableIndex)
                        yield return tableIndex;
                }
            }

            foreach (var tableIndex in CollectNestedValues(node, CollectTaskScopedStoredTableIndexes))
                yield return tableIndex;
        }
    }

    private static IEnumerable<string> CollectSourceRowNames(ExecutionBlock block)
    {
        foreach (var node in block.Nodes)
        {
            if (node is ExecutionSourceScan sourceScan)
                yield return sourceScan.Rows.Name;

            if (node is ExecutionCreateValuesRows valuesRows)
                yield return valuesRows.Rows.Name;

            if (node is ExecutionParallelBlock parallel)
            {
                foreach (var sourceRowName in CollectSourceRowNames(parallel.Merge.Body))
                    yield return sourceRowName;

                continue;
            }

            foreach (var sourceRowName in CollectNestedValues(node, CollectSourceRowNames))
                yield return sourceRowName;
        }
    }

    private static IEnumerable<string> CollectCreatedTableNames(ExecutionBlock block)
    {
        foreach (var node in block.Nodes)
        {
            if (node is ExecutionCreateTable createTable)
                yield return createTable.Table.Name;

            if (node is ExecutionMaterializeRecordListToTable materialize)
                yield return materialize.Target.Name;

            if (node is ExecutionDistinctTable distinct)
                yield return distinct.Target.Name;

            if (node is ExecutionParallelBlock parallel)
            {
                foreach (var tableName in CollectCreatedTableNames(parallel.Merge.Body))
                    yield return tableName;

                continue;
            }

            foreach (var tableName in CollectNestedValues(node, CollectCreatedTableNames))
                yield return tableName;
        }
    }

    private static IEnumerable<string> CollectLoopItemNames(ExecutionBlock block)
    {
        foreach (var node in block.Nodes)
        {
            switch (node)
            {
                case ExecutionForEach forEach:
                    yield return forEach.Item.Name;
                    break;
                case ExecutionForEachWithOrdinality forEach:
                    yield return forEach.Item.Name;
                    yield return forEach.Ordinal.Name;
                    break;
                case ExecutionForEachIndexed forEachIndexed:
                    yield return forEachIndexed.Item.Name;
                    break;
            }

            if (node is ExecutionParallelBlock parallel)
            {
                foreach (var loopItemName in CollectLoopItemNames(parallel.Merge.Body))
                    yield return loopItemName;

                continue;
            }

            foreach (var loopItemName in CollectNestedValues(node, CollectLoopItemNames))
                yield return loopItemName;
        }
    }

    private static IEnumerable<string> CollectAggregateDeclarationNames(ExecutionBlock block)
    {
        foreach (var node in block.Nodes)
        {
            switch (node)
            {
                case ExecutionCreateAggregateLibrary library:
                    yield return library.Library.Name;
                    break;
                case ExecutionCreateAggregateContext context:
                    yield return context.RootGroup.Name;
                    yield return context.Groups.Name;
                    yield return context.CurrentGroup.Name;
                    break;
                case ExecutionCreateSingleKeyAggregateContext context:
                    yield return context.RootGroup.Name;
                    yield return context.GroupsToFinalize.Name;
                    yield return context.Groups.Name;
                    if (context.NullGroup != null)
                        yield return context.NullGroup.Name;
                    break;
                case ExecutionCreateValueTupleAggregateContext context:
                    yield return context.RootGroup.Name;
                    yield return context.GroupsToFinalize.Name;
                    foreach (var groupDictionary in context.GroupDictionaries)
                        yield return groupDictionary.Variable.Name;
                    break;
            }
        }
    }

    private static IEnumerable<T> CollectNestedValues<T>(ExecutionNode node, Func<ExecutionBlock, IEnumerable<T>> collect)
    {
        return GetNestedBlocks(node).SelectMany(collect);
    }

    private static IEnumerable<ExecutionBlock> GetNestedBlocks(ExecutionNode node)
    {
        return ExecutionIrAnalysis.GetChildBlocks(node);
    }
}

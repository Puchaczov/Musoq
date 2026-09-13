using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

internal static class TypedStoredTableResultResolver
{
    public static IReadOnlyDictionary<int, TypedStoredTableResult> Resolve(ExecutionPlan plan)
    {
        var results = new Dictionary<int, TypedStoredTableResult>();
        var usageShapes = CollectStoredTableRowsUsageShapes(plan.Body);

        foreach (var recursive in ExecutionIrAnalysis.CollectNodes<ExecutionRecursiveCte>(plan.Body))
        {
            var result = new TypedStoredTableResult(recursive.TableIndex, recursive.RowShape);
            if (IsTypedStoredTableResultCompatibleWithUsages(result, usageShapes))
                AddTypedStoredTableResult(results, result);
        }

        foreach (var result in StoredTableBuildDiscovery.Collect(plan.Body)
                     .Select(TryCreateTypedStoredTableResult)
                     .Where(result => result != null && IsTypedStoredTableResultCompatibleWithUsages(result, usageShapes)))
        {
            AddTypedStoredTableResult(results, result!);
        }

        foreach (var result in CollectParallelTypedStoredTableResults(plan.Body)
                     .Where(result => IsTypedStoredTableResultCompatibleWithUsages(result, usageShapes)))
            AddTypedStoredTableResult(results, result);

        foreach (var result in CollectFusedTypedStoredTableResults(plan.Body)
                     .Where(result => IsTypedStoredTableResultCompatibleWithUsages(result, usageShapes)))
            AddTypedStoredTableResult(results, result);

        // Set-operation producers do not have an ExecutionCreateTable node
        // for their result: the set renderer creates the result buffer while
        // rendering ExecutionSetOperation.  Discover those stored results
        // from the authoritative arm shape so a structural CTE consumer can
        // receive the generated row list without a Row cast.
        foreach (var result in CollectStoredSetOperationTypedStoredTableResults(plan.Body, usageShapes)
                     .Where(result => IsTypedStoredTableResultCompatibleWithUsages(result, usageShapes)))
            AddTypedStoredTableResult(results, result);

        return results;
    }

    private static IEnumerable<TypedStoredTableResult> CollectStoredSetOperationTypedStoredTableResults(
        ExecutionBlock block,
        IReadOnlyDictionary<int, HashSet<string?>> usageShapes)
    {
        var nodes = ExecutionIrAnalysis.FlattenNodes(block).ToArray();
        var storedTables = nodes
            .OfType<ExecutionStoreTable>()
            .ToDictionary(static store => store.Table.Name, static store => store.TableIndex, StringComparer.Ordinal);
        var rowShapes = ExecutionTypedRowBufferResolver.CreateTableRowShapeMap(new ExecutionBlock(nodes));
        var structurallyDemandedTableIndexes = ExecutionIrAnalysis
            .CollectExpressions<ExecutionCteCollectionInput>(new ExecutionBlock(nodes))
            .Where(ExecutionTypedRowBufferResolver.IsStructuralCteConsumer)
            .Select(static input => input.Rows)
            .OfType<ExecutionStoredTableRows>()
            .Select(static rows => rows.TableIndex)
            .ToHashSet();

        foreach (var setOperation in nodes.OfType<ExecutionSetOperation>())
        {
            if (!storedTables.TryGetValue(setOperation.Target.Name, out var tableIndex) ||
                !structurallyDemandedTableIndexes.Contains(tableIndex) ||
                !usageShapes.ContainsKey(tableIndex) ||
                !rowShapes.TryGetValue(setOperation.Left.Name, out var leftShape) ||
                !rowShapes.TryGetValue(setOperation.Right.Name, out var rightShape) ||
                !AreSetArmShapesCompatible(leftShape, rightShape))
            {
                continue;
            }

            yield return new TypedStoredTableResult(tableIndex, leftShape);
        }
    }

    private static bool AreSetArmShapesCompatible(GeneratedRowShape left, GeneratedRowShape right)
    {
        if (left.Fields.Count != right.Fields.Count)
            return false;

        for (var index = 0; index < left.Fields.Count; index++)
        {
            var leftField = left.Fields[index];
            var rightField = right.Fields[index];
            if (!string.Equals(leftField.Type.StableId, rightField.Type.StableId, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static IReadOnlyDictionary<int, HashSet<string?>> CollectStoredTableRowsUsageShapes(ExecutionBlock block)
    {
        var shapes = new Dictionary<int, HashSet<string?>>();
        foreach (var rows in ExecutionIrAnalysis.CollectExpressions<ExecutionStoredTableRows>(block))
        {
            if (!shapes.TryGetValue(rows.TableIndex, out var tableShapes))
            {
                tableShapes = new HashSet<string?>(StringComparer.Ordinal);
                shapes.Add(rows.TableIndex, tableShapes);
            }

            tableShapes.Add(rows.GeneratedRowShape?.TypeName);
        }

        return shapes;
    }

    private static bool IsTypedStoredTableResultCompatibleWithUsages(
        TypedStoredTableResult? result,
        IReadOnlyDictionary<int, HashSet<string?>> usageShapes)
    {
        if (result == null || !usageShapes.TryGetValue(result.TableIndex, out var shapes))
            return result != null;

        // A structural CTE consumer is initially lowered without a row shape.
        // The planner still owns the authoritative generated shape, which is
        // resolved from the stored-table producer below.  Unknown usage shapes
        // therefore must not disqualify an otherwise safe typed table result;
        // only two different concrete generated row types are incompatible.
        var concreteShapes = shapes
            .Where(static shape => shape != null)
            .ToHashSet(StringComparer.Ordinal);
        return concreteShapes.Count == 0 || concreteShapes.SetEquals([result.RowShape.TypeName]);
    }

    private static IEnumerable<TypedStoredTableResult> CollectFusedTypedStoredTableResults(ExecutionBlock block)
    {
        foreach (var producer in ExecutionIrAnalysis.CollectNodes<ExecutionFusedCteProducer>(block))
        {
            foreach (var output in producer.Outputs)
            {
                if (!output.StoreRows)
                    continue;

                yield return new TypedStoredTableResult(output.TableIndex, output.RowShape);
            }
        }
    }

    private static TypedStoredTableResult? TryCreateTypedStoredTableResult(StoredTableBuild build)
    {
        if (!TryGetStoredTableBuildRowShape(build, out var rowShape) ||
            !StoredTableRowBufferEligibility.CanUseTypedRowBuffer(build.Nodes, build.Table, rowShape))
        {
            return null;
        }

        return new TypedStoredTableResult(build.TableIndex, rowShape);
    }

    private static IEnumerable<TypedStoredTableResult> CollectParallelTypedStoredTableResults(ExecutionBlock block)
    {
        foreach (var parallel in ExecutionIrAnalysis.CollectNodes<ExecutionParallelBlock>(block))
        {
            foreach (var task in parallel.Tasks)
            {
                if (string.IsNullOrWhiteSpace(task.Output.GeneratedRowTypeName) ||
                    !TryCreateParallelStoredTableBuild(parallel, task, out var build))
                {
                    continue;
                }

                var result = TryCreateTypedStoredTableResult(build);
                if (result != null)
                    yield return result;
            }
        }
    }

    private static bool TryCreateParallelStoredTableBuild(
        ExecutionParallelBlock parallel,
        ExecutionParallelTask task,
        out StoredTableBuild build)
    {
        build = null!;
        var store = parallel.Merge.Body.Nodes
            .OfType<ExecutionStoreTable>()
            .SingleOrDefault(node => string.Equals(node.Table.Name, task.Output.Name, StringComparison.Ordinal));

        if (store == null ||
            !StoredTableBuildDiscovery.TryGetParallelTaskResultTable(task, out var table))
        {
            return false;
        }

        build = new StoredTableBuild(store.TableIndex, task.Body.Nodes, table, []);
        return true;
    }

    private static void AddTypedStoredTableResult(
        IDictionary<int, TypedStoredTableResult> results,
        TypedStoredTableResult result)
    {
        if (!results.TryGetValue(result.TableIndex, out var existing))
        {
            results.Add(result.TableIndex, result);
            return;
        }

        if (!string.Equals(existing.RowShape.TypeName, result.RowShape.TypeName, StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"CTE row result slot {result.TableIndex.ToString(CultureInfo.InvariantCulture)} has inconsistent generated row types.");
    }

    private static bool TryGetStoredTableBuildRowShape(
        StoredTableBuild build,
        out GeneratedRowShape rowShape)
    {
        foreach (var createTable in build.Nodes.OfType<ExecutionCreateTable>())
        {
            if (!string.Equals(createTable.Table.Name, build.Table.Name, StringComparison.Ordinal))
                continue;

            rowShape = createTable.RowShape;
            return true;
        }

        rowShape = null!;
        return false;
    }
}

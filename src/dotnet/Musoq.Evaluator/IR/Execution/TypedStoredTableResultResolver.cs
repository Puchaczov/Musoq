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

        return results;
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
        return result != null &&
               (!usageShapes.TryGetValue(result.TableIndex, out var shapes) ||
                shapes.SetEquals([result.RowShape.TypeName]));
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
            !TryGetParallelTaskResultTable(task, out var table))
        {
            return false;
        }

        build = new StoredTableBuild(store.TableIndex, task.Body.Nodes, table, []);
        return true;
    }

    public static bool TryGetParallelTaskResultTable(
        ExecutionParallelTask task,
        out ExecutionVariable table)
    {
        foreach (var assign in task.Body.Nodes.OfType<ExecutionAssign>().Reverse())
        {
            if (string.Equals(assign.Variable.Name, task.Output.Name, StringComparison.Ordinal) &&
                assign.Value is ExecutionVariableRead read)
            {
                table = read.Variable;
                return true;
            }
        }

        table = null!;
        return false;
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

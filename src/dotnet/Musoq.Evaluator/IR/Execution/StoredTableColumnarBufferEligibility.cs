using System.Linq;
using Musoq.Evaluator.IR.Execution.Facts;

namespace Musoq.Evaluator.IR.Execution;

internal static class StoredTableColumnarBufferEligibility
{
    public static bool CanUseWindowBuffer(
        ExecutionPlan plan,
        int tableIndex,
        GeneratedRowShape rowShape)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(rowShape);

        if (!HasSupportedRowShape(rowShape))
            return false;

        var nodes = ExecutionIrAnalysis.FlattenNodes(plan.Body).ToArray();
        var stores = nodes
            .OfType<ExecutionStoreTable>()
            .Where(store => store.TableIndex == tableIndex)
            .ToArray();
        if (stores.Length != 1)
            return false;

        var tableName = stores[0].Table.Name;
        if (nodes.OfType<ExecutionStoreTable>()
                .Count(store => string.Equals(store.Table.Name, tableName, StringComparison.Ordinal)) != 1 ||
            nodes.OfType<ExecutionCreateTable>()
                .Count(create => string.Equals(create.Table.Name, tableName, StringComparison.Ordinal)) != 1)
        {
            return false;
        }

        var build = StoredTableBuildDiscovery.Collect(plan.Body)
            .SingleOrDefault(candidate => candidate.TableIndex == tableIndex);
        if (build == null || !HasDirectScalarAppends(build, rowShape))
            return false;

        var materializations = nodes
            .OfType<ExecutionMaterializeList>()
            .Where(materialize => materialize.Source is ExecutionStoredTableRows rows &&
                                  rows.TableIndex == tableIndex &&
                                  (rows.GeneratedRowShape == null ||
                                   string.Equals(rows.GeneratedRowShape.TypeName, rowShape.TypeName, StringComparison.Ordinal)))
            .ToArray();
        if (materializations.Length != 1)
            return false;

        var bufferName = materializations[0].Buffer.Name;
        var storedRowReads = ExecutionIrAnalysis
            .CollectExpressions<ExecutionStoredTableRows>(plan.Body)
            .Count(rows => rows.TableIndex == tableIndex);
        if (storedRowReads != 1 ||
            ExecutionIrAnalysis.CollectExpressions<ExecutionCteCollectionInput>(plan.Body)
                .Any(input => input.Rows is ExecutionStoredTableRows rows && rows.TableIndex == tableIndex))
        {
            return false;
        }

        var windowKernels = nodes
            .OfType<ExecutionWindowAggregateKernel>()
            .Where(kernel => string.Equals(kernel.Buffer.Name, bufferName, StringComparison.Ordinal))
            .ToArray();
        if (windowKernels.Length == 0 ||
            windowKernels.Any(kernel => !CanReadWindowFieldsDirectly(kernel)))
        {
            return false;
        }

        if (nodes.OfType<ExecutionComputeRankingWindow>()
                .Any(kernel => string.Equals(kernel.Buffer.Name, bufferName, StringComparison.Ordinal)) ||
            nodes.OfType<ExecutionComputeOffsetWindow>()
                .Any(kernel => string.Equals(kernel.Buffer.Name, bufferName, StringComparison.Ordinal)) ||
            nodes.OfType<ExecutionComputePluginWindow>()
                .Any(kernel => string.Equals(kernel.Buffer.Name, bufferName, StringComparison.Ordinal)))
        {
            return false;
        }

        var indexedConsumers = nodes
            .OfType<ExecutionForEachIndexed>()
            .Where(loop => string.Equals(loop.Source.Name, bufferName, StringComparison.Ordinal))
            .ToArray();
        if (indexedConsumers.Length != 1 ||
            indexedConsumers[0].RowAccessMode != ExecutionRowAccessMode.Direct ||
            !CanReadProjectionFieldsDirectly(indexedConsumers[0]))
        {
            return false;
        }

        foreach (var node in nodes)
        {
            if (!ExecutionNodeFacts.GetDirectVariableReferences(node)
                .Any(variable => string.Equals(variable.Name, bufferName, StringComparison.Ordinal)))
                continue;

            if (node is ExecutionMaterializeList materialize &&
                string.Equals(materialize.Buffer.Name, bufferName, StringComparison.Ordinal))
                continue;

            if (node is ExecutionCreateTable
                {
                    CapacityHint: ExecutionCollectionCountCapacityHint capacity
                } && string.Equals(capacity.Collection.Name, bufferName, StringComparison.Ordinal))
                continue;

            if (node is ExecutionForEachIndexed indexedLoop &&
                string.Equals(indexedLoop.Source.Name, bufferName, StringComparison.Ordinal))
                continue;

            if (node is ExecutionWindowAggregateKernel windowKernel &&
                string.Equals(windowKernel.Buffer.Name, bufferName, StringComparison.Ordinal))
                continue;

            return false;
        }

        return !ExecutionIrAnalysis.CollectExpressions<ExecutionVariableRead>(plan.Body)
            .Any(read => string.Equals(read.Variable.Name, bufferName, StringComparison.Ordinal));
    }

    private static bool HasSupportedRowShape(GeneratedRowShape rowShape)
    {
        if (!rowShape.SupportsGeneratedFieldAccess ||
            rowShape.Contexts.Count != 0 ||
            rowShape.Fields.Count == 0)
        {
            return false;
        }

        var fields = rowShape.Fields.OrderBy(static field => field.OutputIndex).ToArray();
        for (var index = 0; index < fields.Length; index++)
        {
            if (fields[index].OutputIndex != index ||
                fields[index].AccessStrategy is not GeneratedFieldAccess ||
                !IsSupportedColumnType(fields[index].Type.ResolveClrType()))
            {
                return false;
            }
        }

        return fields.Select(static field => field.Name).Distinct(StringComparer.Ordinal).Count() == fields.Length;
    }

    private static bool IsSupportedColumnType(Type type)
    {
        var valueType = Nullable.GetUnderlyingType(type) ?? type;
        return valueType.IsPrimitive ||
               valueType.IsEnum ||
               valueType == typeof(string) ||
               valueType == typeof(decimal) ||
               valueType == typeof(DateTime) ||
               valueType == typeof(DateTimeOffset) ||
               valueType == typeof(TimeSpan) ||
               valueType == typeof(Guid);
    }

    private static bool HasDirectScalarAppends(StoredTableBuild build, GeneratedRowShape rowShape)
    {
        var tableName = build.Table.Name;
        var buildNodes = ExecutionIrAnalysis.FlattenNodes(new ExecutionBlock(build.Nodes)).ToArray();
        var createTables = buildNodes
            .OfType<ExecutionCreateTable>()
            .Where(createTable => string.Equals(createTable.Table.Name, tableName, StringComparison.Ordinal))
            .ToArray();
        var appends = buildNodes
            .OfType<ExecutionAppendRow>()
            .Where(append => string.Equals(append.Table.Name, tableName, StringComparison.Ordinal))
            .ToArray();

        if (createTables.Length != 1 || appends.Length == 0 ||
            buildNodes.OfType<ExecutionAppendExistingRow>()
                .Any(append => string.Equals(append.Table.Name, tableName, StringComparison.Ordinal)))
        {
            return false;
        }

        var expectedFields = rowShape.Fields.Select(static field => field.Name).ToHashSet(StringComparer.Ordinal);
        return appends.All(append =>
            string.Equals(append.RowShape.TypeName, rowShape.TypeName, StringComparison.Ordinal) &&
            append.Contexts.Count == 0 &&
            append.Values.Count == rowShape.Fields.Count &&
            append.Values.All(static value => value.Value is ExecutionFieldRead) &&
            append.Values.Select(static value => value.FieldName).ToHashSet(StringComparer.Ordinal).SetEquals(expectedFields));
    }

    private static bool CanReadWindowFieldsDirectly(ExecutionWindowAggregateKernel kernel)
    {
        if (kernel.RowAccessMode != ExecutionRowAccessMode.Direct ||
            kernel.Value is not ExecutionFieldRead valueRead ||
            !IsGeneratedRowFieldRead(valueRead, kernel.Item.Name) ||
            kernel.OrderKeys.Count == 0 ||
            kernel.OrderKeys.Any(key => key.Expression is not ExecutionFieldRead orderRead ||
                                        !IsGeneratedRowFieldRead(orderRead, kernel.Item.Name)) ||
            kernel.PartitionKey is { } partitionKey &&
            (partitionKey is not ExecutionFieldRead partitionRead ||
             !IsGeneratedRowFieldRead(partitionRead, kernel.Item.Name)) ||
            kernel.FilterPredicate != null ||
            kernel.MethodTargets is { Count: > 0 })
        {
            return false;
        }

        return true;
    }

    private static bool CanReadProjectionFieldsDirectly(ExecutionForEachIndexed loop)
    {
        var fieldReads = ExecutionIrAnalysis.CollectExpressions<ExecutionFieldRead>(loop.Body);
        return fieldReads.Any() && fieldReads.All(read => IsGeneratedRowFieldRead(read, loop.Item.Name));
    }

    private static bool IsGeneratedRowFieldRead(ExecutionFieldRead fieldRead, string itemName)
    {
        return string.Equals(fieldRead.Alias, itemName, StringComparison.Ordinal) &&
               fieldRead.AccessStrategy is GeneratedFieldAccess or GeneratedRowTypeAccess;
    }
}

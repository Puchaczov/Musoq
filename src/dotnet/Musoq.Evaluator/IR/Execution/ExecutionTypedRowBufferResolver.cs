using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Execution.Facts;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal static class ExecutionTypedRowBufferResolver
{
    public static IReadOnlyDictionary<string, GeneratedRowShape> Resolve(
        ExecutionBlock block,
        string? finalShapeTableName = null)
    {
        var result = new Dictionary<string, GeneratedRowShape>(
            ResolveSetOperationTypedRowBuffers(block, finalShapeTableName),
            StringComparer.Ordinal);

        foreach (var (name, rowShape) in ResolveMaterializedGeneratedRowTypedBuffers(block))
            result.TryAdd(name, rowShape);

        return result;
    }

    private static IReadOnlyDictionary<string, GeneratedRowShape> ResolveSetOperationTypedRowBuffers(
        ExecutionBlock block,
        string? finalShapeTableName)
    {
        var rowShapesByTableName = CreateTableRowShapeMap(block);
        var candidates = new HashSet<string>(StringComparer.Ordinal);
        var nodes = ExecutionIrAnalysis.FlattenNodes(block).ToArray();
        var tableStoredNames = nodes
            .OfType<ExecutionStoreTable>()
            .Select(static store => store.Table.Name)
            .ToHashSet(StringComparer.Ordinal);
        var blocked = CreateBlockedSetOperationTypedRowBuffers(nodes, tableStoredNames);

        foreach (var setOperation in nodes
                     .OfType<ExecutionSetOperation>()
                     .Where(setOperation => CanUseTypedSetOperationBuffers(setOperation, tableStoredNames, blocked)))
        {
            candidates.Add(setOperation.Left.Name);
            candidates.Add(setOperation.Right.Name);

            if (finalShapeTableName == null ||
                !string.Equals(setOperation.Target.Name, finalShapeTableName, StringComparison.Ordinal))
            {
                candidates.Add(setOperation.Target.Name);
            }
        }

        var changed = true;
        while (changed)
        {
            changed = false;

            foreach (var node in nodes)
            {
                if (ExecutionNodeFacts.TryGetTablePostOperation(node, out var postOperation) &&
                    candidates.Contains(postOperation.Target.Name) &&
                    candidates.Add(postOperation.Source.Name))
                {
                    changed = true;
                    continue;
                }

                if (node is ExecutionSetOperation setOperation &&
                    CanUseTypedSetOperationBuffers(setOperation, tableStoredNames, blocked) &&
                    candidates.Contains(setOperation.Target.Name))
                {
                    changed |= candidates.Add(setOperation.Left.Name);
                    changed |= candidates.Add(setOperation.Right.Name);
                }
            }
        }

        return candidates
            .Except(blocked, StringComparer.Ordinal)
            .Where(rowShapesByTableName.ContainsKey)
            .ToDictionary(
                static name => name,
                name => rowShapesByTableName[name],
                StringComparer.Ordinal);
    }

    private static IReadOnlyDictionary<string, GeneratedRowShape> ResolveMaterializedGeneratedRowTypedBuffers(
        ExecutionBlock block)
    {
        var rowShapesByTableName = CreateTableRowShapeMap(block);
        var nodes = ExecutionIrAnalysis.FlattenNodes(block).ToArray();
        var tableStoredNames = nodes
            .OfType<ExecutionStoreTable>()
            .Select(static store => store.Table.Name)
            .ToHashSet(StringComparer.Ordinal);
        var candidates = new Dictionary<string, GeneratedRowShape>(StringComparer.Ordinal);

        foreach (var materialize in nodes.OfType<ExecutionMaterializeList>())
        {
            if (materialize.GeneratedRowShape == null ||
                !TryGetMaterializedGeneratedRowTableSource(materialize.Source, out var sourceName) ||
                tableStoredNames.Contains(sourceName) ||
                !rowShapesByTableName.TryGetValue(sourceName, out var rowShape) ||
                !string.Equals(rowShape.TypeName, materialize.GeneratedRowShape.TypeName, StringComparison.Ordinal) ||
                !CanUseMaterializedGeneratedRowTypedBuffer(nodes, sourceName))
            {
                continue;
            }

            candidates.TryAdd(sourceName, rowShape);
        }

        foreach (var materialize in nodes.OfType<ExecutionMaterializeFilteredList>())
        {
            if (materialize.GeneratedRowShape == null ||
                !TryGetMaterializedGeneratedRowTableSource(materialize.Source, out var sourceName) ||
                tableStoredNames.Contains(sourceName) ||
                !rowShapesByTableName.TryGetValue(sourceName, out var rowShape) ||
                !string.Equals(rowShape.TypeName, materialize.GeneratedRowShape.TypeName, StringComparison.Ordinal) ||
                !CanUseMaterializedGeneratedRowTypedBuffer(nodes, sourceName))
            {
                continue;
            }

            candidates.TryAdd(sourceName, rowShape);
        }

        return candidates;
    }

    private static Dictionary<string, GeneratedRowShape> CreateTableRowShapeMap(ExecutionBlock block)
    {
        var result = new Dictionary<string, GeneratedRowShape>(StringComparer.Ordinal);

        foreach (var node in ExecutionIrAnalysis.FlattenNodes(block))
        {
            switch (node)
            {
                case ExecutionCreateTable createTable:
                    result[createTable.Table.Name] = createTable.RowShape;
                    break;
                case ExecutionProjectTable projectTable:
                    result[projectTable.Target.Name] = projectTable.RowShape;
                    break;
                case ExecutionMaterializeRecordListToTable materialize:
                    result[materialize.Target.Name] = materialize.RowShape;
                    break;
                case ExecutionDistinctTable distinct when result.TryGetValue(distinct.Source.Name, out var distinctShape):
                    result[distinct.Target.Name] = distinctShape;
                    break;
                case ExecutionSortTable sort when result.TryGetValue(sort.Source.Name, out var sortShape):
                    result[sort.Target.Name] = sortShape;
                    break;
                case ExecutionTopNTable topN when result.TryGetValue(topN.Source.Name, out var topNShape):
                    result[topN.Target.Name] = topNShape;
                    break;
                case ExecutionTopOffsetTable topOffset when result.TryGetValue(topOffset.Source.Name, out var topOffsetShape):
                    result[topOffset.Target.Name] = topOffsetShape;
                    break;
                case ExecutionSkipTable skip when result.TryGetValue(skip.Source.Name, out var skipShape):
                    result[skip.Target.Name] = skipShape;
                    break;
                case ExecutionTakeTable take when result.TryGetValue(take.Source.Name, out var takeShape):
                    result[take.Target.Name] = takeShape;
                    break;
                case ExecutionSliceTable slice when result.TryGetValue(slice.Source.Name, out var sliceShape):
                    result[slice.Target.Name] = sliceShape;
                    break;
                case ExecutionSetOperation setOperation when result.TryGetValue(setOperation.Left.Name, out var setOperationShape):
                    result[setOperation.Target.Name] = setOperationShape;
                    break;
            }
        }

        return result;
    }

    private static bool TryGetMaterializedGeneratedRowTableSource(
        ExecutionExpression source,
        out string tableName)
    {
        if (source is ExecutionRowStream
            {
                Kind: ExecutionRowStreamKind.Rows,
                RowsAccess: ExecutionRowStreamRowsAccess.TableRows
            } rows)
        {
            tableName = rows.Variable.Name;
            return true;
        }

        tableName = string.Empty;
        return false;
    }

    private static bool CanUseMaterializedGeneratedRowTypedBuffer(
        IReadOnlyList<ExecutionNode> nodes,
        string tableName)
    {
        foreach (var node in nodes)
        {
            if (IsAllowedMaterializedGeneratedRowBufferNode(node, tableName))
                continue;

            if (NodeReferencesVariableWithoutChildBlocks(node, tableName))
                return false;
        }

        return true;
    }

    private static bool IsAllowedMaterializedGeneratedRowBufferNode(
        ExecutionNode node,
        string tableName)
    {
        return node switch
        {
            ExecutionCreateTable createTable => HasName(createTable.Table, tableName),
            ExecutionEnsureTableCapacity ensureCapacity => HasName(ensureCapacity.Table, tableName),
            ExecutionAppendRow appendRow => HasName(appendRow.Table, tableName),
            ExecutionAppendExistingRow appendRow => HasName(appendRow.Table, tableName),
            ExecutionMaterializeList materialize => TryGetMaterializedGeneratedRowTableSource(materialize.Source, out var sourceName) &&
                                                    string.Equals(sourceName, tableName, StringComparison.Ordinal),
            ExecutionMaterializeFilteredList materialize => TryGetMaterializedGeneratedRowTableSource(materialize.Source, out var sourceName) &&
                                                            string.Equals(sourceName, tableName, StringComparison.Ordinal),
            _ => false
        };
    }

    private static bool NodeReferencesVariableWithoutChildBlocks(
        ExecutionNode node,
        string variableName)
    {
        return ExecutionNodeFacts.GetDirectVariableReferences(node).Any(variable => HasName(variable, variableName)) ||
               ExecutionIrAnalysis.ContainsVariableUse(ExecutionIrAnalysis.GetNodeExpressions(node), variableName);
    }

    private static bool CanUseTypedSetOperationBuffers(
        ExecutionSetOperation setOperation,
        IReadOnlySet<string> tableStoredNames,
        IReadOnlySet<string> blocked)
    {
        return !tableStoredNames.Contains(setOperation.Target.Name) &&
               !blocked.Contains(setOperation.Target.Name) &&
               !blocked.Contains(setOperation.Left.Name) &&
               !blocked.Contains(setOperation.Right.Name) &&
               (setOperation.Kind == SetOpKind.UnionAll ||
                setOperation.Strategy == ExecutionSetOperationStrategy.HashSet);
    }

    private static HashSet<string> CreateBlockedSetOperationTypedRowBuffers(
        IReadOnlyList<ExecutionNode> nodes,
        IReadOnlySet<string> tableStoredNames)
    {
        var blocked = new HashSet<string>(StringComparer.Ordinal);

        foreach (var setOperation in nodes.OfType<ExecutionSetOperation>())
        {
            if (!tableStoredNames.Contains(setOperation.Target.Name) &&
                (setOperation.Kind == SetOpKind.UnionAll ||
                 setOperation.Strategy == ExecutionSetOperationStrategy.HashSet))
            {
                continue;
            }

            blocked.Add(setOperation.Target.Name);
            blocked.Add(setOperation.Left.Name);
            blocked.Add(setOperation.Right.Name);
        }

        var changed = true;
        while (changed)
        {
            changed = false;

            foreach (var node in nodes)
            {
                if (ExecutionNodeFacts.TryGetTablePostOperation(node, out var postOperation) &&
                    blocked.Contains(postOperation.Target.Name) &&
                    blocked.Add(postOperation.Source.Name))
                {
                    changed = true;
                    continue;
                }

                if (node is ExecutionSetOperation setOperation &&
                    blocked.Contains(setOperation.Target.Name))
                {
                    changed |= blocked.Add(setOperation.Left.Name);
                    changed |= blocked.Add(setOperation.Right.Name);
                }
            }
        }

        return blocked;
    }

    private static bool HasName(ExecutionVariable variable, string variableName)
    {
        return string.Equals(variable.Name, variableName, StringComparison.Ordinal);
    }
}

using System;
using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

internal static class StoredTableRowBufferEligibility
{
    public static bool CanUseTypedRowBuffer(
        IReadOnlyList<ExecutionNode> nodes,
        ExecutionVariable table,
        GeneratedRowShape rowShape)
    {
        foreach (var node in ExecutionIrAnalysis.FlattenNodes(new ExecutionBlock(nodes)))
        {
            if (!CanUseTypedRowBuffer(node, table.Name, rowShape))
                return false;
        }

        return true;
    }

    private static bool CanUseTypedRowBuffer(
        ExecutionNode node,
        string tableName,
        GeneratedRowShape rowShape)
    {
        return node switch
        {
            ExecutionSetOperation or
                ExecutionDistinctTable or
                ExecutionSortTable or
                ExecutionTopNTable or
                ExecutionTopOffsetTable or
                ExecutionSkipTable or
                ExecutionTakeTable or
                ExecutionSliceTable or
                ExecutionProjectTable or
                ExecutionMaterializeRecordListToTable or
                ExecutionParallelBlock or
                ExecutionParallelFilterProjectLoop => false,
            ExecutionCreateTable createTable when !string.Equals(createTable.Table.Name, tableName, StringComparison.Ordinal) => false,
            ExecutionEnsureTableCapacity ensureCapacity when !string.Equals(ensureCapacity.Table.Name, tableName, StringComparison.Ordinal) => false,
            ExecutionAppendRow appendRow when string.Equals(appendRow.Table.Name, tableName, StringComparison.Ordinal) =>
                string.Equals(appendRow.RowShape.TypeName, rowShape.TypeName, StringComparison.Ordinal),
            ExecutionAppendExistingRow appendRow when string.Equals(appendRow.Table.Name, tableName, StringComparison.Ordinal) =>
                string.Equals(appendRow.Row.GeneratedRowTypeName, rowShape.TypeName, StringComparison.Ordinal),
            ExecutionAppendRow or ExecutionAppendExistingRow => false,
            _ => true
        };
    }
}

using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static TableBuildResult ApplyCteRowBufferCapacity(TableBuildResult result)
    {
        if (!result.IsBuilt)
            return result;

        var nodes = result.Nodes.ToList();
        var createTableIndex = nodes.FindIndex(node =>
            node is ExecutionCreateTable createTable &&
            string.Equals(createTable.Table.Name, result.Table.Name, StringComparison.Ordinal));
        if (createTableIndex < 0 ||
            nodes[createTableIndex] is not ExecutionCreateTable { CapacityHint: null } createTable)
        {
            return result;
        }

        var capacityHint = TryCreateCteRowBufferCapacityCandidate(
            new ExecutionBlock(nodes),
            result.Table,
            result.RowShape);
        if (capacityHint == null)
            return result;

        nodes[createTableIndex] = createTable with { CapacityHint = capacityHint };
        return result with { Nodes = nodes };
    }

    private static ExecutionCapacityHint? TryCreateCteRowBufferCapacityCandidate(
        ExecutionBlock block,
        ExecutionVariable table,
        GeneratedRowShape rowShape)
    {
        foreach (var node in block.Nodes)
        {
            var capacityHint = TryCreateCteRowBufferCapacityCandidate(node, table, rowShape);
            if (capacityHint != null)
                return capacityHint;
        }

        return null;
    }

    private static ExecutionCapacityHint? TryCreateCteRowBufferCapacityCandidate(
        ExecutionNode node,
        ExecutionVariable table,
        GeneratedRowShape rowShape)
    {
        return node switch
        {
            ExecutionForEach forEach when ContainsTargetAppend(forEach.Body, table, rowShape) =>
                CreateRowsCapacityCandidate(table, forEach.Source),
            ExecutionForEachWithOrdinality forEach when ContainsTargetAppend(forEach.Body, table, rowShape) =>
                CreateRowsCapacityCandidate(table, forEach.Source),
            ExecutionForEachIndexed forEachIndexed when ContainsTargetAppend(forEachIndexed.Body, table, rowShape) =>
                CreateRowsCapacityCandidate(table, new ExecutionRowStream(forEachIndexed.Source, ExecutionRowStreamKind.Rows)),
            _ => TryCreateCteRowBufferCapacityCandidateFromChildren(node, table, rowShape)
        };
    }

    private static ExecutionCapacityHint? TryCreateCteRowBufferCapacityCandidateFromChildren(
        ExecutionNode node,
        ExecutionVariable table,
        GeneratedRowShape rowShape)
    {
        foreach (var block in ExecutionIrAnalysis.GetChildBlocks(node))
        {
            var capacityHint = TryCreateCteRowBufferCapacityCandidate(block, table, rowShape);
            if (capacityHint != null)
                return capacityHint;
        }

        return null;
    }

    private static bool ContainsTargetAppend(
        ExecutionBlock block,
        ExecutionVariable table,
        GeneratedRowShape rowShape)
    {
        return ExecutionIrAnalysis.FlattenNodes(block).Any(node => node switch
        {
            ExecutionAppendRow appendRow => IsTargetAppend(appendRow.Table, appendRow.RowShape.TypeName, table, rowShape),
            ExecutionAppendExistingRow appendRow => IsTargetAppend(appendRow.Table, appendRow.Row.GeneratedRowTypeName, table, rowShape),
            _ => false
        });
    }

    private static bool IsTargetAppend(
        ExecutionVariable appendTable,
        string? appendRowTypeName,
        ExecutionVariable table,
        GeneratedRowShape rowShape)
    {
        return string.Equals(appendTable.Name, table.Name, StringComparison.Ordinal) &&
               string.Equals(appendRowTypeName, rowShape.TypeName, StringComparison.Ordinal);
    }
}

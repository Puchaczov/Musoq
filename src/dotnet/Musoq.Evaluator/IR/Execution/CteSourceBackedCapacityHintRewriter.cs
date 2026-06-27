using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

internal static class CteSourceBackedCapacityHintRewriter
{
    public static ExecutionNode RewriteNode(
        ExecutionNode node,
        int tableIndex,
        ExecutionExpression sourceRows)
    {
        return node switch
        {
            ExecutionCteSidecarIndexBuildCandidate candidate => candidate with
            {
                Indexes = candidate.Indexes
                    .Select(spec => spec with
                    {
                        CapacityHint = Rewrite(spec.CapacityHint, tableIndex, sourceRows, spec.Index)
                    })
                    .ToArray()
            },
            ExecutionCreateHash createHash => createHash with
            {
                CapacityHint = Rewrite(createHash.CapacityHint, tableIndex, sourceRows, createHash.Hash)
            },
            ExecutionCreateKeySet createSet => createSet with
            {
                CapacityHint = Rewrite(createSet.CapacityHint, tableIndex, sourceRows, createSet.Set)
            },
            _ => node
        };
    }

    private static ExecutionCapacityHint? Rewrite(
        ExecutionCapacityHint? capacityHint,
        int tableIndex,
        ExecutionExpression sourceRows,
        ExecutionVariable target)
    {
        if (capacityHint is ExecutionRowsCapacityHintCandidate
            {
                Rows: ExecutionStoredTableRows storedRows
            } candidate &&
            storedRows.TableIndex == tableIndex)
        {
            return ExecutionCapacityHintCandidates.CreateRowsCandidate(target, sourceRows);
        }

        return capacityHint is ExecutionStoredTableCountCapacityHint storedTable &&
               storedTable.TableIndex == tableIndex
            ? null
            : capacityHint;
    }
}

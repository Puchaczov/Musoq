using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionRecursiveCteAppend(
    string Name,
    ExecutionVariable Result,
    ExecutionVariable Frontier,
    ExecutionVariable? Seen,
    int[] IdentityFieldIndexes,
    int MaxRows,
    ExecutionAppendRow AppendRow
) : ExecutionNode
{
    internal IReadOnlyList<int> NaNDistinctFieldIndexes { get; } =
        ExecutionIrCollections.Freeze(
            IdentityFieldIndexes
                .Where(index =>
                    SetOperationEqualityTypes.IsNanSensitive(AppendRow.RowShape.Fields[index].Type.ResolveClrType())
                )
                .ToArray()
        );
}

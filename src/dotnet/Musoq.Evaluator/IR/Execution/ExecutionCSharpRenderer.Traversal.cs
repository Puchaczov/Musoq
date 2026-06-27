using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private static Dictionary<int, string> CreateStoredRowsCacheNames(ExecutionBlock block)
    {
        var singleStoreIndexes = CollectStoredTableIndexes(block)
            .GroupBy(static index => index)
            .Where(static group => group.Count() == 1)
            .Select(static group => group.Key)
            .ToHashSet();

        return CollectStoredTableRowsIndexes(block)
            .GroupBy(static index => index)
            .Where(group => group.Count() > 1 && singleStoreIndexes.Contains(group.Key))
            .ToDictionary(
                static group => group.Key,
                static group => CreateIdentifierCandidate(
                    $"__storedTable{group.Key.ToString(CultureInfo.InvariantCulture)}Rows",
                    0));
    }

    private static IEnumerable<int> CollectStoredTableRowsIndexes(ExecutionBlock block)
    {
        return ExecutionIrAnalysis
            .CollectExpressions<ExecutionStoredTableRows>(block)
            .Select(static storedRows => storedRows.TableIndex);
    }


    private static IEnumerable<ExecutionFieldRead> CollectFieldReads(ExecutionBlock block)
    {
        return ExecutionIrAnalysis.CollectExpressions<ExecutionFieldRead>(block);
    }

}

using System.Collections.Generic;
using System.Linq;

namespace Musoq.Targets.CSharpClr;

internal sealed record ExecutionPhaseScopeStoredRowsCache(
    ExecutionStoredTableRows StoredRows,
    string CacheName);

internal static class ExecutionPhaseScopeStoredRowsHoister
{
    public static IReadOnlyList<ExecutionPhaseScopeStoredRowsCache> Find(
        IReadOnlyList<ExecutionNode> nodes,
        int beginIndex,
        int endIndex,
        IReadOnlyList<ExecutionNode> bodyNodes,
        IReadOnlyDictionary<int, string> cacheNames,
        HashSet<int> declaredCaches)
    {
        if (cacheNames.Count == 0)
            return [];

        var body = new ExecutionBlock(bodyNodes);
        var builtInScope = ExecutionIrAnalysis
            .CollectNodes<ExecutionStoreTable>(body)
            .Select(static store => store.TableIndex)
            .ToHashSet();
        var usedLater = ExecutionIrAnalysis
            .CollectExpressions<ExecutionStoredTableRows>(
                new ExecutionBlock(nodes.Skip(endIndex + 1).ToArray()))
            .Select(static storedRows => storedRows.TableIndex)
            .ToHashSet();

        return ExecutionIrAnalysis
            .CollectExpressions<ExecutionStoredTableRows>(body)
            .GroupBy(static storedRows => storedRows.TableIndex)
            .Select(static group => group.First())
            .Where(storedRows => !builtInScope.Contains(storedRows.TableIndex))
            .Where(storedRows => usedLater.Contains(storedRows.TableIndex))
            .Where(storedRows => cacheNames.ContainsKey(storedRows.TableIndex))
            .Where(storedRows => declaredCaches.Add(storedRows.TableIndex))
            .Select(storedRows => new ExecutionPhaseScopeStoredRowsCache(
                storedRows,
                cacheNames[storedRows.TableIndex]))
            .ToArray();
    }
}

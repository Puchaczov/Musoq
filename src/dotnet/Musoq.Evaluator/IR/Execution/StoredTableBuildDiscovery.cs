using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

internal static class StoredTableBuildDiscovery
{
    public static IEnumerable<StoredTableBuild> Collect(ExecutionBlock block)
    {
        var pending = new List<ExecutionNode>();
        var yieldedTableIndexes = new HashSet<int>();
        var nodes = block.Nodes;

        for (var index = 0; index < nodes.Count; index++)
        {
            var node = nodes[index];
            if (node is ExecutionParallelBlock)
            {
                pending.Clear();
                continue;
            }

            if (node is ExecutionStoreTable store &&
                !yieldedTableIndexes.Contains(store.TableIndex) &&
                TryCreate(nodes, index, pending, store, out var build))
            {
                yieldedTableIndexes.Add(store.TableIndex);
                yield return build;
                pending.Clear();
                continue;
            }

            pending.Add(node);
        }
    }

    public static bool TryCreate(
        IReadOnlyList<ExecutionNode> nodes,
        int storeIndex,
        List<ExecutionNode> pendingNodes,
        ExecutionStoreTable store,
        out StoredTableBuild build)
    {
        build = null!;

        if (pendingNodes.Count == 0 ||
            pendingNodes.OfType<ExecutionStoreTable>().Any() ||
            ExecutionIrAnalysis.CollectNodes<ExecutionSetOperation>(new ExecutionBlock(pendingNodes)).Any() ||
            !pendingNodes.OfType<ExecutionCreateTable>().Any(createTable =>
                string.Equals(createTable.Table.Name, store.Table.Name, StringComparison.Ordinal)) ||
            ExecutionIrAnalysis.IsVariableUsedAfter(nodes, storeIndex, store.Table.Name))
        {
            return false;
        }

        var enclosingPhaseNodes = pendingNodes
            .OfType<ExecutionPhaseBoundary>()
            .Where(static boundary => string.IsNullOrEmpty(boundary.QueryIdSuffix))
            .Cast<ExecutionNode>()
            .ToArray();
        var nestedPhaseSuffixes = pendingNodes
            .OfType<ExecutionPhaseBoundary>()
            .Where(static boundary =>
                boundary.Phase == QueryPhase.Begin &&
                !string.IsNullOrEmpty(boundary.QueryIdSuffix))
            .Select(static boundary => boundary.QueryIdSuffix)
            .ToHashSet(StringComparer.Ordinal);
        var trailingPhaseNodes = nodes
            .Skip(storeIndex + 1)
            .TakeWhile(node => node is ExecutionPhaseBoundary
            {
                Phase: QueryPhase.End,
                QueryIdSuffix: { Length: > 0 } suffix
            } && nestedPhaseSuffixes.Contains(suffix))
            .ToArray();
        var helperNodes = pendingNodes
            .Where(static node => node is not ExecutionPhaseBoundary
            {
                QueryIdSuffix: null or ""
            })
            .ToArray();

        build = new StoredTableBuild(store.TableIndex, helperNodes, store.Table, [])
        {
            EnclosingPhaseNodes = enclosingPhaseNodes,
            TrailingPhaseNodes = trailingPhaseNodes
        };
        return true;
    }
}

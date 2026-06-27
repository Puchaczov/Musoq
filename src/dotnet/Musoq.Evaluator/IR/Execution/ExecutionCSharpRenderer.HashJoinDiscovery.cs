using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private List<HashJoinHelperSet> CollectHashJoinHelperSets(ExecutionBlock block)
    {
        var buildLoopsByHashName = new Dictionary<string, (int Index, ExecutionSourceLoop Loop, ExecutionHashAdd HashAdd)>(StringComparer.Ordinal);
        var helperSets = new List<HashJoinHelperSet>();
        var usedFunctionNames = new Dictionary<string, int>(StringComparer.Ordinal);
        var nodes = block.Nodes;
        var pending = new List<ExecutionNode>();

        for (var index = 0; index < nodes.Count; index++)
        {
            var node = nodes[index];
            if (node is ExecutionStoreTable store &&
                TryCreateStoredTableBuild(nodes, index, pending, store, out _))
            {
                pending.Clear();
                continue;
            }

            if (IsInsidePendingStoredTableBuild(nodes, index, pending))
            {
                pending.Add(node);
                continue;
            }

            if (TryCreateHashBuildHelperCandidate(node, out var buildLoop, out var hashAdd))
            {
                buildLoopsByHashName.TryAdd(hashAdd.Hash.Name, (index, buildLoop, hashAdd));
                pending.Add(node);
                continue;
            }

            if (!TryCreateHashProbeHelperCandidate(node, out var probeLoop, out var hashProbe) ||
                !buildLoopsByHashName.TryGetValue(hashProbe.Hash.Name, out var build))
            {
                pending.Add(node);
                continue;
            }

            buildLoopsByHashName.Remove(hashProbe.Hash.Name);
            var buildFunctionName = ReserveFunctionName(CreateHashBuildFunctionBaseName(build.HashAdd), usedFunctionNames);
            var probeFunctionName = ReserveFunctionName(CreateHashProbeFunctionBaseName(hashProbe), usedFunctionNames);
            var buildHelper = CreateHashBuildHelper(buildFunctionName, build.Loop, build.HashAdd);
            var probeHelper = CreateHashProbeHelper(probeFunctionName, probeLoop, hashProbe);

            helperSets.Add(new HashJoinHelperSet(build.Index, index, buildHelper, probeHelper));
            pending.Add(node);
        }

        return helperSets;
    }

    private static bool IsInsidePendingStoredTableBuild(
        IReadOnlyList<ExecutionNode> nodes,
        int currentIndex,
        IReadOnlyList<ExecutionNode> pending)
    {
        var pendingTableNames = pending
            .OfType<ExecutionCreateTable>()
            .Select(static createTable => createTable.Table.Name)
            .ToHashSet(StringComparer.Ordinal);

        if (pendingTableNames.Count == 0)
            return false;

        for (var index = currentIndex + 1; index < nodes.Count; index++)
        {
            if (nodes[index] is ExecutionStoreTable store && pendingTableNames.Contains(store.Table.Name))
                return true;
        }

        return false;
    }

    private static bool TryCreateHashBuildHelperCandidate(
        ExecutionNode node,
        out ExecutionSourceLoop loop,
        out ExecutionHashAdd hashAdd)
    {
        if (node is ExecutionSourceLoop sourceLoop &&
            TryGetHashBuildLoopAdd(sourceLoop, out var add) &&
            CanExtractHashBuildLoop(sourceLoop) &&
            !ContainsStoredTableRows(add.Key))
        {
            loop = sourceLoop;
            hashAdd = add;
            return true;
        }

        loop = null!;
        hashAdd = null!;
        return false;
    }

    private static bool TryGetHashBuildLoopAdd(
        ExecutionSourceLoop forEach,
        out ExecutionHashAdd hashAdd)
    {
        if (forEach.Body.Nodes is [ExecutionHashAdd direct])
        {
            hashAdd = direct;
            return true;
        }

        if (forEach.Body.Nodes is [ExecutionCreateGeneratedRow or ExecutionCreateHashPayload, ExecutionHashAdd projected])
        {
            hashAdd = projected;
            return true;
        }

        if (forEach.Body.Nodes.Count > 2 &&
            forEach.Body.Nodes[^1] is ExecutionHashAdd precomputed &&
            IsPrecomputedHashBuildBody(forEach.Body.Nodes))
        {
            hashAdd = precomputed;
            return true;
        }

        hashAdd = null!;
        return false;
    }

    private static bool IsPrecomputedHashBuildBody(IReadOnlyList<ExecutionNode> nodes)
    {
        var createPayloadCount = 0;

        for (var index = 0; index < nodes.Count - 1; index++)
        {
            switch (nodes[index])
            {
                case ExecutionLet:
                case ExecutionContinueIf:
                    continue;
                case ExecutionCreateHashPayload or ExecutionCreateGeneratedRow:
                    createPayloadCount++;
                    if (createPayloadCount > 1)
                        return false;
                    continue;
                default:
                    return false;
            }
        }

        return createPayloadCount == 1;
    }

    private static bool TryCreateHashProbeHelperCandidate(
        ExecutionNode node,
        out ExecutionSourceLoop loop,
        out ExecutionHashProbe hashProbe)
    {
        if (node is ExecutionSourceLoop { Body.Nodes: [ExecutionHashProbe probe] } sourceLoop &&
            CanExtractHashProbeLoop(sourceLoop) &&
            CanExtractHashProbe(probe) &&
            CollectHashProbeAppendTargets(probe).Length > 0)
        {
            loop = sourceLoop;
            hashProbe = probe;
            return true;
        }

        loop = null!;
        hashProbe = null!;
        return false;
    }

    private static bool CanExtractHashBuildLoop(ExecutionSourceLoop loop)
    {
        return TryGetRawGeneratedRowsShape(loop.Source) != null || !ContainsStoredTableRows(loop.Source);
    }

    private static bool CanExtractHashProbeLoop(ExecutionSourceLoop loop)
    {
        return !ContainsStoredTableRows(loop.Source);
    }

    private static GeneratedRowShape? TryGetRawGeneratedRowsShape(ExecutionExpression source)
    {
        return source is ExecutionStoredTableRows { GeneratedRowShape: not null } storedRows
            ? storedRows.GeneratedRowShape
            : null;
    }

    private static bool CanExtractHashProbe(ExecutionHashProbe probe)
    {
        if (ContainsStoredTableRows(probe.Key) ||
            ContainsStoredTableRows(probe.Body) ||
            probe.NoMatchBody is not null && ContainsStoredTableRows(probe.NoMatchBody))
        {
            return false;
        }

        if (ContainsNode<ExecutionIf>(probe.Body) ||
            probe.NoMatchBody is not null && ContainsNode<ExecutionIf>(probe.NoMatchBody))
        {
            return false;
        }

        return true;
    }

    private static bool ContainsStoredTableRows(ExecutionBlock block)
    {
        return ExecutionIrAnalysis.CollectExpressions<ExecutionStoredTableRows>(block).Any();
    }

    private static bool ContainsStoredTableRows(ExecutionExpression? expression)
    {
        return ExecutionIrAnalysis.FlattenExpressions(expression)
            .Any(static current => current is ExecutionStoredTableRows);
    }
}

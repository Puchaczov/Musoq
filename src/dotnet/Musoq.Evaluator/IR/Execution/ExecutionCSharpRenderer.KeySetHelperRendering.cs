using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private sealed record KeySetHelperSet(
        int BuildLoopIndex,
        int ProbeLoopIndex,
        KeySetBuildHelper Build,
        KeySetProbeHelper Probe);

    private sealed record KeySetBuildHelper(
        string FunctionName,
        string RowsParameterName,
        ExecutionSourceLoop Loop,
        ExecutionKeySetAdd KeySetAdd,
        GeneratedRowShape? RawRowsShape,
        IReadOnlyList<CapturedLocal> Captures);

    private sealed record KeySetProbeHelper(
        string FunctionName,
        string RowsParameterName,
        ExecutionSourceLoop Loop,
        ExecutionKeySetProbe KeySetProbe,
        IReadOnlyList<ExecutionVariable> AppendTargets,
        IReadOnlyList<CapturedLocal> Captures);

    private List<KeySetHelperSet> CollectKeySetHelperSets(ExecutionBlock block)
    {
        var buildLoopsBySetName = new Dictionary<string, (int Index, ExecutionSourceLoop Loop, ExecutionKeySetAdd KeySetAdd)>(StringComparer.Ordinal);
        var helperSets = new List<KeySetHelperSet>();
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

            if (TryCreateKeySetBuildHelperCandidate(node, out var buildLoop, out var keySetAdd))
            {
                buildLoopsBySetName.TryAdd(keySetAdd.Set.Name, (index, buildLoop, keySetAdd));
                pending.Add(node);
                continue;
            }

            if (!TryCreateKeySetProbeHelperCandidate(node, out var probeLoop, out var keySetProbe) ||
                !buildLoopsBySetName.TryGetValue(keySetProbe.Set.Name, out var build))
            {
                pending.Add(node);
                continue;
            }

            buildLoopsBySetName.Remove(keySetProbe.Set.Name);
            var buildFunctionName = ReserveFunctionName(CreateKeySetBuildFunctionBaseName(build.KeySetAdd), usedFunctionNames);
            var probeFunctionName = ReserveFunctionName(CreateKeySetProbeFunctionBaseName(keySetProbe), usedFunctionNames);
            var buildHelper = CreateKeySetBuildHelper(buildFunctionName, build.Loop, build.KeySetAdd);
            var probeHelper = CreateKeySetProbeHelper(probeFunctionName, probeLoop, keySetProbe);

            helperSets.Add(new KeySetHelperSet(build.Index, index, buildHelper, probeHelper));
            pending.Add(node);
        }

        return helperSets;
    }

    private static bool TryCreateKeySetBuildHelperCandidate(
        ExecutionNode node,
        out ExecutionSourceLoop loop,
        out ExecutionKeySetAdd keySetAdd)
    {
        if (node is ExecutionSourceLoop { Body.Nodes: [ExecutionKeySetAdd add] } sourceLoop &&
            CanExtractHashBuildLoop(sourceLoop) &&
            !ContainsStoredTableRows(add.Key))
        {
            loop = sourceLoop;
            keySetAdd = add;
            return true;
        }

        loop = null!;
        keySetAdd = null!;
        return false;
    }

    private static bool TryCreateKeySetProbeHelperCandidate(
        ExecutionNode node,
        out ExecutionSourceLoop loop,
        out ExecutionKeySetProbe keySetProbe)
    {
        if (node is ExecutionSourceLoop { Body.Nodes: [ExecutionKeySetProbe probe] } sourceLoop &&
            CanExtractHashProbeLoop(sourceLoop) &&
            CanExtractKeySetProbe(probe) &&
            CollectKeySetProbeAppendTargets(probe).Length > 0)
        {
            loop = sourceLoop;
            keySetProbe = probe;
            return true;
        }

        loop = null!;
        keySetProbe = null!;
        return false;
    }

    private static bool CanExtractKeySetProbe(ExecutionKeySetProbe probe)
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

    private KeySetBuildHelper CreateKeySetBuildHelper(
        string functionName,
        ExecutionSourceLoop loop,
        ExecutionKeySetAdd keySetAdd)
    {
        var rowsParameterName = CreateValueTupleRowsParameterName(loop);
        var captures = CollectKeySetBuildCaptures(loop, keySetAdd);
        var rawRowsShape = TryGetRawGeneratedRowsShape(loop.Source);

        return new KeySetBuildHelper(functionName, rowsParameterName, loop, keySetAdd, rawRowsShape, captures);
    }

    private KeySetProbeHelper CreateKeySetProbeHelper(
        string functionName,
        ExecutionSourceLoop loop,
        ExecutionKeySetProbe keySetProbe)
    {
        var rowsParameterName = CreateValueTupleRowsParameterName(loop);
        var appendTargets = CollectKeySetProbeAppendTargets(keySetProbe);
        var captures = CollectKeySetProbeCaptures(loop, keySetProbe, appendTargets);

        return new KeySetProbeHelper(functionName, rowsParameterName, loop, keySetProbe, appendTargets, captures);
    }

    private CapturedLocal[] CollectKeySetBuildCaptures(
        ExecutionSourceLoop loop,
        ExecutionKeySetAdd keySetAdd)
    {
        var excludedNames = new HashSet<string>(StringComparer.Ordinal)
        {
            loop.Item.Name,
            keySetAdd.Set.Name
        };
        foreach (var variableName in CollectHashJoinDeclaredVariableNames(loop.Body))
            excludedNames.Add(variableName);

        var captures = new Dictionary<string, CapturedLocal>(StringComparer.Ordinal);
        AddHashJoinBodyHelperCaptures(loop.Body, excludedNames, captures);
        return captures.Values.ToArray();
    }

    private CapturedLocal[] CollectKeySetProbeCaptures(
        ExecutionSourceLoop loop,
        ExecutionKeySetProbe keySetProbe,
        IReadOnlyList<ExecutionVariable> appendTargets)
    {
        var excludedNames = new HashSet<string>(StringComparer.Ordinal)
        {
            loop.Item.Name,
            keySetProbe.Set.Name
        };

        if (keySetProbe.MatchFound is not null)
            excludedNames.Add(keySetProbe.MatchFound.Name);

        foreach (var appendTarget in appendTargets)
            excludedNames.Add(appendTarget.Name);

        foreach (var variableName in CollectHashJoinDeclaredVariableNames(keySetProbe.Body))
            excludedNames.Add(variableName);

        if (keySetProbe.NoMatchBody is not null)
            foreach (var variableName in CollectHashJoinDeclaredVariableNames(keySetProbe.NoMatchBody))
                excludedNames.Add(variableName);

        var captures = new Dictionary<string, CapturedLocal>(StringComparer.Ordinal);
        AddHelperCaptures(keySetProbe.Key, excludedNames, captures);
        AddHashJoinBodyHelperCaptures(keySetProbe.Body, excludedNames, captures);
        if (keySetProbe.NoMatchBody is not null)
            AddHashJoinBodyHelperCaptures(keySetProbe.NoMatchBody, excludedNames, captures);

        return captures.Values.ToArray();
    }

    private static ExecutionVariable[] CollectKeySetProbeAppendTargets(ExecutionKeySetProbe keySetProbe)
    {
        var targets = new Dictionary<string, ExecutionVariable>(StringComparer.Ordinal);
        AddAppendTargets(keySetProbe.Body, targets);
        if (keySetProbe.NoMatchBody is not null)
            AddAppendTargets(keySetProbe.NoMatchBody, targets);

        return targets.Values.ToArray();
    }
}

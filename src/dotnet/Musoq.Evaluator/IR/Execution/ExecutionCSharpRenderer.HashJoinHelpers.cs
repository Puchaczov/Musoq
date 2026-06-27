using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private sealed record HashJoinHelperSet(
        int BuildLoopIndex,
        int ProbeLoopIndex,
        HashBuildHelper Build,
        HashProbeHelper Probe);

    private sealed record HashBuildHelper(
        string FunctionName,
        string RowsParameterName,
        ExecutionSourceLoop Loop,
        ExecutionHashAdd HashAdd,
        GeneratedRowShape? RawRowsShape,
        IReadOnlyList<CapturedLocal> Captures);

    private sealed record HashProbeHelper(
        string FunctionName,
        string RowsParameterName,
        ExecutionSourceLoop Loop,
        ExecutionHashProbe HashProbe,
        IReadOnlyList<ExecutionVariable> AppendTargets,
        IReadOnlyList<CapturedLocal> Captures);

    private HashBuildHelper CreateHashBuildHelper(
        string functionName,
        ExecutionSourceLoop loop,
        ExecutionHashAdd hashAdd)
    {
        var rowsParameterName = CreateValueTupleRowsParameterName(loop);
        var captures = CollectHashBuildCaptures(loop, hashAdd);
        var rawRowsShape = TryGetRawGeneratedRowsShape(loop.Source);

        return new HashBuildHelper(functionName, rowsParameterName, loop, hashAdd, rawRowsShape, captures);
    }

    private HashProbeHelper CreateHashProbeHelper(
        string functionName,
        ExecutionSourceLoop loop,
        ExecutionHashProbe hashProbe)
    {
        var rowsParameterName = CreateValueTupleRowsParameterName(loop);
        var appendTargets = CollectHashProbeAppendTargets(hashProbe);
        var captures = CollectHashProbeCaptures(loop, hashProbe, appendTargets);

        return new HashProbeHelper(functionName, rowsParameterName, loop, hashProbe, appendTargets, captures);
    }

    private CapturedLocal[] CollectHashBuildCaptures(
        ExecutionSourceLoop loop,
        ExecutionHashAdd hashAdd)
    {
        var excludedNames = new HashSet<string>(StringComparer.Ordinal)
        {
            loop.Item.Name,
            hashAdd.Hash.Name
        };
        AddProfileRecorderExcludedName(excludedNames);

        foreach (var variableName in CollectHashJoinDeclaredVariableNames(loop.Body))
            excludedNames.Add(variableName);

        var captures = new Dictionary<string, CapturedLocal>(StringComparer.Ordinal);

        AddHashJoinBodyHelperCaptures(loop.Body, excludedNames, captures);
        return captures.Values.ToArray();
    }

    private CapturedLocal[] CollectHashProbeCaptures(
        ExecutionSourceLoop loop,
        ExecutionHashProbe hashProbe,
        IReadOnlyList<ExecutionVariable> appendTargets)
    {
        var excludedNames = new HashSet<string>(StringComparer.Ordinal)
        {
            loop.Item.Name,
            hashProbe.Hash.Name,
            hashProbe.Matches.Name
        };
        AddProfileRecorderExcludedName(excludedNames);

        if (hashProbe.MatchFound is not null)
            excludedNames.Add(hashProbe.MatchFound.Name);

        foreach (var appendTarget in appendTargets)
            excludedNames.Add(appendTarget.Name);

        foreach (var variableName in CollectHashJoinDeclaredVariableNames(hashProbe.Body))
            excludedNames.Add(variableName);

        if (hashProbe.NoMatchBody is not null)
            foreach (var variableName in CollectHashJoinDeclaredVariableNames(hashProbe.NoMatchBody))
                excludedNames.Add(variableName);

        var captures = new Dictionary<string, CapturedLocal>(StringComparer.Ordinal);
        AddHelperCaptures(hashProbe.Key, excludedNames, captures);
        AddHashJoinBodyHelperCaptures(hashProbe.Body, excludedNames, captures);
        if (hashProbe.NoMatchBody is not null)
            AddHashJoinBodyHelperCaptures(hashProbe.NoMatchBody, excludedNames, captures);

        return captures.Values.ToArray();
    }

    private static ExecutionVariable[] CollectHashProbeAppendTargets(ExecutionHashProbe hashProbe)
    {
        var targets = new Dictionary<string, ExecutionVariable>(StringComparer.Ordinal);
        AddAppendTargets(hashProbe.Body, targets);
        if (hashProbe.NoMatchBody is not null)
            AddAppendTargets(hashProbe.NoMatchBody, targets);

        return targets.Values.ToArray();
    }

    private static void AddAppendTargets(
        ExecutionBlock block,
        Dictionary<string, ExecutionVariable> targets)
    {
        foreach (var node in block.Nodes)
        {
            if (node is ExecutionAppendRow appendRow)
                targets.TryAdd(appendRow.Table.Name, appendRow.Table);

            foreach (var childBlock in GetChildBlocks(node))
                AddAppendTargets(childBlock, targets);
        }
    }
}

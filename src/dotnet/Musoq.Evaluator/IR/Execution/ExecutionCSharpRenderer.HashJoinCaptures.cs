using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private void AddHashJoinBodyHelperCaptures(
        ExecutionBlock block,
        HashSet<string> excludedNames,
        Dictionary<string, CapturedLocal> captures)
    {
        foreach (var node in block.Nodes)
            AddHashJoinBodyHelperCaptures(node, excludedNames, captures);
    }

    private void AddHashJoinBodyHelperCaptures(
        ExecutionNode node,
        HashSet<string> excludedNames,
        Dictionary<string, CapturedLocal> captures)
    {
        switch (node)
        {
            case ExecutionAppendRow appendRow:
                AddHelperCaptures(appendRow.Values.Select(static value => value.Value), excludedNames, captures);
                AddHelperCaptures(appendRow.Contexts, excludedNames, captures);
                AddHelperCaptures(GetContextLayoutExpressions(appendRow.ContextLayout), excludedNames, captures);
                break;
            case ExecutionForEach forEach:
                AddHelperCaptures(forEach.Source, excludedNames, captures);
                AddHashJoinBodyHelperCaptures(forEach.Body, excludedNames, captures);
                break;
            case ExecutionForEachWithOrdinality forEach:
                AddHelperCaptures(forEach.Source, excludedNames, captures);
                AddHashJoinBodyHelperCaptures(forEach.Body, excludedNames, captures);
                break;
            case ExecutionForEachIndexed forEachIndexed:
                AddHelperCaptures(new ExecutionVariableRead(forEachIndexed.Source), excludedNames, captures);
                AddHashJoinBodyHelperCaptures(forEachIndexed.Body, excludedNames, captures);
                break;
            case ExecutionIf branch:
                AddHelperCaptures(branch.Condition, excludedNames, captures);
                AddHashJoinBodyHelperCaptures(branch.Body, excludedNames, captures);
                break;
            case ExecutionHashProbe hashProbe:
                AddHelperCaptures(hashProbe.Key, excludedNames, captures);
                AddHashJoinBodyHelperCaptures(hashProbe.Body, excludedNames, captures);
                if (hashProbe.NoMatchBody is not null)
                    AddHashJoinBodyHelperCaptures(hashProbe.NoMatchBody, excludedNames, captures);
                break;
            case ExecutionKeySetProbe keySetProbe:
                AddHelperCaptures(keySetProbe.Key, excludedNames, captures);
                AddHashJoinBodyHelperCaptures(keySetProbe.Body, excludedNames, captures);
                if (keySetProbe.NoMatchBody is not null)
                    AddHashJoinBodyHelperCaptures(keySetProbe.NoMatchBody, excludedNames, captures);
                break;
            default:
                AddHelperCaptures(node, excludedNames, captures);
                break;
        }
    }

    private static void AddHashJoinBodyCaptures(
        ExecutionBlock block,
        HashSet<string> excludedNames,
        Dictionary<string, ExecutionVariable> captures)
    {
        foreach (var node in block.Nodes)
            AddHashJoinBodyCaptures(node, excludedNames, captures);
    }

    private static void AddHashJoinBodyCaptures(
        ExecutionNode node,
        HashSet<string> excludedNames,
        Dictionary<string, ExecutionVariable> captures)
    {
        switch (node)
        {
            case ExecutionAppendRow appendRow:
                AddCaptures(appendRow.Values.Select(static value => value.Value), excludedNames, captures);
                AddCaptures(appendRow.Contexts, excludedNames, captures);
                AddCaptures(GetContextLayoutExpressions(appendRow.ContextLayout), excludedNames, captures);
                break;
            case ExecutionForEach forEach:
                AddCaptures(forEach.Source, excludedNames, captures);
                AddHashJoinBodyCaptures(forEach.Body, excludedNames, captures);
                break;
            case ExecutionForEachWithOrdinality forEach:
                AddCaptures(forEach.Source, excludedNames, captures);
                AddHashJoinBodyCaptures(forEach.Body, excludedNames, captures);
                break;
            case ExecutionForEachIndexed forEachIndexed:
                AddCaptures(new ExecutionVariableRead(forEachIndexed.Source), excludedNames, captures);
                AddHashJoinBodyCaptures(forEachIndexed.Body, excludedNames, captures);
                break;
            case ExecutionIf branch:
                AddCaptures(branch.Condition, excludedNames, captures);
                AddHashJoinBodyCaptures(branch.Body, excludedNames, captures);
                break;
            case ExecutionHashProbe hashProbe:
                AddCaptures(hashProbe.Key, excludedNames, captures);
                AddHashJoinBodyCaptures(hashProbe.Body, excludedNames, captures);
                if (hashProbe.NoMatchBody is not null)
                    AddHashJoinBodyCaptures(hashProbe.NoMatchBody, excludedNames, captures);
                break;
            case ExecutionKeySetProbe keySetProbe:
                AddCaptures(keySetProbe.Key, excludedNames, captures);
                AddHashJoinBodyCaptures(keySetProbe.Body, excludedNames, captures);
                if (keySetProbe.NoMatchBody is not null)
                    AddHashJoinBodyCaptures(keySetProbe.NoMatchBody, excludedNames, captures);
                break;
            default:
                AddCaptures(node, excludedNames, captures);
                break;
        }
    }

    private static IEnumerable<string> CollectHashJoinDeclaredVariableNames(ExecutionBlock block)
    {
        foreach (var node in block.Nodes)
        {
            switch (node)
            {
                case ExecutionLet let:
                    yield return let.Variable.Name;
                    break;
                case ExecutionCreateObject createObject: yield return createObject.Target.Name; break;
                case ExecutionMethodTargetDeclarationCandidate candidate: yield return candidate.Target.Name; break;
                case ExecutionForEach forEach:
                    yield return forEach.Item.Name;
                    break;
                case ExecutionForEachWithOrdinality forEach:
                    yield return forEach.Item.Name;
                    yield return forEach.Ordinal.Name;
                    break;
                case ExecutionForEachIndexed forEachIndexed:
                    yield return forEachIndexed.Item.Name;
                    yield return forEachIndexed.Index.Name;
                    break;
                case ExecutionCreateGeneratedRow createRow:
                    yield return createRow.Row.Name;
                    break;
                case ExecutionCreateHashPayload createPayload:
                    yield return createPayload.Payload.Name;
                    break;
                case ExecutionHashProbe hashProbe:
                    yield return hashProbe.Matches.Name;
                    if (hashProbe.MatchFound is not null)
                        yield return hashProbe.MatchFound.Name;
                    break;
                case ExecutionKeySetProbe keySetProbe:
                    if (keySetProbe.MatchFound is not null)
                        yield return keySetProbe.MatchFound.Name;
                    break;
            }

            foreach (var childBlock in GetChildBlocks(node))
            {
                foreach (var variableName in CollectHashJoinDeclaredVariableNames(childBlock))
                    yield return variableName;
            }
        }
    }

    private static string CreateHashBuildFunctionBaseName(ExecutionHashAdd hashAdd)
    {
        return $"Build{CreatePascalIdentifier(hashAdd.Hash.Name)}";
    }

    private static string CreateHashProbeFunctionBaseName(ExecutionHashProbe hashProbe)
    {
        return hashProbe.NoMatchBody is { Nodes.Count: > 0 }
            ? "AppendLeftJoinRows"
            : "AppendHashJoinRows";
    }

    private static string ReserveFunctionName(
        string baseName,
        Dictionary<string, int> usedFunctionNames)
    {
        if (!usedFunctionNames.TryGetValue(baseName, out var count))
        {
            usedFunctionNames.Add(baseName, 1);
            return baseName;
        }

        usedFunctionNames[baseName] = count + 1;
        return $"{baseName}{count.ToString(CultureInfo.InvariantCulture)}";
    }
}

using System.Collections.Generic;
using System.Linq;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private CapturedLocal[] CollectParallelSingleKeyAggregateCaptures(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate)
    {
        var excludedNames = new HashSet<string>(StringComparer.Ordinal)
        {
            parallelAggregate.Source.Name,
            parallelAggregate.Group.Name,
            parallelAggregate.RootGroup.Name,
            parallelAggregate.GroupsToFinalize.Name
        };
        var captures = new Dictionary<string, CapturedLocal>(StringComparer.Ordinal);

        foreach (var variableName in ExecutionIrAnalysis.CollectDeclaredVariableNames(parallelAggregate.AggregateBody))
            excludedNames.Add(variableName);
        AddHelperCaptures(parallelAggregate.Key, excludedNames, captures);
        AddHelperCaptures(parallelAggregate.AggregateBody, excludedNames, captures);

        return captures.Values.ToArray();
    }

    private static void AddCapture(
        ExecutionVariable variable,
        HashSet<string> excludedNames,
        Dictionary<string, ExecutionVariable> captures)
    {
        if (string.IsNullOrWhiteSpace(variable.Name) || excludedNames.Contains(variable.Name))
            return;

        captures.TryAdd(variable.Name, variable);
    }

    private static void AddCaptures(
        ExecutionBlock block,
        HashSet<string> excludedNames,
        Dictionary<string, ExecutionVariable> captures)
    {
        foreach (var node in block.Nodes)
            AddCaptures(node, excludedNames, captures);
    }

    private static void AddCaptures(
        ExecutionNode node,
        HashSet<string> excludedNames,
        Dictionary<string, ExecutionVariable> captures)
    {
        switch (node)
        {
            case ExecutionLet let:
                AddCaptures(let.Value, excludedNames, captures);
                break;
            case ExecutionAssign assign:
                AddCaptures(assign.Value, excludedNames, captures);
                break;
            case ExecutionIf branch:
                AddCaptures(branch.Condition, excludedNames, captures);
                AddCaptures(branch.Body, excludedNames, captures);
                break;
            case ExecutionAggregateSet aggregateSet:
                AddCaptures(
                    AggregateKernelArgumentSelector.SelectValueArgumentsAfterGroup(aggregateSet.Arguments),
                    excludedNames,
                    captures);
                AddCaptures(aggregateSet.AccumulatorInput, excludedNames, captures);
                break;
            case ExecutionAggregateCapturedValueSet capturedValueSet:
                AddCaptures(capturedValueSet.Value, excludedNames, captures);
                break;
            case ExecutionGetOrAddSingleKeyAggregateGroup getOrAdd:
                AddCaptures(getOrAdd.Key, excludedNames, captures);
                break;
            case ExecutionGetOrAddValueTupleAggregateGroup getOrAdd:
                AddCaptures(getOrAdd.Keys, excludedNames, captures);
                break;
            case ExecutionHashProbe hashProbe:
                AddCapture(
                    hashProbe.Hash with
                    {
                        Type = ExecutionClrBindingFactory.FromClr(typeof(object)),
                        GeneratedRowTypeName = CreateHashTypeName(
                            hashProbe.KeyType.RequireClrType(),
                            hashProbe.RowType.RequireClrType(),
                            hashProbe.GeneratedRowTypeName)
                    },
                    excludedNames,
                    captures);
                AddCaptures(hashProbe.Key, excludedNames, captures);
                AddCaptures(hashProbe.Body, excludedNames, captures);
                if (hashProbe.NoMatchBody != null)
                    AddCaptures(hashProbe.NoMatchBody, excludedNames, captures);
                break;
            case ExecutionKeySetProbe keySetProbe:
                AddCapture(
                    keySetProbe.Set with
                    {
                        Type = ExecutionClrBindingFactory.FromClr(typeof(object)),
                        GeneratedRowTypeName = CreateKeySetTypeName(keySetProbe.KeyType)
                    },
                    excludedNames,
                    captures);
                AddCaptures(keySetProbe.Key, excludedNames, captures);
                AddCaptures(keySetProbe.Body, excludedNames, captures);
                if (keySetProbe.NoMatchBody != null)
                    AddCaptures(keySetProbe.NoMatchBody, excludedNames, captures);
                break;
            default:
                foreach (var childBlock in GetChildBlocks(node))
                    AddCaptures(childBlock, excludedNames, captures);
                break;
        }
    }

    private static void AddCaptures(
        IEnumerable<ExecutionExpression> expressions,
        HashSet<string> excludedNames,
        Dictionary<string, ExecutionVariable> captures)
    {
        foreach (var expression in expressions)
            AddCaptures(expression, excludedNames, captures);
    }

    private static void AddCaptures(
        ExecutionExpression? expression,
        HashSet<string> excludedNames,
        Dictionary<string, ExecutionVariable> captures)
    {
        if (expression == null)
            return;

        foreach (var current in ExecutionIrAnalysis.FlattenExpressions(expression))
        {
            switch (current)
            {
                case ExecutionVariableRead variableRead:
                    AddCapture(variableRead.Variable, excludedNames, captures);
                    break;
                case ExecutionMethodCall methodCall:
                    if (methodCall.Target != null)
                        AddCapture(methodCall.Target, excludedNames, captures);
                    if (methodCall.Cache != null)
                        AddCapture(methodCall.Cache, excludedNames, captures);
                    break;
                case ExecutionRowStream rows:
                    AddCapture(rows.Variable, excludedNames, captures);
                    break;
                case ExecutionScalarRowStream rows:
                    AddCapture(rows.Variable, excludedNames, captures);
                    break;
                case ExecutionRowContextsRead rowContexts:
                    AddCapture(rowContexts.Row, excludedNames, captures);
                    break;
                case ExecutionWindowValueRead windowValue:
                    AddCapture(windowValue.Results, excludedNames, captures);
                    AddCapture(windowValue.Index, excludedNames, captures);
                    break;
                case ExecutionAggregateCall aggregateCall:
                    AddCapture(aggregateCall.Group, excludedNames, captures);
                    break;
                case ExecutionGroupKeyRead groupKey:
                    AddCapture(groupKey.Group, excludedNames, captures);
                    break;
                case ExecutionAggregateCapturedValueRead capturedValue:
                    AddCapture(capturedValue.Group, excludedNames, captures);
                    break;
            }
        }
    }
}

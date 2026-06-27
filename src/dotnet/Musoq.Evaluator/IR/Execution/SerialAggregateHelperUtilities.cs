using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private static ExecutionGetOrAddSingleKeyAggregateGroup CreateSerialSingleKeyAggregateHelperGroupAcquisition(
        ExecutionGetOrAddSingleKeyAggregateGroup getOrAddGroup)
    {
        if (getOrAddGroup.Groups.Name == SerialSingleKeyAggregateGroupsParameterName &&
            getOrAddGroup.GroupsToFinalize.Name == SerialSingleKeyAggregateGroupsToFinalizeParameterName &&
            (getOrAddGroup.NullGroup is null ||
             getOrAddGroup.NullGroup.Name == SerialSingleKeyAggregateNullGroupParameterName))
        {
            return getOrAddGroup;
        }

        return getOrAddGroup with
        {
            Groups = getOrAddGroup.Groups with { Name = SerialSingleKeyAggregateGroupsParameterName },
            GroupsToFinalize = getOrAddGroup.GroupsToFinalize with
            {
                Name = SerialSingleKeyAggregateGroupsToFinalizeParameterName
            },
            NullGroup = getOrAddGroup.NullGroup is null
                ? null
                : getOrAddGroup.NullGroup with { Name = SerialSingleKeyAggregateNullGroupParameterName }
        };
    }

    private CapturedLocal[] CollectSerialSingleKeyAggregateCaptures(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate)
    {
        var getOrAddGroup = GetSerialSingleKeyAggregateGroupAcquisition(parallelAggregate);
        var excludedNames = new HashSet<string>(StringComparer.Ordinal)
        {
            parallelAggregate.SerialLoop.Item.Name,
            SerialSingleKeyAggregateRowsParameterName,
            getOrAddGroup.RootGroup.Name,
            getOrAddGroup.Groups.Name,
            getOrAddGroup.GroupsToFinalize.Name,
            getOrAddGroup.Group.Name
        };
        AddProfileRecorderExcludedName(excludedNames);

        if (getOrAddGroup.NullGroup is not null)
            excludedNames.Add(getOrAddGroup.NullGroup.Name);

        foreach (var variableName in CollectDeclaredVariableNames(parallelAggregate.SerialLoop.Body))
            excludedNames.Add(variableName);

        var captures = new Dictionary<string, CapturedLocal>(StringComparer.Ordinal);
        AddHelperCaptures(parallelAggregate.SerialLoop.Body, excludedNames, captures);
        return captures.Values.ToArray();
    }

    private static IEnumerable<string> CollectDeclaredVariableNames(ExecutionBlock block)
    {
        return ExecutionIrAnalysis.CollectDeclaredVariableNames(block);
    }

    private static ExecutionGetOrAddSingleKeyAggregateGroup GetSerialSingleKeyAggregateGroupAcquisition(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate)
    {
        var getOrAddGroups = parallelAggregate.SerialLoop.Body.Nodes
            .OfType<ExecutionGetOrAddSingleKeyAggregateGroup>()
            .ToArray();

        if (getOrAddGroups.Length != 1)
        {
            throw new InvalidOperationException(
                $"Parallel single-key aggregate loop for '{parallelAggregate.GroupsToFinalize.Name}' expected exactly one serial group acquisition, but found {getOrAddGroups.Length.ToString(CultureInfo.InvariantCulture)}.");
        }

        return getOrAddGroups[0];
    }

    private static GenericNameSyntax CreateEnumerableTypeSyntax(TypeSyntax itemType)
    {
        return SyntaxFactory.GenericName(nameof(IEnumerable<>))
            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(itemType)));
    }

    private static TypeSyntax CreateAggregateRowsParameterType(ExecutionExpression sourceRows, TypeSyntax rowType)
    {
        return ExecutionRowStreams.IsChunked(sourceRows)
            ? CreateEnumerableTypeSyntax(CreateReadOnlyListTypeSyntax(rowType))
            : CreateEnumerableTypeSyntax(rowType);
    }
}

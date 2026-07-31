using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Execution.Facts;
using Musoq.Targets.CSharpClr.Rendering.CodeGeneration;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    internal sealed record SingleKeyAggregateUpdateHelper(
        string FunctionName,
        ExecutionBlock Body,
        ExecutionGetOrAddSingleKeyAggregateGroup GroupAcquisition,
        IReadOnlyList<CapturedLocal> Captures);

    private sealed record SingleKeyAggregateUpdateCandidate(
        ExecutionBlock Block,
        ExecutionGetOrAddSingleKeyAggregateGroup GroupAcquisition,
        int AggregateUpdateCount,
        IReadOnlyDictionary<string, ExecutionVariable> ScopedVariables);

    private IReadOnlyDictionary<ExecutionBlock, SingleKeyAggregateUpdateHelper> CollectSingleKeyAggregateUpdateHelpersByBlock(
        ExecutionBlock block)
    {
        var candidates = new List<SingleKeyAggregateUpdateCandidate>();
        CollectSingleKeyAggregateUpdateCandidates(
            block,
            candidates,
            new Dictionary<string, ExecutionVariable>(StringComparer.Ordinal));

        var helpers = new Dictionary<ExecutionBlock, SingleKeyAggregateUpdateHelper>();
        var functionNameCounts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var candidate in candidates)
            helpers.Add(candidate.Block, CreateSingleKeyAggregateUpdateHelper(candidate, functionNameCounts));

        return helpers;
    }

    private void CollectSingleKeyAggregateUpdateCandidates(
        ExecutionBlock block,
        List<SingleKeyAggregateUpdateCandidate> candidates,
        IReadOnlyDictionary<string, ExecutionVariable> scopedVariables)
    {
        if (TryCreateSingleKeyAggregateUpdateCandidate(block, scopedVariables, out var candidate))
            candidates.Add(candidate);

        var currentScope = new Dictionary<string, ExecutionVariable>(scopedVariables, StringComparer.Ordinal);
        foreach (var node in block.Nodes)
        {
            var nodeScope = new Dictionary<string, ExecutionVariable>(currentScope, StringComparer.Ordinal);
            AddDeclaredVariables(node, nodeScope);

            foreach (var childBlock in ExecutionIrAnalysis.GetChildBlocks(node))
            {
                CollectSingleKeyAggregateUpdateCandidates(
                    childBlock,
                    candidates,
                    nodeScope);
            }

            AddDeclaredVariables(node, currentScope);
        }
    }

    private bool TryCreateSingleKeyAggregateUpdateCandidate(
        ExecutionBlock block,
        IReadOnlyDictionary<string, ExecutionVariable> scopedVariables,
        out SingleKeyAggregateUpdateCandidate candidate)
    {
        candidate = null!;
        var nodes = block.Nodes;
        if (nodes.Count < 3)
            return false;

        var index = 0;
        while (index < nodes.Count && nodes[index] is ExecutionLet)
            index++;

        if (index >= nodes.Count || nodes[index] is not ExecutionGetOrAddSingleKeyAggregateGroup getOrAddGroup)
            return false;

        index++;
        var aggregateUpdateCount = 0;
        for (; index < nodes.Count; index++)
        {
            if (!IsAggregateUpdateForGroup(nodes[index], getOrAddGroup.Group))
                return false;

            aggregateUpdateCount++;
        }

        if (aggregateUpdateCount < 1)
            return false;

        if (ContainsUnsafeObjectBackedFieldRead(block, scopedVariables))
            return false;

        candidate = new SingleKeyAggregateUpdateCandidate(
            block,
            getOrAddGroup,
            aggregateUpdateCount,
            new Dictionary<string, ExecutionVariable>(scopedVariables, StringComparer.Ordinal));
        return true;
    }

    private static bool ContainsUnsafeObjectBackedFieldRead(
        ExecutionBlock block,
        IReadOnlyDictionary<string, ExecutionVariable> scopedVariables)
    {
        return ExecutionIrAnalysis
            .CollectExpressions<ExecutionFieldRead>(block)
            .Any(fieldRead =>
                !string.IsNullOrWhiteSpace(fieldRead.Alias) &&
                scopedVariables.TryGetValue(fieldRead.Alias, out var variable) &&
                variable.Type.RequireClrType() == typeof(object) &&
                string.IsNullOrWhiteSpace(variable.GeneratedRowTypeName) &&
                !HasHelperSafeObjectFieldAccess(fieldRead.AccessStrategy));
    }

    private static bool HasHelperSafeObjectFieldAccess(FieldAccessStrategy? strategy)
    {
        return strategy is GeneratedRowNestedAccess or
            GeneratedRowTypeAccess or
            GeneratedFieldAccess or
            PositionalAccess or
            DirectScalarValueAccess;
    }

    private SingleKeyAggregateUpdateHelper CreateSingleKeyAggregateUpdateHelper(
        SingleKeyAggregateUpdateCandidate candidate,
        Dictionary<string, int> functionNameCounts)
    {
        var getOrAddGroup = candidate.GroupAcquisition;
        var baseFunctionName = CreateSingleKeyAggregateUpdateFunctionName(getOrAddGroup.Groups.Name);
        functionNameCounts.TryGetValue(baseFunctionName, out var functionIndex);
        functionNameCounts[baseFunctionName] = functionIndex + 1;

        return new SingleKeyAggregateUpdateHelper(
            CreateSingleKeyAggregateUpdateFunctionName(getOrAddGroup.Groups.Name, functionIndex),
            candidate.Block,
            getOrAddGroup,
            CollectSingleKeyAggregateUpdateCaptures(candidate.Block, getOrAddGroup, candidate.ScopedVariables));
    }

    private static void AddDeclaredVariables(
        ExecutionNode node,
        IDictionary<string, ExecutionVariable> scopedVariables)
    {
        foreach (var variable in ExecutionNodeFacts.GetDeclaredVariables(node))
            scopedVariables[variable.Name] = variable;
    }

    private static bool IsAggregateUpdateForGroup(ExecutionNode node, ExecutionVariable group)
    {
        return node switch
        {
            ExecutionAggregateSet aggregateSet => HasSameName(aggregateSet.Group, group),
            ExecutionAggregateCapturedValueSet capturedValueSet => HasSameName(capturedValueSet.Group, group),
            _ => false
        };
    }

    private static bool HasSameName(ExecutionVariable left, ExecutionVariable right)
    {
        return string.Equals(left.Name, right.Name, StringComparison.Ordinal);
    }

    private static string CreateSingleKeyAggregateUpdateFunctionName(string groupsName, int helperIndex = 0)
    {
        var suffix = helperIndex == 0
            ? string.Empty
            : helperIndex.ToString(CultureInfo.InvariantCulture);
        return $"Update{CreatePascalIdentifier(groupsName)}Aggregates{suffix}";
    }

    private CapturedLocal[] CollectSingleKeyAggregateUpdateCaptures(
        ExecutionBlock block,
        ExecutionGetOrAddSingleKeyAggregateGroup getOrAddGroup,
        IReadOnlyDictionary<string, ExecutionVariable> scopedVariables)
    {
        var excludedNames = new HashSet<string>(StringComparer.Ordinal)
        {
            getOrAddGroup.RootGroup.Name,
            getOrAddGroup.Groups.Name,
            getOrAddGroup.GroupsToFinalize.Name,
            getOrAddGroup.Group.Name
        };
        AddProfileRecorderExcludedName(excludedNames);

        if (getOrAddGroup.NullGroup is not null)
            excludedNames.Add(getOrAddGroup.NullGroup.Name);

        foreach (var variableName in CollectDeclaredVariableNames(block))
            excludedNames.Add(variableName);

        var captures = new Dictionary<string, CapturedLocal>(StringComparer.Ordinal);
        AddHelperCaptures(block, excludedNames, captures);
        foreach (var fieldRead in ExecutionIrAnalysis.CollectExpressions<ExecutionFieldRead>(block))
        {
            if (!string.IsNullOrWhiteSpace(fieldRead.Alias) &&
                scopedVariables.TryGetValue(fieldRead.Alias, out var variable))
            {
                AddHelperCapture(variable, excludedNames, captures);
            }
        }

        return captures.Values.ToArray();
    }

    private MethodDeclarationSyntax CreateSingleKeyAggregateUpdateFunction(
        SingleKeyAggregateUpdateHelper helper,
        ExecutionRenderContext context)
    {
        var previousSuppressAggregateUpdateHelpers = context.Session.SuppressSingleKeyAggregateUpdateHelpers;
        context.Session.SuppressSingleKeyAggregateUpdateHelpers = true;

        try
        {
            return SyntaxFactory.MethodDeclaration(
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                    helper.FunctionName)
                .WithAttributeLists(SyntaxFactory.SingletonList(CreateAggressiveInliningAttribute()))
                .WithModifiers(CreatePrivateStaticModifiers())
            .WithParameterList(CreateSingleKeyAggregateUpdateParameterList(helper, context))
                .WithBody(StatementEmitter.CreateBlock(RenderIsolatedHelperBlock(
                    helper.Body,
                    context,
                    profileRecorderInScope: IsInstrumentationEnabled)));
        }
        finally
        {
            context.Session.SuppressSingleKeyAggregateUpdateHelpers = previousSuppressAggregateUpdateHelpers;
        }
    }

    private ParameterListSyntax CreateSingleKeyAggregateUpdateParameterList(
        SingleKeyAggregateUpdateHelper helper,
        ExecutionRenderContext context)
    {
        var parameters = new List<ParameterSyntax>();
        var getOrAddGroup = helper.GroupAcquisition;

        parameters.AddRange(CreateSingleKeyAggregateUpdateContextParameters(getOrAddGroup, context));
        AddProfileRecorderParameter(parameters);
        parameters.AddRange(helper.Captures.Select(CreateCapturedLocalParameter));

        return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters));
    }

    private IEnumerable<ParameterSyntax> CreateSingleKeyAggregateUpdateContextParameters(
        ExecutionGetOrAddSingleKeyAggregateGroup getOrAddGroup,
        ExecutionRenderContext context)
    {
        foreach (var rootLevel in getOrAddGroup.GroupPlan.Levels.Where(static level => level.IsRoot))
            yield return CreateParameter(getOrAddGroup.RootGroup.Name, CreateAggregateGroupType(rootLevel.Shape, context));

        var groupType = CreateAggregateGroupType(getOrAddGroup.GroupShape, context);
        yield return CreateParameter(getOrAddGroup.GroupsToFinalize.Name, CreateListTypeSyntax(groupType));
        yield return CreateParameter(
            getOrAddGroup.Groups.Name,
            CreateGroupDictionaryTypeSyntax(getOrAddGroup.KeyType, groupType));

        if (getOrAddGroup.NullGroup is not null)
        {
            yield return CreateParameter(getOrAddGroup.NullGroup.Name, groupType)
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.RefKeyword)));
        }
    }

    private StatementSyntax CreateSingleKeyAggregateUpdateInvocation(SingleKeyAggregateUpdateHelper helper)
    {
        var arguments = new List<ArgumentSyntax>();
        var getOrAddGroup = helper.GroupAcquisition;

        arguments.AddRange(CreateSingleKeyAggregateUpdateContextArguments(getOrAddGroup));
        AddProfileRecorderArgument(arguments);
        arguments.AddRange(helper.Captures
            .Select(capture => SyntaxFactory.Argument(CreateCapturedLocalArgument(capture))));

        return CreateHelperInvocationWithArguments(helper.FunctionName, arguments);
    }

    private static IEnumerable<ArgumentSyntax> CreateSingleKeyAggregateUpdateContextArguments(
        ExecutionGetOrAddSingleKeyAggregateGroup getOrAddGroup)
    {
        foreach (var _ in getOrAddGroup.GroupPlan.Levels.Where(static level => level.IsRoot))
            yield return SyntaxFactory.Argument(SyntaxFactory.IdentifierName(getOrAddGroup.RootGroup.Name));

        yield return SyntaxFactory.Argument(SyntaxFactory.IdentifierName(getOrAddGroup.GroupsToFinalize.Name));
        yield return SyntaxFactory.Argument(SyntaxFactory.IdentifierName(getOrAddGroup.Groups.Name));

        if (getOrAddGroup.NullGroup is not null)
        {
            yield return SyntaxFactory.Argument(SyntaxFactory.IdentifierName(getOrAddGroup.NullGroup.Name))
                .WithRefKindKeyword(SyntaxFactory.Token(SyntaxKind.RefKeyword));
        }
    }
}

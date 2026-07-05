using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Execution.Facts;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    internal sealed record EnumerableTraversalHelper(
        string FunctionName,
        ExecutionBlock Block,
        ExecutionEnumerableSource Source,
        ExecutionForEach Loop,
        IReadOnlyList<CapturedLocal> Captures,
        IReadOnlySet<string> RefCaptureNames,
        IReadOnlyDictionary<string, TypeSyntax> CaptureTypeOverrides);

    private IReadOnlyDictionary<ExecutionBlock, EnumerableTraversalHelper> CollectEnumerableTraversalHelpersByBlock(
        ExecutionBlock block,
        ExecutionRenderContext context)
    {
        var helpers = new Dictionary<ExecutionBlock, EnumerableTraversalHelper>();
        var functionNameCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        CollectEnumerableTraversalHelpersByBlock(
            block,
            helpers,
            functionNameCounts,
            new Dictionary<string, ExecutionVariable>(StringComparer.Ordinal),
            context);
        return helpers;
    }

    private void CollectEnumerableTraversalHelpersByBlock(
        ExecutionBlock block,
        Dictionary<ExecutionBlock, EnumerableTraversalHelper> helpers,
        Dictionary<string, int> functionNameCounts,
        IReadOnlyDictionary<string, ExecutionVariable> scopedVariables,
        ExecutionRenderContext context)
    {
        if (TryCreateEnumerableTraversalHelper(block, functionNameCounts, scopedVariables, context, out var helper))
            helpers.Add(block, helper);

        var currentScope = new Dictionary<string, ExecutionVariable>(scopedVariables, StringComparer.Ordinal);
        foreach (var node in block.Nodes)
        {
            var nodeScope = new Dictionary<string, ExecutionVariable>(currentScope, StringComparer.Ordinal);
            AddDeclaredVariables(node, nodeScope);

            foreach (var childBlock in ExecutionIrAnalysis.GetChildBlocks(node))
            {
                CollectEnumerableTraversalHelpersByBlock(
                    childBlock,
                    helpers,
                    functionNameCounts,
                    nodeScope,
                    context);
            }

            AddDeclaredVariables(node, currentScope);
        }
    }

    private bool TryCreateEnumerableTraversalHelper(
        ExecutionBlock block,
        Dictionary<string, int> functionNameCounts,
        IReadOnlyDictionary<string, ExecutionVariable> scopedVariables,
        ExecutionRenderContext context,
        out EnumerableTraversalHelper helper)
    {
        helper = null!;

        if (block.Nodes.Count != 2 ||
            block.Nodes[0] is not ExecutionEnumerableSource source ||
            block.Nodes[1] is not ExecutionForEach loop ||
            !IsLoopOverRows(loop.Source, source.Rows.Name) ||
            !ShouldExtractEnumerableTraversal(loop.Body))
        {
            return false;
        }

        var baseFunctionName = CreateEnumerableTraversalFunctionName(source.Rows.Name);
        functionNameCounts.TryGetValue(baseFunctionName, out var functionIndex);
        functionNameCounts[baseFunctionName] = functionIndex + 1;

        var excludedNames = CollectDeclaredVariableNames(block).ToHashSet(StringComparer.Ordinal);
        AddProfileRecorderExcludedName(excludedNames);
        var captureTypeOverrides = CollectEnumerableTraversalCaptureTypeOverrides(block, context);
        var refCaptureNames = CollectEnumerableTraversalRefCaptureNames(block, excludedNames);

        helper = new EnumerableTraversalHelper(
            CreateEnumerableTraversalFunctionName(source.Rows.Name, functionIndex),
            block,
            source,
            loop,
            CollectEnumerableTraversalCaptures(block, excludedNames, scopedVariables, captureTypeOverrides),
            refCaptureNames,
            captureTypeOverrides);
        return true;
    }

    private static bool ShouldExtractEnumerableTraversal(ExecutionBlock body)
    {
        return ExecutionIrAnalysis.CollectNodes<ExecutionEnumerableSource>(body).Any() ||
               ExecutionIrAnalysis.CollectNodes<ExecutionGetOrAddSingleKeyAggregateGroup>(body).Any() ||
               ExecutionIrAnalysis.CollectNodes<ExecutionGetOrAddValueTupleAggregateGroup>(body).Any();
    }

    private static bool IsLoopOverRows(ExecutionExpression source, string rowsName)
    {
        return source switch
        {
            ExecutionRowStream rowStream => string.Equals(rowStream.Variable.Name, rowsName, StringComparison.Ordinal),
            ExecutionVariableRead variableRead => string.Equals(variableRead.Variable.Name, rowsName, StringComparison.Ordinal),
            _ => false
        };
    }

    private static string CreateEnumerableTraversalFunctionName(string rowsName, int helperIndex = 0)
    {
        var suffix = helperIndex == 0
            ? string.Empty
            : helperIndex.ToString(CultureInfo.InvariantCulture);
        return $"Traverse{CreatePascalIdentifier(rowsName)}{suffix}";
    }

    private CapturedLocal[] CollectEnumerableTraversalCaptures(
        ExecutionBlock block,
        HashSet<string> excludedNames,
        IReadOnlyDictionary<string, ExecutionVariable> scopedVariables,
        IReadOnlyDictionary<string, TypeSyntax> captureTypeOverrides)
    {
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

        foreach (var node in ExecutionIrAnalysis.FlattenNodes(block))
        {
            if (node is ExecutionGetOrAddSingleKeyAggregateGroup or ExecutionGetOrAddValueTupleAggregateGroup)
                continue;

            foreach (var variable in ExecutionNodeFacts.GetDirectVariableReferences(node))
            {
                var capture = scopedVariables.TryGetValue(variable.Name, out var scopedVariable)
                    ? scopedVariable
                    : variable;
                AddHelperCapture(capture, excludedNames, captures);
            }
        }

        foreach (var name in captureTypeOverrides.Keys)
        {
            if (!excludedNames.Contains(name) && !captures.ContainsKey(name))
                captures.Add(name, new CapturedLocal(name, typeof(object)));
        }

        return captures.Values.ToArray();
    }

    private Dictionary<string, TypeSyntax> CollectEnumerableTraversalCaptureTypeOverrides(
        ExecutionBlock block,
        ExecutionRenderContext context)
    {
        var result = new Dictionary<string, TypeSyntax>(StringComparer.Ordinal);

        foreach (var appendRow in ExecutionIrAnalysis.CollectNodes<ExecutionAppendRow>(block))
        {
            var type = TryGetTypedRowBufferShape(appendRow.Table.Name, context, out var rowShape)
                ? CreateListTypeSyntax(rowShape.TypeName)
                : CreateTypeSyntax(typeof(Musoq.Evaluator.Tables.Table));
            result.TryAdd(appendRow.Table.Name, type);
        }

        foreach (var getOrAdd in ExecutionIrAnalysis.CollectNodes<ExecutionGetOrAddSingleKeyAggregateGroup>(block))
        {
            foreach (var rootLevel in getOrAdd.GroupPlan.Levels.Where(static level => level.IsRoot))
                result.TryAdd(getOrAdd.RootGroup.Name, CreateAggregateGroupType(rootLevel.Shape, context));

            var groupType = CreateAggregateGroupType(getOrAdd.GroupShape, context);
            result.TryAdd(getOrAdd.GroupsToFinalize.Name, CreateListTypeSyntax(groupType));
            result.TryAdd(getOrAdd.Groups.Name, CreateGroupDictionaryTypeSyntax(getOrAdd.KeyType, groupType));

            if (getOrAdd.NullGroup is not null)
                result.TryAdd(getOrAdd.NullGroup.Name, groupType);
        }

        foreach (var getOrAdd in ExecutionIrAnalysis.CollectNodes<ExecutionGetOrAddValueTupleAggregateGroup>(block))
        {
            foreach (var rootLevel in getOrAdd.GroupPlan.Levels.Where(static level => level.IsRoot))
                result.TryAdd(getOrAdd.RootGroup.Name, CreateAggregateGroupType(rootLevel.Shape, context));

            result.TryAdd(
                getOrAdd.GroupsToFinalize.Name,
                CreateListTypeSyntax(CreateAggregateGroupType(getOrAdd.GroupShape, context)));

            foreach (var dictionary in getOrAdd.GroupDictionaries)
            {
                var level = GetAggregateGroupLevel(getOrAdd.GroupPlan, dictionary.PrefixLength);
                result.TryAdd(
                    dictionary.Variable.Name,
                    CreateValueTupleGroupDictionaryTypeSyntax(
                        getOrAdd.KeyTypes,
                        dictionary.PrefixLength,
                        CreateAggregateGroupType(level.Shape, context)));
            }
        }

        return result;
    }

    private static HashSet<string> CollectEnumerableTraversalRefCaptureNames(
        ExecutionBlock block,
        HashSet<string> excludedNames)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);

        foreach (var getOrAdd in ExecutionIrAnalysis.CollectNodes<ExecutionGetOrAddSingleKeyAggregateGroup>(block))
        {
            if (getOrAdd.NullGroup is { } nullGroup && !excludedNames.Contains(nullGroup.Name))
                result.Add(nullGroup.Name);
        }

        foreach (var assignment in ExecutionIrAnalysis.CollectNodes<ExecutionAssign>(block))
        {
            if (!excludedNames.Contains(assignment.Variable.Name))
                result.Add(assignment.Variable.Name);
        }

        return result;
    }

    private MethodDeclarationSyntax CreateEnumerableTraversalFunction(
        EnumerableTraversalHelper helper,
        ExecutionRenderContext context)
    {
        context.Session.SuppressedEnumerableTraversalHelperBlocks.Add(helper.Block);
        try
        {
            return SyntaxFactory.MethodDeclaration(
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                    helper.FunctionName)
                .WithAttributeLists(SyntaxFactory.SingletonList(CreateAggressiveInliningAttribute()))
                .WithModifiers(CreatePrivateStaticModifiers())
                .WithParameterList(CreateEnumerableTraversalParameterList(helper))
                .WithBody(StatementEmitter.CreateBlock([
                    QueryEmitter.GenerateCancellationCheck(),
                    ..RenderIsolatedHelperBlock(
                        helper.Block,
                        context,
                        profileRecorderInScope: IsInstrumentationEnabled,
                        emitChunkLoopCancellationChecks: true)
                ]));
        }
        finally
        {
            context.Session.SuppressedEnumerableTraversalHelperBlocks.Remove(helper.Block);
        }
    }

    private ParameterListSyntax CreateEnumerableTraversalParameterList(EnumerableTraversalHelper helper)
    {
        var parameters = new List<ParameterSyntax>
        {
            CreateParameter("token", CreateTypeSyntax(typeof(CancellationToken)))
        };

        AddProfileRecorderParameter(parameters);
        parameters.AddRange(helper.Captures.Select(capture => CreateEnumerableTraversalCapturedParameter(
            capture,
            helper.RefCaptureNames,
            helper.CaptureTypeOverrides)));

        return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters));
    }

    private static ParameterSyntax CreateEnumerableTraversalCapturedParameter(
        CapturedLocal capture,
        IReadOnlySet<string> refCaptureNames,
        IReadOnlyDictionary<string, TypeSyntax> captureTypeOverrides)
    {
        var parameter = captureTypeOverrides.TryGetValue(capture.Name, out var typeOverride)
            ? CreateParameter(capture.Name, typeOverride)
            : CreateCapturedLocalParameter(capture);
        return refCaptureNames.Contains(capture.Name)
            ? parameter.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.RefKeyword)))
            : parameter;
    }

    private StatementSyntax CreateEnumerableTraversalInvocation(EnumerableTraversalHelper helper)
    {
        var arguments = new List<ArgumentSyntax>
        {
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("token"))
        };

        AddProfileRecorderArgument(arguments);
        arguments.AddRange(helper.Captures.Select(capture =>
        {
            var argument = SyntaxFactory.Argument(CreateCapturedLocalArgument(capture));
            return helper.RefCaptureNames.Contains(capture.Name)
                ? argument.WithRefKindKeyword(SyntaxFactory.Token(SyntaxKind.RefKeyword))
                : argument;
        }));

        return CreateHelperInvocationWithArguments(helper.FunctionName, arguments);
    }
}

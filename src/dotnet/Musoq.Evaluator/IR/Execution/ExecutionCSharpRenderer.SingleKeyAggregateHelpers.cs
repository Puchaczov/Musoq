using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private sealed record SingleKeyAggregateHelper(
        string PopulateFunctionName,
        string FinalizeFunctionName,
        ExecutionCreateSingleKeyAggregateContext Context,
        ExecutionSourceLoop AccumulationLoop,
        ExecutionEnsureTableCapacity EnsureCapacity,
        ExecutionForEach FinalizationLoop);

    private static SingleKeyAggregateHelper? CreateSingleKeyHashAggregateHelper(
        IReadOnlyList<ExecutionNode> nodes,
        int startIndex)
    {
        if (startIndex + 3 >= nodes.Count ||
            nodes[startIndex] is not ExecutionCreateSingleKeyAggregateContext context ||
            nodes[startIndex + 1] is not ExecutionSourceLoop accumulationLoop ||
            nodes[startIndex + 2] is not ExecutionEnsureTableCapacity ensureCapacity ||
            nodes[startIndex + 3] is not ExecutionForEach finalizationLoop)
        {
            return null;
        }

        return new SingleKeyAggregateHelper(
            CreateSingleKeyPopulateFunctionName(ensureCapacity.Table.Name),
            CreateSingleKeyFinalizeFunctionName(ensureCapacity.Table.Name),
            context,
            accumulationLoop,
            ensureCapacity,
            finalizationLoop);
    }

    private static ExpressionStatementSyntax CreateHelperInvocationWithArguments(
        string functionName,
        IReadOnlyList<ArgumentSyntax> arguments)
    {
        var invocation = SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(functionName))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));
        return SyntaxFactory.ExpressionStatement(
            CodegenHelperExtractionMetadata.AnnotateCallSite(invocation, functionName));
    }

    private static IEnumerable<SingleKeyAggregateHelper> CollectSingleKeyHashAggregateHelpers(ExecutionBlock block)
    {
        foreach (var (helper, _) in CollectSingleKeyHashAggregateHelpersWithIndexes(block))
            yield return helper;
    }

    private static IEnumerable<(SingleKeyAggregateHelper Helper, int Index)> CollectSingleKeyHashAggregateHelpersWithIndexes(
        ExecutionBlock block) =>
        CollectAggregateHelpersWithIndexes(
            block,
            CreateSingleKeyHashAggregateHelper,
            AssignSingleKeyAggregateHelperNames);

    private static SingleKeyAggregateHelper AssignSingleKeyAggregateHelperNames(
        SingleKeyAggregateHelper helper,
        int helperIndex) =>
        helper with
        {
            PopulateFunctionName = CreateSingleKeyPopulateFunctionName(helper.EnsureCapacity.Table.Name, helperIndex),
            FinalizeFunctionName = CreateSingleKeyFinalizeFunctionName(helper.EnsureCapacity.Table.Name, helperIndex)
        };

    private static string CreateSingleKeyPopulateFunctionName(string tableName, int helperIndex = 0)
    {
        var suffix = helperIndex == 0
            ? string.Empty
            : helperIndex.ToString(CultureInfo.InvariantCulture);
        return $"Populate{CreatePascalIdentifier(tableName)}SingleKeyGroups{suffix}";
    }

    private static string CreateSingleKeyFinalizeFunctionName(string tableName, int helperIndex = 0)
    {
        var suffix = helperIndex == 0
            ? string.Empty
            : helperIndex.ToString(CultureInfo.InvariantCulture);
        return $"Finalize{CreatePascalIdentifier(tableName)}SingleKeyGroups{suffix}";
    }

    private IEnumerable<MethodDeclarationSyntax> CreateSingleKeyAggregateFunctions(
        SingleKeyAggregateHelper helper,
        ExecutionRenderContext context)
    {
        yield return CreateSingleKeyPopulateFunction(helper, context);
        yield return CreateSingleKeyFinalizeFunction(helper, context);
    }

    private MethodDeclarationSyntax CreateSingleKeyPopulateFunction(
        SingleKeyAggregateHelper helper,
        ExecutionRenderContext context)
    {
        var rowsParameterName = CreateSingleKeyRowsParameterName(helper.AccumulationLoop);
        var helperLoop = ReplaceLoopSource(helper.AccumulationLoop, rowsParameterName);

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                helper.PopulateFunctionName)
            .WithAttributeLists(SyntaxFactory.SingletonList(CreateAggressiveInliningAttribute()))
            .WithModifiers(CreatePrivateStaticModifiers())
            .WithParameterList(CreateSingleKeyPopulateParameterList(helper, rowsParameterName, context))
            .WithBody(StatementEmitter.CreateBlock([
                QueryEmitter.GenerateCancellationCheck(),
                ..RenderIsolatedHelperBlock(
                    new ExecutionBlock([helperLoop]),
                    context,
                    profileRecorderInScope: IsInstrumentationEnabled,
                    emitChunkLoopCancellationChecks: true)
            ]));
    }

    private MethodDeclarationSyntax CreateSingleKeyFinalizeFunction(
        SingleKeyAggregateHelper helper,
        ExecutionRenderContext context)
    {
        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                helper.FinalizeFunctionName)
            .WithAttributeLists(SyntaxFactory.SingletonList(CreateAggressiveInliningAttribute()))
            .WithModifiers(CreatePrivateStaticModifiers())
            .WithParameterList(CreateSingleKeyFinalizeParameterList(helper, context))
            .WithBody(StatementEmitter.CreateBlock([
                QueryEmitter.GenerateCancellationCheck(),
                ..RenderIsolatedHelperBlock(new ExecutionBlock([
                    helper.EnsureCapacity,
                    helper.FinalizationLoop
                ]), context,
                    profileRecorderInScope: IsInstrumentationEnabled,
                    emitChunkLoopCancellationChecks: true)
            ]));
    }

    private ParameterListSyntax CreateSingleKeyPopulateParameterList(
        SingleKeyAggregateHelper helper,
        string rowsParameterName,
        ExecutionRenderContext context)
    {
        var parameters = new List<ParameterSyntax>
        {
            CreateParameter(
                rowsParameterName,
                CreateAggregateRowsParameterType(
                    helper.AccumulationLoop.Source,
                    CreateVariableTypeSyntax(helper.AccumulationLoop.Item)))
        };

        parameters.AddRange(CreateSingleKeyContextParameters(helper.Context, context));
        parameters.Add(CreateParameter("token", CreateTypeSyntax(typeof(CancellationToken))));
        AddProfileRecorderParameter(parameters);
        parameters.AddRange(CollectSingleKeyPopulateCaptures(helper)
            .Select(CreateCapturedLocalParameter));

        return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters));
    }

    private ParameterListSyntax CreateSingleKeyFinalizeParameterList(
        SingleKeyAggregateHelper helper,
        ExecutionRenderContext context)
    {
        var parameters = new List<ParameterSyntax>
        {
            CreateParameter(helper.EnsureCapacity.Table.Name, CreateAggregateOutputTargetType(helper.EnsureCapacity.Table, context)),
            CreateParameter(
                helper.Context.GroupsToFinalize.Name,
                CreateListTypeSyntax(CreateAggregateGroupType(helper.Context.GroupShape, context)))
        };

        AddProfileRecorderParameter(parameters);
        parameters.Insert(2, CreateParameter("token", CreateTypeSyntax(typeof(CancellationToken))));
        parameters.AddRange(CollectSingleKeyFinalizeCaptures(helper)
            .Select(CreateCapturedLocalParameter));

        return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters));
    }

    private TypeSyntax CreateAggregateOutputTargetType(
        ExecutionVariable table,
        ExecutionRenderContext context)
    {
        return TryGetTypedRowBufferShape(table.Name, context, out var rowShape)
            ? CreateListTypeSyntax(rowShape.TypeName)
            : CreateTypeSyntax(typeof(Table));
    }

    private IEnumerable<ParameterSyntax> CreateSingleKeyContextParameters(
        ExecutionCreateSingleKeyAggregateContext aggregateContext,
        ExecutionRenderContext context)
    {
        foreach (var rootLevel in aggregateContext.GroupPlan.Levels.Where(static level => level.IsRoot))
            yield return CreateParameter(aggregateContext.RootGroup.Name, CreateAggregateGroupType(rootLevel.Shape, context));

        var groupType = CreateAggregateGroupType(aggregateContext.GroupShape, context);
        yield return CreateParameter(aggregateContext.GroupsToFinalize.Name, CreateListTypeSyntax(groupType));
        yield return CreateParameter(aggregateContext.Groups.Name, CreateGroupDictionaryTypeSyntax(aggregateContext.KeyType, groupType));

        if (aggregateContext.NullGroup is not null)
        {
            yield return CreateParameter(aggregateContext.NullGroup.Name, groupType)
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.RefKeyword)));
        }
    }

    private List<ArgumentSyntax> CreateSingleKeyPopulateArguments(
        SingleKeyAggregateHelper helper,
        ExecutionRenderContext context)
    {
        var arguments = new List<ArgumentSyntax>
        {
            SyntaxFactory.Argument(RenderExpression(helper.AccumulationLoop.Source, context))
        };
        arguments.AddRange(CreateSingleKeyContextArguments(helper.Context));
        arguments.Add(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("token")));
        AddProfileRecorderArgument(arguments);
        arguments.AddRange(CollectSingleKeyPopulateCaptures(helper)
            .Select(capture => SyntaxFactory.Argument(CreateCapturedLocalArgument(capture))));
        return arguments;
    }

    private List<ExpressionSyntax> CreateSingleKeyFinalizeArguments(SingleKeyAggregateHelper helper)
    {
        var arguments = new List<ExpressionSyntax>
        {
            SyntaxFactory.IdentifierName(helper.EnsureCapacity.Table.Name),
            SyntaxFactory.IdentifierName(helper.Context.GroupsToFinalize.Name),
            SyntaxFactory.IdentifierName("token")
        };
        AddProfileRecorderArgument(arguments);
        arguments.AddRange(CollectSingleKeyFinalizeCaptures(helper)
            .Select(CreateCapturedLocalArgument));
        return arguments;
    }

    private static IEnumerable<ArgumentSyntax> CreateSingleKeyContextArguments(
        ExecutionCreateSingleKeyAggregateContext context)
    {
        foreach (var _ in context.GroupPlan.Levels.Where(static level => level.IsRoot))
            yield return SyntaxFactory.Argument(SyntaxFactory.IdentifierName(context.RootGroup.Name));

        yield return SyntaxFactory.Argument(SyntaxFactory.IdentifierName(context.GroupsToFinalize.Name));
        yield return SyntaxFactory.Argument(SyntaxFactory.IdentifierName(context.Groups.Name));

        if (context.NullGroup is not null)
        {
            yield return SyntaxFactory.Argument(SyntaxFactory.IdentifierName(context.NullGroup.Name))
                .WithRefKindKeyword(SyntaxFactory.Token(SyntaxKind.RefKeyword));
        }
    }

    private CapturedLocal[] CollectSingleKeyPopulateCaptures(SingleKeyAggregateHelper helper)
    {
        var excludedNames = new HashSet<string>(StringComparer.Ordinal)
        {
            helper.AccumulationLoop.Item.Name,
            helper.Context.RootGroup.Name,
            helper.Context.Groups.Name,
            helper.Context.GroupsToFinalize.Name
        };
        AddProfileRecorderExcludedName(excludedNames);

        if (helper.Context.NullGroup is not null)
            excludedNames.Add(helper.Context.NullGroup.Name);

        foreach (var variableName in CollectDeclaredVariableNames(helper.AccumulationLoop.Body))
            excludedNames.Add(variableName);

        var captures = new Dictionary<string, CapturedLocal>(StringComparer.Ordinal);
        AddHelperCaptures(helper.AccumulationLoop.Body, excludedNames, captures);
        return captures.Values.ToArray();
    }

    private CapturedLocal[] CollectSingleKeyFinalizeCaptures(SingleKeyAggregateHelper helper)
    {
        var excludedNames = new HashSet<string>(StringComparer.Ordinal)
        {
            helper.FinalizationLoop.Item.Name,
            helper.EnsureCapacity.Table.Name,
            helper.Context.GroupsToFinalize.Name
        };
        AddProfileRecorderExcludedName(excludedNames);

        foreach (var variableName in CollectDeclaredVariableNames(helper.FinalizationLoop.Body))
            excludedNames.Add(variableName);

        var captures = new Dictionary<string, CapturedLocal>(StringComparer.Ordinal);
        AddHelperCaptures(helper.FinalizationLoop.Body, excludedNames, captures);
        return captures.Values.ToArray();
    }

    private static string CreateSingleKeyRowsParameterName(ExecutionSourceLoop loop)
    {
        return loop.Source is ExecutionVariableRead variableRead
            ? variableRead.Variable.Name
            : "rows";
    }
}

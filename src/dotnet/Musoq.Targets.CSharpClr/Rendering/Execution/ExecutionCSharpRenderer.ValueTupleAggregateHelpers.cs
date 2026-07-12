using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static IEnumerable<ValueTupleAggregateHelper> CollectValueTupleAggregateHelpers(ExecutionBlock block)
    {
        foreach (var (helper, _) in CollectValueTupleAggregateHelpersWithIndexes(block))
            yield return helper;
    }

    private static IEnumerable<(ValueTupleAggregateHelper Helper, int Index)> CollectValueTupleAggregateHelpersWithIndexes(
        ExecutionBlock block) =>
        CollectAggregateHelpersWithIndexes(
            block,
            CreateValueTupleAggregateHelper,
            AssignValueTupleAggregateHelperNames);

    private static ValueTupleAggregateHelper AssignValueTupleAggregateHelperNames(
        ValueTupleAggregateHelper helper,
        int helperIndex) =>
        helper with
        {
            PopulateFunctionName = CreateValueTuplePopulateFunctionName(helper.EnsureCapacity.Table.Name, helperIndex),
            FinalizeFunctionName = CreateValueTupleFinalizeFunctionName(helper.EnsureCapacity.Table.Name, helperIndex)
        };

    private static string CreateValueTuplePopulateFunctionName(string tableName, int helperIndex = 0)
    {
        var suffix = helperIndex == 0
            ? string.Empty
            : helperIndex.ToString(CultureInfo.InvariantCulture);
        return $"Populate{CreatePascalIdentifier(tableName)}Groups{suffix}";
    }

    private static string CreateValueTupleFinalizeFunctionName(string tableName, int helperIndex = 0)
    {
        var suffix = helperIndex == 0
            ? string.Empty
            : helperIndex.ToString(CultureInfo.InvariantCulture);
        return $"Finalize{CreatePascalIdentifier(tableName)}Groups{suffix}";
    }

    private IEnumerable<MethodDeclarationSyntax> CreateValueTupleAggregateFunctions(
        ValueTupleAggregateHelper helper,
        ExecutionRenderContext context)
    {
        yield return CreateValueTuplePopulateFunction(helper, context);
        yield return CreateValueTupleFinalizeFunction(helper, context);
    }

    private MethodDeclarationSyntax CreateValueTuplePopulateFunction(
        ValueTupleAggregateHelper helper,
        ExecutionRenderContext context)
    {
        var rowsParameterName = CreateValueTupleRowsParameterName(helper.AccumulationLoop);
        var helperLoop = ReplaceLoopSource(helper.AccumulationLoop, rowsParameterName);

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                helper.PopulateFunctionName)
            .WithAttributeLists(SyntaxFactory.SingletonList(CreateAggressiveInliningAttribute()))
            .WithModifiers(CreatePrivateStaticModifiers())
            .WithParameterList(CreateValueTuplePopulateParameterList(helper, rowsParameterName, context))
            .WithBody(StatementEmitter.CreateBlock([
                QueryEmitter.GenerateCancellationCheck(),
                ..RenderIsolatedHelperBlock(
                    new ExecutionBlock([helperLoop]),
                    context,
                    profileRecorderInScope: IsInstrumentationEnabled,
                    emitChunkLoopCancellationChecks: true)
            ]));
    }

    private MethodDeclarationSyntax CreateValueTupleFinalizeFunction(
        ValueTupleAggregateHelper helper,
        ExecutionRenderContext context)
    {
        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                helper.FinalizeFunctionName)
            .WithAttributeLists(SyntaxFactory.SingletonList(CreateAggressiveInliningAttribute()))
            .WithModifiers(CreatePrivateStaticModifiers())
            .WithParameterList(CreateValueTupleFinalizeParameterList(helper, context))
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

    private ParameterListSyntax CreateValueTuplePopulateParameterList(
        ValueTupleAggregateHelper helper,
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

        parameters.AddRange(CreateValueTupleContextParameters(helper.Context, context));
        parameters.Add(CreateParameter("token", CreateTypeSyntax(typeof(CancellationToken))));
        AddProfileRecorderParameter(parameters);
        parameters.AddRange(CollectValueTuplePopulateCaptures(helper)
            .Select(CreateCapturedLocalParameter));

        return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters));
    }

    private ParameterListSyntax CreateValueTupleFinalizeParameterList(
        ValueTupleAggregateHelper helper,
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
        parameters.AddRange(CollectValueTupleFinalizeCaptures(helper)
            .Select(CreateCapturedLocalParameter));

        return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters));
    }

    private IEnumerable<ParameterSyntax> CreateValueTupleContextParameters(
        ExecutionCreateValueTupleAggregateContext aggregateContext,
        ExecutionRenderContext context)
    {
        foreach (var rootLevel in aggregateContext.GroupPlan.Levels.Where(static level => level.IsRoot))
            yield return CreateParameter(aggregateContext.RootGroup.Name, CreateAggregateGroupType(rootLevel.Shape, context));

        yield return CreateParameter(
            aggregateContext.GroupsToFinalize.Name,
            CreateListTypeSyntax(CreateAggregateGroupType(aggregateContext.GroupShape, context)));

        foreach (var dictionary in aggregateContext.GroupDictionaries)
        {
            var level = GetAggregateGroupLevel(aggregateContext.GroupPlan, dictionary.PrefixLength);
            yield return CreateParameter(
                dictionary.Variable.Name,
                CreateValueTupleGroupDictionaryTypeSyntax(
                    aggregateContext.KeyTypes,
                    dictionary.PrefixLength,
                    CreateAggregateGroupType(level.Shape, context)));
        }
    }

    private List<ExpressionSyntax> CreateValueTuplePopulateArguments(
        ValueTupleAggregateHelper helper,
        ExecutionRenderContext context)
    {
        var arguments = new List<ExpressionSyntax>
        {
            RenderExpression(helper.AccumulationLoop.Source, context)
        };
        arguments.AddRange(CreateValueTupleContextArguments(helper.Context));
        arguments.Add(SyntaxFactory.IdentifierName("token"));
        AddProfileRecorderArgument(arguments);
        arguments.AddRange(CollectValueTuplePopulateCaptures(helper)
            .Select(CreateCapturedLocalArgument));
        return arguments;
    }

    private List<ExpressionSyntax> CreateValueTupleFinalizeArguments(ValueTupleAggregateHelper helper)
    {
        var arguments = new List<ExpressionSyntax>
        {
            SyntaxFactory.IdentifierName(helper.EnsureCapacity.Table.Name),
            SyntaxFactory.IdentifierName(helper.Context.GroupsToFinalize.Name),
            SyntaxFactory.IdentifierName("token")
        };
        AddProfileRecorderArgument(arguments);
        arguments.AddRange(CollectValueTupleFinalizeCaptures(helper)
            .Select(CreateCapturedLocalArgument));
        return arguments;
    }

    private static IEnumerable<ExpressionSyntax> CreateValueTupleContextArguments(
        ExecutionCreateValueTupleAggregateContext context)
    {
        foreach (var _ in context.GroupPlan.Levels.Where(static level => level.IsRoot))
            yield return SyntaxFactory.IdentifierName(context.RootGroup.Name);

        yield return SyntaxFactory.IdentifierName(context.GroupsToFinalize.Name);

        foreach (var dictionary in context.GroupDictionaries)
            yield return SyntaxFactory.IdentifierName(dictionary.Variable.Name);
    }

    private CapturedLocal[] CollectValueTuplePopulateCaptures(
        ValueTupleAggregateHelper helper)
    {
        var excludedNames = new HashSet<string>(StringComparer.Ordinal)
        {
            helper.AccumulationLoop.Item.Name,
            helper.Context.RootGroup.Name,
            helper.Context.GroupsToFinalize.Name
        };
        AddProfileRecorderExcludedName(excludedNames);

        foreach (var dictionary in helper.Context.GroupDictionaries)
            excludedNames.Add(dictionary.Variable.Name);

        foreach (var variableName in CollectDeclaredVariableNames(helper.AccumulationLoop.Body))
            excludedNames.Add(variableName);

        var captures = new Dictionary<string, CapturedLocal>(StringComparer.Ordinal);
        AddHelperCaptures(helper.AccumulationLoop.Body, excludedNames, captures);
        return captures.Values.ToArray();
    }

    private CapturedLocal[] CollectValueTupleFinalizeCaptures(
        ValueTupleAggregateHelper helper)
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

    private static string CreateValueTupleRowsParameterName(ExecutionSourceLoop loop)
    {
        return loop.Source is ExecutionVariableRead variableRead
            ? variableRead.Variable.Name
            : "rows";
    }

}

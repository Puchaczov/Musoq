using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private MethodDeclarationSyntax CreateRankingWindowKeyExtractionFunction(RankingWindowKeyExtractionHelper helper)
    {
        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                helper.FunctionName)
            .WithAttributeLists(SyntaxFactory.SingletonList(CreateAggressiveInliningAttribute()))
            .WithModifiers(CreatePrivateStaticModifiers())
            .WithParameterList(CreateRankingWindowKeyExtractionParameterList(helper))
            .WithBody(StatementEmitter.CreateBlock(CreateRankingWindowKeyExtractionBody(helper)));
    }

    private static ExpressionStatementSyntax CreateRankingWindowKeyExtractionInvocation(RankingWindowKeyExtractionHelper helper)
    {
        return CreateHelperInvocation(helper.FunctionName, CreateRankingWindowKeyExtractionArguments(helper));
    }

    private IReadOnlyList<StatementSyntax> CreateRankingWindowKeyExtractionBody(RankingWindowKeyExtractionHelper helper)
    {
        const string indexVariableName = "windowIndex";
        var index = new ExecutionVariable(indexVariableName, typeof(int));
        var item = CreateWindowHelperItem(helper.Ranking.Item, helper.BufferItemGeneratedRowTypeName);
        var statements = new List<StatementSyntax>();

        statements.AddRange(CreateIndexedItemDeclarations(
            item,
            helper.Ranking.Buffer,
            index,
            helper.Ranking.RowAccessMode));

        if (helper.PartitionKeys is { ShouldExtract: true })
            statements.AddRange(CreateWindowPartitionKeyExtractionStatements(
                helper.PartitionKeys,
                helper.PartitionBuilder,
                index.Name,
                CreateRankingPartitionKeyExpression(helper.PartitionKeys, helper.Ranking.PartitionKey!)));

        if (helper.OrderKeys.ShouldExtract)
        {
            statements.Add(CreateWindowKeyArrayAssignment(
                helper.OrderKeys,
                index.Name,
                CreateRankingOrderKeyExpression(helper.OrderKeys, helper.Ranking.OrderKeys)));
        }

        return
        [
            CreateIndexedForLoop(
                index.Name,
                helper.Ranking.Buffer,
                StatementEmitter.CreateBlock(statements))
        ];
    }

    private static ParameterListSyntax CreateRankingWindowKeyExtractionParameterList(RankingWindowKeyExtractionHelper helper)
    {
        var parameters = new List<ParameterSyntax>
        {
            CreateParameter(
                helper.Ranking.Buffer.Name,
                CreateWindowRowsParameterType(
                    helper.Ranking.Buffer,
                    helper.Ranking.Item,
                    helper.BufferItemGeneratedRowTypeName))
        };

        if (ShouldMaterializeWindowKeyArray(helper.PartitionKeys))
            parameters.Add(CreateParameter(helper.PartitionKeys.Variable.Name, CreateVariableTypeSyntax(helper.PartitionKeys.Variable)));

        if (helper.PartitionBuilder != null)
            parameters.Add(CreateParameter(helper.PartitionBuilder.Name, CreateVariableTypeSyntax(helper.PartitionBuilder)));

        if (helper.OrderKeys.ShouldExtract)
            parameters.Add(CreateParameter(helper.OrderKeys.Variable.Name, CreateVariableTypeSyntax(helper.OrderKeys.Variable)));

        parameters.AddRange(helper.Captures.Select(CreateCapturedLocalParameter));
        return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters));
    }

    private static List<ExpressionSyntax> CreateRankingWindowKeyExtractionArguments(RankingWindowKeyExtractionHelper helper)
    {
        var arguments = new List<ExpressionSyntax>
        {
            SyntaxFactory.IdentifierName(helper.Ranking.Buffer.Name)
        };

        if (ShouldMaterializeWindowKeyArray(helper.PartitionKeys))
            arguments.Add(SyntaxFactory.IdentifierName(helper.PartitionKeys.Variable.Name));

        if (helper.PartitionBuilder != null)
            arguments.Add(SyntaxFactory.IdentifierName(helper.PartitionBuilder.Name));

        if (helper.OrderKeys.ShouldExtract)
            arguments.Add(SyntaxFactory.IdentifierName(helper.OrderKeys.Variable.Name));

        arguments.AddRange(helper.Captures.Select(CreateCapturedLocalArgument));
        return arguments;
    }

    private IEnumerable<RankingWindowKeyExtractionHelper> CollectRankingWindowKeyExtractionHelpers(ExecutionBlock block)
    {
        foreach (var helper in CollectRankingWindowKeyExtractionHelpers(block, new Dictionary<string, string>(StringComparer.Ordinal)))
            yield return helper;
    }

    private IEnumerable<RankingWindowKeyExtractionHelper> CollectRankingWindowKeyExtractionHelpers(
        ExecutionBlock block,
        Dictionary<string, string> materializedRowTypeNames)
    {
        foreach (var node in block.Nodes)
        {
            AddMaterializedRowTypeName(node, materializedRowTypeNames);

            if (node is ExecutionWindowKernelPlan plan &&
                plan.Kernels.All(static kernel => kernel is ExecutionComputeRankingWindow))
            {
                var rankings = plan.Kernels.Cast<ExecutionComputeRankingWindow>().ToArray();
                if (rankings.Length == 1 && CanUseFusedIntOrderRankingWindow(rankings[0]))
                    continue;

                if (rankings.Length > 0)
                {
                    var first = rankings[0];
                    var generatedRowTypeName = ResolveGeneratedRowTypeName(first.Buffer, first.Item, materializedRowTypeNames);
                    var helper = CreateRankingWindowKeyExtractionHelper(first, generatedRowTypeName);
                    if (helper.PartitionKeys is { ShouldExtract: true } || helper.OrderKeys.ShouldExtract)
                        yield return helper;
                }

                continue;
            }

            if (node is ExecutionComputeRankingWindow ranking)
            {
                if (CanUseFusedIntOrderRankingWindow(ranking))
                    continue;

                var generatedRowTypeName = ResolveGeneratedRowTypeName(ranking.Buffer, ranking.Item, materializedRowTypeNames);
                var helper = CreateRankingWindowKeyExtractionHelper(ranking, generatedRowTypeName);
                if (helper.PartitionKeys is { ShouldExtract: true } || helper.OrderKeys.ShouldExtract)
                    yield return helper;
            }

            foreach (var childBlock in GetChildBlocks(node))
            {
                foreach (var helper in CollectRankingWindowKeyExtractionHelpers(
                             childBlock,
                             new Dictionary<string, string>(materializedRowTypeNames, StringComparer.Ordinal)))
                {
                    yield return helper;
                }
            }
        }
    }

    private RankingWindowKeyExtractionHelper CreateRankingWindowKeyExtractionHelper(
        ExecutionComputeRankingWindow ranking,
        string? generatedRowTypeName)
    {
        var partitionKeys = ResolveRankingPartitionKeyArray(ranking);
        var orderKeys = ResolveRankingOrderKeyArray(ranking);

        return new RankingWindowKeyExtractionHelper(
            CreateRankingWindowKeyExtractionFunctionName(ranking),
            ranking,
            generatedRowTypeName,
            partitionKeys,
            CreateWindowPartitionBuilderVariable(ranking.Results, partitionKeys, ranking.Partitions),
            orderKeys,
            CollectRankingWindowKeyExtractionCaptures(ranking, partitionKeys, orderKeys));
    }

    private CapturedLocal[] CollectRankingWindowKeyExtractionCaptures(
        ExecutionComputeRankingWindow ranking,
        ExecutionWindowKeyArray? partitionKeys,
        ExecutionWindowKeyArray orderKeys)
    {
        var excludedNames = new HashSet<string>(StringComparer.Ordinal)
        {
            ranking.Buffer.Name,
            ranking.Item.Name,
            "windowIndex"
        };

        if (ShouldMaterializeWindowKeyArray(partitionKeys))
            excludedNames.Add(partitionKeys.Variable.Name);

        if (orderKeys.ShouldExtract)
            excludedNames.Add(orderKeys.Variable.Name);

        var captures = new Dictionary<string, CapturedLocal>(StringComparer.Ordinal);
        AddHelperCaptures(ranking.PartitionKey, excludedNames, captures);
        AddHelperCaptures(ranking.OrderKeys.Select(static key => key.Expression), excludedNames, captures);
        return captures.Values.ToArray();
    }

    private static string CreateRankingWindowKeyExtractionFunctionName(ExecutionComputeRankingWindow ranking)
    {
        return $"Extract{CreatePascalIdentifier(ranking.Results.Name)}WindowKeys";
    }
}

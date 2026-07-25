using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Targets.CSharpClr.Rendering.CodeGeneration;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private List<StatementSyntax> RenderComputeRankingWindow(ExecutionComputeRankingWindow ranking)
    {
        const string indexVariableName = "windowIndex";
        var index = new ExecutionVariable(indexVariableName, typeof(int));
        var partitionKeys = ResolveRankingPartitionKeyArray(ranking);
        var orderKeys = ResolveRankingOrderKeyArray(ranking);
        var extractionStatements = new List<StatementSyntax>();
        var statements = new List<StatementSyntax>();
        var extractionHelper = CreateRankingWindowKeyExtractionHelper(ranking, generatedRowTypeName: null);

        if (CanUseFusedIntOrderRankingWindow(ranking))
            return RenderFusedIntOrderRankingWindow(ranking, partitionKeys);

        if ((partitionKeys?.ShouldExtract ?? false) || orderKeys.ShouldExtract)
        {
            extractionStatements.AddRange(CreateIndexedItemDeclarations(
                ranking.Item,
                ranking.Buffer,
                index,
                ranking.RowAccessMode));

            if (partitionKeys is { ShouldExtract: true })
                extractionStatements.AddRange(CreateWindowPartitionKeyExtractionStatements(
                    partitionKeys,
                    extractionHelper.PartitionBuilder,
                    index.Name,
                    CreateRankingPartitionKeyExpression(partitionKeys, ranking.PartitionKey!)));

            if (orderKeys.ShouldExtract)
            {
                extractionStatements.Add(CreateWindowKeyArrayAssignment(
                    orderKeys,
                    index.Name,
                    CreateRankingOrderKeyExpression(orderKeys, ranking.OrderKeys)));
            }
        }

        if (ShouldMaterializeWindowKeyArray(partitionKeys))
        {
            statements.Add(CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                partitionKeys.Variable.Name,
                CreateWindowKeyArrayCreation(partitionKeys, CreateBufferCountExpression(ranking.Buffer))));
        }

        if (extractionHelper.PartitionBuilder != null)
            statements.Add(CreateWindowPartitionBuilderDeclaration(
                extractionHelper.PartitionBuilder,
                ranking.Buffer));

        if (orderKeys.ShouldExtract)
        {
            statements.Add(CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                orderKeys.Variable.Name,
                CreateWindowKeyArrayCreation(orderKeys, CreateBufferCountExpression(ranking.Buffer))));
        }

        if (extractionStatements.Count > 0)
        {
            statements.Add(CreateRankingWindowKeyExtractionInvocation(
                extractionHelper));
        }

        AddWindowPartitionDeclarations(
            statements,
            ranking.Partitions,
            partitionKeys?.Variable,
            ranking.Buffer,
            extractionHelper.PartitionBuilder);

        var rankingPartitions = ranking.Partitions;
        if (rankingPartitions == null)
        {
            rankingPartitions = new ExecutionWindowPartitionSet(
                new ExecutionVariable($"{ranking.Results.Name}Partitions", typeof(Musoq.Evaluator.Helpers.WindowPartitionSet)),
                true);
            statements.Add(CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                rankingPartitions.Variable.Name,
                CreateWindowHelperInvocation(
                    nameof(Musoq.Evaluator.Helpers.WindowFunctionHelpers.ResolvePartitionSet),
                    CreateBufferCountExpression(ranking.Buffer),
                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression))));
        }

        var rankingSortedPartitions = ranking.SortedPartitions ??
                                      new ExecutionWindowPartitionSet(rankingPartitions.Variable, true, true);
        AddWindowSortedPartitionDeclarations(
            statements,
            rankingSortedPartitions,
            rankingPartitions,
            orderKeys,
            ranking.OrderKeys);

        statements.AddRange(CreateRankingKernelStatements(ranking, orderKeys, rankingSortedPartitions));

        return statements;
    }

    private List<StatementSyntax> RenderFusedIntOrderRankingWindow(
        ExecutionComputeRankingWindow ranking,
        ExecutionWindowKeyArray? partitionKeys)
    {
        const string indexVariableName = "windowIndex";
        var index = new ExecutionVariable(indexVariableName, typeof(int));
        var builder = CreateFusedIntOrderBuilderVariable(ranking, partitionKeys);
        var extractionStatements = new List<StatementSyntax>();
        var statements = new List<StatementSyntax>
        {
            CreateFusedIntOrderBuilderDeclaration(builder, ranking.Buffer)
        };

        extractionStatements.AddRange(CreateIndexedItemDeclarations(
            ranking.Item,
            ranking.Buffer,
            index,
            ranking.RowAccessMode));
        extractionStatements.Add(CreateFusedIntOrderBuilderAddStatement(
            builder,
            partitionKeys,
            ranking,
            indexVariableName));

        statements.Add(CreateIndexedForLoop(
            indexVariableName,
            ranking.Buffer,
            StatementEmitter.CreateBlock(extractionStatements)));
        AddFusedIntOrderPartitionDeclarations(statements, ranking, builder);
        statements.Add(CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            ranking.Results.Name,
            CreateFusedIntRankingInvocation(ranking, builder, ranking.SortedPartitions ?? ranking.Partitions)));

        return statements;
    }
}

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<StatementSyntax> RenderWindowKernelPlan(
        ExecutionWindowKernelPlan plan,
        ExecutionRenderContext context)
    {
        if (plan.Kernels.All(static kernel => kernel is ExecutionComputeRankingWindow))
        {
            return RenderRankingWindowKernelPlan(
                plan.Kernels.Cast<ExecutionComputeRankingWindow>().ToArray());
        }

        return RenderBlock(new ExecutionBlock(plan.Kernels), context).Statements;
    }

    private List<StatementSyntax> RenderRankingWindowKernelPlan(
        IReadOnlyList<ExecutionComputeRankingWindow> rankings)
    {
        if (rankings.Count == 0)
            return [];

        if (rankings.Any(static ranking => ranking.LibraryGet is null))
            throw UnsupportedShape.Of("Ranking execution requires registered library operations.");
        const string indexVariableName = "windowIndex";
        var first = rankings[0];
        var partitionKeys = ResolveRankingPartitionKeyArray(first);
        var orderKeys = ResolveRankingOrderKeyArray(first);
        var extractionStatements = new List<StatementSyntax>();
        var statements = new List<StatementSyntax>();
        var extractionHelper = CreateRankingWindowKeyExtractionHelper(first, generatedRowTypeName: null);

        if (rankings.Count == 1 && CanUseFusedIntOrderRankingWindow(first))
            return RenderComputeRankingWindow(first);

        if ((partitionKeys?.ShouldExtract ?? false) || orderKeys.ShouldExtract)
        {
            extractionStatements.AddRange(CreateIndexedItemDeclarations(
                first.Item,
                first.Buffer,
                new ExecutionVariable(indexVariableName, typeof(int)),
                first.RowAccessMode));

            if (partitionKeys is { ShouldExtract: true })
                extractionStatements.AddRange(CreateWindowPartitionKeyExtractionStatements(
                    partitionKeys,
                    extractionHelper.PartitionBuilder,
                    indexVariableName,
                    CreateRankingPartitionKeyExpression(partitionKeys, first.PartitionKey!)));

            if (orderKeys.ShouldExtract)
            {
                extractionStatements.Add(CreateWindowKeyArrayAssignment(
                    orderKeys,
                    indexVariableName,
                    CreateRankingOrderKeyExpression(orderKeys, first.OrderKeys)));
            }
        }

        if (ShouldMaterializeWindowKeyArray(partitionKeys))
        {
            statements.Add(CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                partitionKeys.Variable.Name,
                CreateWindowKeyArrayCreation(partitionKeys, CreateBufferCountExpression(first.Buffer))));
        }

        if (extractionHelper.PartitionBuilder != null)
            statements.Add(CreateWindowPartitionBuilderDeclaration(extractionHelper.PartitionBuilder, first.Buffer));

        if (orderKeys.ShouldExtract)
        {
            statements.Add(CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                orderKeys.Variable.Name,
                CreateWindowKeyArrayCreation(orderKeys, CreateBufferCountExpression(first.Buffer))));
        }

        if (extractionStatements.Count > 0)
            statements.Add(CreateRankingWindowKeyExtractionInvocation(extractionHelper));

        AddWindowPartitionDeclarations(
            statements,
            first.Partitions,
            partitionKeys?.Variable,
            first.Buffer,
            extractionHelper.PartitionBuilder);

        var partitions = first.Partitions;
        if (partitions == null)
        {
            partitions = new ExecutionWindowPartitionSet(
                new ExecutionVariable(
                    $"{first.Results.Name}Partitions",
                    typeof(WindowPartitionSet)),
                true);
            statements.Add(CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                partitions.Variable.Name,
                CreateWindowPartitionSetInvocation(first.Buffer)));
        }

        var sortedPartitions = first.SortedPartitions ?? new ExecutionWindowPartitionSet(partitions.Variable, true, true);
        AddWindowSortedPartitionDeclarations(
            statements,
            sortedPartitions,
            partitions,
            orderKeys);

        foreach (var ranking in rankings)
        {
            statements.Add(CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                ranking.Results.Name,
                CreateSizedArrayCreation(GetArrayElementType(ranking.Results), CreateBufferCountExpression(ranking.Buffer))));
        }

        if (rankings.Any(static ranking => ranking.QualifyUpperBound is null or > 0))
        {
            statements.Add(rankings.Any(static ranking => ranking.LibraryGet is not null && ranking.RequiresPeerMetadata || ranking.Function is
                    ExecutionRankingWindowFunction.PercentRank or ExecutionRankingWindowFunction.CumeDist)
                ? WindowDistributionRankingSyntax.CreateKernelLoop(rankings, orderKeys, sortedPartitions)
                : rankings.All(static ranking => ranking.LibraryGet is not null && !ranking.RequiresPeerMetadata)
                    ? StatementEmitter.CreateBlock(rankings.Select(ranking => CreateRankingKernelLoop(ranking, orderKeys, sortedPartitions)))
                    : WindowDistributionRankingSyntax.CreateKernelLoop(rankings, orderKeys, sortedPartitions));
        }

        return statements;
    }

}

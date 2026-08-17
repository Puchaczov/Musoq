using System.Collections.Generic;
using System.Globalization;
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
                CreateWindowHelperInvocation(
                    nameof(WindowFunctionHelpers.ResolvePartitionSet),
                    CreateBufferCountExpression(first.Buffer),
                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression))));
        }

        var sortedPartitions = first.SortedPartitions ?? new ExecutionWindowPartitionSet(partitions.Variable, true, true);
        AddWindowSortedPartitionDeclarations(
            statements,
            sortedPartitions,
            partitions,
            orderKeys,
            first.OrderKeys);

        foreach (var ranking in rankings)
        {
            statements.Add(CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                ranking.Results.Name,
                CreateSizedArrayCreation(typeof(long), CreateBufferCountExpression(ranking.Buffer))));
        }

        if (rankings.Any(static ranking => ranking.QualifyUpperBound is null or > 0))
            statements.Add(CreateFusedRankingKernelLoop(rankings, orderKeys, sortedPartitions));

        return statements;
    }

    private static StatementSyntax CreateFusedRankingKernelLoop(
        IReadOnlyList<ExecutionComputeRankingWindow> rankings,
        ExecutionWindowKeyArray orderKeys,
        ExecutionWindowPartitionSet partitions)
    {
        var planName = $"{rankings[0].Results.Name}WindowPlan";
        var partitionsName = partitions.Variable.Name;
        var partitionSetIndex = $"{planName}PartitionSetIndex";
        var partitionStart = $"{planName}PartitionStart";
        var partitionCount = $"{planName}PartitionCount";
        var partitionIndices = $"{planName}PartitionIndices";
        var partitionIndex = $"{planName}PartitionIndex";
        var currentIndex = $"{planName}CurrentIndex";
        var previousIndex = $"{planName}PreviousIndex";
        var needsRank = rankings.Any(static ranking => ranking.Function == ExecutionRankingWindowFunction.Rank);
        var needsDenseRank = rankings.Any(static ranking => ranking.Function == ExecutionRankingWindowFunction.DenseRank);
        var rankName = $"{planName}Rank";
        var denseRankName = $"{planName}DenseRank";
        var stateDeclarations = CreateFusedRankingStateDeclarations(needsRank, needsDenseRank, rankName, denseRankName);
        var peerUpdate = CreateFusedRankingPeerUpdate(
            orderKeys,
            partitionStart,
            partitionIndices,
            partitionIndex,
            currentIndex,
            previousIndex,
            needsRank,
            needsDenseRank,
            rankName,
            denseRankName);
        var assignments = CreateFusedRankingAssignments(
            rankings,
            partitionIndex,
            currentIndex,
            rankName,
            denseRankName);

        var source =
            $"for (int {partitionSetIndex} = 0; {partitionSetIndex} < {partitionsName}.PartitionCount; ++{partitionSetIndex})" + Environment.NewLine +
            "{" + Environment.NewLine +
            $"    var {partitionStart} = {partitionsName}.GetStart({partitionSetIndex});" + Environment.NewLine +
            $"    var {partitionCount} = {partitionsName}.GetLength({partitionSetIndex});" + Environment.NewLine +
            $"    var {partitionIndices} = {partitionsName}.Indices;" + Environment.NewLine +
            stateDeclarations +
            $"    for (int {partitionIndex} = 0; {partitionIndex} < {partitionCount}; ++{partitionIndex})" + Environment.NewLine +
            "    {" + Environment.NewLine +
            $"        var {currentIndex} = {partitionIndices}[{partitionStart} + {partitionIndex}];" + Environment.NewLine +
            peerUpdate +
            assignments +
            "    }" + Environment.NewLine +
            "}";

        return SyntaxFactory.ParseStatement(source);
    }

    private static string CreateFusedRankingStateDeclarations(
        bool needsRank,
        bool needsDenseRank,
        string rankName,
        string denseRankName)
    {
        var declarations = new List<string>();
        if (needsRank)
            declarations.Add($"    long {rankName} = 1L;");

        if (needsDenseRank)
            declarations.Add($"    long {denseRankName} = 1L;");

        return declarations.Count == 0
            ? string.Empty
            : string.Join(Environment.NewLine, declarations) + Environment.NewLine;
    }

    private static string CreateFusedRankingPeerUpdate(
        ExecutionWindowKeyArray orderKeys,
        string partitionStart,
        string partitionIndices,
        string partitionIndex,
        string currentIndex,
        string previousIndex,
        bool needsRank,
        bool needsDenseRank,
        string rankName,
        string denseRankName)
    {
        if (!needsRank && !needsDenseRank)
            return string.Empty;

        var updates = new List<string>();
        if (needsRank)
            updates.Add($"                {rankName} = {partitionIndex} + 1L;");

        if (needsDenseRank)
            updates.Add($"                {denseRankName}++;");

        return $$"""
                if ({{partitionIndex}} > 0)
                {
                    var {{previousIndex}} = {{partitionIndices}}[{{partitionStart}} + {{partitionIndex}} - 1];
                    if (!{{CreateRankingPeerEqualityExpression(orderKeys, currentIndex, previousIndex)}})
                    {
{{string.Join(Environment.NewLine, updates)}}
                    }
                }
""";
    }

    private static string CreateFusedRankingAssignments(
        IReadOnlyList<ExecutionComputeRankingWindow> rankings,
        string partitionIndex,
        string currentIndex,
        string rankName,
        string denseRankName)
    {
        return string.Concat(rankings.Select(ranking => ranking.Function switch
        {
            ExecutionRankingWindowFunction.RowNumber => CreateFusedRowNumberAssignment(
                ranking,
                partitionIndex,
                currentIndex),
            ExecutionRankingWindowFunction.Rank => CreateFusedRankAssignment(
                ranking,
                currentIndex,
                rankName),
            ExecutionRankingWindowFunction.DenseRank => CreateFusedRankAssignment(
                ranking,
                currentIndex,
                denseRankName),
            _ => throw new ArgumentOutOfRangeException(nameof(rankings), ranking.Function, null)
        }));
    }

    private static string CreateFusedRowNumberAssignment(
        ExecutionComputeRankingWindow ranking,
        string partitionIndex,
        string currentIndex)
    {
        var assignment = $"        {ranking.Results.Name}[{currentIndex}] = {partitionIndex} + 1L;" + Environment.NewLine;
        if (!ranking.QualifyUpperBound.HasValue)
            return assignment;

        var upperBound = ranking.QualifyUpperBound.Value.ToString(CultureInfo.InvariantCulture);
        return
            $"        if ({partitionIndex} < {upperBound}L)" + Environment.NewLine +
            "        {" + Environment.NewLine +
            $"    {assignment}" +
            "        }" + Environment.NewLine;
    }

    private static string CreateFusedRankAssignment(
        ExecutionComputeRankingWindow ranking,
        string currentIndex,
        string valueName)
    {
        var assignment = $"        {ranking.Results.Name}[{currentIndex}] = {valueName};" + Environment.NewLine;
        if (!ranking.QualifyUpperBound.HasValue)
            return assignment;

        var upperBound = ranking.QualifyUpperBound.Value.ToString(CultureInfo.InvariantCulture);
        return
            $"        if ({valueName} <= {upperBound}L)" + Environment.NewLine +
            "        {" + Environment.NewLine +
            $"    {assignment}" +
            "        }" + Environment.NewLine;
    }
}

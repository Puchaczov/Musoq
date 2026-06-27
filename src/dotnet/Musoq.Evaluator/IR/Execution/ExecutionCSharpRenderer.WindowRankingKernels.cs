using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private static IReadOnlyList<StatementSyntax> CreateRankingKernelStatements(
        ExecutionComputeRankingWindow ranking,
        ExecutionWindowKeyArray orderKeys,
        ExecutionWindowPartitionSet? partitions)
    {
        if (partitions == null)
            throw new InvalidOperationException("Ranking kernels require a partition set.");

        var statements = new List<StatementSyntax>
        {
            CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                ranking.Results.Name,
                CreateSizedArrayCreation(typeof(long), CreateBufferCountExpression(ranking.Buffer)))
        };

        if (ranking.QualifyUpperBound is <= 0)
            return statements;

        statements.Add(CreateRankingKernelLoop(ranking, orderKeys, partitions));
        return statements;
    }

    private static StatementSyntax CreateRankingKernelLoop(
        ExecutionComputeRankingWindow ranking,
        ExecutionWindowKeyArray orderKeys,
        ExecutionWindowPartitionSet partitions)
    {
        var result = ranking.Results.Name;
        var partitionsName = partitions.Variable.Name;
        var partitionSetIndex = $"{result}PartitionSetIndex";
        var partitionStart = $"{result}PartitionStart";
        var partitionCount = $"{result}PartitionCount";
        var partitionIndices = $"{result}PartitionIndices";
        var partitionIndex = $"{result}PartitionIndex";
        var currentIndex = $"{result}CurrentIndex";
        var previousIndex = $"{result}PreviousIndex";

        var body = ranking.Function switch
        {
            ExecutionRankingWindowFunction.RowNumber => CreateRowNumberKernelBody(
                ranking,
                result,
                partitionStart,
                partitionCount,
                partitionIndices,
                partitionIndex,
                currentIndex),
            ExecutionRankingWindowFunction.Rank => CreateRankKernelBody(
                ranking,
                orderKeys,
                result,
                partitionStart,
                partitionCount,
                partitionIndices,
                partitionIndex,
                currentIndex,
                previousIndex),
            ExecutionRankingWindowFunction.DenseRank => CreateDenseRankKernelBody(
                ranking,
                orderKeys,
                result,
                partitionStart,
                partitionCount,
                partitionIndices,
                partitionIndex,
                currentIndex,
                previousIndex),
            _ => throw new ArgumentOutOfRangeException(nameof(ranking), ranking.Function, null)
        };

        var source =
            $"for (int {partitionSetIndex} = 0; {partitionSetIndex} < {partitionsName}.PartitionCount; ++{partitionSetIndex})" + Environment.NewLine +
            "{" + Environment.NewLine +
            $"    var {partitionStart} = {partitionsName}.GetStart({partitionSetIndex});" + Environment.NewLine +
            $"    var {partitionCount} = {partitionsName}.GetLength({partitionSetIndex});" + Environment.NewLine +
            $"    var {partitionIndices} = {partitionsName}.Indices;" + Environment.NewLine +
            body +
            "}";

        return SyntaxFactory.ParseStatement(source);
    }

    private static string CreateRowNumberKernelBody(
        ExecutionComputeRankingWindow ranking,
        string result,
        string partitionStart,
        string partitionCount,
        string partitionIndices,
        string partitionIndex,
        string currentIndex)
    {
        var limitDeclaration = ranking.QualifyUpperBound.HasValue
            ? $"                var {result}PartitionLimit = (int)System.Math.Min((long){partitionCount}, {ranking.QualifyUpperBound.Value.ToString(CultureInfo.InvariantCulture)}L);"
            : $"                var {result}PartitionLimit = {partitionCount};";

        return $$"""
{{limitDeclaration}}
                for (int {{partitionIndex}} = 0; {{partitionIndex}} < {{result}}PartitionLimit; ++{{partitionIndex}})
                {
                    var {{currentIndex}} = {{partitionIndices}}[{{partitionStart}} + {{partitionIndex}}];
                    {{result}}[{{currentIndex}}] = {{partitionIndex}} + 1L;
                }
""";
    }

    private static string CreateRankKernelBody(
        ExecutionComputeRankingWindow ranking,
        ExecutionWindowKeyArray orderKeys,
        string result,
        string partitionStart,
        string partitionCount,
        string partitionIndices,
        string partitionIndex,
        string currentIndex,
        string previousIndex)
    {
        var maxRankGuard = ranking.QualifyUpperBound.HasValue
            ? $$"""

                    if ({{result}}Rank > {{ranking.QualifyUpperBound.Value.ToString(CultureInfo.InvariantCulture)}}L)
                        break;
            """
            : string.Empty;

        return $$"""
                long {{result}}Rank = 1L;
                for (int {{partitionIndex}} = 0; {{partitionIndex}} < {{partitionCount}}; ++{{partitionIndex}})
                {
                    var {{currentIndex}} = {{partitionIndices}}[{{partitionStart}} + {{partitionIndex}}];
                    if ({{partitionIndex}} > 0)
                    {
                        var {{previousIndex}} = {{partitionIndices}}[{{partitionStart}} + {{partitionIndex}} - 1];
                        if (!{{CreateRankingPeerEqualityExpression(orderKeys, currentIndex, previousIndex)}})
                            {{result}}Rank = {{partitionIndex}} + 1L;
                    }{{maxRankGuard}}

                    {{result}}[{{currentIndex}}] = {{result}}Rank;
                }
""";
    }

    private static string CreateDenseRankKernelBody(
        ExecutionComputeRankingWindow ranking,
        ExecutionWindowKeyArray orderKeys,
        string result,
        string partitionStart,
        string partitionCount,
        string partitionIndices,
        string partitionIndex,
        string currentIndex,
        string previousIndex)
    {
        var maxRankGuard = ranking.QualifyUpperBound.HasValue
            ? $$"""

                    if ({{result}}DenseRank > {{ranking.QualifyUpperBound.Value.ToString(CultureInfo.InvariantCulture)}}L)
                        break;
            """
            : string.Empty;

        return $$"""
                long {{result}}DenseRank = 1L;
                for (int {{partitionIndex}} = 0; {{partitionIndex}} < {{partitionCount}}; ++{{partitionIndex}})
                {
                    var {{currentIndex}} = {{partitionIndices}}[{{partitionStart}} + {{partitionIndex}}];
                    if ({{partitionIndex}} > 0)
                    {
                        var {{previousIndex}} = {{partitionIndices}}[{{partitionStart}} + {{partitionIndex}} - 1];
                        if (!{{CreateRankingPeerEqualityExpression(orderKeys, currentIndex, previousIndex)}})
                            {{result}}DenseRank++;
                    }{{maxRankGuard}}

                    {{result}}[{{currentIndex}}] = {{result}}DenseRank;
                }
""";
    }

    private static string CreateRankingPeerEqualityExpression(
        ExecutionWindowKeyArray orderKeys,
        string currentIndex,
        string previousIndex)
    {
        if (HasGeneratedWindowKeyType(orderKeys))
            return $"{orderKeys.Variable.Name}[{currentIndex}].PeerEquals({orderKeys.Variable.Name}[{previousIndex}])";

        var elementTypeName = EvaluationHelper.GetCastableType(GetArrayElementType(orderKeys.Variable));

        return $"System.Collections.Generic.EqualityComparer<{elementTypeName}>.Default.Equals({orderKeys.Variable.Name}[{currentIndex}], {orderKeys.Variable.Name}[{previousIndex}])";
    }
}

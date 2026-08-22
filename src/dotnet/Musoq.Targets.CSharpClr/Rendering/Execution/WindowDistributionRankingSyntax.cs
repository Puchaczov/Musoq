using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Targets.CSharpClr;

internal static class WindowDistributionRankingSyntax
{
    internal static StatementSyntax CreateKernelLoop(
        IReadOnlyList<ExecutionComputeRankingWindow> rankings,
        ExecutionWindowKeyArray orderKeys,
        ExecutionWindowPartitionSet partitions)
    {
        var result = rankings[0].Results.Name;
        var partitionSetIndex = $"{result}WindowPlanPartitionSetIndex";
        var partitionStart = $"{result}WindowPlanPartitionStart";
        var partitionCount = $"{result}WindowPlanPartitionCount";
        var partitionIndices = $"{result}WindowPlanPartitionIndices";
        var names = new DistributionRankingKernelNames(
            result,
            partitionStart,
            partitionCount,
            partitionIndices);
        var source =
            $"for (int {partitionSetIndex} = 0; {partitionSetIndex} < {partitions.Variable.Name}.PartitionCount; ++{partitionSetIndex})" + Environment.NewLine +
            "{" + Environment.NewLine +
            $"    var {partitionStart} = {partitions.Variable.Name}.GetStart({partitionSetIndex});" + Environment.NewLine +
            $"    var {partitionCount} = {partitions.Variable.Name}.GetLength({partitionSetIndex});" + Environment.NewLine +
            $"    var {partitionIndices} = {partitions.Variable.Name}.Indices;" + Environment.NewLine +
            CreateKernelBody(rankings, orderKeys, names) +
            "}";

        return SyntaxFactory.ParseStatement(source);
    }

    internal static string CreatePeerEqualityExpression(
        ExecutionWindowKeyArray orderKeys,
        string leftIndex,
        string rightIndex)
    {
        if (ExecutionCSharpRenderer.HasGeneratedWindowKeyType(orderKeys))
            return $"{orderKeys.Variable.Name}[{leftIndex}].PeerEquals({orderKeys.Variable.Name}[{rightIndex}])";

        var elementTypeName = EvaluationHelper.GetCastableType(
            ExecutionCSharpRenderer.GetArrayElementType(orderKeys.Variable));
        return $"System.Collections.Generic.EqualityComparer<{elementTypeName}>.Default.Equals({orderKeys.Variable.Name}[{leftIndex}], {orderKeys.Variable.Name}[{rightIndex}])";
    }

    internal static string CreateKernelBody(
        IReadOnlyList<ExecutionComputeRankingWindow> rankings,
        ExecutionWindowKeyArray orderKeys,
        DistributionRankingKernelNames names)
    {
        var needsDenseRank = rankings.Any(static ranking =>
            ranking.Function == ExecutionRankingWindowFunction.DenseRank);
        var state = needsDenseRank
            ? $"                long {names.DenseRank} = 1L;" + Environment.NewLine
            : string.Empty;
        var assignments = string.Concat(rankings.Select(ranking => CreateAssignment(ranking, names)));
        var denseRankUpdate = needsDenseRank
            ? $"                    {names.DenseRank}++;" + Environment.NewLine
            : string.Empty;

        return $$"""
{{state}}                for (int {{names.PeerStart}} = 0; {{names.PeerStart}} < {{names.PartitionCount}};)
                {
                    var {{names.CurrentIndex}} = {{names.PartitionIndices}}[{{names.PartitionStart}} + {{names.PeerStart}}];
                    var {{names.PeerEnd}} = {{names.PeerStart}};
                    while ({{names.PeerEnd}} + 1 < {{names.PartitionCount}})
                    {
                        var {{names.CandidateIndex}} = {{names.PartitionIndices}}[{{names.PartitionStart}} + {{names.PeerEnd}} + 1];
                        if (!{{CreatePeerEqualityExpression(orderKeys, names.CurrentIndex, names.CandidateIndex)}})
                            break;

                        {{names.PeerEnd}}++;
                    }

                    for (int {{names.PeerIndex}} = {{names.PeerStart}}; {{names.PeerIndex}} <= {{names.PeerEnd}}; ++{{names.PeerIndex}})
                    {
                        {{names.CurrentIndex}} = {{names.PartitionIndices}}[{{names.PartitionStart}} + {{names.PeerIndex}}];
{{assignments}}                    }

{{denseRankUpdate}}                    {{names.PeerStart}} = {{names.PeerEnd}} + 1;
                }
""";
    }

    private static string CreateAssignment(
        ExecutionComputeRankingWindow ranking,
        DistributionRankingKernelNames names)
    {
        var value = ranking.Function switch
        {
            ExecutionRankingWindowFunction.RowNumber => $"{names.PeerIndex} + 1L",
            ExecutionRankingWindowFunction.Rank => $"{names.PeerStart} + 1L",
            ExecutionRankingWindowFunction.DenseRank => names.DenseRank,
            ExecutionRankingWindowFunction.PercentRank =>
                $"{names.PartitionCount} == 1 ? 0d : (double){names.PeerStart} / ({names.PartitionCount} - 1)",
            ExecutionRankingWindowFunction.CumeDist =>
                $"(double)({names.PeerEnd} + 1) / {names.PartitionCount}",
            _ => throw new ArgumentOutOfRangeException(nameof(ranking), ranking.Function, null)
        };
        var assignment = $"                        {ranking.Results.Name}[{names.CurrentIndex}] = {value};" + Environment.NewLine;
        if (!ranking.QualifyUpperBound.HasValue)
            return assignment;

        var upperBound = ranking.QualifyUpperBound.Value.ToString(CultureInfo.InvariantCulture);
        var condition = ranking.Function switch
        {
            ExecutionRankingWindowFunction.RowNumber => $"{names.PeerIndex} < {upperBound}L",
            ExecutionRankingWindowFunction.Rank => $"{names.PeerStart} + 1L <= {upperBound}L",
            ExecutionRankingWindowFunction.DenseRank => $"{names.DenseRank} <= {upperBound}L",
            _ => throw new InvalidOperationException("Only ordinal rankings support QUALIFY upper bounds.")
        };
        return
            $"                        if ({condition})" + Environment.NewLine +
            "                        {" + Environment.NewLine +
            $"    {assignment}" +
            "                        }" + Environment.NewLine;
    }

    internal sealed record DistributionRankingKernelNames(
        string Result,
        string PartitionStart,
        string PartitionCount,
        string PartitionIndices)
    {
        internal string PeerStart => $"{Result}PeerStart";

        internal string PeerEnd => $"{Result}PeerEnd";

        internal string PeerIndex => $"{Result}PeerIndex";

        internal string CurrentIndex => $"{Result}CurrentIndex";

        internal string CandidateIndex => $"{Result}CandidateIndex";

        internal string DenseRank => $"{Result}DenseRank";
    }
}

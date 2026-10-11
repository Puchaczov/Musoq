using System.Collections.Generic;
using System.Globalization;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

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
                CreateSizedArrayCreation(GetArrayElementType(ranking.Results), CreateBufferCountExpression(ranking.Buffer)))
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

        if (ranking.LibraryGet is null)
            throw UnsupportedShape.Of("Ranking execution requires a registered library operation.");
        var body = ranking.RequiresPeerMetadata
            ? WindowDistributionRankingSyntax.CreateKernelBody([ranking], orderKeys,
                new WindowDistributionRankingSyntax.DistributionRankingKernelNames(
                    result, partitionStart, partitionCount, partitionIndices))
            : CreateRowNumberKernelBody(ranking, result, partitionStart, partitionCount,
                partitionIndices, partitionIndex, currentIndex);

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
        var operation = ranking.LibraryGet ?? throw UnsupportedShape.Of("Ranking requires a library operation.");
        var kernelType = Musoq.Evaluator.Helpers.EvaluationHelper.GetCastableType(operation.RequireClrMethod().DeclaringType!);
        var context = $"var {result}Context = new Musoq.Plugins.WindowRankingContext({partitionIndex}, 0, 0, 0L, {partitionCount}, token);";
        var assignment = $"{kernelType}.{operation.MethodName}(in {result}Context)";
        var limitDeclaration = ranking.QualifyUpperBound.HasValue
            ? $"                var {result}PartitionLimit = (int)System.Math.Min((long){partitionCount}, {ranking.QualifyUpperBound.Value.ToString(CultureInfo.InvariantCulture)}L);"
            : $"                var {result}PartitionLimit = {partitionCount};";

        return $$"""
{{limitDeclaration}}
                for (int {{partitionIndex}} = 0; {{partitionIndex}} < {{result}}PartitionLimit; ++{{partitionIndex}})
                {
                    var {{currentIndex}} = {{partitionIndices}}[{{partitionStart}} + {{partitionIndex}}];
                    {{context}}
                    {{result}}[{{currentIndex}}] = {{assignment}};
                }
""";
    }

}

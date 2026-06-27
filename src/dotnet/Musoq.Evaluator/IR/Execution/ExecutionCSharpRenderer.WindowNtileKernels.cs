using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private static IReadOnlyList<StatementSyntax> CreateNtileKernelStatements(
        ExecutionComputePluginWindow plugin,
        ExecutionVariable buckets,
        ExecutionWindowPartitionSet? partitions)
    {
        if (partitions == null)
            throw new InvalidOperationException("NTILE kernels require a sorted partition set.");

        return
        [
            CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                plugin.Results.Name,
                CreateSizedArrayCreation(typeof(long), CreateBufferCountExpression(plugin.Buffer))),
            CreateNtileKernelLoop(plugin, buckets, partitions)
        ];
    }

    private static StatementSyntax CreateNtileKernelLoop(
        ExecutionComputePluginWindow plugin,
        ExecutionVariable buckets,
        ExecutionWindowPartitionSet partitions)
    {
        var result = plugin.Results.Name;
        var partitionsName = partitions.Variable.Name;
        var partitionSetIndex = $"{result}PartitionSetIndex";
        var partitionStart = $"{result}PartitionStart";
        var partitionCount = $"{result}PartitionCount";
        var partitionIndices = $"{result}PartitionIndices";
        var partitionIndex = $"{result}PartitionIndex";
        var currentIndex = $"{result}CurrentIndex";
        var bucketsName = $"{result}BucketCount";
        var position = $"{result}Position";
        var rowsPerBucket = $"{result}RowsPerBucket";
        var extra = $"{result}ExtraRows";
        var largeGroupBoundary = $"{result}LargeGroupBoundary";

        var source =
            $"for (int {partitionSetIndex} = 0; {partitionSetIndex} < {partitionsName}.PartitionCount; ++{partitionSetIndex})" + Environment.NewLine +
            "{" + Environment.NewLine +
            $"    var {partitionStart} = {partitionsName}.GetStart({partitionSetIndex});" + Environment.NewLine +
            $"    var {partitionCount} = {partitionsName}.GetLength({partitionSetIndex});" + Environment.NewLine +
            $"    var {partitionIndices} = {partitionsName}.Indices;" + Environment.NewLine +
            $"    var {bucketsName} = 0;" + Environment.NewLine +
            $"    for (int {partitionIndex} = 0; {partitionIndex} < {partitionCount}; ++{partitionIndex})" + Environment.NewLine +
            "    {" + Environment.NewLine +
            $"        var {currentIndex} = {partitionIndices}[{partitionStart} + {partitionIndex}];" + Environment.NewLine +
            $"        if ({bucketsName} == 0)" + Environment.NewLine +
            $"            {bucketsName} = {buckets.Name}[{currentIndex}];" + Environment.NewLine +
            $"        var {position} = {partitionIndex} + 1;" + Environment.NewLine +
            $"        if ({bucketsName} <= 0)" + Environment.NewLine +
            "        {" + Environment.NewLine +
            $"            {result}[{currentIndex}] = 1L;" + Environment.NewLine +
            "            continue;" + Environment.NewLine +
            "        }" + Environment.NewLine +
            $"        var {rowsPerBucket} = {partitionCount} / {bucketsName};" + Environment.NewLine +
            $"        var {extra} = {partitionCount} % {bucketsName};" + Environment.NewLine +
            $"        var {largeGroupBoundary} = {extra} * ({rowsPerBucket} + 1);" + Environment.NewLine +
            $"        {result}[{currentIndex}] = {position} <= {largeGroupBoundary}" + Environment.NewLine +
            $"            ? (({position} - 1) / ({rowsPerBucket} + 1)) + 1L" + Environment.NewLine +
            $"            : (({position} - 1 - {largeGroupBoundary}) / {rowsPerBucket}) + {extra} + 1L;" + Environment.NewLine +
            "    }" + Environment.NewLine +
            "}";

        return SyntaxFactory.ParseStatement(source);
    }
}

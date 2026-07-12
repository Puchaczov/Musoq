using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private IReadOnlyList<StatementSyntax> CreateOffsetKernelStatements(
        ExecutionComputeOffsetWindow offset,
        ExecutionVariable values,
        ExecutionVariable? offsets,
        ExecutionVariable? defaultValues,
        ExecutionWindowPartitionSet? partitions)
    {
        if (partitions == null)
            throw new InvalidOperationException("Offset window kernels require a sorted partition set.");

        return
        [
            CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                offset.Results.Name,
                CreateSizedArrayCreation(CreateWindowResultElementType(offset.Results), CreateBufferCountExpression(offset.Buffer))),
            CreateOffsetKernelLoop(offset, values, offsets, defaultValues, partitions)
        ];
    }

    private StatementSyntax CreateOffsetKernelLoop(
        ExecutionComputeOffsetWindow offset,
        ExecutionVariable values,
        ExecutionVariable? offsets,
        ExecutionVariable? defaultValues,
        ExecutionWindowPartitionSet partitions)
    {
        var result = offset.Results.Name;
        var partitionsName = partitions.Variable.Name;
        var partitionSetIndex = $"{result}PartitionSetIndex";
        var partitionStart = $"{result}PartitionStart";
        var partitionCount = $"{result}PartitionCount";
        var partitionIndices = $"{result}PartitionIndices";
        var partitionIndex = $"{result}PartitionIndex";
        var currentIndex = $"{result}CurrentIndex";
        var sourcePartitionIndex = $"{result}SourcePartitionIndex";
        var offsetExpression = offsets == null
            ? RenderExpressionSource(offset.Offset)
            : $"{offsets.Name}[{currentIndex}]";
        var resultElementType = CreateWindowResultElementType(offset.Results);
        var valueExpression = CreateCastSource(
            resultElementType,
            $"{values.Name}[{partitionIndices}[{partitionStart} + {sourcePartitionIndex}]]");
        var defaultExpression = defaultValues == null
            ? CreateCastSource(resultElementType, RenderExpressionSource(offset.DefaultValue))
            : $"{defaultValues.Name}[{currentIndex}]";
        var sourceIndexExpression = offset.Function == ExecutionOffsetWindowFunction.Lag
            ? $"{partitionIndex} - {offsetExpression}"
            : $"{partitionIndex} + {offsetExpression}";
        var sourceIndexGuard = offset.Function == ExecutionOffsetWindowFunction.Lag
            ? $"{sourcePartitionIndex} >= 0"
            : $"{sourcePartitionIndex} < {partitionCount}";

        var source =
            $"for (int {partitionSetIndex} = 0; {partitionSetIndex} < {partitionsName}.PartitionCount; ++{partitionSetIndex})" + Environment.NewLine +
            "{" + Environment.NewLine +
            $"    var {partitionStart} = {partitionsName}.GetStart({partitionSetIndex});" + Environment.NewLine +
            $"    var {partitionCount} = {partitionsName}.GetLength({partitionSetIndex});" + Environment.NewLine +
            $"    var {partitionIndices} = {partitionsName}.Indices;" + Environment.NewLine +
            $"    for (int {partitionIndex} = 0; {partitionIndex} < {partitionCount}; ++{partitionIndex})" + Environment.NewLine +
            "    {" + Environment.NewLine +
            $"        var {currentIndex} = {partitionIndices}[{partitionStart} + {partitionIndex}];" + Environment.NewLine +
            $"        var {sourcePartitionIndex} = {sourceIndexExpression};" + Environment.NewLine +
            $"        {result}[{currentIndex}] = {sourceIndexGuard} ? {valueExpression} : {defaultExpression};" + Environment.NewLine +
            "    }" + Environment.NewLine +
            "}";

        return SyntaxFactory.ParseStatement(source);
    }

    private string RenderExpressionSource(ExecutionExpression expression)
    {
        return RenderExpression(expression).NormalizeWhitespace().ToFullString();
    }

    private static string CreateCastSource(Type type, string expression)
    {
        return $"({EvaluationHelper.GetCastableType(type)})({expression})";
    }
}

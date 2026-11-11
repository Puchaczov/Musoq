using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Targets.CSharpClr;

internal sealed record WindowPartitionPositionSyntax(string Indices, string Start, string Count);

internal static class LibraryWindowBatchSyntax
{
    internal static bool HasLibraryWindowBatch(ExecutionWindowAggregateKernel kernel) =>
        kernel.Descriptor.LibraryBatch is not null;

    internal static string LibraryWindowBatchValuesName(ExecutionWindowAggregateKernel kernel) =>
        $"{kernel.Results.Name}BatchValues";

    public static StatementSyntax CreatePartition(
        ExecutionWindowAggregateKernel kernel,
        WindowPartitionPositionSyntax partition,
        StatementSyntax serialFallback
    )
    {
        var indices = partition.Indices;
        var start = partition.Start;
        var count = partition.Count;
        var prefix = kernel.Results.Name;
        var position = $"{prefix}BatchPosition";
        var starts = $"{prefix}BatchStarts";
        var ends = $"{prefix}BatchEnds";
        var context = $"{prefix}BatchContext";
        var frame = kernel.Frame!;
        var frameStart = WindowRangeFrameSyntax
            .CreateAggregateFrameStartExpressionForKernel(kernel, frame.Start, indices, start, position, count)
            .NormalizeWhitespace()
            .ToFullString();
        var frameEnd = WindowRangeFrameSyntax
            .CreateAggregateFrameEndExpressionForKernel(kernel, frame.End, indices, start, position, count)
            .NormalizeWhitespace()
            .ToFullString();
        var operation = kernel.Descriptor.LibraryBatch!.RequireClrMethod();
        var contextType = EvaluationHelper.GetCastableType(
            operation.GetParameters()[0].ParameterType.GetElementType()!
        );
        var batchType = EvaluationHelper.GetCastableType(operation.DeclaringType!);
        var fallback = serialFallback.NormalizeWhitespace().ToFullString();
        return SyntaxFactory.ParseStatement(
            $"{{\n    var {starts} = new int[{count}];\n    var {ends} = new int[{count}];\n    for (int {position} = 0; {position} < {count}; ++{position})\n    {{\n        if (({position} & 1023) == 0)\n            token.ThrowIfCancellationRequested();\n        {starts}[{position}] = {frameStart};\n        {ends}[{position}] = {frameEnd};\n    }}\n    var {context} = new {contextType}({LibraryWindowBatchValuesName(kernel)},\n        new System.ReadOnlySpan<int>({indices}, {start}, {count}), {starts}, {ends}, {kernel.Results.Name}, token);\n    if (!{batchType}.{operation.Name}(in {context}))\n    {{\n        {fallback}\n    }}\n}}"
        );
    }
}

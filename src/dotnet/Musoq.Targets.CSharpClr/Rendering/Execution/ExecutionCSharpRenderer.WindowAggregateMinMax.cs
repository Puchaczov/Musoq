using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private StatementSyntax CreateWindowAggregateBoundedMinMaxLoop(
        ExecutionWindowAggregateKernel kernel,
        string partitionIndicesName,
        string partitionStartName,
        string partitionCountName)
    {
        var frame = kernel.Frame ??
                    throw new InvalidOperationException("Bounded window aggregate kernels require a frame.");
        var result = kernel.Results.Name;
        var partitionIndex = $"{result}PartitionIndex";
        var currentIndex = $"{result}CurrentIndex";
        var frameStart = $"{result}FrameStart";
        var frameEnd = $"{result}FrameEnd";
        var frameValueIndex = $"{result}FrameValueIndex";
        var dequeValues = CreateWindowAggregateDequeValuesName(kernel);
        var dequeIndices = CreateWindowAggregateDequeIndicesName(kernel);
        var dequeHead = CreateWindowAggregateDequeHeadName(kernel);
        var dequeTail = CreateWindowAggregateDequeTailName(kernel);
        var dequeFrameEnd = CreateWindowAggregateDequeFrameEndName(kernel);
        var value = CreateWindowAggregateValueName(kernel);
        var valueRead = $"{value}{(Nullable.GetUnderlyingType(kernel.Descriptor.InputType.RequireClrType()) != null ? ".Value" : string.Empty)}";
        var valuePresent = Nullable.GetUnderlyingType(kernel.Descriptor.InputType.RequireClrType()) != null
            ? $"{value}.HasValue"
            : "true";
        var comparison = kernel.Descriptor.Function == ExecutionWindowAggregateFunction.Min
            ? ">= 0"
            : "<= 0";
        var itemDeclarations = CreateWindowAggregateBoundedMinMaxItemDeclarations(
            kernel,
            frameValueIndex,
            12);
        var resultType = EvaluationHelper.GetCastableType(kernel.Descriptor.ResultType.RequireClrType());
        var source =
            $"for (int {partitionIndex} = 0; {partitionIndex} < {partitionCountName}; ++{partitionIndex})" + Environment.NewLine +
            "{" + Environment.NewLine +
            $"    var {currentIndex} = {partitionIndicesName}[{partitionStartName} + {partitionIndex}];" + Environment.NewLine +
            $"    var {frameStart} = {CreateWindowAggregateFrameStartExpressionForKernel(kernel, frame.Start, partitionIndicesName, partitionStartName, partitionIndex, partitionCountName).NormalizeWhitespace().ToFullString()};" + Environment.NewLine +
            $"    var {frameEnd} = {CreateWindowAggregateFrameEndExpressionForKernel(kernel, frame.End, partitionIndicesName, partitionStartName, partitionIndex, partitionCountName).NormalizeWhitespace().ToFullString()};" + Environment.NewLine +
            $"    while ({dequeFrameEnd} < {frameEnd})" + Environment.NewLine +
            "    {" + Environment.NewLine +
            $"        ++{dequeFrameEnd};" + Environment.NewLine +
            $"        var {frameValueIndex} = {partitionIndicesName}[{partitionStartName} + {dequeFrameEnd}];" + Environment.NewLine +
            itemDeclarations +
            $"        var {value} = {RenderExpressionSource(kernel.Value)};" + Environment.NewLine +
            $"        if ({valuePresent})" + Environment.NewLine +
            "        {" + Environment.NewLine +
            $"            while ({dequeTail} > {dequeHead} && {dequeValues}[{dequeTail} - 1].CompareTo({valueRead}) {comparison})" + Environment.NewLine +
            $"                --{dequeTail};" + Environment.NewLine +
            $"            {dequeValues}[{dequeTail}] = {valueRead};" + Environment.NewLine +
            $"            {dequeIndices}[{dequeTail}] = {dequeFrameEnd};" + Environment.NewLine +
            $"            ++{dequeTail};" + Environment.NewLine +
            "        }" + Environment.NewLine +
            "    }" + Environment.NewLine +
            $"    while ({dequeHead} < {dequeTail} && {dequeIndices}[{dequeHead}] < {frameStart})" + Environment.NewLine +
            $"        ++{dequeHead};" + Environment.NewLine +
            $"    {result}[{currentIndex}] = {dequeHead} < {dequeTail} ? ({resultType}){dequeValues}[{dequeHead}] : default({resultType});" + Environment.NewLine +
            "}";

        return SyntaxFactory.ParseStatement(source);
    }

    private string CreateWindowAggregateBoundedMinMaxItemDeclarations(
        ExecutionWindowAggregateKernel kernel,
        string frameValueIndex,
        int indentSpaces)
    {
        var index = new ExecutionVariable(frameValueIndex, typeof(int));
        var indent = new string(' ', indentSpaces);
        var statements = CreateIndexedItemDeclarations(
            kernel.Item,
            kernel.Buffer,
            index,
            kernel.RowAccessMode);
        return string.Concat(statements.Select(statement =>
            indent + statement.NormalizeWhitespace().ToFullString() + Environment.NewLine));
    }

    private static string CreateWindowAggregateDequeValuesName(ExecutionWindowAggregateKernel kernel)
    {
        return $"{kernel.Results.Name}DequeValues";
    }

    private static string CreateWindowAggregateDequeIndicesName(ExecutionWindowAggregateKernel kernel)
    {
        return $"{kernel.Results.Name}DequeIndices";
    }

    private static string CreateWindowAggregateDequeHeadName(ExecutionWindowAggregateKernel kernel)
    {
        return $"{kernel.Results.Name}DequeHead";
    }

    private static string CreateWindowAggregateDequeTailName(ExecutionWindowAggregateKernel kernel)
    {
        return $"{kernel.Results.Name}DequeTail";
    }

    private static string CreateWindowAggregateDequeFrameEndName(ExecutionWindowAggregateKernel kernel)
    {
        return $"{kernel.Results.Name}DequeFrameEnd";
    }
}

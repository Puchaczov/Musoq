using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Targets.CSharpClr;

internal sealed record LibraryWindowContextBuffers(ExecutionVariable Values,
        IReadOnlyList<ExecutionVariable> Arguments, ExecutionVariable? RangeKeys, ExecutionVariable? OrderKeyValues, Type ContextType, Type KernelType);

internal static class LibraryWindowContextSyntax
{
    public static StatementSyntax CreateLoop(ExecutionComputePluginWindow window,
        LibraryWindowContextBuffers buffers, ExecutionWindowPartitionSet partitions, string frame)
    {
        var prefix = window.Results.Name;
        var partition = $"{prefix}Partition";
        var start = $"{prefix}Start";
        var count = $"{prefix}Count";
        var indices = $"{prefix}Indices";
        var position = $"{prefix}Position";
        var current = $"{prefix}Current";
        var frameStart = $"{prefix}FrameStart";
        var frameEnd = $"{prefix}FrameEnd";
        var context = $"{prefix}Context";
        var contextType = EvaluationHelper.GetCastableType(buffers.ContextType);
        var kernelType = EvaluationHelper.GetCastableType(buffers.KernelType);
        var orderValues = buffers.OrderKeyValues is null ? string.Empty : $", {buffers.OrderKeyValues.Name}";
        var arguments = string.Concat(buffers.Arguments.Select(argument => $", {argument.Name}[{current}]"));
        return SyntaxFactory.ParseStatement($$"""
            for (int {{partition}} = 0; {{partition}} < {{partitions.Variable.Name}}.PartitionCount; ++{{partition}})
            {
                var {{start}} = {{partitions.Variable.Name}}.GetStart({{partition}});
                var {{count}} = {{partitions.Variable.Name}}.GetLength({{partition}});
                var {{indices}} = {{partitions.Variable.Name}}.Indices;
                for (int {{position}} = 0; {{position}} < {{count}}; ++{{position}})
                {
                    if (({{position}} & 1023) == 0)
                        token.ThrowIfCancellationRequested();
                    var {{current}} = {{indices}}[{{start}} + {{position}}];
                    {{frame}}
                    var {{context}} = new {{contextType}}({{buffers.Values.Name}}{{orderValues}},
                        new System.ReadOnlySpan<int>({{indices}}, {{start}}, {{count}}), {{position}}, {{frameStart}}, {{frameEnd}}, token);
                    {{window.Results.Name}}[{{current}}] = {{kernelType}}.{{window.LibraryGet!.MethodName}}(in {{context}}{{arguments}});
                }
            }
            """);
    }
}

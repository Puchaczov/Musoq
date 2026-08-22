using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private enum BuiltInDirectPluginWindowFunction
    {
        FirstValue,
        LastValue,
        NthValue,
        Ntile
    }

    private static bool IsBuiltInDirectPluginWindow(ExecutionComputePluginWindow plugin)
    {
        return ResolveBuiltInDirectPluginWindowFunction(plugin.FunctionName) != null;
    }

    private static BuiltInDirectPluginWindowFunction? ResolveBuiltInDirectPluginWindowFunction(string functionName)
    {
        var normalized = functionName.Replace("_", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
        return normalized switch
        {
            "FIRSTVALUE" => BuiltInDirectPluginWindowFunction.FirstValue,
            "LASTVALUE" => BuiltInDirectPluginWindowFunction.LastValue,
            "NTHVALUE" => BuiltInDirectPluginWindowFunction.NthValue,
            "NTILE" => BuiltInDirectPluginWindowFunction.Ntile,
            _ => null
        };
    }

    private List<StatementSyntax> RenderBuiltInDirectPluginWindow(ExecutionComputePluginWindow plugin)
    {
        var function = ResolveBuiltInDirectPluginWindowFunction(plugin.FunctionName) ??
                       throw new InvalidOperationException($"Window function {plugin.FunctionName} is not a direct built-in window.");
        const string indexVariableName = "windowIndex";
        var index = new ExecutionVariable(indexVariableName, typeof(int));
        var partitionKeys = ResolvePluginPartitionKeyArray(plugin);
        var orderKeys = ResolvePluginOrderKeyArray(plugin);
        var requiresRangeKeys = plugin.Frame is { Kind: ExecutionWindowFrameKind.Range } frame &&
                                WindowRangeFrameSyntax.HasRangeOffsetBound(frame);
        var rangeKeys = requiresRangeKeys
            ? new ExecutionVariable(
                $"{plugin.Results.Name}RangeKeys",
                plugin.OrderKeys[0].Expression.ReturnType.RequireClrType().MakeArrayType())
            : null;
        var valueElementType = function == BuiltInDirectPluginWindowFunction.Ntile
            ? typeof(int)
            : plugin.Value.ReturnType.RequireClrType();
        var values = new ExecutionVariable(
            function == BuiltInDirectPluginWindowFunction.Ntile
                ? $"{plugin.Results.Name}Buckets"
                : $"{plugin.Results.Name}Values",
            valueElementType.MakeArrayType());
        var nthArguments = function == BuiltInDirectPluginWindowFunction.NthValue &&
                           plugin.RowScopedArguments.Count > 0 &&
                           plugin.RowScopedArguments[0]
            ? new ExecutionVariable($"{plugin.Results.Name}Arguments", typeof(int[]))
            : null;
        var extractionStatements = new List<StatementSyntax>();
        var statements = new List<StatementSyntax>();

        extractionStatements.AddRange(CreateIndexedItemDeclarations(
            plugin.Item,
            plugin.Buffer,
            index,
            plugin.RowAccessMode));

        if (partitionKeys is { ShouldExtract: true })
        {
            extractionStatements.Add(CreateWindowKeyArrayAssignment(
                partitionKeys,
                index.Name,
                CreateWindowPartitionKeyExpression(partitionKeys, plugin.PartitionKey!)));
        }

        if (orderKeys is { ShouldExtract: true })
        {
            extractionStatements.Add(CreateWindowKeyArrayAssignment(
                orderKeys,
                index.Name,
                CreateWindowOrderKeyExpression(orderKeys, plugin.OrderKeys)));
        }

        if (rangeKeys != null)
        {
            extractionStatements.Add(CreateWindowKeyArrayAssignment(
                rangeKeys,
                index.Name,
                RenderExpression(plugin.OrderKeys[0].Expression)));
        }

        extractionStatements.Add(function == BuiltInDirectPluginWindowFunction.Ntile
            ? CreateIntArrayAssignment(values.Name, index.Name, RenderExpression(plugin.Value))
            : CreateArrayAssignment(values.Name, index.Name, RenderExpression(plugin.Value), valueElementType));

        if (nthArguments != null)
            extractionStatements.Add(CreateIntArrayAssignment(nthArguments.Name, index.Name, RenderExpression(plugin.Arguments[0])));

        if (ShouldMaterializeWindowKeyArray(partitionKeys))
        {
            statements.Add(CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                partitionKeys.Variable.Name,
                CreateWindowKeyArrayCreation(partitionKeys, CreateBufferCountExpression(plugin.Buffer))));
        }

        if (orderKeys is { ShouldExtract: true })
        {
            statements.Add(CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                orderKeys.Variable.Name,
                CreateWindowKeyArrayCreation(orderKeys, CreateBufferCountExpression(plugin.Buffer))));
        }

        if (rangeKeys != null)
        {
            statements.Add(CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                rangeKeys.Name,
                CreateSizedArrayCreation(
                    plugin.OrderKeys[0].Expression.ReturnType,
                    CreateBufferCountExpression(plugin.Buffer))));
        }

        statements.Add(CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            values.Name,
            CreateSizedArrayCreation(valueElementType, CreateBufferCountExpression(plugin.Buffer))));

        if (nthArguments != null)
        {
            statements.Add(CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                nthArguments.Name,
                CreateSizedArrayCreation("int", CreateBufferCountExpression(plugin.Buffer))));
        }

        statements.Add(CreateIndexedForLoop(
            index.Name,
            plugin.Buffer,
            StatementEmitter.CreateBlock(extractionStatements)));

        AddWindowPartitionDeclarations(statements, plugin.Partitions, partitionKeys?.Variable, plugin.Buffer);
        if (orderKeys != null)
        {
            AddWindowSortedPartitionDeclarations(
                statements,
                plugin.SortedPartitions,
                plugin.Partitions,
                orderKeys,
                plugin.OrderKeys);
        }

        var partitions = orderKeys == null
            ? plugin.Partitions
            : plugin.SortedPartitions ?? plugin.Partitions;

        statements.AddRange(function == BuiltInDirectPluginWindowFunction.Ntile
            ? CreateNtileKernelStatements(plugin, values, partitions)
            : CreateValueAccessKernelStatements(
                plugin,
                function,
                values,
                nthArguments,
                partitions,
                orderKeys,
                rangeKeys));

        return statements;
    }

    private IReadOnlyList<StatementSyntax> CreateValueAccessKernelStatements(
        ExecutionComputePluginWindow plugin,
        BuiltInDirectPluginWindowFunction function,
        ExecutionVariable values,
        ExecutionVariable? nthArguments,
        ExecutionWindowPartitionSet? partitions,
        ExecutionWindowKeyArray? orderKeys,
        ExecutionVariable? rangeKeys)
    {
        if (partitions == null)
            throw new InvalidOperationException("Value window kernels require a partition set.");

        return
        [
            CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                plugin.Results.Name,
                CreateSizedArrayCreation(CreateWindowResultElementType(plugin.Results), CreateBufferCountExpression(plugin.Buffer))),
            CreateValueAccessKernelLoop(
                plugin,
                function,
                values,
                nthArguments,
                partitions,
                orderKeys,
                rangeKeys)
        ];
    }

    private StatementSyntax CreateValueAccessKernelLoop(
        ExecutionComputePluginWindow plugin,
        BuiltInDirectPluginWindowFunction function,
        ExecutionVariable values,
        ExecutionVariable? nthArguments,
        ExecutionWindowPartitionSet partitions,
        ExecutionWindowKeyArray? orderKeys,
        ExecutionVariable? rangeKeys)
    {
        var result = plugin.Results.Name;
        var partitionsName = partitions.Variable.Name;
        var partitionSetIndex = $"{result}PartitionSetIndex";
        var partitionStart = $"{result}PartitionStart";
        var partitionCount = $"{result}PartitionCount";
        var partitionIndices = $"{result}PartitionIndices";
        var partitionIndex = $"{result}PartitionIndex";
        var currentIndex = $"{result}CurrentIndex";
        var frameStart = $"{result}FrameStart";
        var frameEnd = $"{result}FrameEnd";
        var sourcePartitionIndex = $"{result}SourcePartitionIndex";
        var nth = $"{result}Nth";
        var frameDeclarations = CreateValueAccessFrameDeclarations(
            plugin,
            orderKeys,
            rangeKeys,
            partitionIndices,
            partitionStart,
            partitionIndex,
            partitionCount,
            frameStart,
            frameEnd);
        var assignment = CreateValueAccessAssignment(
            plugin,
            function,
            values,
            nthArguments,
            partitionStart,
            partitionCount,
            partitionIndices,
            partitionIndex,
            currentIndex,
            frameStart,
            frameEnd,
            sourcePartitionIndex,
            nth);

        var source =
            $"for (int {partitionSetIndex} = 0; {partitionSetIndex} < {partitionsName}.PartitionCount; ++{partitionSetIndex})" + Environment.NewLine +
            "{" + Environment.NewLine +
            $"    var {partitionStart} = {partitionsName}.GetStart({partitionSetIndex});" + Environment.NewLine +
            $"    var {partitionCount} = {partitionsName}.GetLength({partitionSetIndex});" + Environment.NewLine +
            $"    var {partitionIndices} = {partitionsName}.Indices;" + Environment.NewLine +
            $"    for (int {partitionIndex} = 0; {partitionIndex} < {partitionCount}; ++{partitionIndex})" + Environment.NewLine +
            "    {" + Environment.NewLine +
            $"        var {currentIndex} = {partitionIndices}[{partitionStart} + {partitionIndex}];" + Environment.NewLine +
            frameDeclarations +
            assignment +
            "    }" + Environment.NewLine +
            "}";

        return SyntaxFactory.ParseStatement(source);
    }

    private static string CreateValueAccessFrameDeclarations(
        ExecutionComputePluginWindow plugin,
        ExecutionWindowKeyArray? orderKeys,
        ExecutionVariable? rangeKeys,
        string partitionIndices,
        string partitionStart,
        string partitionIndex,
        string partitionCount,
        string frameStart,
        string frameEnd)
    {
        if (plugin.Frame == null)
        {
            var end = plugin.OrderKeys.Count == 0
                ? $"{partitionCount} - 1"
                : partitionIndex;

            return
                $"        var {frameStart} = 0;" + Environment.NewLine +
                $"        var {frameEnd} = {end};" + Environment.NewLine;
        }

        var start = WindowRangeFrameSyntax.CreatePluginWindowFrameExpression(
            plugin,
            plugin.Frame.Start,
            true,
            orderKeys,
            rangeKeys,
            partitionIndices,
            partitionStart,
            partitionIndex,
            partitionCount);
        var endExpression = WindowRangeFrameSyntax.CreatePluginWindowFrameExpression(
            plugin,
            plugin.Frame.End,
            false,
            orderKeys,
            rangeKeys,
            partitionIndices,
            partitionStart,
            partitionIndex,
            partitionCount);

        return
            $"        var {frameStart} = {start.NormalizeWhitespace().ToFullString()};" + Environment.NewLine +
            $"        var {frameEnd} = {endExpression.NormalizeWhitespace().ToFullString()};" + Environment.NewLine;
    }

    private string CreateValueAccessAssignment(
        ExecutionComputePluginWindow plugin,
        BuiltInDirectPluginWindowFunction function,
        ExecutionVariable values,
        ExecutionVariable? nthArguments,
        string partitionStart,
        string partitionCount,
        string partitionIndices,
        string partitionIndex,
        string currentIndex,
        string frameStart,
        string frameEnd,
        string sourcePartitionIndex,
        string nth)
    {
        var result = plugin.Results.Name;
        var resultType = CreateWindowResultElementType(plugin.Results);
        var defaultValue = $"default({EvaluationHelper.GetCastableType(resultType)})";
        var sourceValue = CreateCastSource(
            resultType,
            $"{values.Name}[{partitionIndices}[{partitionStart} + {sourcePartitionIndex}]]");

        return function switch
        {
            BuiltInDirectPluginWindowFunction.FirstValue => CreateValueAccessPositionAssignment(
                result,
                currentIndex,
                sourcePartitionIndex,
                frameStart,
                $"{frameStart} <= {frameEnd}",
                sourceValue,
                defaultValue),
            BuiltInDirectPluginWindowFunction.LastValue => CreateValueAccessPositionAssignment(
                result,
                currentIndex,
                sourcePartitionIndex,
                frameEnd,
                $"{frameStart} <= {frameEnd}",
                sourceValue,
                defaultValue),
            BuiltInDirectPluginWindowFunction.NthValue => CreateNthValueAssignment(
                plugin,
                nthArguments,
                result,
                currentIndex,
                sourcePartitionIndex,
                nth,
                frameStart,
                frameEnd,
                sourceValue,
                defaultValue),
            _ => throw new ArgumentOutOfRangeException(nameof(function), function, null)
        };
    }

    private static string CreateValueAccessPositionAssignment(
        string result,
        string currentIndex,
        string sourcePartitionIndex,
        string sourceExpression,
        string guard,
        string sourceValue,
        string defaultValue)
    {
        return
            $"        var {sourcePartitionIndex} = {sourceExpression};" + Environment.NewLine +
            $"        {result}[{currentIndex}] = {guard} ? {sourceValue} : {defaultValue};" + Environment.NewLine;
    }

    private string CreateNthValueAssignment(
        ExecutionComputePluginWindow plugin,
        ExecutionVariable? nthArguments,
        string result,
        string currentIndex,
        string sourcePartitionIndex,
        string nth,
        string frameStart,
        string frameEnd,
        string sourceValue,
        string defaultValue)
    {
        var nthExpression = nthArguments == null
            ? RenderExpressionSource(plugin.Arguments[0])
            : $"{nthArguments.Name}[{currentIndex}]";

        return
            $"        var {nth} = {nthExpression};" + Environment.NewLine +
            $"        var {sourcePartitionIndex} = {frameStart} + {nth} - 1;" + Environment.NewLine +
            $"        {result}[{currentIndex}] = {nth} > 0 && {sourcePartitionIndex} <= {frameEnd}" + Environment.NewLine +
            $"            ? {sourceValue}" + Environment.NewLine +
            $"            : {defaultValue};" + Environment.NewLine;
    }

    private static string CreateFrameStartSource(
        ExecutionWindowFrame frame,
        string partitionIndex,
        string partitionCount)
    {
        return CreateWindowAggregateFrameStartExpression(frame.Start, partitionIndex, partitionCount)
            .NormalizeWhitespace()
            .ToFullString();
    }

    private static string CreateFrameEndSource(
        ExecutionWindowFrame frame,
        string partitionIndex,
        string partitionCount)
    {
        return CreateWindowAggregateFrameEndExpression(frame.End, partitionIndex, partitionCount)
            .NormalizeWhitespace()
            .ToFullString();
    }
}

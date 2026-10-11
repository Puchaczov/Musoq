using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{

    private List<StatementSyntax> RenderLibraryWindowContext(ExecutionComputePluginWindow window)
    {
        var operation = window.LibraryGet!.RequireClrMethod();
        var capture = window.ContextBuffers ??
            throw new InvalidOperationException("Typed window context storage must be selected before rendering.");
        var buffers = new LibraryWindowContextBuffers(capture.Values, capture.Arguments,
            capture.RangeKeys, capture.OrderKeyValues, capture.ContextType.RequireClrType(), operation.DeclaringType!);
        var rangeKeys = buffers.RangeKeys;
        var partitionKeys = ResolvePluginPartitionKeyArray(window);
        var orderKeys = ResolvePluginOrderKeyArray(window);
        var statements = new List<StatementSyntax>();
        AddWindowMethodTargetDeclarations(statements, window.MethodTargets);
        if (ShouldMaterializeWindowKeyArray(partitionKeys))
            statements.Add(CreateLibraryWindowKeyDeclaration(partitionKeys!, window.Buffer));
        if (orderKeys is { ShouldExtract: true })
            statements.Add(CreateLibraryWindowKeyDeclaration(orderKeys, window.Buffer));
        foreach (var buffer in new[] { buffers.Values }.Concat(buffers.Arguments))
            statements.Add(CreateLibraryWindowArrayDeclaration(buffer, window.Buffer));
        if (rangeKeys is not null)
            statements.Add(CreateLibraryWindowArrayDeclaration(rangeKeys, window.Buffer));
        if (buffers.OrderKeyValues is { } orderValues)
            statements.Add(CreateLibraryWindowArrayDeclaration(orderValues, window.Buffer));
        statements.Add(CreateLibraryWindowExtraction(window, buffers, partitionKeys, orderKeys));
        AddWindowPartitionDeclarations(statements, window.Partitions, partitionKeys?.Variable, window.Buffer);
        if (orderKeys is not null)
            AddWindowSortedPartitionDeclarations(statements, window.SortedPartitions, window.Partitions, orderKeys, window.OrderKeys);
        statements.Add(CreateLibraryWindowArrayDeclaration(window.Results, window.Buffer));
        var partitions = window.SortedPartitions ?? window.Partitions ??
                         throw new InvalidOperationException("Typed context windows require resolved partitions.");
        statements.Add(CreateLibraryWindowContextLoop(window, buffers, orderKeys, partitions));
        return statements;
    }

    private static StatementSyntax CreateLibraryWindowKeyDeclaration(ExecutionWindowKeyArray keys, ExecutionVariable source)
        => CreateLocalDeclaration(SyntaxFactory.IdentifierName("var"), keys.Variable.Name,
            CreateWindowKeyArrayCreation(keys, CreateBufferCountExpression(source)));

    private static StatementSyntax CreateLibraryWindowArrayDeclaration(ExecutionVariable array, ExecutionVariable source)
        => CreateLocalDeclaration(SyntaxFactory.IdentifierName("var"), array.Name,
            CreateSizedArrayCreation(GetArrayElementType(array), CreateBufferCountExpression(source)));

    private StatementSyntax CreateLibraryWindowExtraction(ExecutionComputePluginWindow window,
        LibraryWindowContextBuffers buffers, ExecutionWindowKeyArray? partitionKeys, ExecutionWindowKeyArray? orderKeys)
    {
        var index = new ExecutionVariable($"{window.Results.Name}ExtractionIndex", typeof(int));
        var statements = new List<StatementSyntax>();
        statements.AddRange(CreateIndexedItemDeclarations(window.Item, window.Buffer, index, window.RowAccessMode));
        if (partitionKeys is { ShouldExtract: true })
            statements.Add(CreateWindowKeyArrayAssignment(partitionKeys, index.Name,
                CreateWindowPartitionKeyExpression(partitionKeys, window.PartitionKey!)));
        if (orderKeys is { ShouldExtract: true })
            statements.Add(CreateWindowKeyArrayAssignment(orderKeys, index.Name,
                CreateWindowOrderKeyExpression(orderKeys, window.OrderKeys)));
        if (buffers.OrderKeyValues is { } orderValues)
        {
            if (orderKeys is null || orderKeys.Shape?.RawValueType is null)
                throw new InvalidOperationException("Typed order-key context storage needs a captured concrete key projection.");
            statements.Add(CreateArrayAssignment(orderValues.Name, index.Name,
                SyntaxFactory.ParseExpression($"{orderKeys.Variable.Name}[{index.Name}].RawValues"), GetArrayElementType(orderValues)));
        }
        if (buffers.RangeKeys is { } rangeKeys)
            statements.Add(CreateArrayAssignment(rangeKeys.Name, index.Name,
                SyntaxFactory.ParseExpression($"{orderKeys!.Variable.Name}[{index.Name}].RawValues"), GetArrayElementType(rangeKeys)));
        statements.Add(CreateArrayAssignment(buffers.Values.Name, index.Name,
            CastIfNeeded(RenderExpression(window.Value), GetArrayElementType(buffers.Values)), GetArrayElementType(buffers.Values)));
        for (var argument = 0; argument < buffers.Arguments.Count; argument++)
        {
            var target = buffers.Arguments[argument];
            statements.Add(CreateArrayAssignment(target.Name, index.Name,
                CastIfNeeded(RenderExpression(window.Arguments[argument]), GetArrayElementType(target)), GetArrayElementType(target)));
        }
        return CreateIndexedForLoop(index.Name, window.Buffer, StatementEmitter.CreateBlock(statements));
    }

    private static StatementSyntax CreateLibraryWindowContextLoop(ExecutionComputePluginWindow window,
        LibraryWindowContextBuffers buffers, ExecutionWindowKeyArray? orderKeys, ExecutionWindowPartitionSet partitions)
    {
        var prefix = window.Results.Name;
        var frame = CreateValueAccessFrameDeclarations(window, orderKeys, buffers.RangeKeys,
            $"{prefix}Indices", $"{prefix}Start", $"{prefix}Position", $"{prefix}Count",
            $"{prefix}FrameStart", $"{prefix}FrameEnd");
        return LibraryWindowContextSyntax.CreateLoop(window, buffers, partitions, frame);
    }
}

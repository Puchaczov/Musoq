using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private List<StatementSyntax> RenderComputePluginWindow(ExecutionComputePluginWindow plugin)
    {
        if (IsBuiltInDirectPluginWindow(plugin))
            return RenderBuiltInDirectPluginWindow(plugin);

        if (!CanRenderStreamingPluginWindow(plugin))
            throw new InvalidOperationException("Custom plugin windows require typed no-boxing streaming rendering.");

        var partitionKeys = ResolveWindowKeyArray(
            plugin.PartitionKey,
            plugin.PartitionKeyArray,
            $"{plugin.Results.Name}PartitionKeys");

        if (CanUseFusedIntOrderStreamingPluginWindow(plugin))
            return RenderFusedIntOrderStreamingPluginWindow(plugin, partitionKeys);

        return RenderStreamingPluginWindow(plugin);
    }

    private List<StatementSyntax> RenderStreamingPluginWindow(ExecutionComputePluginWindow plugin)
    {
        const string indexVariableName = "windowIndex";
        var index = new ExecutionVariable(indexVariableName, typeof(int));
        var partitionKeys = ResolveWindowKeyArray(
            plugin.PartitionKey,
            plugin.PartitionKeyArray,
            $"{plugin.Results.Name}PartitionKeys");
        var orderKeys = plugin.OrderKeys.Count == 0
            ? null
            : ResolveWindowKeyArray(
                plugin.OrderKeyArray,
                plugin.OrderKeys,
                $"{plugin.Results.Name}OrderKeys");
        var extractionStatements = new List<StatementSyntax>();
        var statements = new List<StatementSyntax>();

        AddWindowMethodTargetDeclarations(statements, plugin.MethodTargets);

        if (partitionKeys is { ShouldExtract: true } || orderKeys is { ShouldExtract: true })
        {
            extractionStatements.AddRange(CreateIndexedItemDeclarations(
                plugin.Item,
                plugin.Buffer,
                index,
                plugin.RowAccessMode));
        }

        if (partitionKeys is { ShouldExtract: true })
        {
            extractionStatements.Add(CreateWindowKeyArrayAssignment(
                partitionKeys.Variable,
                index.Name,
                RenderExpression(plugin.PartitionKey!)));
        }

        if (orderKeys is { ShouldExtract: true })
        {
            extractionStatements.Add(CreateWindowKeyArrayAssignment(
                orderKeys.Variable,
                index.Name,
                RenderWindowOrderKey(plugin.OrderKeys, orderKeys.Variable)));
        }

        if (ShouldMaterializeWindowKeyArray(partitionKeys))
        {
            statements.Add(CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                partitionKeys.Variable.Name,
                CreateWindowKeyArrayCreation(partitionKeys.Variable, CreateBufferCountExpression(plugin.Buffer))));
        }

        if (orderKeys is { ShouldExtract: true })
        {
            statements.Add(CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                orderKeys.Variable.Name,
                CreateWindowKeyArrayCreation(orderKeys.Variable, CreateBufferCountExpression(plugin.Buffer))));
        }

        if (extractionStatements.Count > 0)
        {
            statements.Add(CreateIndexedForLoop(
                index.Name,
                plugin.Buffer,
                StatementEmitter.CreateBlock(extractionStatements)));
        }

        AddWindowPartitionDeclarations(statements, plugin.Partitions, partitionKeys?.Variable, plugin.Buffer);
        if (orderKeys != null)
        {
            AddWindowSortedPartitionDeclarations(
                statements,
                plugin.SortedPartitions,
                plugin.Partitions,
                orderKeys.Variable,
                plugin.OrderKeys);
        }

        statements.AddRange(CreateStreamingPluginWindowComputation(plugin));

        return statements;
    }

    private List<StatementSyntax> RenderFusedIntOrderStreamingPluginWindow(
        ExecutionComputePluginWindow plugin,
        ExecutionWindowKeyArray? partitionKeys)
    {
        const string indexVariableName = "windowIndex";
        var index = new ExecutionVariable(indexVariableName, typeof(int));
        var builder = CreateFusedIntOrderBuilderVariable(plugin.Results, partitionKeys);
        var extractionStatements = new List<StatementSyntax>();
        var statements = new List<StatementSyntax>();

        AddWindowMethodTargetDeclarations(statements, plugin.MethodTargets);
        statements.Add(CreateFusedIntOrderBuilderDeclaration(builder, plugin.Buffer));

        extractionStatements.AddRange(CreateIndexedItemDeclarations(
            plugin.Item,
            plugin.Buffer,
            index,
            plugin.RowAccessMode));
        extractionStatements.Add(CreateFusedIntOrderBuilderAddStatement(
            builder,
            partitionKeys,
            plugin.PartitionKey,
            plugin.OrderKeys[0].Expression,
            indexVariableName));

        statements.Add(CreateIndexedForLoop(
            indexVariableName,
            plugin.Buffer,
            StatementEmitter.CreateBlock(extractionStatements)));
        AddFusedIntOrderPartitionDeclarations(
            statements,
            plugin.Partitions,
            plugin.SortedPartitions,
            builder,
            plugin.OrderKeys[0].Descending);
        statements.AddRange(CreateStreamingPluginWindowComputation(plugin));

        return statements;
    }

    private static bool CanUseFusedIntOrderStreamingPluginWindow(ExecutionComputePluginWindow plugin)
    {
        if (!CanUseFusedIntOrderWindow(plugin.OrderKeys))
            return false;

        var targetPartitions = plugin.SortedPartitions ?? plugin.Partitions;
        return targetPartitions is { ShouldCreate: true };
    }
}

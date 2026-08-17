using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Plugins;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private List<StatementSyntax> CreateStreamingPluginWindowComputation(ExecutionComputePluginWindow plugin)
    {
        if (!TryGetStreamingPluginWindowMode(plugin, out var mode))
            throw new InvalidOperationException("Streaming plugin window frame shape is not supported.");

        var statements = new List<StatementSyntax>();
        var functionName = $"{plugin.Results.Name}Function";
        var resultElementType = plugin.Results.Type.RequireClrType().GetElementType() ?? typeof(object);
        var partitions = GetStreamingPluginWindowPartitions(plugin, mode);

        if (!TryGetTypedPluginWindowCallTypes(plugin, out var inputType, out var resultType))
            throw new InvalidOperationException("Streaming plugin windows require typed no-boxing plugin contracts.");

        statements.Add(CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            functionName,
            CreatePluginFactoryInvocation(
                plugin.FactoryMethod.RequireClrMethod(),
                CreatePluginWindowFunctionType(inputType, resultType))));
        var typedArguments = CreateTypedPluginArgumentsStatement(plugin, functionName);
        if (typedArguments != null)
            statements.Add(typedArguments);
        statements.Add(CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            plugin.Results.Name,
            CreateSizedArrayCreation(resultElementType, CreateBufferCountExpression(plugin.Buffer))));
        statements.Add(CreateStreamingPluginPartitionLoop(plugin, functionName, partitions, mode));

        return statements;
    }

    private static ExecutionWindowPartitionSet GetStreamingPluginWindowPartitions(
        ExecutionComputePluginWindow plugin,
        StreamingPluginWindowMode mode)
    {
        if (mode == StreamingPluginWindowMode.Running || plugin.OrderKeys.Count > 0)
        {
            return plugin.SortedPartitions ?? plugin.Partitions ??
                   throw new InvalidOperationException("Streaming plugin windows require resolved partitions.");
        }

        return plugin.Partitions ??
               throw new InvalidOperationException("Streaming plugin windows require resolved partitions.");
    }

    private ForStatementSyntax CreateStreamingPluginPartitionLoop(
        ExecutionComputePluginWindow plugin,
        string functionName,
        ExecutionWindowPartitionSet partitions,
        StreamingPluginWindowMode mode)
    {
        var partitionSetIndexName = $"{plugin.Results.Name}PartitionSetIndex";
        var partitionStartName = $"{plugin.Results.Name}PartitionStart";
        var partitionCountName = $"{plugin.Results.Name}PartitionCount";
        var partitionIndicesName = $"{plugin.Results.Name}PartitionIndices";
        var body = new List<StatementSyntax>
        {
            CreateWindowPartitionStartDeclaration(
                partitions.Variable.Name,
                partitionSetIndexName,
                partitionStartName),
            CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                partitionCountName,
                CreateWindowPartitionLengthExpression(partitions.Variable.Name, partitionSetIndexName)),
            CreateWindowPartitionIndicesDeclaration(partitions.Variable.Name, partitionIndicesName),
            CreateInvocationStatement(
                functionName,
                nameof(IWindowFunction.SetPartitionSize),
                SyntaxFactory.IdentifierName(partitionCountName)),
            CreateInvocationStatement(functionName, nameof(IWindowFunction.PartitionStart))
        };

        if (mode == StreamingPluginWindowMode.Running)
        {
            body.Add(CreateStreamingPluginSortedRowsLoop(
                plugin,
                functionName,
                partitionIndicesName,
                partitionStartName,
                partitionCountName));
        }
        else
        {
            body.Add(CreateStreamingPluginUnsortedAccumulationLoop(
                plugin,
                functionName,
                partitionIndicesName,
                partitionStartName,
                partitionCountName));
            body.Add(CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                $"{plugin.Results.Name}FinalValue",
                CreateStreamingPluginValueExpression(plugin, functionName)));
            body.Add(CreateStreamingPluginUnsortedAssignmentLoop(
                plugin,
                partitionIndicesName,
                partitionStartName,
                partitionCountName));
        }

        return CreateWindowPartitionSetForLoop(
            partitionSetIndexName,
            partitions.Variable.Name,
            StatementEmitter.CreateBlock(body));
    }

    private ForStatementSyntax CreateStreamingPluginSortedRowsLoop(
        ExecutionComputePluginWindow plugin,
        string functionName,
        string partitionIndicesName,
        string partitionStartName,
        string partitionCountName)
    {
        var partitionIndexName = $"{plugin.Results.Name}PartitionIndex";
        var currentIndexName = $"{plugin.Results.Name}CurrentIndex";
        var currentIndex = new ExecutionVariable(currentIndexName, typeof(int));
        var body = new List<StatementSyntax>
        {
            CreateStreamingCurrentIndexDeclaration(
                partitionIndicesName,
                partitionStartName,
                partitionIndexName,
                currentIndexName)
        };
        body.AddRange(CreateIndexedItemDeclarations(
            plugin.Item,
            plugin.Buffer,
            currentIndex,
            plugin.RowAccessMode));
        body.Add(CreateStreamingPluginAccumulateStatement(plugin, functionName));
        body.Add(CreateWindowResultAssignment(
            plugin.Results.Name,
            SyntaxFactory.IdentifierName(currentIndexName),
            CreateStreamingPluginValueExpression(plugin, functionName)));

        return CreatePartitionIndexedForLoop(
            partitionIndexName,
            partitionCountName,
            StatementEmitter.CreateBlock(body));
    }

    private ForStatementSyntax CreateStreamingPluginUnsortedAccumulationLoop(
        ExecutionComputePluginWindow plugin,
        string functionName,
        string partitionIndicesName,
        string partitionStartName,
        string partitionCountName)
    {
        var partitionIndexName = $"{plugin.Results.Name}PartitionIndex";
        var currentIndexName = $"{plugin.Results.Name}CurrentIndex";
        var currentIndex = new ExecutionVariable(currentIndexName, typeof(int));
        var body = new List<StatementSyntax>
        {
            CreateStreamingCurrentIndexDeclaration(
                partitionIndicesName,
                partitionStartName,
                partitionIndexName,
                currentIndexName)
        };
        body.AddRange(CreateIndexedItemDeclarations(
            plugin.Item,
            plugin.Buffer,
            currentIndex,
            plugin.RowAccessMode));
        body.Add(CreateStreamingPluginAccumulateStatement(plugin, functionName));

        return CreatePartitionIndexedForLoop(
            partitionIndexName,
            partitionCountName,
            StatementEmitter.CreateBlock(body));
    }

    private static ForStatementSyntax CreateStreamingPluginUnsortedAssignmentLoop(
        ExecutionComputePluginWindow plugin,
        string partitionIndicesName,
        string partitionStartName,
        string partitionCountName)
    {
        var partitionIndexName = $"{plugin.Results.Name}PartitionIndex";
        var currentIndexName = $"{plugin.Results.Name}CurrentIndex";

        return CreatePartitionIndexedForLoop(
            partitionIndexName,
            partitionCountName,
            StatementEmitter.CreateBlock(
                CreateStreamingCurrentIndexDeclaration(
                    partitionIndicesName,
                    partitionStartName,
                    partitionIndexName,
                    currentIndexName),
                CreateWindowResultAssignment(
                    plugin.Results.Name,
                    SyntaxFactory.IdentifierName(currentIndexName),
                    SyntaxFactory.IdentifierName($"{plugin.Results.Name}FinalValue"))));
    }

    private ExpressionStatementSyntax CreateStreamingPluginAccumulateStatement(
        ExecutionComputePluginWindow plugin,
        string functionName)
    {
        return CreateInvocationStatement(
            functionName,
            nameof(IWindowFunction<object, object>.Accumulate),
            RenderExpression(plugin.Value));
    }

    private static InvocationExpressionSyntax CreateStreamingPluginValueExpression(
        ExecutionComputePluginWindow plugin,
        string functionName)
    {
        return CreateInvocationExpression(
            functionName,
            nameof(IWindowFunction<object, object>.GetValue));
    }

}

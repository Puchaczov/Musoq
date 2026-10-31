using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private List<StatementSyntax> RenderWindowAggregateKernel(ExecutionWindowAggregateKernel kernel)
    {
        const string indexVariableName = "windowIndex";
        var index = new ExecutionVariable(indexVariableName, typeof(int));
        var partitionKeys = ResolveAggregatePartitionKeyArray(kernel);
        var orderKeys = kernel.OrderKeys.Count == 0
            ? null
            : ResolveAggregateOrderKeyArray(kernel);
        var requiresRangeKeys = kernel is
        {
            Frame.Kind: ExecutionWindowFrameKind.Range,
            Descriptor.Mode: ExecutionWindowAggregateMode.BoundedRows
        } && WindowRangeFrameSyntax.HasRangeOffsetBound(kernel.Frame);
        var rangeKeys = requiresRangeKeys
            ? new ExecutionVariable(
                WindowRangeFrameSyntax.GetAggregateRangeKeysName(kernel),
                kernel.OrderKeys[0].Expression.ReturnType.RequireClrType().MakeArrayType())
            : null;
        var extractionStatements = new List<StatementSyntax>();
        var statements = new List<StatementSyntax>();
        var partitionBuilder = CreateWindowPartitionBuilderVariable(kernel.Results, partitionKeys, kernel.Partitions);

        if (CanUseFusedIntOrderAggregateKernel(kernel))
            return RenderFusedIntOrderAggregateKernel(kernel, partitionKeys!);

        AddWindowMethodTargetDeclarations(statements, kernel.MethodTargets);

        if (partitionKeys is { ShouldExtract: true } || orderKeys is { ShouldExtract: true } || requiresRangeKeys)
        {
            extractionStatements.AddRange(CreateIndexedItemDeclarations(
                kernel.Item,
                kernel.Buffer,
                index,
                kernel.RowAccessMode));
        }

        if (partitionKeys is { ShouldExtract: true })
            extractionStatements.AddRange(CreateWindowPartitionKeyExtractionStatements(
                partitionKeys,
                partitionBuilder,
                index.Name,
                CreateWindowPartitionKeyExpression(partitionKeys, kernel.PartitionKey!)));

        if (orderKeys is { ShouldExtract: true })
        {
            extractionStatements.Add(CreateWindowKeyArrayAssignment(
                orderKeys,
                index.Name,
                CreateWindowOrderKeyExpression(orderKeys, kernel.OrderKeys)));
        }

        if (rangeKeys != null)
        {
            extractionStatements.Add(CreateWindowKeyArrayAssignment(
                rangeKeys,
                index.Name,
                Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseExpression($"{orderKeys!.Variable.Name}[{index.Name}].RawValues")));
        }

        if (ShouldMaterializeWindowKeyArray(partitionKeys))
        {
            statements.Add(CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                partitionKeys.Variable.Name,
                CreateWindowKeyArrayCreation(partitionKeys, CreateBufferCountExpression(kernel.Buffer))));
        }

        if (partitionBuilder != null)
            statements.Add(CreateWindowPartitionBuilderDeclaration(partitionBuilder, kernel.Buffer));

        if (orderKeys is { ShouldExtract: true })
        {
            statements.Add(CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                orderKeys.Variable.Name,
                CreateWindowKeyArrayCreation(orderKeys, CreateBufferCountExpression(kernel.Buffer))));
        }

        if (rangeKeys != null)
        {
            statements.Add(CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                rangeKeys.Name,
                CreateSizedArrayCreation(
                    kernel.OrderKeys[0].Expression.ReturnType,
                    CreateBufferCountExpression(kernel.Buffer))));
        }

        if (extractionStatements.Count > 0)
        {
            statements.Add(CreateIndexedForLoop(
                index.Name,
                kernel.Buffer,
                StatementEmitter.CreateBlock(extractionStatements)));
        }

        AddWindowPartitionDeclarations(statements, kernel.Partitions, partitionKeys?.Variable, kernel.Buffer, partitionBuilder);
        if (orderKeys != null)
        {
            AddWindowSortedPartitionDeclarations(
                statements,
                kernel.SortedPartitions,
                kernel.Partitions,
                orderKeys);
        }

        statements.Add(CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            kernel.Results.Name,
            CreateSizedArrayCreation(
                kernel.Descriptor.ResultType,
                CreateBufferCountExpression(kernel.Buffer))));
        AddLibraryWindowBatchValues(kernel, statements);
        statements.Add(CreateWindowAggregateKernelPartitionLoop(
            kernel,
            GetWindowAggregateKernelPartitions(kernel)));

        return statements;
    }


    private List<StatementSyntax> RenderFusedIntOrderAggregateKernel(
        ExecutionWindowAggregateKernel kernel,
        ExecutionWindowKeyArray? partitionKeys)
    {
        const string indexVariableName = "windowIndex";
        var index = new ExecutionVariable(indexVariableName, typeof(int));
        var builder = CreateFusedIntOrderBuilderVariable(kernel.Results, partitionKeys);
        var extractionStatements = new List<StatementSyntax>();
        var statements = new List<StatementSyntax>();

        AddWindowMethodTargetDeclarations(statements, kernel.MethodTargets);
        statements.Add(CreateFusedIntOrderBuilderDeclaration(builder, kernel.Buffer));

        extractionStatements.AddRange(CreateIndexedItemDeclarations(
            kernel.Item,
            kernel.Buffer,
            index,
            kernel.RowAccessMode));
        extractionStatements.Add(CreateFusedIntOrderBuilderAddStatement(
            builder,
            partitionKeys,
            kernel.PartitionKey,
            kernel.OrderKeys[0].Expression,
            indexVariableName));

        statements.Add(CreateIndexedForLoop(
            indexVariableName,
            kernel.Buffer,
            StatementEmitter.CreateBlock(extractionStatements)));
        AddFusedIntOrderPartitionDeclarations(
            statements,
            kernel.Partitions,
            kernel.SortedPartitions,
            builder,
            kernel.OrderKeys[0].Descending);
        statements.Add(CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            kernel.Results.Name,
            CreateSizedArrayCreation(
                kernel.Descriptor.ResultType,
                CreateBufferCountExpression(kernel.Buffer))));
        AddLibraryWindowBatchValues(kernel, statements);
        statements.Add(CreateWindowAggregateKernelPartitionLoop(
            kernel,
            GetWindowAggregateKernelPartitions(kernel)));

        return statements;
    }

    private static bool CanUseFusedIntOrderAggregateKernel(ExecutionWindowAggregateKernel kernel)
    {
        return kernel.UseIntegerOrderIndex;
    }

    private static ExecutionWindowPartitionSet GetWindowAggregateKernelPartitions(
        ExecutionWindowAggregateKernel kernel)
    {
        if (kernel.Descriptor.Mode == ExecutionWindowAggregateMode.Running || kernel.OrderKeys.Count > 0)
        {
            return kernel.SortedPartitions ?? kernel.Partitions ??
                   throw new InvalidOperationException("Window aggregate kernels require resolved partitions.");
        }

        return kernel.Partitions ??
               throw new InvalidOperationException("Window aggregate kernels require resolved partitions.");
    }

    private ForStatementSyntax CreateWindowAggregateKernelPartitionLoop(
        ExecutionWindowAggregateKernel kernel,
        ExecutionWindowPartitionSet partitions)
    {
        var partitionSetIndexName = $"{kernel.Results.Name}PartitionSetIndex";
        var partitionStartName = $"{kernel.Results.Name}PartitionStart";
        var partitionCountName = $"{kernel.Results.Name}PartitionCount";
        var partitionIndicesName = $"{kernel.Results.Name}PartitionIndices";
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
            CreateWindowPartitionIndicesDeclaration(partitions.Variable.Name, partitionIndicesName)
        };

        if (kernel.Descriptor.UseRunningPeers)
        {
            body.AddRange(CreateWindowAggregateAccumulatorDeclarations(kernel));
            body.Add(CreateLibraryWindowPeerLoop(kernel, partitionIndicesName, partitionStartName, partitionCountName));
        }
        else if (kernel.Descriptor.Mode == ExecutionWindowAggregateMode.Running)
        {
            body.AddRange(CreateWindowAggregateAccumulatorDeclarations(kernel));
            body.Add(CreateWindowAggregateRunningLoop(
                kernel,
                partitionIndicesName,
                partitionStartName,
                partitionCountName));
        }
        else if (kernel.Descriptor.Mode == ExecutionWindowAggregateMode.WholePartition)
        {
            body.AddRange(CreateWindowAggregateAccumulatorDeclarations(kernel));
            body.Add(CreateWindowAggregateAccumulationLoop(
                kernel,
                partitionIndicesName,
                partitionStartName,
                partitionCountName));
            body.Add(CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                $"{kernel.Results.Name}FinalValue",
                CreateWindowAggregateValueExpression(kernel)));
            body.Add(CreateWindowAggregateWholePartitionAssignmentLoop(
                kernel,
                partitionIndicesName,
                partitionStartName,
                partitionCountName));
        }
        else
        {
            body.AddRange(CreateWindowAggregateAccumulatorDeclarations(kernel));
            body.Add(HasLibraryWindowBatch(kernel)
                ? CreateLibraryWindowBatchPartition(kernel, partitionIndicesName, partitionStartName, partitionCountName)
                : CreateLibraryWindowFrameLoop(kernel, partitionIndicesName, partitionStartName, partitionCountName));
        }

        return CreateWindowPartitionSetForLoop(
            partitionSetIndexName,
            partitions.Variable.Name,
            StatementEmitter.CreateBlock(body));
    }

    private static IEnumerable<StatementSyntax> CreateWindowAggregateAccumulatorDeclarations(ExecutionWindowAggregateKernel kernel)
    {
        var library = kernel.Descriptor.LibraryKernel ??
            throw new InvalidOperationException("Window state requires a registered typed library kernel.");
        var state = new ExecutionVariable(CreateLibraryWindowStateName(kernel), library.StateType);
        return RenderInitializeAggregateState(new ExecutionInitializeAggregateState(state, library.ResetMethod));
    }
}

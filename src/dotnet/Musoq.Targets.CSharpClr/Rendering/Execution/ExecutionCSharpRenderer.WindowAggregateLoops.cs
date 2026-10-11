using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private ForStatementSyntax CreateWindowAggregateRunningLoop(
        ExecutionWindowAggregateKernel kernel,
        string partitionIndicesName,
        string partitionStartName,
        string partitionCountName)
    {
        var partitionIndexName = $"{kernel.Results.Name}PartitionIndex";
        var currentIndexName = $"{kernel.Results.Name}CurrentIndex";
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
            kernel.Item,
            kernel.Buffer,
            currentIndex,
            kernel.RowAccessMode));
        body.AddRange(CreateWindowAggregateAccumulateStatements(kernel));
        body.Add(CreateWindowResultAssignment(
            kernel.Results.Name,
            SyntaxFactory.IdentifierName(currentIndexName),
            CreateWindowAggregateValueExpression(kernel)));

        return CreatePartitionIndexedForLoop(
            partitionIndexName,
            partitionCountName,
            StatementEmitter.CreateBlock(body));
    }

    private ForStatementSyntax CreateWindowAggregateAccumulationLoop(
        ExecutionWindowAggregateKernel kernel,
        string partitionIndicesName,
        string partitionStartName,
        string partitionCountName)
    {
        var partitionIndexName = $"{kernel.Results.Name}PartitionIndex";
        var currentIndexName = $"{kernel.Results.Name}CurrentIndex";
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
            kernel.Item,
            kernel.Buffer,
            currentIndex,
            kernel.RowAccessMode));
        body.AddRange(CreateWindowAggregateAccumulateStatements(kernel));

        return CreatePartitionIndexedForLoop(
            partitionIndexName,
            partitionCountName,
            StatementEmitter.CreateBlock(body));
    }

    private static ForStatementSyntax CreateWindowAggregateWholePartitionAssignmentLoop(
        ExecutionWindowAggregateKernel kernel,
        string partitionIndicesName,
        string partitionStartName,
        string partitionCountName)
    {
        var partitionIndexName = $"{kernel.Results.Name}PartitionIndex";
        var currentIndexName = $"{kernel.Results.Name}CurrentIndex";

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
                    kernel.Results.Name,
                    SyntaxFactory.IdentifierName(currentIndexName),
                    SyntaxFactory.IdentifierName($"{kernel.Results.Name}FinalValue"))));
    }

}

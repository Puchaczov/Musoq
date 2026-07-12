using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private List<StatementSyntax> RenderComputeOffsetWindow(ExecutionComputeOffsetWindow offset)
    {
        const string indexVariableName = "windowIndex";
        var index = new ExecutionVariable(indexVariableName, typeof(int));
        var partitionKeys = ResolveOffsetPartitionKeyArray(offset);
        var orderKeys = ResolveOffsetOrderKeyArray(offset);
        var valueElementType = CreateWindowOffsetValueElementType(offset.Value.ReturnType.RequireClrType());
        var resultElementType = CreateWindowResultElementType(offset.Results);
        var values = new ExecutionVariable($"{offset.Results.Name}Values", valueElementType.MakeArrayType());
        var useScalarOffsetArguments = CanUseScalarOffsetArguments(offset);
        var offsets = useScalarOffsetArguments
            ? null
            : new ExecutionVariable($"{offset.Results.Name}Offsets", typeof(int[]));
        var defaultValues = useScalarOffsetArguments
            ? null
            : new ExecutionVariable($"{offset.Results.Name}Defaults", resultElementType.MakeArrayType());
        var extractionStatements = new List<StatementSyntax>();
        var statements = new List<StatementSyntax>();

        extractionStatements.AddRange(CreateIndexedItemDeclarations(
            offset.Item,
            offset.Buffer,
            index,
            offset.RowAccessMode));

        if (partitionKeys is { ShouldExtract: true })
        {
            extractionStatements.Add(CreateWindowKeyArrayAssignment(
                partitionKeys,
                index.Name,
                CreateWindowPartitionKeyExpression(partitionKeys, offset.PartitionKey!)));
        }

        if (orderKeys.ShouldExtract)
        {
            extractionStatements.Add(CreateWindowKeyArrayAssignment(
                orderKeys,
                index.Name,
                CreateWindowOrderKeyExpression(orderKeys, offset.OrderKeys)));
        }

        extractionStatements.Add(CreateArrayAssignment(
            values.Name,
            index.Name,
            RenderExpression(offset.Value),
            valueElementType));

        if (!useScalarOffsetArguments)
        {
            extractionStatements.Add(CreateIntArrayAssignment(offsets!.Name, index.Name, RenderExpression(offset.Offset)));
            extractionStatements.Add(CreateArrayAssignment(defaultValues!.Name, index.Name, RenderExpression(offset.DefaultValue), resultElementType));
        }

        if (ShouldMaterializeWindowKeyArray(partitionKeys))
        {
            statements.Add(CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                partitionKeys.Variable.Name,
                CreateWindowKeyArrayCreation(partitionKeys, CreateBufferCountExpression(offset.Buffer))));
        }

        if (orderKeys.ShouldExtract)
        {
            statements.Add(CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                orderKeys.Variable.Name,
                CreateWindowKeyArrayCreation(orderKeys, CreateBufferCountExpression(offset.Buffer))));
        }

        statements.AddRange(
        [
            CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                values.Name,
                CreateSizedArrayCreation(valueElementType, CreateBufferCountExpression(offset.Buffer))),
        ]);

        if (!useScalarOffsetArguments)
        {
            statements.AddRange(
            [
                CreateLocalDeclaration(
                    SyntaxFactory.IdentifierName("var"),
                    offsets!.Name,
                    CreateSizedArrayCreation("int", CreateBufferCountExpression(offset.Buffer))),
                CreateLocalDeclaration(
                    SyntaxFactory.IdentifierName("var"),
                    defaultValues!.Name,
                    CreateSizedArrayCreation(resultElementType, CreateBufferCountExpression(offset.Buffer)))
            ]);
        }

        statements.AddRange(
        [
            CreateIndexedForLoop(
                index.Name,
                offset.Buffer,
                StatementEmitter.CreateBlock(extractionStatements)),
        ]);

        var offsetPartitions = offset.Partitions;
        if (offsetPartitions == null)
        {
            offsetPartitions = new ExecutionWindowPartitionSet(
                new ExecutionVariable($"{offset.Results.Name}Partitions", typeof(Musoq.Evaluator.Helpers.WindowPartitionSet)),
                true);
            statements.Add(CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                offsetPartitions.Variable.Name,
                CreateWindowHelperInvocation(
                    nameof(Musoq.Evaluator.Helpers.WindowFunctionHelpers.ResolvePartitionSet),
                    CreateBufferCountExpression(offset.Buffer),
                    CreatePartitionKeysArgument(partitionKeys?.Variable))));
        }
        else
        {
            AddWindowPartitionDeclarations(statements, offsetPartitions, partitionKeys?.Variable, offset.Buffer);
        }

        var offsetSortedPartitions = offset.SortedPartitions ??
                                     new ExecutionWindowPartitionSet(offsetPartitions.Variable, true, true);
        AddWindowSortedPartitionDeclarations(
            statements,
            offsetSortedPartitions,
            offsetPartitions,
            orderKeys,
            offset.OrderKeys);

        statements.AddRange(CreateOffsetKernelStatements(
            offset,
            values,
            offsets,
            defaultValues,
            offsetSortedPartitions));

        return statements;
    }

}

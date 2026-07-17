using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private List<StatementSyntax> RenderPartitionCountWindow(
        ExecutionWindowAggregateKernel kernel,
        ExecutionWindowKeyArray partitionKeys)
    {
        const string indexVariableName = "windowIndex";
        var index = new ExecutionVariable(indexVariableName, typeof(int));
        var builder = CreatePartitionCountBuilderVariable(kernel, partitionKeys);
        var extractionStatements = new List<StatementSyntax>();
        var statements = new List<StatementSyntax>();

        AddWindowMethodTargetDeclarations(statements, kernel.MethodTargets);
        statements.Add(CreatePartitionCountBuilderDeclaration(builder, kernel.Buffer));

        extractionStatements.AddRange(CreateIndexedItemDeclarations(
            kernel.Item,
            kernel.Buffer,
            index,
            kernel.RowAccessMode));
        extractionStatements.AddRange(CreatePartitionCountBuilderAddStatements(
            builder,
            partitionKeys,
            kernel,
            indexVariableName));

        statements.Add(CreateIndexedForLoop(
            indexVariableName,
            kernel.Buffer,
            StatementEmitter.CreateBlock(extractionStatements)));
        statements.Add(CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            kernel.Results.Name,
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(builder.Name),
                        SyntaxFactory.IdentifierName(nameof(WindowPartitionCountBuilder<>.ToResultInPlaceUnchecked))))
                .WithArgumentList(CreateArgumentList())));

        return statements;
    }
}

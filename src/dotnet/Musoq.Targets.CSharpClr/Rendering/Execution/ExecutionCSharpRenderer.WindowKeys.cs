using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static void AddWindowPartitionDeclarations(
        List<StatementSyntax> statements,
        ExecutionWindowPartitionSet? partitions,
        ExecutionVariable? partitionKeys,
        ExecutionVariable buffer,
        ExecutionVariable? partitionBuilder = null)
    {
        if (partitions is not { ShouldCreate: true })
            return;

        if (partitionBuilder != null)
        {
            statements.Add(CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                partitions.Variable.Name,
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(partitionBuilder.Name),
                        SyntaxFactory.IdentifierName(nameof(WindowPartitionBuilder<object>.ToPartitionSet))))));
            return;
        }

        statements.Add(CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            partitions.Variable.Name,
            CreateWindowPartitionSetInvocation(buffer, partitionKeys)));
    }

    private static InvocationExpressionSyntax CreateWindowPartitionSetInvocation(
        ExecutionVariable buffer, ExecutionVariable? partitionKeys = null)
    {
        if (partitionKeys is null)
            return CreateWindowHelperInvocation(nameof(WindowFunctionHelpers.ResolvePartitionSet), CreateBufferCountExpression(buffer));
        return CreateWindowHelperInvocation(nameof(WindowFunctionHelpers.ResolvePartitionSet),
            CreateBufferCountExpression(buffer), CreatePartitionKeysArgument(partitionKeys));
    }

    private static void AddWindowMethodTargetDeclarations(
        List<StatementSyntax> statements,
        IReadOnlyList<ExecutionVariable>? methodTargets)
    {
        if (methodTargets == null)
            return;

        foreach (var target in methodTargets)
        {
            statements.Add(RenderCreateAggregateLibrary(
                new ExecutionCreateAggregateLibrary(target, target.Type.RequireClrType())));
        }
    }

    private static void AddWindowSortedPartitionDeclarations(
        List<StatementSyntax> statements,
        ExecutionWindowPartitionSet? sortedPartitions,
        ExecutionWindowPartitionSet? partitions,
        ExecutionWindowKeyArray orderKeys)
    {
        if (!HasGeneratedWindowKeyType(orderKeys))
            throw UnsupportedShape.Of("Sorted window partitions require a lowered concrete order carrier.");
        AddGeneratedWindowSortedPartitionDeclarations(statements, sortedPartitions, partitions, orderKeys.Variable);
    }

    private static void AddGeneratedWindowSortedPartitionDeclarations(
        List<StatementSyntax> statements,
        ExecutionWindowPartitionSet? sortedPartitions,
        ExecutionWindowPartitionSet? partitions,
        ExecutionVariable orderKeys)
    {
        if (sortedPartitions is not { ShouldCreate: true })
            return;

        if (partitions == null)
            throw new InvalidOperationException("Sorted window partitions require a partition set.");

        var helperName = sortedPartitions.SortInPlace
            ? nameof(WindowFunctionHelpers.SortStructPartitionSetInPlace)
            : nameof(WindowFunctionHelpers.SortStructPartitionSet);
        var invocation = CreateWindowHelperInvocation(
            helperName,
            SyntaxFactory.IdentifierName(partitions.Variable.Name),
            SyntaxFactory.IdentifierName(orderKeys.Name),
            CreateBooleanLiteral(false));

        if (sortedPartitions.SortInPlace)
        {
            statements.Add(SyntaxFactory.ExpressionStatement(invocation));
            return;
        }

        statements.Add(CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            sortedPartitions.Variable.Name,
            invocation));
    }

}

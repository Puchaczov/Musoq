using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static LocalDeclarationStatementSyntax CreateFusedIntOrderBuilderDeclaration(
        ExecutionVariable builder,
        ExecutionVariable buffer)
    {
        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            builder.Name,
            SyntaxFactory.ObjectCreationExpression(CreateVariableTypeSyntax(builder))
                .WithArgumentList(CreateArgumentList(CreateBufferCountExpression(buffer))));
    }

    private static LocalDeclarationStatementSyntax CreatePartitionCountBuilderDeclaration(
        ExecutionVariable builder,
        ExecutionVariable buffer)
    {
        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            builder.Name,
            SyntaxFactory.ObjectCreationExpression(CreateVariableTypeSyntax(builder))
                .WithArgumentList(CreateArgumentList(CreateBufferCountExpression(buffer))));
    }

    private static LocalDeclarationStatementSyntax CreateWindowPartitionBuilderDeclaration(
        ExecutionVariable partitionBuilder,
        ExecutionVariable buffer)
    {
        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            partitionBuilder.Name,
            SyntaxFactory.ObjectCreationExpression(CreateVariableTypeSyntax(partitionBuilder))
                .WithArgumentList(CreateArgumentList(CreateBufferCountExpression(buffer))));
    }

    private ExpressionStatementSyntax CreateFusedIntOrderBuilderAddStatement(
        ExecutionVariable builder,
        ExecutionWindowKeyArray? partitionKeys,
        ExecutionComputeRankingWindow ranking,
        string indexVariableName)
    {
        return CreateFusedIntOrderBuilderAddStatement(
            builder,
            partitionKeys,
            ranking.PartitionKey,
            ranking.OrderKeys[0].Expression,
            indexVariableName);
    }

    private ExpressionStatementSyntax CreateFusedIntOrderBuilderAddStatement(
        ExecutionVariable builder,
        ExecutionWindowKeyArray? partitionKeys,
        ExecutionExpression? partitionKey,
        ExecutionExpression orderKey,
        string indexVariableName)
    {
        var arguments = new List<ExpressionSyntax>();

        if (partitionKeys != null)
        {
            var requiredPartitionKey = partitionKey ??
                                       throw new InvalidOperationException("Partition key expression is required when partition keys are materialized.");
            arguments.Add(HasGeneratedWindowKeyType(partitionKeys)
                ? CreateWindowPartitionKeyExpression(partitionKeys, requiredPartitionKey)
                : CastIfNeeded(
                    RenderExpression(requiredPartitionKey),
                    GetArrayElementType(partitionKeys.Variable)));
        }

        arguments.Add(CastIfNeeded(RenderExpression(orderKey), typeof(int)));
        arguments.Add(SyntaxFactory.IdentifierName(indexVariableName));

        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(builder.Name),
                        SyntaxFactory.IdentifierName(nameof(WindowIntOrderBuilder.Add))))
                .WithArgumentList(CreateArgumentList(arguments.ToArray())));
    }

    private IEnumerable<StatementSyntax> CreatePartitionCountBuilderAddStatements(
        ExecutionVariable builder,
        ExecutionWindowKeyArray partitionKeys,
        ExecutionWindowAggregateKernel kernel,
        string indexVariableName)
    {
        var valueExpression = RenderExpression(kernel.Value);
        var valuePresent = CreateWindowAggregateValuePresentExpression(
            valueExpression,
            kernel.Descriptor.InputType.RequireClrType());

        if (IsTrueLiteral(valuePresent))
            yield return CreateDiscardAssignment(CastIfNeeded(valueExpression, kernel.Descriptor.InputType));

        var partitionKey = kernel.PartitionKey ??
                           throw new InvalidOperationException("Partition count builder requires a partition key expression.");
        var partitionKeyType = GetArrayElementType(partitionKeys.Variable);
        var addMethodName = HasGeneratedWindowKeyType(partitionKeys) || partitionKeyType.IsValueType
            ? nameof(WindowPartitionCountBuilder<object>.AddUnchecked)
            : nameof(WindowPartitionCountBuilder<object>.AddReferenceUnchecked);
        var keyExpression = HasGeneratedWindowKeyType(partitionKeys)
            ? CreateWindowPartitionKeyExpression(partitionKeys, partitionKey)
            : CastIfNeededWhenRequired(
                RenderExpression(partitionKey),
                partitionKey.ReturnType.RequireClrType(),
                partitionKeyType);
        yield return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(builder.Name),
                        SyntaxFactory.IdentifierName(addMethodName)))
                .WithArgumentList(CreateArgumentList(
                    keyExpression,
                    valuePresent,
                    SyntaxFactory.IdentifierName(indexVariableName))));
    }

    private static ExpressionSyntax CastIfNeededWhenRequired(
        ExpressionSyntax expression,
        Type sourceType,
        Type targetType)
    {
        return targetType.IsAssignableFrom(sourceType)
            ? expression
            : CastIfNeeded(expression, targetType);
    }
}

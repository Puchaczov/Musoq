using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static LocalDeclarationStatementSyntax CreateStreamingCurrentIndexDeclaration(
        string partitionIndicesName,
        string partitionStartName,
        string partitionIndexName,
        string currentIndexName)
    {
        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            currentIndexName,
            CreateElementAccess(
                SyntaxFactory.IdentifierName(partitionIndicesName),
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.AddExpression,
                    SyntaxFactory.IdentifierName(partitionStartName),
                    SyntaxFactory.IdentifierName(partitionIndexName))));
    }

    private static LocalDeclarationStatementSyntax CreateWindowPartitionStartDeclaration(
        string partitionsName,
        string partitionSetIndexName,
        string partitionStartName)
    {
        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            partitionStartName,
            CreateWindowPartitionStartExpression(partitionsName, partitionSetIndexName));
    }

    private static LocalDeclarationStatementSyntax CreateWindowPartitionIndicesDeclaration(
        string partitionsName,
        string partitionIndicesName)
    {
        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            partitionIndicesName,
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(partitionsName),
                SyntaxFactory.IdentifierName(nameof(WindowPartitionSet.Indices))));
    }

    private static InvocationExpressionSyntax CreateWindowPartitionStartExpression(
        string partitionsName,
        string partitionSetIndexName)
    {
        return CreateInvocationExpression(
            partitionsName,
            nameof(WindowPartitionSet.GetStart),
            SyntaxFactory.IdentifierName(partitionSetIndexName));
    }

    private static InvocationExpressionSyntax CreateWindowPartitionLengthExpression(
        string partitionsName,
        string partitionSetIndexName)
    {
        return CreateInvocationExpression(
            partitionsName,
            nameof(WindowPartitionSet.GetLength),
            SyntaxFactory.IdentifierName(partitionSetIndexName));
    }

    private static ForStatementSyntax CreateWindowPartitionSetForLoop(
        string partitionSetIndexName,
        string partitionsName,
        StatementSyntax body)
    {
        return StatementEmitter.CreateForLoop(
            partitionSetIndexName,
            0,
            SyntaxFactory.BinaryExpression(
                SyntaxKind.LessThanExpression,
                SyntaxFactory.IdentifierName(partitionSetIndexName),
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(partitionsName),
                    SyntaxFactory.IdentifierName(nameof(WindowPartitionSet.PartitionCount)))),
            SyntaxFactory.PrefixUnaryExpression(
                SyntaxKind.PreIncrementExpression,
                SyntaxFactory.IdentifierName(partitionSetIndexName)),
            body);
    }

    private static ForStatementSyntax CreatePartitionIndexedForLoop(
        string partitionIndexName,
        string partitionCountName,
        StatementSyntax body)
    {
        return StatementEmitter.CreateForLoop(
            partitionIndexName,
            0,
            SyntaxFactory.BinaryExpression(
                SyntaxKind.LessThanExpression,
                SyntaxFactory.IdentifierName(partitionIndexName),
                SyntaxFactory.IdentifierName(partitionCountName)),
            SyntaxFactory.PrefixUnaryExpression(
                SyntaxKind.PreIncrementExpression,
                SyntaxFactory.IdentifierName(partitionIndexName)),
            body);
    }

    private static ExpressionStatementSyntax CreateWindowResultAssignment(
        string arrayName,
        ExpressionSyntax index,
        ExpressionSyntax value)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                CreateElementAccess(SyntaxFactory.IdentifierName(arrayName), index),
                value));
    }

    private static ExpressionStatementSyntax CreateInvocationStatement(
        string instanceName,
        string methodName,
        params ExpressionSyntax[] arguments)
    {
        return SyntaxFactory.ExpressionStatement(CreateInvocationExpression(instanceName, methodName, arguments));
    }

    private static InvocationExpressionSyntax CreateInvocationExpression(
        string instanceName,
        string methodName,
        params ExpressionSyntax[] arguments)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(instanceName),
                    SyntaxFactory.IdentifierName(methodName)))
            .WithArgumentList(CreateArgumentList(arguments));
    }
}

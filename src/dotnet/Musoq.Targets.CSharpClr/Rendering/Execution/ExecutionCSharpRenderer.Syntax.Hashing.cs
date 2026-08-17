using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static IfStatementSyntax CreateContinueIfNull(string keyVariableName)
    {
        return SyntaxFactory.IfStatement(
            SyntaxFactory.BinaryExpression(
                SyntaxKind.EqualsExpression,
                SyntaxFactory.IdentifierName(keyVariableName),
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
            SyntaxFactory.ContinueStatement());
    }

    private static BlockSyntax CreateHashBucketAddOrCreateStatement(
        ExecutionHashAdd hashAdd,
        string keyVariableName,
        string bucketRefVariableName)
    {
        var bucketCreation = SyntaxFactory.ObjectCreationExpression(CreateHashBucketTypeSyntax(
            hashAdd.RowType.RequireClrType(),
            hashAdd.GeneratedRowTypeName))
            .WithArgumentList(CreateArgumentList(SyntaxFactory.IdentifierName(hashAdd.Row.Name)));
        var existsVariableName = $"{bucketRefVariableName}Exists";
        var refDeclaration = CreateHashBucketRefDeclaration(
            hashAdd.Hash.Name,
            keyVariableName,
            bucketRefVariableName,
            existsVariableName);
        var initializeBucketStatement = SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName(bucketRefVariableName),
                bucketCreation));
        var addMatchedRowStatement = SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(bucketRefVariableName),
                        SyntaxFactory.IdentifierName("Add")))
                .WithArgumentList(CreateArgumentList(SyntaxFactory.IdentifierName(hashAdd.Row.Name))));

        return StatementEmitter.CreateBlock(refDeclaration, SyntaxFactory.IfStatement(
                SyntaxFactory.PrefixUnaryExpression(
                    SyntaxKind.LogicalNotExpression,
                    SyntaxFactory.IdentifierName(existsVariableName)),
                StatementEmitter.CreateBlock(initializeBucketStatement),
                SyntaxFactory.ElseClause(StatementEmitter.CreateBlock(addMatchedRowStatement))));
    }

    private static LocalDeclarationStatementSyntax CreateHashBucketRefDeclaration(
        string hashVariableName,
        string keyVariableName,
        string bucketRefVariableName,
        string existsVariableName)
    {
        var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.ParseExpression("System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault"))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
            [
                SyntaxFactory.Argument(SyntaxFactory.IdentifierName(hashVariableName)),
                SyntaxFactory.Argument(SyntaxFactory.IdentifierName(keyVariableName)),
                SyntaxFactory.Argument(SyntaxFactory.DeclarationExpression(
                        SyntaxFactory.IdentifierName("var"),
                        SyntaxFactory.SingleVariableDesignation(SyntaxFactory.Identifier(existsVariableName))))
                    .WithRefKindKeyword(SyntaxFactory.Token(SyntaxKind.OutKeyword))
            ])));

        return SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                    .WithVariables(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(bucketRefVariableName)
                            .WithInitializer(SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.RefExpression(invocation))))))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.RefKeyword)));
    }

    private static InvocationExpressionSyntax CreateHashTryGetValueExpression(
        string hashVariableName,
        string keyVariableName,
        string matchesVariableName)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(hashVariableName),
                    SyntaxFactory.IdentifierName("TryGetValue")))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
            [
                SyntaxFactory.Argument(SyntaxFactory.IdentifierName(keyVariableName)),
                SyntaxFactory.Argument(
                        SyntaxFactory.DeclarationExpression(
                            SyntaxFactory.IdentifierName("var"),
                            SyntaxFactory.SingleVariableDesignation(SyntaxFactory.Identifier(matchesVariableName))))
                    .WithRefKindKeyword(SyntaxFactory.Token(SyntaxKind.OutKeyword))
            ])));
    }

    private static TypeSyntax CreateHashTypeSyntax(Type keyType, Type rowType, string? generatedRowTypeName = null)
    {
        return SyntaxFactory.ParseTypeName(CreateHashTypeName(keyType, rowType, generatedRowTypeName));
    }

    private static string CreateHashTypeName(Type keyType, Type rowType, string? generatedRowTypeName = null)
    {
        return $"Dictionary<{EvaluationHelper.GetCastableType(keyType)}, HashJoinBucket<{CreateCastableRowTypeName(rowType, generatedRowTypeName)}>>";
    }

    private static TypeSyntax CreateHashBucketTypeSyntax(Type rowType, string? generatedRowTypeName = null)
    {
        return SyntaxFactory.ParseTypeName($"HashJoinBucket<{CreateCastableRowTypeName(rowType, generatedRowTypeName)}>");
    }

    private static string CreateCastableRowTypeName(Type rowType, string? generatedRowTypeName)
    {
        return string.IsNullOrWhiteSpace(generatedRowTypeName)
            ? EvaluationHelper.GetCastableType(rowType)
            : generatedRowTypeName;
    }
}

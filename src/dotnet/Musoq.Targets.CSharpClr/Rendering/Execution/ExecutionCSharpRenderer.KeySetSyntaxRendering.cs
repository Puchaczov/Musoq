using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static ExpressionStatementSyntax CreateKeySetAddStatement(
        string setVariableName,
        string keyVariableName)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(setVariableName),
                        SyntaxFactory.IdentifierName(nameof(HashSet<>.Add))))
                .WithArgumentList(CreateArgumentList(SyntaxFactory.IdentifierName(keyVariableName))));
    }

    private static InvocationExpressionSyntax CreateKeySetContainsExpression(
        string setVariableName,
        string keyVariableName)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(setVariableName),
                    SyntaxFactory.IdentifierName(nameof(HashSet<>.Contains))))
            .WithArgumentList(CreateArgumentList(SyntaxFactory.IdentifierName(keyVariableName)));
    }

    private static TypeSyntax CreateKeySetTypeSyntax(Type keyType)
    {
        return SyntaxFactory.ParseTypeName(CreateKeySetTypeName(keyType));
    }

    private static TypeSyntax CreateKeySetTypeSyntax(ExecutionTypeRef keyType) =>
        CreateKeySetTypeSyntax(keyType.RequireClrType());

    private static string CreateKeySetTypeName(Type keyType)
    {
        return $"HashSet<{EvaluationHelper.GetCastableType(keyType)}>";
    }

    private static string CreateKeySetTypeName(ExecutionTypeRef keyType) =>
        CreateKeySetTypeName(keyType.RequireClrType());
}

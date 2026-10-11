using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

internal static class TypedBufferSyntax
{
    public static ArrayCreationExpressionSyntax CreateSizedArrayCreation(
        string elementTypeName,
        ExpressionSyntax size)
    {
        return SyntaxFactory.ArrayCreationExpression(
            SyntaxFactory.ArrayType(SyntaxFactory.IdentifierName(elementTypeName))
                .WithRankSpecifiers(SyntaxFactory.SingletonList(
                    SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList(size)))));
    }

    public static ArrayCreationExpressionSyntax CreateSizedArrayCreation(
        Type elementType,
        ExpressionSyntax size)
    {
        return SyntaxFactory.ArrayCreationExpression(
            SyntaxFactory.ArrayType(CreateTypeSyntax(elementType))
                .WithRankSpecifiers(SyntaxFactory.SingletonList(
                    SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList(size)))));
    }

    public static ArrayCreationExpressionSyntax CreateSizedArrayCreation(
        ExecutionTypeRef elementType,
        ExpressionSyntax size) =>
        CreateSizedArrayCreation(elementType.RequireClrType(), size);

    public static ForStatementSyntax CreateIndexedForLoop(
        string indexVariableName,
        ExecutionVariable buffer,
        StatementSyntax body)
    {
        return StatementEmitter.CreateForLoop(
            indexVariableName,
            0,
            SyntaxFactory.BinaryExpression(
                SyntaxKind.LessThanExpression,
                SyntaxFactory.IdentifierName(indexVariableName),
                CreateBufferCountExpression(buffer)),
            SyntaxFactory.PrefixUnaryExpression(
                SyntaxKind.PreIncrementExpression,
                SyntaxFactory.IdentifierName(indexVariableName)),
            body);
    }

    public static MemberAccessExpressionSyntax CreateBufferCountExpression(ExecutionVariable buffer)
    {
        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(buffer.Name),
            SyntaxFactory.IdentifierName("Count"));
    }

    public static ExpressionStatementSyntax CreateArrayAssignment(
        string arrayName,
        string indexVariableName,
        ExpressionSyntax value,
        Type elementType)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                CreateElementAccess(
                    SyntaxFactory.IdentifierName(arrayName),
                    SyntaxFactory.IdentifierName(indexVariableName)),
                SyntaxFactory.CastExpression(
                    CreateTypeSyntax(elementType),
                    SyntaxFactory.ParenthesizedExpression(value))));
    }

}

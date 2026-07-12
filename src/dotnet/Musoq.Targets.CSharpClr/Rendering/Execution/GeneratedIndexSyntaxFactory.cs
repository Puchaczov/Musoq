using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

internal static class GeneratedIndexSyntaxFactory
{
    public static LocalDeclarationStatementSyntax CreateIndexDeclaration(
        string variableName,
        TypeSyntax indexType,
        ExpressionSyntax? capacity)
    {
        var argumentList = capacity == null
            ? SyntaxFactory.ArgumentList()
            : SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(capacity)));
        return SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                .WithVariables(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator(variableName)
                        .WithInitializer(SyntaxFactory.EqualsValueClause(
                            SyntaxFactory.ObjectCreationExpression(indexType)
                                .WithArgumentList(argumentList))))));
    }

    public static ExpressionSyntax CreateRowsCountExpression(string rowsOwnerName)
    {
        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(rowsOwnerName),
                SyntaxFactory.IdentifierName("Rows")),
            SyntaxFactory.IdentifierName("Count"));
    }

    public static ExpressionSyntax CreateCombinedRowsCountExpression(string leftRowsOwnerName, string rightRowsOwnerName)
    {
        return SyntaxFactory.BinaryExpression(
            SyntaxKind.AddExpression,
            CreateRowsCountExpression(leftRowsOwnerName),
            CreateRowsCountExpression(rightRowsOwnerName));
    }
}

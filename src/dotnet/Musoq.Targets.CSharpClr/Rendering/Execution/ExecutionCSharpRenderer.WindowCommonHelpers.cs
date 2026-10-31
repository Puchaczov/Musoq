using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static IReadOnlyList<StatementSyntax> CreateIndexedItemDeclarations(
        ExecutionVariable item,
        ExecutionVariable source,
        ExecutionVariable index,
        ExecutionRowAccessMode accessMode)
    {
        var itemAccess = CreateElementAccess(
            SyntaxFactory.IdentifierName(source.Name),
            SyntaxFactory.IdentifierName(index.Name));

        if (accessMode == ExecutionRowAccessMode.Direct)
            return [CreateLocalDeclaration(CreateIndexedItemTypeSyntax(item, source), item.Name, itemAccess)];

        throw UnsupportedShape.Of($"Indexed row access mode {accessMode}");
    }

    private static TypeSyntax CreateIndexedItemTypeSyntax(
        ExecutionVariable item,
        ExecutionVariable source)
    {
        if (item.Type.RequireClrType() == typeof(Evaluator.Tables.Row) &&
            !string.IsNullOrWhiteSpace(source.GeneratedRowTypeName))
        {
            return SyntaxFactory.ParseTypeName(source.GeneratedRowTypeName);
        }

        return CreateVariableTypeSyntax(item);
    }

    private static Type CreateWindowResultElementType(ExecutionVariable results)
    {
        return results.Type.RequireClrType().GetElementType() ??
               throw new InvalidOperationException($"Window result variable {results.Name} must be an array.");
    }

    private static ExpressionStatementSyntax CreateWindowKeyArrayAssignment(
        ExecutionVariable array,
        string indexVariableName,
        ExpressionSyntax value)
    {
        return CreateArrayAssignment(array.Name, indexVariableName, value, GetArrayElementType(array));
    }

    private static ExpressionStatementSyntax CreateIntArrayAssignment(
        string arrayName,
        string indexVariableName,
        ExpressionSyntax value)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                CreateElementAccess(
                    SyntaxFactory.IdentifierName(arrayName),
                    SyntaxFactory.IdentifierName(indexVariableName)),
                CreateIntCastExpression(value)));
    }

    private static InvocationExpressionSyntax CreateWindowHelperInvocation(
        string helperName,
        params ExpressionSyntax[] arguments)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(WindowFunctionHelpers)),
                    SyntaxFactory.IdentifierName(helperName)))
            .WithArgumentList(CreateArgumentList(arguments));
    }

}

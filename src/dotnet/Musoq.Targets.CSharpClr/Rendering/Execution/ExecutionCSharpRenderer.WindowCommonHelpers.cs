using System.Collections.Generic;
using System.Linq;
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

        if (accessMode == ExecutionRowAccessMode.ExpandoAdapter)
            return [CreateLocalDeclaration(SyntaxFactory.IdentifierName("var"), item.Name, itemAccess)];

        throw UnsupportedShape.Of($"Indexed row access mode {accessMode}");
    }

    private static TypeSyntax CreateIndexedItemTypeSyntax(
        ExecutionVariable item,
        ExecutionVariable source)
    {
        if (item.Type.RequireClrType() == typeof(Musoq.Evaluator.Tables.Row) &&
            !string.IsNullOrWhiteSpace(source.GeneratedRowTypeName))
        {
            return SyntaxFactory.ParseTypeName(source.GeneratedRowTypeName);
        }

        return CreateVariableTypeSyntax(item);
    }

    private static ForStatementSyntax CreateIndexedForLoop(
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

    private static MemberAccessExpressionSyntax CreateBufferCountExpression(ExecutionVariable buffer)
    {
        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(buffer.Name),
            SyntaxFactory.IdentifierName("Count"));
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

    private static ExpressionStatementSyntax CreateArrayAssignment(
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

    private static ArrayCreationExpressionSyntax CreateWindowOrderDescendingArray(
        IReadOnlyList<ExecutionWindowOrderKey> orderKeys)
    {
        return CreateArrayCreation("bool", orderKeys.Select(key => CreateBooleanLiteral(key.Descending)));
    }
}

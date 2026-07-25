using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

internal static class RecursiveCteIdentitySyntaxFactory
{
    public static LocalDeclarationStatementSyntax CreateSeenDeclaration(ExecutionRecursiveCte recursiveCte)
    {
        var fieldTypes = recursiveCte.IdentityFieldIndexes
            .Select(index => recursiveCte.RowShape.Fields[index].Type.RequireClrType())
            .ToArray();
        return GeneratedIndexSyntaxFactory.CreateIndexDeclaration(
            recursiveCte.Seen!.Name,
            SetOperationKeySyntaxFactory.CreateHashSetTypeSyntax(fieldTypes),
            capacity: null);
    }

    public static InvocationExpressionSyntax CreateSeenAddExpression(
        ExecutionRecursiveCteAppend append,
        IReadOnlyList<ExecutionVariable> candidateVariables)
    {
        var keyParts = append.IdentityFieldIndexes
            .Select(index => (ExpressionSyntax)SyntaxFactory.IdentifierName(candidateVariables[index].Name))
            .ToArray();
        var key = keyParts.Length == 1
            ? keyParts[0]
            : SyntaxFactory.TupleExpression(SyntaxFactory.SeparatedList(
                keyParts.Select(SyntaxFactory.Argument)));

        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(append.Seen!.Name),
                    SyntaxFactory.IdentifierName(nameof(HashSet<object>.Add))))
            .WithArgumentList(ExecutionSyntaxFactory.CreateArgumentList(key));
    }
}

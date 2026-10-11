using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Tables;

namespace Musoq.Targets.CSharpClr;

internal sealed class IntersectDistinctSyntax(ExecutionCSharpRenderer renderer, ExecutionRenderContext context)
{
    internal StatementSyntax CreateState(ExecutionSetOperation operation)
    {
        var name = $"{operation.Target.Name}DistinctRows";
        if (operation.Strategy == ExecutionSetOperationStrategy.HashSet)
            return ExecutionCSharpRenderer.CreateSetKeyHashSetDeclaration(name, operation.DistinctFieldTypes.RequireClrTypes(), null);

        var rowType = context.Session.TypedRowBufferVariables.GetValueOrDefault(operation.Left.Name)?.TypeName ?? nameof(Row);
        return CreateLocalDeclaration(SyntaxFactory.IdentifierName("var"), name,
            SyntaxFactory.ObjectCreationExpression(CreateListTypeSyntax(rowType))
                .WithArgumentList(SyntaxFactory.ArgumentList()));
    }

    internal StatementSyntax CreateOutput(ExecutionSetOperation operation, string rowName, StatementSyntax output)
    {
        var name = $"{operation.Target.Name}DistinctRows";
        var completeRow = operation with
        {
            FieldIndexes = operation.DistinctFieldIndexes,
            FieldTypes = operation.DistinctFieldTypes
        };
        if (operation.Strategy == ExecutionSetOperationStrategy.HashSet)
            return SyntaxFactory.IfStatement(
                renderer.CreateHashSetInvocation(name, "Add", rowName, completeRow, operation.Left, context),
                StatementEmitter.CreateBlock(output));

        return SyntaxFactory.IfStatement(
            SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression,
                renderer.CreateSetRowsAnyMatch(SyntaxFactory.IdentifierName(name), $"{name}Existing", rowName,
                    operation.Left, operation.Left, completeRow, context)),
            StatementEmitter.CreateBlock(
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName(name), SyntaxFactory.IdentifierName("Add")))
                    .WithArgumentList(CreateArgumentList(SyntaxFactory.IdentifierName(rowName)))), output));
    }
}

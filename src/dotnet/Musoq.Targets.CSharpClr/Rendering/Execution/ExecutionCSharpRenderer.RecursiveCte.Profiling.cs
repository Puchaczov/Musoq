using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static IEnumerable<StatementSyntax> RenderProfiledRecursiveCte(
        ExecutionPlanOperatorDescriptor descriptor,
        ExecutionRecursiveCte recursiveCte,
        IReadOnlyList<StatementSyntax> statements)
    {
        if (statements.Count == 0 ||
            statements[0] is not LocalDeclarationStatementSyntax declaration ||
            declaration.Declaration.Variables.Count != 1 ||
            declaration.Declaration.Variables[0] is not { Initializer.Value: ObjectCreationExpressionSyntax creation } variable)
        {
            throw new InvalidOperationException(
                $"Recursive CTE '{recursiveCte.Name}' must begin with its typed result-buffer declaration.");
        }

        var resultDeclaration = declaration.WithDeclaration(
            declaration.Declaration
                .WithType(creation.Type)
                .WithVariables(SyntaxFactory.SingletonSeparatedList(variable.WithInitializer(null))));
        var resultInitialization = SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName(variable.Identifier),
                creation));

        return
        [
            ..CreateOperatorRowCounterDeclarations(descriptor, recursiveCte),
            CreateOperatorScopeDeclaration(descriptor),
            resultDeclaration,
            CreateOperatorTryFinallyStatement(
                descriptor,
                recursiveCte,
                [resultInitialization, ..statements.Skip(1)])
        ];
    }
}

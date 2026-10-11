using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

internal static class LibraryWindowLoopSyntax
{
    public static ForStatementSyntax Accumulate(string index, ExpressionSyntax first,
        ExpressionSyntax limit, IEnumerable<StatementSyntax> body)
        => SyntaxFactory.ForStatement(StatementEmitter.CreateBlock(body))
            .WithDeclaration(SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                .WithVariables(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(index)
                    .WithInitializer(SyntaxFactory.EqualsValueClause(first)))))
            .WithCondition(limit)
            .WithIncrementors(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                SyntaxFactory.PostfixUnaryExpression(SyntaxKind.PostIncrementExpression, SyntaxFactory.IdentifierName(index))));
}

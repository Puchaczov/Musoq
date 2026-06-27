using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.Visitors.CodeGeneration;

internal static class QueryEmitter
{
    public static StatementSyntax GeneratePhaseChangeStatement(string queryId, QueryPhase phase)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName("OnPhaseChanged"))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList([
                            SyntaxFactory.Argument(
                                SyntaxFactory.LiteralExpression(
                                    SyntaxKind.StringLiteralExpression,
                                    SyntaxFactory.Literal(queryId))),
                            SyntaxFactory.Argument(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName(nameof(QueryPhase)),
                                    SyntaxFactory.IdentifierName(phase.ToString())))
                        ]))));
    }

    public static StatementSyntax GenerateCancellationCheck()
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("token"),
                    SyntaxFactory.IdentifierName(nameof(CancellationToken.ThrowIfCancellationRequested)))));
    }
}

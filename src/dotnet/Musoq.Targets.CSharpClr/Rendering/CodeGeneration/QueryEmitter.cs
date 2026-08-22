using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator;

namespace Musoq.Targets.CSharpClr.Rendering.CodeGeneration;

internal static class QueryEmitter
{
    public static StatementSyntax GeneratePhaseChangeStatement(string queryId, QueryPhase phase)
    {
        return GeneratePhaseChangeStatement(queryId, phase, useInstanceHandler: false);
    }

    public static StatementSyntax GeneratePhaseChangeStatement(
        string queryId,
        QueryPhase phase,
        bool useInstanceHandler)
    {
        ExpressionSyntax handler = useInstanceHandler
            ? SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.ThisExpression(),
                SyntaxFactory.IdentifierName("OnPhaseChanged"))
            : SyntaxFactory.IdentifierName("OnPhaseChanged");
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(handler)
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

    public static StatementSyntax GenerateCompletionAndEndStatement(string queryId)
    {
        return SyntaxFactory.TryStatement()
            .WithBlock(SyntaxFactory.Block(
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.ConditionalAccessExpression(
                        SyntaxFactory.IdentifierName("__musoqProgressContext"),
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberBindingExpression(
                                SyntaxFactory.IdentifierName(nameof(QueryRunContext.CompleteQueryProgress))))))))
            .WithFinally(SyntaxFactory.FinallyClause(
                SyntaxFactory.Block(GeneratePhaseChangeStatement(queryId, QueryPhase.End))));
    }
}

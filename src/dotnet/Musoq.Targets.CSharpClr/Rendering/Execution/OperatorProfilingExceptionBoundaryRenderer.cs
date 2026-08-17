using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Diagnostics;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private const string ProfileScopeDepthVariableName = "__profileScopeDepth";
    private const string ProfileExceptionVariableName = "__profileException";

    private BlockSyntax CreateProfileExceptionBoundaryBlock(
        IEnumerable<StatementSyntax> statements,
        ExecutionRenderContext context,
        bool includeExceptionBoundary = true)
    {
        var renderedStatements = statements.ToArray();
        if (!IsOperatorProfilingEnabledFor(context) || !includeExceptionBoundary)
            return StatementEmitter.CreateBlock(renderedStatements);

        return StatementEmitter.CreateBlock(
            CreateProfileScopeDepthDeclaration(),
            CreateProfileExceptionBoundary(renderedStatements));
    }

    private BlockSyntax CreateProfiledHelperBody(
        IEnumerable<StatementSyntax> statements,
        ExecutionRenderContext context)
    {
        var helperStatements = statements.ToArray();
        var usage = CollectOperatorProfileUsage(helperStatements);
        var renderedStatements = CreateOperatorHandleDeclarations(usage, context)
            .Concat(CreateOperatorCounterDeclarations(usage, context))
            .Concat(helperStatements)
            .ToArray();

        return CreateProfileExceptionBoundaryBlock(
            AddOperatorCounterFlushesBeforeTopLevelReturns(renderedStatements, usage, context, appendAtEnd: true),
            context);
    }

    private IEnumerable<StatementSyntax> AddOperatorCounterFlushesBeforeTopLevelReturns(
        IReadOnlyList<StatementSyntax> statements,
        OperatorProfileUsage usage,
        ExecutionRenderContext context,
        bool appendAtEnd)
    {
        var hasReturn = false;

        foreach (var statement in statements)
        {
            if (statement is ReturnStatementSyntax)
            {
                hasReturn = true;
                foreach (var flushStatement in CreateOperatorCounterFlushStatements(usage, context))
                    yield return flushStatement;
            }

            yield return statement;
        }

        if (!appendAtEnd || hasReturn)
            yield break;

        foreach (var flushStatement in CreateOperatorCounterFlushStatements(usage, context))
            yield return flushStatement;
    }

    private static LocalDeclarationStatementSyntax CreateProfileScopeDepthDeclaration()
    {
        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            ProfileScopeDepthVariableName,
            SyntaxFactory.BinaryExpression(
                SyntaxKind.CoalesceExpression,
                SyntaxFactory.ConditionalAccessExpression(
                    SyntaxFactory.IdentifierName(ProfileRecorderVariableName),
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberBindingExpression(
                            SyntaxFactory.IdentifierName(nameof(QueryProfileRecorder.GetCurrentOperatorScopeDepth))))),
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(0))));
    }

    private static TryStatementSyntax CreateProfileExceptionBoundary(IReadOnlyList<StatementSyntax> statements)
    {
        return SyntaxFactory.TryStatement()
            .WithBlock(StatementEmitter.CreateBlock(statements))
            .WithCatches(SyntaxFactory.SingletonList(
                SyntaxFactory.CatchClause()
                    .WithDeclaration(SyntaxFactory.CatchDeclaration(CreateTypeSyntax(typeof(Exception)))
                        .WithIdentifier(SyntaxFactory.Identifier(ProfileExceptionVariableName)))
                    .WithFilter(SyntaxFactory.CatchFilterClause(CreateProfileExceptionFilter()))
                    .WithBlock(StatementEmitter.CreateBlock(
                        CreateDisposeActiveOperatorScopesStatement(),
                        SyntaxFactory.ThrowStatement()))));
    }

    private static ExpressionSyntax CreateProfileExceptionFilter()
    {
        return SyntaxFactory.BinaryExpression(
            SyntaxKind.LogicalAndExpression,
            SyntaxFactory.BinaryExpression(
                SyntaxKind.NotEqualsExpression,
                SyntaxFactory.IdentifierName(ProfileRecorderVariableName),
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(ProfileRecorderVariableName),
                        SyntaxFactory.IdentifierName(nameof(QueryProfileRecorder.RecordActiveOperatorException))))
                .WithArgumentList(CreateArgumentList(
                    SyntaxFactory.IdentifierName(ProfileExceptionVariableName),
                    SyntaxFactory.IdentifierName(ProfileScopeDepthVariableName))));
    }

    private static StatementSyntax CreateDisposeActiveOperatorScopesStatement()
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(ProfileRecorderVariableName),
                        SyntaxFactory.IdentifierName(nameof(QueryProfileRecorder.DisposeActiveOperatorScopes))))
                .WithArgumentList(CreateArgumentList(
                    SyntaxFactory.IdentifierName(ProfileScopeDepthVariableName))));
    }
}

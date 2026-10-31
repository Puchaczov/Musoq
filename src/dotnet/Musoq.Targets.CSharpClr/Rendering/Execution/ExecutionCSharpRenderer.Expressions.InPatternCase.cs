using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{

    private ExpressionSyntax RenderInCheck(ExecutionInCheck inCheck, ExecutionRenderContext context) =>
        new ExecutionMembershipSyntaxFactory(
            expression => RenderExpression(expression, context),
            set => TryGetConstantInSetFieldName(set, context, out var name) ? name : null,
            RenderLiteral, ++_membershipExpressionSequence).Render(inCheck);

    private InvocationExpressionSyntax RenderPatternMatch(
        ExecutionPatternMatch patternMatch,
        ExecutionRenderContext context)
        => ExecutionLikeSyntaxFactory.Render(
            patternMatch,
            expression => RenderExpression(expression, context),
            context.Session.RegexMatchTimeout);

    private ExpressionSyntax RenderCaseWhen(ExecutionCaseWhen caseWhen, ExecutionRenderContext context)
    {
        var fallback = caseWhen.ElseExpression == null
            ? CreateMissingCaseElseExpression(caseWhen.ReturnType.RequireClrType())
            : CastIfNeeded(RenderExpression(caseWhen.ElseExpression, context), caseWhen.ReturnType);

        for (var index = caseWhen.Branches.Count - 1; index >= 0; index--)
        {
            var branch = caseWhen.Branches[index];
            fallback = SyntaxFactory.ParenthesizedExpression(
                SyntaxFactory.ConditionalExpression(
                    this.RenderBooleanCondition(branch.Condition, context),
                    CastIfNeeded(RenderExpression(branch.Result, context), caseWhen.ReturnType),
                    fallback));
        }

        return fallback;
    }

    private ExpressionSyntax RenderCoalesce(ExecutionCoalesce coalesce, ExecutionRenderContext context)
    {
        if (coalesce.Expressions.Count == 0)
            throw new NotSupportedException("Coalesce must contain at least one expression.");

        var current = RenderExpression(coalesce.Expressions[^1], context);

        for (var index = coalesce.Expressions.Count - 2; index >= 0; index--)
        {
            current = SyntaxFactory.BinaryExpression(
                SyntaxKind.CoalesceExpression,
                RenderExpression(coalesce.Expressions[index], context),
                current);
        }

        return SyntaxFactory.ParenthesizedExpression(current);
    }

    private static ExpressionSyntax CreateMissingCaseElseExpression(Type returnType)
    {
        if (!returnType.IsValueType || Nullable.GetUnderlyingType(returnType) is not null)
        {
            return SyntaxFactory.CastExpression(
                CreateTypeSyntax(returnType),
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));
        }

        return SyntaxFactory.DefaultExpression(CreateTypeSyntax(returnType));
    }
}

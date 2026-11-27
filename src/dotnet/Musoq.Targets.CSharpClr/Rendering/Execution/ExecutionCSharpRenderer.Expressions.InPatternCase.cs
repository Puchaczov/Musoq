using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private ExpressionSyntax RenderInCheck(ExecutionInCheck inCheck, ExecutionRenderContext context) =>
        new ExecutionMembershipSyntaxFactory(
            expression => RenderExpression(expression, context),
            set => TryGetConstantInSetFieldName(set, context, out var name) ? name : null,
            RenderLiteral,
            ++_membershipExpressionSequence
        ).Render(inCheck);

    private InvocationExpressionSyntax RenderPatternMatch(
        ExecutionPatternMatch patternMatch,
        ExecutionRenderContext context
    ) =>
        ExecutionLikeSyntaxFactory.Render(
            patternMatch,
            expression => RenderExpression(expression, context),
            context.Session.RegexMatchTimeout
        );

    private ExpressionSyntax RenderCaseWhen(ExecutionCaseWhen caseWhen, ExecutionRenderContext context)
    {
        var generated = caseWhen.InterpretationShape?.GetGeneratedTypeName();
        ExpressionSyntax Result(ExecutionExpression branch)
        {
            var value = RenderExpression(branch, context);
            return generated == null
                ? CastIfNeeded(value, caseWhen.ReturnType)
                : SyntaxFactory.CastExpression(SyntaxFactory.ParseTypeName(generated), value);
        }

        var fallback =
            caseWhen.ElseExpression == null
                ? generated == null
                    ? CreateMissingCaseElseExpression(caseWhen.ReturnType.RequireClrType())
                    : SyntaxFactory.DefaultExpression(SyntaxFactory.ParseTypeName(generated))
                : Result(caseWhen.ElseExpression);

        for (var index = caseWhen.Branches.Count - 1; index >= 0; index--)
        {
            var branch = caseWhen.Branches[index];
            fallback = SyntaxFactory.ParenthesizedExpression(
                SyntaxFactory.ConditionalExpression(
                    this.RenderBooleanCondition(branch.Condition, context),
                    Result(branch.Result),
                    fallback
                )
            );
        }

        return fallback;
    }

    private ExpressionSyntax RenderCoalesce(ExecutionCoalesce coalesce, ExecutionRenderContext context)
    {
        if (coalesce.Expressions.Count == 0)
            throw new NotSupportedException("Coalesce must contain at least one expression.");

        var generated = coalesce.InterpretationShape?.GetGeneratedTypeName();
        ExpressionSyntax Result(ExecutionExpression operand)
        {
            var value = RenderExpression(operand, context);
            return generated == null
                ? value
                : SyntaxFactory.CastExpression(SyntaxFactory.ParseTypeName(generated), value);
        }

        var current = Result(coalesce.Expressions[^1]);

        for (var index = coalesce.Expressions.Count - 2; index >= 0; index--)
        {
            current = SyntaxFactory.BinaryExpression(
                SyntaxKind.CoalesceExpression,
                Result(coalesce.Expressions[index]),
                current
            );
        }

        return SyntaxFactory.ParenthesizedExpression(current);
    }

    private static ExpressionSyntax CreateMissingCaseElseExpression(Type returnType)
    {
        if (!returnType.IsValueType || Nullable.GetUnderlyingType(returnType) is not null)
        {
            return SyntaxFactory.CastExpression(
                CreateTypeSyntax(returnType),
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
            );
        }

        return SyntaxFactory.DefaultExpression(CreateTypeSyntax(returnType));
    }
}

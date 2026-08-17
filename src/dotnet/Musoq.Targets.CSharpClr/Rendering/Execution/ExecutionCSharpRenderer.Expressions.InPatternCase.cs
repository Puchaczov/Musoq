using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{

    private ExpressionSyntax RenderInCheck(ExecutionInCheck inCheck, ExecutionRenderContext context)
    {
        if (inCheck.Values.Count == 0)
            return SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);

        if (inCheck.ConstantSet is { Kind: ExecutionConstantInSetKind.Switch } constantSet)
            return RenderConstantSwitchInCheck(constantSet, inCheck.Expression, context);

        if (inCheck.ConstantSet != null &&
            TryGetConstantInSetFieldName(inCheck.ConstantSet, context, out var fieldName))
        {
            return inCheck.ConstantSet.Kind is ExecutionConstantInSetKind.HashSet or ExecutionConstantInSetKind.FrozenSet
                ? RenderConstantHashSetInCheck(fieldName, inCheck.Expression, context)
                : RenderConstantArrayInCheck(fieldName, inCheck.Expression, context);
        }

        var indexOf = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(Array)),
                SyntaxFactory.IdentifierName(nameof(Array.IndexOf))))
            .WithArgumentList(CreateArgumentList(
                CreateArrayCreation(inCheck.Expression.ReturnType, inCheck.Values.Select(value => RenderExpression(value, context))),
                RenderExpression(inCheck.Expression, context)));

        return SyntaxFactory.ParenthesizedExpression(
            SyntaxFactory.BinaryExpression(
                SyntaxKind.GreaterThanOrEqualExpression,
                indexOf,
            CreateIntLiteral(0)));
    }

    private ParenthesizedExpressionSyntax RenderConstantSwitchInCheck(
        ExecutionConstantInSet constantSet,
        ExecutionExpression expression,
        ExecutionRenderContext context)
    {
        var matchPattern = CreateConstantInSwitchPattern(constantSet.Values);
        var arms = SyntaxFactory.SeparatedList(
        [
            SyntaxFactory.SwitchExpressionArm(
                matchPattern,
                SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)),
            SyntaxFactory.SwitchExpressionArm(
                SyntaxFactory.DiscardPattern(),
                SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))
        ]);

        return SyntaxFactory.ParenthesizedExpression(
            SyntaxFactory.SwitchExpression(RenderExpression(expression, context), arms));
    }

    private static PatternSyntax CreateConstantInSwitchPattern(IReadOnlyList<ExecutionConstantValue> values)
    {
        return values
            .Distinct()
            .Select(value => (PatternSyntax)SyntaxFactory.ConstantPattern(RenderLiteral(value)))
            .Aggregate(static (left, right) => SyntaxFactory.BinaryPattern(SyntaxKind.OrPattern, left, right));
    }

    private ParenthesizedExpressionSyntax RenderConstantArrayInCheck(
        string fieldName,
        ExecutionExpression expression,
        ExecutionRenderContext context)
    {
        return SyntaxFactory.ParenthesizedExpression(
            SyntaxFactory.BinaryExpression(
                SyntaxKind.GreaterThanOrEqualExpression,
                CreateArrayIndexOfExpression(SyntaxFactory.IdentifierName(fieldName), RenderExpression(expression, context)),
                CreateIntLiteral(0)));
    }

    private ParenthesizedExpressionSyntax RenderConstantHashSetInCheck(
        string fieldName,
        ExecutionExpression expression,
        ExecutionRenderContext context)
    {
        return SyntaxFactory.ParenthesizedExpression(
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(fieldName),
                        SyntaxFactory.IdentifierName(nameof(HashSet<object>.Contains))))
                .WithArgumentList(CreateArgumentList(RenderExpression(expression, context))));
    }

    private static InvocationExpressionSyntax CreateArrayIndexOfExpression(
        ExpressionSyntax values,
        ExpressionSyntax value)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(Array)),
                    SyntaxFactory.IdentifierName(nameof(Array.IndexOf))))
            .WithArgumentList(CreateArgumentList(values, value));
    }

    private InvocationExpressionSyntax RenderPatternMatch(
        ExecutionPatternMatch patternMatch,
        ExecutionRenderContext context)
    {
        var methodName = patternMatch.Kind switch
        {
            PatternKind.Like => nameof(Operators.Like),
            PatternKind.RLike => nameof(Operators.RLike),
            _ => throw UnsupportedShape.Of($"Pattern kind {patternMatch.Kind}")
        };

        var operatorsInstance = SyntaxFactory.ObjectCreationExpression(CreateTypeSyntax(typeof(Operators)))
            .WithArgumentList(SyntaxFactory.ArgumentList());

        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    operatorsInstance,
                    SyntaxFactory.IdentifierName(methodName)))
            .WithArgumentList(CreateArgumentList(
                RenderExpression(patternMatch.Expression, context),
                RenderExpression(patternMatch.Pattern, context)));
    }

    private ParenthesizedExpressionSyntax RenderBetween(ExecutionBetween between, ExecutionRenderContext context)
    {
        var greaterOrEqual = RenderBinary(new ExecutionBinary(
            BinaryOpKind.GreaterOrEqual,
            between.Expression,
            between.Low,
            typeof(bool)), context);
        var lessOrEqual = RenderBinary(new ExecutionBinary(
            BinaryOpKind.LessOrEqual,
            between.Expression,
            between.High,
            typeof(bool)), context);

        return SyntaxFactory.ParenthesizedExpression(
            SyntaxFactory.BinaryExpression(
                SyntaxKind.LogicalAndExpression,
                greaterOrEqual,
                lessOrEqual));
    }

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
                    RenderExpression(branch.Condition, context),
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

        return current;
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

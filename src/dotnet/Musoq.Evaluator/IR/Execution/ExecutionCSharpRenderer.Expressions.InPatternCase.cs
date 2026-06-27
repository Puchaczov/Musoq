using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{

    private ExpressionSyntax RenderInCheck(ExecutionInCheck inCheck)
    {
        if (inCheck.Values.Count == 0)
            return SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);

        if (inCheck.ConstantSet is { Kind: ExecutionConstantInSetKind.Switch } constantSet)
            return RenderConstantSwitchInCheck(constantSet, inCheck.Expression);

        if (inCheck.ConstantSet != null &&
            _constantInSetFieldNames.TryGetValue(inCheck.ConstantSet, out var fieldName))
        {
            return inCheck.ConstantSet.Kind is ExecutionConstantInSetKind.HashSet or ExecutionConstantInSetKind.FrozenSet
                ? RenderConstantHashSetInCheck(fieldName, inCheck.Expression)
                : RenderConstantArrayInCheck(fieldName, inCheck.Expression);
        }

        var indexOf = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(Array)),
                    SyntaxFactory.IdentifierName(nameof(Array.IndexOf))))
            .WithArgumentList(CreateArgumentList(
                CreateArrayCreation(inCheck.Expression.ReturnType, inCheck.Values.Select(RenderExpression)),
                RenderExpression(inCheck.Expression)));

        return SyntaxFactory.ParenthesizedExpression(
            SyntaxFactory.BinaryExpression(
                SyntaxKind.GreaterThanOrEqualExpression,
                indexOf,
            CreateIntLiteral(0)));
    }

    private ParenthesizedExpressionSyntax RenderConstantSwitchInCheck(
        ExecutionConstantInSet constantSet,
        ExecutionExpression expression)
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
            SyntaxFactory.SwitchExpression(RenderExpression(expression), arms));
    }

    private static PatternSyntax CreateConstantInSwitchPattern(IReadOnlyList<object?> values)
    {
        return values
            .Distinct()
            .Select(value => (PatternSyntax)SyntaxFactory.ConstantPattern(RenderLiteral(value)))
            .Aggregate(static (left, right) => SyntaxFactory.BinaryPattern(SyntaxKind.OrPattern, left, right));
    }

    private ParenthesizedExpressionSyntax RenderConstantArrayInCheck(string fieldName, ExecutionExpression expression)
    {
        return SyntaxFactory.ParenthesizedExpression(
            SyntaxFactory.BinaryExpression(
                SyntaxKind.GreaterThanOrEqualExpression,
                CreateArrayIndexOfExpression(SyntaxFactory.IdentifierName(fieldName), RenderExpression(expression)),
                CreateIntLiteral(0)));
    }

    private ParenthesizedExpressionSyntax RenderConstantHashSetInCheck(string fieldName, ExecutionExpression expression)
    {
        return SyntaxFactory.ParenthesizedExpression(
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(fieldName),
                        SyntaxFactory.IdentifierName(nameof(HashSet<>.Contains))))
                .WithArgumentList(CreateArgumentList(RenderExpression(expression))));
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

    private InvocationExpressionSyntax RenderPatternMatch(ExecutionPatternMatch patternMatch)
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
                RenderExpression(patternMatch.Expression),
                RenderExpression(patternMatch.Pattern)));
    }

    private ParenthesizedExpressionSyntax RenderBetween(ExecutionBetween between)
    {
        var greaterOrEqual = RenderBinary(new ExecutionBinary(
            BinaryOpKind.GreaterOrEqual,
            between.Expression,
            between.Low,
            typeof(bool)));
        var lessOrEqual = RenderBinary(new ExecutionBinary(
            BinaryOpKind.LessOrEqual,
            between.Expression,
            between.High,
            typeof(bool)));

        return SyntaxFactory.ParenthesizedExpression(
            SyntaxFactory.BinaryExpression(
                SyntaxKind.LogicalAndExpression,
                greaterOrEqual,
                lessOrEqual));
    }

    private ExpressionSyntax RenderCaseWhen(ExecutionCaseWhen caseWhen)
    {
        var fallback = caseWhen.ElseExpression == null
            ? CreateMissingCaseElseExpression(caseWhen.ReturnType)
            : CastIfNeeded(RenderExpression(caseWhen.ElseExpression), caseWhen.ReturnType);

        for (var index = caseWhen.Branches.Count - 1; index >= 0; index--)
        {
            var branch = caseWhen.Branches[index];
            fallback = SyntaxFactory.ParenthesizedExpression(
                SyntaxFactory.ConditionalExpression(
                    RenderExpression(branch.Condition),
                    CastIfNeeded(RenderExpression(branch.Result), caseWhen.ReturnType),
                    fallback));
        }

        return fallback;
    }

    private ExpressionSyntax RenderCoalesce(ExecutionCoalesce coalesce)
    {
        if (coalesce.Expressions.Count == 0)
            throw new NotSupportedException("Coalesce must contain at least one expression.");

        var current = RenderExpression(coalesce.Expressions[^1]);

        for (var index = coalesce.Expressions.Count - 2; index >= 0; index--)
        {
            current = SyntaxFactory.BinaryExpression(
                SyntaxKind.CoalesceExpression,
                RenderExpression(coalesce.Expressions[index]),
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

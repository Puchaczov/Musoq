using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

internal sealed partial class ExecutionMembershipSyntaxFactory(
    Func<ExecutionExpression, ExpressionSyntax> renderExpression,
    Func<ExecutionConstantInSet, string?> resolveConstantField,
    Func<ExecutionConstantValue, ExpressionSyntax> renderLiteral,
    int variableSequence)
{
    public ExpressionSyntax Render(ExecutionInCheck inCheck)
    {
        if (inCheck.UsesSqlNullSemantics && inCheck.Values.Count != 0)
            return RenderSqlInCheck(inCheck);

        if (inCheck.Values.Count == 0)
        {
            return SyntaxFactory.LiteralExpression(
                inCheck.IsNegated ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression);
        }

        if (inCheck.ConstantSet is { Kind: ExecutionConstantInSetKind.Switch } constantSet)
            return ApplyInNegation(
                RenderConstantSwitchInCheck(constantSet, inCheck.Expression),
                inCheck.IsNegated);

        if (inCheck.ConstantSet != null &&
            resolveConstantField(inCheck.ConstantSet) is { } fieldName)
        {
            var constantMatch = inCheck.ConstantSet.Kind is ExecutionConstantInSetKind.HashSet or ExecutionConstantInSetKind.FrozenSet
                ? RenderConstantHashSetInCheck(fieldName, inCheck.Expression)
                : RenderConstantArrayInCheck(fieldName, inCheck.Expression);
            return ApplyInNegation(constantMatch, inCheck.IsNegated);
        }

        return ApplyInNegation(RenderTypedMembership(inCheck), inCheck.IsNegated);
    }

    private ExpressionSyntax RenderSqlInCheck(ExecutionInCheck inCheck)
    {
        var operand = renderExpression(inCheck.Expression);
        var parameter = SyntaxFactory.IdentifierName("membershipValue");
        ExpressionSyntax? lookup = null;
        if (inCheck.ConstantSet is { Kind: ExecutionConstantInSetKind.Switch } switchSet)
            lookup = RenderConstantSwitchInCheck(switchSet, inCheck.Expression, parameter);
        else if (inCheck.ConstantSet != null && resolveConstantField(inCheck.ConstantSet) is { } fieldName)
            lookup = inCheck.ConstantSet.Kind is ExecutionConstantInSetKind.HashSet or ExecutionConstantInSetKind.FrozenSet
                ? RenderConstantHashSetInCheck(fieldName, inCheck.Expression, parameter)
                : RenderConstantArrayInCheck(fieldName, inCheck.Expression, parameter);

        if (lookup != null && operand.IsKind(SyntaxKind.NullLiteralExpression))
            operand = SyntaxFactory.CastExpression(CreateTypeSyntax(inCheck.ConstantSet!.ElementType), operand);

        if (lookup is null)
            return ApplyInNegation(RenderTypedMembership(inCheck), inCheck.IsNegated);

        var arguments = CreateArgumentList(operand,
            SyntaxFactory.SimpleLambdaExpression(SyntaxFactory.Parameter(parameter.Identifier), lookup),
            SyntaxFactory.LiteralExpression(inCheck.ConstantSet!.Values.Any(value => value.Kind == ExecutionConstantKind.Null)
                ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression));
        var result = SyntaxFactory.InvocationExpression(SyntaxFactory.ParseExpression("Musoq.Evaluator.Operators.SqlIn"))
            .WithArgumentList(arguments);
        return ApplyInNegation(result, inCheck.IsNegated);
    }

    private static ExpressionSyntax ApplyInNegation(ExpressionSyntax expression, bool isNegated)
    {
        return isNegated
            ? SyntaxFactory.PrefixUnaryExpression(
                SyntaxKind.LogicalNotExpression,
                SyntaxFactory.ParenthesizedExpression(expression))
            : expression;
    }

    private ParenthesizedExpressionSyntax RenderConstantSwitchInCheck(
        ExecutionConstantInSet constantSet,
        ExecutionExpression expression,
        ExpressionSyntax? input = null)
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
            SyntaxFactory.SwitchExpression(input ?? renderExpression(expression), arms));
    }

    private PatternSyntax CreateConstantInSwitchPattern(IReadOnlyList<ExecutionConstantValue> values)
    {
        return values
            .Distinct()
            .Select(value => (PatternSyntax)SyntaxFactory.ConstantPattern(renderLiteral(value)))
            .Aggregate(static (left, right) => SyntaxFactory.BinaryPattern(SyntaxKind.OrPattern, left, right));
    }

    private ParenthesizedExpressionSyntax RenderConstantArrayInCheck(
        string fieldName,
        ExecutionExpression expression,
        ExpressionSyntax? input = null)
    {
        return SyntaxFactory.ParenthesizedExpression(
            SyntaxFactory.BinaryExpression(
                SyntaxKind.GreaterThanOrEqualExpression,
                CreateArrayIndexOfExpression(SyntaxFactory.IdentifierName(fieldName), input ?? renderExpression(expression)),
                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0))));
    }

    private ParenthesizedExpressionSyntax RenderConstantHashSetInCheck(
        string fieldName,
        ExecutionExpression expression,
        ExpressionSyntax? input = null)
    {
        return SyntaxFactory.ParenthesizedExpression(
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(fieldName),
                        SyntaxFactory.IdentifierName(nameof(HashSet<object>.Contains))))
                .WithArgumentList(CreateArgumentList(input ?? renderExpression(expression))));
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

}

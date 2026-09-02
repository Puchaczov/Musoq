using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Targets.CSharpClr;

internal static class ExecutionBooleanSemantics
{
    internal static bool RequiresSqlNullPropagation(this ExecutionBinary binary) =>
        binary.UsesSqlNullSemantics && ExecutionSyntaxFactory.IsSqlComparison(binary.Kind) &&
        (ExecutionSyntaxFactory.CanExecutionExpressionBeNull(binary.Left) ||
         ExecutionSyntaxFactory.CanExecutionExpressionBeNull(binary.Right));

    internal static bool RequiresNullableBoolean(this ExecutionExpression expression) =>
        ExecutionSyntaxFactory.RequiresNullableBoolean(expression);

    internal static ExpressionSyntax RenderBooleanCondition(
        this ExecutionCSharpRenderer renderer,
        ExecutionExpression expression,
        ExecutionRenderContext context) =>
        ExecutionSyntaxFactory.CreateBooleanCondition(
            renderer.RenderNullableOperand(expression, context),
            expression.RequiresNullableBoolean());

    internal static ExpressionSyntax RenderNullableOperand(
        this ExecutionCSharpRenderer renderer,
        ExecutionExpression expression,
        ExecutionRenderContext context)
    {
        var rendered = renderer.RenderExpression(expression, context);
        return expression is ExecutionLiteral { Value.Kind: ExecutionConstantKind.Null }
            ? SyntaxFactory.CastExpression(CreateTypeSyntax(typeof(bool?)), rendered)
            : rendered;
    }

    internal static ExpressionSyntax RenderNullableLogical(
        this ExecutionCSharpRenderer renderer,
        ExecutionBinary binary,
        ExecutionRenderContext context) =>
        SyntaxFactory.ParenthesizedExpression(SyntaxFactory.BinaryExpression(
            binary.Kind == BinaryOpKind.And ? SyntaxKind.BitwiseAndExpression : SyntaxKind.BitwiseOrExpression,
            renderer.RenderNullableOperand(binary.Left, context), renderer.RenderNullableOperand(binary.Right, context)));

    internal static ExpressionSyntax RenderSqlNullComparison(
        this ExecutionCSharpRenderer renderer,
        ExecutionBinary binary,
        ExecutionRenderContext context) =>
        SyntaxFactory.ParenthesizedExpression(ExecutionSyntaxFactory.CreateSqlNullComparison(
            binary,
            renderer.RenderExpression(binary.Left, context),
            renderer.RenderExpression(binary.Right, context)));

    internal static ParenthesizedExpressionSyntax RenderBetween(
        this ExecutionCSharpRenderer renderer,
        ExecutionBetween between,
        ExecutionRenderContext context)
    {
        var greaterOrEqual = CreateBetweenComparison(BinaryOpKind.GreaterOrEqual, between.Expression, between.Low);
        var lessOrEqual = CreateBetweenComparison(BinaryOpKind.LessOrEqual, between.Expression, between.High);
        if (between.RequiresNullableBoolean())
        {
            return (ParenthesizedExpressionSyntax)renderer.RenderNullableLogical(
                new ExecutionBinary(BinaryOpKind.And, greaterOrEqual, lessOrEqual, typeof(bool?)), context);
        }

        return SyntaxFactory.ParenthesizedExpression(SyntaxFactory.BinaryExpression(
            SyntaxKind.LogicalAndExpression,
            renderer.RenderExpression(greaterOrEqual, context),
            renderer.RenderExpression(lessOrEqual, context)));
    }

    private static ExecutionBinary CreateBetweenComparison(
        BinaryOpKind kind,
        ExecutionExpression expression,
        ExecutionExpression bound) =>
        new(kind, expression, bound, typeof(bool)) { UsesSqlNullSemantics = true };
}

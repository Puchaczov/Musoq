using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private ExpressionSyntax RenderBinary(ExecutionBinary binary, ExecutionRenderContext context)
    {
        if (RequiresStringComparison(binary))
            return RenderStringComparison(binary, context);

        if (TryRenderCharStringEquality(binary, context, out var charStringEquality))
            return charStringEquality;

        if (IsNullSafeDistinctComparison(binary.Kind))
            return RenderEquality(binary, context);

        if (RequiresNullableTemporalSubtraction(binary))
            return RenderNullableTemporalSubtractionOrDefault(binary, context);

        var expression = SyntaxFactory.ParenthesizedExpression(
            SyntaxFactory.BinaryExpression(
                GetBinaryExpressionKind(binary.Kind),
                RenderExpression(binary.Left, context),
                RenderExpression(binary.Right, context)));

        return RequiresBinaryResultCast(binary)
            ? CastIfNeeded(expression, binary.ReturnType)
            : expression;
    }

    private static bool RequiresBinaryResultCast(ExecutionBinary binary)
    {
        return binary.ReturnType.RequireClrType().IsValueType &&
               binary.ReturnType.RequireClrType() != typeof(bool) &&
               (ContainsMethodCall(binary.Left) ||
                ContainsMethodCall(binary.Right) ||
                IsNullableValueType(binary.Left.ReturnType) ||
                IsNullableValueType(binary.Right.ReturnType));
    }

    private InvocationExpressionSyntax RenderNullableTemporalSubtractionOrDefault(
        ExecutionBinary binary,
        ExecutionRenderContext context)
    {
        return SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                RenderNullableTemporalSubtractionValue(binary, context),
                SyntaxFactory.IdentifierName(nameof(Nullable<>.GetValueOrDefault))));
    }

    private ParenthesizedExpressionSyntax RenderNullableTemporalSubtractionValue(
        ExecutionBinary binary,
        ExecutionRenderContext context)
    {
        return SyntaxFactory.ParenthesizedExpression(
            SyntaxFactory.BinaryExpression(
                SyntaxKind.SubtractExpression,
                RenderExpression(binary.Left, context),
                RenderExpression(binary.Right, context)));
    }

    private ParenthesizedExpressionSyntax RenderStringComparison(ExecutionBinary binary, ExecutionRenderContext context)
    {
        var comparison = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("string"),
                    SyntaxFactory.IdentifierName(nameof(string.Compare))))
            .WithArgumentList(CreateArgumentList(
                RenderExpression(binary.Left, context),
                RenderExpression(binary.Right, context),
                CreateOrdinalStringComparisonExpression()));

        return SyntaxFactory.ParenthesizedExpression(
            SyntaxFactory.BinaryExpression(
                GetBinaryExpressionKind(binary.Kind),
                comparison,
                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0))));
    }

    private static bool RequiresStringComparison(ExecutionBinary binary)
    {
        return IsRelationalComparison(binary.Kind) &&
               binary.Left.ReturnType.RequireClrType() == typeof(string) &&
               binary.Right.ReturnType.RequireClrType() == typeof(string);
    }

    private bool TryRenderCharStringEquality(
        ExecutionBinary binary,
        ExecutionRenderContext context,
        [NotNullWhen(true)] out ExpressionSyntax? result)
    {
        result = null;
        if (binary.Kind != BinaryOpKind.Equal &&
            binary.Kind != BinaryOpKind.NotEqual &&
            !IsNullSafeDistinctComparison(binary.Kind))
            return false;

        var leftIsChar = binary.Left.ReturnType.RequireClrType() == typeof(char);
        var rightIsChar = binary.Right.ReturnType.RequireClrType() == typeof(char);
        var leftStringLiteral = binary.Left as ExecutionLiteral;
        var rightStringLiteral = binary.Right as ExecutionLiteral;
        var leftIsStringLiteral = leftStringLiteral is not null && leftStringLiteral.ReturnType.RequireClrType() == typeof(string);
        var rightIsStringLiteral = rightStringLiteral is not null && rightStringLiteral.ReturnType.RequireClrType() == typeof(string);

        if (!((leftIsChar && rightIsStringLiteral) || (rightIsChar && leftIsStringLiteral)))
            return false;

        ExpressionSyntax left;
        ExpressionSyntax right;
        if (leftIsChar)
        {
            left = RenderExpression(binary.Left, context);
            right = CreateCharLiteralFromStringLiteral(rightStringLiteral ?? throw new InvalidOperationException("Char/string comparison requires a string literal."));
        }
        else
        {
            left = CreateCharLiteralFromStringLiteral(leftStringLiteral ?? throw new InvalidOperationException("Char/string comparison requires a string literal."));
            right = RenderExpression(binary.Right, context);
        }

        result = SyntaxFactory.ParenthesizedExpression(
            SyntaxFactory.BinaryExpression(GetEqualitySyntaxKind(binary.Kind), left, right));
        return true;
    }

    private ParenthesizedExpressionSyntax RenderEquality(ExecutionBinary binary, ExecutionRenderContext context) =>
        SyntaxFactory.ParenthesizedExpression(SyntaxFactory.BinaryExpression(
            GetEqualitySyntaxKind(binary.Kind), RenderExpression(binary.Left, context), RenderExpression(binary.Right, context)));

    private static bool IsNullSafeDistinctComparison(BinaryOpKind kind) =>
        kind is BinaryOpKind.IsDistinctFrom or BinaryOpKind.IsNotDistinctFrom;

    private static SyntaxKind GetEqualitySyntaxKind(BinaryOpKind kind)
    {
        return kind switch
        {
            BinaryOpKind.NotEqual or BinaryOpKind.IsDistinctFrom => SyntaxKind.NotEqualsExpression,
            BinaryOpKind.Equal or BinaryOpKind.IsNotDistinctFrom => SyntaxKind.EqualsExpression,
            _ => GetBinaryExpressionKind(kind)
        };
    }

    private static LiteralExpressionSyntax CreateCharLiteralFromStringLiteral(ExecutionLiteral literal)
    {
        var text = literal.Value.RequireClrValue() as string ?? string.Empty;
        var character = text.Length > 0 ? text[0] : '\0';

        return SyntaxFactory.LiteralExpression(SyntaxKind.CharacterLiteralExpression, SyntaxFactory.Literal(character));
    }

    private static bool IsRelationalComparison(BinaryOpKind kind)
    {
        return kind is BinaryOpKind.GreaterThan
            or BinaryOpKind.LessThan
            or BinaryOpKind.GreaterOrEqual
            or BinaryOpKind.LessOrEqual;
    }

    private static MemberAccessExpressionSyntax CreateOrdinalStringComparisonExpression()
    {
        return CreateStringComparisonExpression(nameof(StringComparison.Ordinal));
    }

    private static MemberAccessExpressionSyntax CreateOrdinalIgnoreCaseStringComparisonExpression()
    {
        return CreateStringComparisonExpression(nameof(StringComparison.OrdinalIgnoreCase));
    }

    private static MemberAccessExpressionSyntax CreateStringComparisonExpression(string memberName)
    {
        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(nameof(StringComparison)),
            SyntaxFactory.IdentifierName(memberName));
    }

    private ExpressionSyntax RenderUnary(ExecutionUnary unary, ExecutionRenderContext context)
    {
        var expression = SyntaxFactory.ParenthesizedExpression(
            SyntaxFactory.PrefixUnaryExpression(
                GetUnaryExpressionKind(unary.Kind),
                RenderExpression(unary.Operand, context)));

        return RequiresUnaryResultCast(unary)
            ? CastIfNeeded(expression, unary.ReturnType)
            : expression;
    }

    private ParenthesizedExpressionSyntax RenderIsNullCheck(
        ExecutionIsNullCheck isNull,
        ExecutionRenderContext context)
    {
        var expression = RenderExpression(isNull.Expression, context);
        var nullLiteral = SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);

        return SyntaxFactory.ParenthesizedExpression(
            SyntaxFactory.BinaryExpression(
                isNull.IsNegated ? SyntaxKind.NotEqualsExpression : SyntaxKind.EqualsExpression,
                expression,
                nullLiteral));
    }

    private static bool RequiresUnaryResultCast(ExecutionUnary unary)
    {
        return unary.ReturnType.RequireClrType().IsValueType &&
               unary.ReturnType.RequireClrType() != typeof(bool) &&
               (ContainsMethodCall(unary.Operand) ||
                IsNullableValueType(unary.Operand.ReturnType));
    }

    private static bool IsNullableValueType(Type type)
    {
        return Nullable.GetUnderlyingType(type) is not null;
    }

    private static bool IsNullableValueType(ExecutionTypeRef type) => IsNullableValueType(type.RequireClrType());
}

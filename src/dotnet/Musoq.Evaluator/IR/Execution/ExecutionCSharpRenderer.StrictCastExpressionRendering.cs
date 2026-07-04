using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private ExpressionSyntax RenderStrictCast(ExecutionStrictCast strictCast)
    {
        var source = RenderExpression(strictCast.Expression);

        if (StrictCastLibraryConversionFacts.IsPassThrough(strictCast.Expression.ReturnType, strictCast.ReturnType))
            return CastExpressionIfNeeded(source, strictCast.ReturnType, strictCast.Expression.ReturnType);

        if (strictCast.Expression.ReturnType == typeof(DBNull))
            return CreateTypedNull(strictCast.ReturnType);

        var methodName = GetLibraryConversionMethodName(StrictCastLibraryConversionFacts.GetCastTargetType(strictCast.ReturnType));
        var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.ParseExpression($"global::{typeof(StrictCastRuntime).FullName}"),
                    SyntaxFactory.IdentifierName(methodName)))
            .WithArgumentList(CreateArgumentList(source));

        return CastExpressionIfNeeded(invocation, strictCast.ReturnType, strictCast.ReturnType);
    }

    private static ExpressionSyntax CastExpressionIfNeeded(
        ExpressionSyntax expression,
        Type targetType,
        Type expressionType)
    {
        return targetType == expressionType ? expression : CastIfNeeded(expression, targetType);
    }

    private static ExpressionSyntax CreateTypedNull(Type type)
    {
        return SyntaxFactory.CastExpression(
            CreateTypeSyntax(type),
            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));
    }

    private static string GetLibraryConversionMethodName(Type targetType)
    {
        if (targetType == typeof(bool))
            return nameof(StrictCastRuntime.ToBoolean);
        if (targetType == typeof(byte))
            return nameof(StrictCastRuntime.ToByte);
        if (targetType == typeof(sbyte))
            return nameof(StrictCastRuntime.ToSByte);
        if (targetType == typeof(short))
            return nameof(StrictCastRuntime.ToInt16);
        if (targetType == typeof(ushort))
            return nameof(StrictCastRuntime.ToUInt16);
        if (targetType == typeof(int))
            return nameof(StrictCastRuntime.ToInt32);
        if (targetType == typeof(uint))
            return nameof(StrictCastRuntime.ToUInt32);
        if (targetType == typeof(long))
            return nameof(StrictCastRuntime.ToInt64);
        if (targetType == typeof(ulong))
            return nameof(StrictCastRuntime.ToUInt64);
        if (targetType == typeof(float))
            return nameof(StrictCastRuntime.ToSingle);
        if (targetType == typeof(double))
            return nameof(StrictCastRuntime.ToDouble);
        if (targetType == typeof(decimal))
            return nameof(StrictCastRuntime.ToDecimal);
        if (targetType == typeof(char))
            return nameof(StrictCastRuntime.ToChar);
        if (targetType == typeof(string))
            return nameof(StrictCastRuntime.ToString);
        if (targetType == typeof(DateTime))
            return nameof(StrictCastRuntime.ToDateTime);
        if (targetType == typeof(DateTimeOffset))
            return nameof(StrictCastRuntime.ToDateTimeOffset);
        if (targetType == typeof(TimeSpan))
            return nameof(StrictCastRuntime.ToTimeSpan);
        if (targetType == typeof(Guid))
            return nameof(StrictCastRuntime.ToGuid);

        throw UnsupportedShape.Of($"Unsupported cast target '{targetType.Name}'.");
    }
}

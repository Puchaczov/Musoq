using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Plugins;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private ExpressionSyntax RenderStrictCast(ExecutionStrictCast strictCast)
    {
        var source = RenderExpression(strictCast.Expression);

        if (StrictCastLibraryConversionFacts.IsPassThrough(strictCast.Expression.ReturnType, strictCast.ReturnType))
            return CastExpressionIfNeeded(source, strictCast.ReturnType, strictCast.Expression.ReturnType);

        if (strictCast.Expression.ReturnType == typeof(DBNull) ||
            !StrictCastLibraryConversionFacts.CanUseLibraryConversion(strictCast.Expression.ReturnType, strictCast.ReturnType))
        {
            return CreateTypedNull(strictCast.ReturnType);
        }

        if (strictCast.Target == null)
            throw UnsupportedShape.Of("Library-backed cast rendering requires a hoisted LibraryBase target.");

        var methodName = GetLibraryConversionMethodName(StrictCastLibraryConversionFacts.GetCastTargetType(strictCast.ReturnType));
        var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(strictCast.Target.Name),
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
            return nameof(LibraryBase.ToBoolean);
        if (targetType == typeof(byte))
            return nameof(LibraryBase.ToByte);
        if (targetType == typeof(sbyte))
            return nameof(LibraryBase.ToSByte);
        if (targetType == typeof(short))
            return nameof(LibraryBase.ToInt16);
        if (targetType == typeof(ushort))
            return nameof(LibraryBase.ToUInt16);
        if (targetType == typeof(int))
            return nameof(LibraryBase.ToInt32);
        if (targetType == typeof(uint))
            return nameof(LibraryBase.ToUInt32);
        if (targetType == typeof(long))
            return nameof(LibraryBase.ToInt64);
        if (targetType == typeof(ulong))
            return nameof(LibraryBase.ToUInt64);
        if (targetType == typeof(float))
            return nameof(LibraryBase.ToSingle);
        if (targetType == typeof(double))
            return nameof(LibraryBase.ToDouble);
        if (targetType == typeof(decimal))
            return nameof(LibraryBase.ToDecimal);
        if (targetType == typeof(char))
            return nameof(LibraryBase.ToChar);
        if (targetType == typeof(string))
            return nameof(LibraryBase.ToString);
        if (targetType == typeof(DateTime))
            return nameof(LibraryBase.ToDateTime);
        if (targetType == typeof(DateTimeOffset))
            return nameof(LibraryBase.ToDateTimeOffset);
        if (targetType == typeof(TimeSpan))
            return nameof(LibraryBase.ToTimeSpan);
        if (targetType == typeof(Guid))
            return nameof(LibraryBase.ToGuid);

        throw UnsupportedShape.Of($"Unsupported cast target '{targetType.Name}'.");
    }
}

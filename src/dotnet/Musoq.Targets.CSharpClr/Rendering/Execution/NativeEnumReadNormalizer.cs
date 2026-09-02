using System;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Schema;

namespace Musoq.Targets.CSharpClr;

internal static class NativeEnumReadNormalizer
{
    public static ExpressionSyntax Normalize(
        ExecutionFieldRead fieldRead,
        ExpressionSyntax sourceValue,
        bool sourceValueIsBoxed = false)
    {
        if (fieldRead.EnumType == null || fieldRead.SourceReadType == null)
            return sourceValue;

        var sourceReadType = fieldRead.SourceReadType.RequireClrType();
        var enumType = Nullable.GetUnderlyingType(sourceReadType) ?? sourceReadType;
        if (!enumType.IsEnum)
            return sourceValue;
        if (fieldRead.EnumType.Origin != EnumTypeOrigin.NativeClr)
            throw new InvalidOperationException("Only native CLR enum reads may require source-boundary casts.");

        ExpressionSyntax typedSourceValue = sourceValue;
        if (sourceValueIsBoxed)
        {
            typedSourceValue = SyntaxFactory.ParenthesizedExpression(
                SyntaxFactory.CastExpression(
                    ExecutionSyntaxFactory.CreateTypeSyntax(fieldRead.SourceReadType),
                    sourceValue));
        }

        return SyntaxFactory.CastExpression(
            ExecutionSyntaxFactory.CreateTypeSyntax(fieldRead.ReturnType),
            SyntaxFactory.ParenthesizedExpression(typedSourceValue));
    }
}

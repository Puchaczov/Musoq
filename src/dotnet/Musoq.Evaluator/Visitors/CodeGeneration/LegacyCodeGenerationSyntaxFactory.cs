using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Visitors.CodeGeneration;

internal static class LegacyCodeGenerationSyntaxFactory
{
    public static ExpressionSyntax CreateColumnCreation(ExecutionColumnMetadataField field)
    {
        return SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(nameof(Column)))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList([
                SyntaxFactory.Argument(CreateStringLiteral(field.Name)),
                SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(CreateTypeOfTypeSyntax(
                    ExecutionColumnMetadataFields.RequireClrTypeForLegacyCodeGeneration(field)))),
                SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(field.Index)))
            ])));
    }

    public static ArrayCreationExpressionSyntax CreateArrayCreation(
        string elementTypeName,
        IEnumerable<ExpressionSyntax> expressions)
    {
        return SyntaxFactory.ArrayCreationExpression(
                SyntaxFactory.ArrayType(SyntaxFactory.ParseTypeName(elementTypeName))
                    .WithRankSpecifiers(SyntaxFactory.SingletonList(
                        SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                            SyntaxFactory.OmittedArraySizeExpression())))))
            .WithInitializer(SyntaxFactory.InitializerExpression(
                SyntaxKind.ArrayInitializerExpression,
                SyntaxFactory.SeparatedList(expressions)));
    }

    public static TypeSyntax CreateTypeSyntax(Type type)
    {
        if (DynamicEntityBoundary.IsDynamicMetaObjectProvider(type))
            return SyntaxFactory.IdentifierName("dynamic");

        return SyntaxFactory.ParseTypeName(EvaluationHelper.GetCastableType(type));
    }

    private static TypeSyntax CreateTypeOfTypeSyntax(Type type)
    {
        return DynamicEntityBoundary.IsDynamicMetaObjectProvider(type)
            ? CreateTypeSyntax(typeof(object))
            : CreateTypeSyntax(type);
    }

    private static LiteralExpressionSyntax CreateStringLiteral(string value)
    {
        return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(value));
    }
}

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private ExpressionSyntax CreateOrderedRowsExpression(
        ExecutionVariable source,
        IReadOnlyList<ExecutionOrderField> keys,
        ExecutionRenderContext context)
    {
        return CreateOrderedRowsExpression(source, keys, context, null);
    }

    private ExpressionSyntax CreateOrderedRowsExpression(
        ExecutionVariable source,
        IReadOnlyList<ExecutionOrderField> keys,
        ExecutionRenderContext context,
        GeneratedRowShape? generatedRowShape)
    {
        if (generatedRowShape != null && CanUseGeneratedRowOrderComparer(keys, generatedRowShape))
            return CreateGeneratedRowOrderedRowsExpression(source, keys, generatedRowShape, context);

        if (HasExplicitNullOrdering(keys)) return CreateExplicitNullOrderedRowsExpression(source, keys, context);
        ExpressionSyntax orderedRows = CreateRowsRead(source, context);

        for (var index = 0; index < keys.Count; index++)
        {
            var methodName = CreateOrderMethodName(keys[index], index == 0);
            orderedRows = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        orderedRows,
                        SyntaxFactory.IdentifierName(methodName)))
                .WithArgumentList(CreateOrderArgumentList(keys[index], generatedRowShape));
        }

        return orderedRows;
    }

    private InvocationExpressionSyntax CreateGeneratedRowOrderedRowsExpression(
        ExecutionVariable source,
        IReadOnlyList<ExecutionOrderField> keys,
        GeneratedRowShape generatedRowShape,
        ExecutionRenderContext context)
    {
        var rows = TryGetTypedRowBufferShape(source.Name, context, out _)
            ? CreateRowsRead(source, context)
            : SyntaxFactory.InvocationExpression(CreateGenericEvaluationHelperMemberAccess(
                    nameof(EvaluationHelper.CastGeneratedRows),
                    generatedRowShape.TypeName))
                .WithArgumentList(CreateArgumentList(CreateTableRowsRead(source.Name)));
        var comparer = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(CreateGeneratedRowOrderComparerTypeName(generatedRowShape, keys)),
            SyntaxFactory.IdentifierName("Instance"));

        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    rows,
                    SyntaxFactory.IdentifierName("OrderBy")))
            .WithArgumentList(CreateArgumentList(CreateIdentitySelector(), comparer));
    }

    private static ParenthesizedLambdaExpressionSyntax CreateIdentitySelector()
    {
        const string rowVariableName = "row";

        return SyntaxFactory.ParenthesizedLambdaExpression(SyntaxFactory.IdentifierName(rowVariableName))
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier(rowVariableName)))));
    }

    private static ArgumentListSyntax CreateOrderArgumentList(
        ExecutionOrderField key,
        GeneratedRowShape? generatedRowShape = null)
    {
        var selector = CreateOrderKeySelector(key, generatedRowShape);
        if (key.Type != typeof(string))
            return CreateArgumentList(selector);

        return CreateArgumentList(selector, CreateOrdinalStringComparerExpression());
    }

    private static MemberAccessExpressionSyntax CreateOrdinalStringComparerExpression()
    {
        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(nameof(StringComparer)),
            SyntaxFactory.IdentifierName(nameof(StringComparer.Ordinal)));
    }

    private static string CreateOrderMethodName(ExecutionOrderField key, bool firstKey)
    {
        return (firstKey, key.Descending) switch
        {
            (true, true) => "OrderByDescending",
            (true, false) => "OrderBy",
            (false, true) => "ThenByDescending",
            _ => "ThenBy"
        };
    }

    private static ParenthesizedLambdaExpressionSyntax CreateOrderKeySelector(
        ExecutionOrderField key,
        GeneratedRowShape? generatedRowShape = null)
    {
        const string rowVariableName = "row";
        ExpressionSyntax keyExpression;

        if (generatedRowShape != null &&
            key.OutputIndex >= 0 &&
            key.OutputIndex < generatedRowShape.Fields.Count)
        {
            var field = generatedRowShape.Fields[key.OutputIndex];
            var generatedRow = SyntaxFactory.ParenthesizedExpression(
                SyntaxFactory.CastExpression(
                    SyntaxFactory.IdentifierName(generatedRowShape.TypeName),
                    SyntaxFactory.IdentifierName(rowVariableName)));

            keyExpression = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                generatedRow,
                CreateIdentifierName(GetGeneratedFieldName(field)));

            if (field.Type != key.Type && key.Type != typeof(object))
                keyExpression = SyntaxFactory.CastExpression(CreateTypeSyntax(key.Type), keyExpression);
        }
        else
        {
            var rowValue = CreateElementAccess(
                SyntaxFactory.IdentifierName(rowVariableName),
                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(key.OutputIndex)));
            keyExpression = rowValue;
            if (key.Type != typeof(object))
                keyExpression = SyntaxFactory.CastExpression(CreateTypeSyntax(key.Type), rowValue);
        }

        return SyntaxFactory.ParenthesizedLambdaExpression(keyExpression)
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier(rowVariableName)))));
    }
}

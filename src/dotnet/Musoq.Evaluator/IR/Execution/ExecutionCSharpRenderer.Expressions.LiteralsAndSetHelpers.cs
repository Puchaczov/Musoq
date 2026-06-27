using System.Collections.Generic;
using System.Globalization;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{

    private static ParenthesizedLambdaExpressionSyntax CreateSetComparer(
        IReadOnlyList<int> fieldIndexes,
        IReadOnlyList<Type> fieldTypes)
    {
        return SyntaxFactory.ParenthesizedLambdaExpression(CreateSetComparerBody(fieldIndexes, fieldTypes))
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList<ParameterSyntax>(
            [
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("first")),
                SyntaxFactory.Token(SyntaxKind.CommaToken),
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("second"))
            ])));
    }

    private static ExpressionSyntax CreateSetComparerBody(
        IReadOnlyList<int> fieldIndexes,
        IReadOnlyList<Type> fieldTypes)
    {
        if (fieldIndexes.Count == 0)
            return SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);

        ExpressionSyntax? body = null;

        for (var index = 0; index < fieldIndexes.Count; index++)
        {
            var fieldType = index < fieldTypes.Count ? fieldTypes[index] : typeof(object);
            var equality = CreateSetFieldEquality(fieldIndexes[index], fieldType);
            body = body == null
                ? equality
                : SyntaxFactory.BinaryExpression(SyntaxKind.LogicalAndExpression, body, equality);
        }

        return body!;
    }

    private static ExpressionSyntax CreateSetFieldEquality(int fieldIndex, Type fieldType)
    {
        var firstFieldAccess = CreateElementAccess(
            SyntaxFactory.IdentifierName("first"),
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(fieldIndex)));
        var secondFieldAccess = CreateElementAccess(
            SyntaxFactory.IdentifierName("second"),
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(fieldIndex)));

        if (fieldType != typeof(object))
        {
            return SyntaxFactory.BinaryExpression(
                SyntaxKind.EqualsExpression,
                SyntaxFactory.CastExpression(CreateTypeSyntax(fieldType), firstFieldAccess),
                SyntaxFactory.CastExpression(CreateTypeSyntax(fieldType), secondFieldAccess));
        }

        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)),
                    SyntaxFactory.IdentifierName(nameof(object.Equals))))
            .WithArgumentList(CreateArgumentList(firstFieldAccess, secondFieldAccess));
    }

    private static string ResolveSetOperationMethodName(SetOpKind kind)
    {
        return kind switch
        {
            SetOpKind.Union => nameof(BaseOperations.Union),
            SetOpKind.UnionAll => nameof(BaseOperations.UnionAll),
            SetOpKind.Except => nameof(BaseOperations.Except),
            SetOpKind.Intersect => nameof(BaseOperations.Intersect),
            _ => throw UnsupportedShape.Of($"Set operation kind {kind}")
        };
    }

    private static ExpressionSyntax RenderLiteral(object? value)
    {
        return value switch
        {
            null => SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression),
            string text => CreateStringLiteral(text),
            bool flag => SyntaxFactory.LiteralExpression(flag ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression),
            char character => SyntaxFactory.LiteralExpression(SyntaxKind.CharacterLiteralExpression, SyntaxFactory.Literal(character)),
            byte number => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(number)),
            sbyte number => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(number)),
            short number => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(number)),
            ushort number => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(number)),
            int number => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(number)),
            uint number => SyntaxFactory.ParseExpression($"{number.ToString(CultureInfo.InvariantCulture)}u"),
            long number => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(number)),
            ulong number => SyntaxFactory.ParseExpression($"{number.ToString(CultureInfo.InvariantCulture)}ul"),
            float number => SyntaxFactory.ParseExpression($"{number.ToString(CultureInfo.InvariantCulture)}f"),
            double number => SyntaxFactory.ParseExpression($"{number.ToString(CultureInfo.InvariantCulture)}d"),
            decimal number => SyntaxFactory.ParseExpression($"{number.ToString(CultureInfo.InvariantCulture)}m"),
            _ => throw UnsupportedShape.Of($"Literal type '{value.GetType().Name}'", "the C# backend")
        };
    }

}

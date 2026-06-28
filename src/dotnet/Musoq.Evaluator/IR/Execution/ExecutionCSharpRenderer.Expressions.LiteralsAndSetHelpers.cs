using System.Collections.Generic;
using System.Globalization;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
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

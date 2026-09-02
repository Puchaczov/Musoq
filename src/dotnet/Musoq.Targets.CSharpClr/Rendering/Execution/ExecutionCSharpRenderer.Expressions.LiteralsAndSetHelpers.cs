using System.Globalization;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    internal static ExpressionSyntax RenderLiteral(object? value)
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

    private static ExpressionSyntax RenderLiteral(ExecutionConstantValue value)
    {
        if (value.Kind == ExecutionConstantKind.FloatingPoint)
        {
            if (value.BitWidth == 32 &&
                (!float.IsFinite((float)value.RequireClrValue()!) || value.FloatingPointBits == 0x80000000UL))
            {
                return SyntaxFactory.ParseExpression(
                    $"System.BitConverter.Int32BitsToSingle(unchecked((int)0x{value.FloatingPointBits:X8}u))");
            }

            if (value.BitWidth == 64 &&
                (!double.IsFinite((double)value.RequireClrValue()!) || value.FloatingPointBits == 0x8000000000000000UL))
            {
                return SyntaxFactory.ParseExpression(
                    $"System.BitConverter.Int64BitsToDouble(unchecked((long)0x{value.FloatingPointBits:X16}ul))");
            }
        }

        return RenderLiteral(value.RequireClrValue());
    }

}

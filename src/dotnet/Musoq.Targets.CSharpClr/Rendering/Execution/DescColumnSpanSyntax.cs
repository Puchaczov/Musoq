using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

internal static class DescColumnSpanSyntax
{
    public static ExpressionSyntax Create(Musoq.Parser.TextSpan? columnSpan)
    {
        if (columnSpan is not { } span)
            return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);

        return SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName("Musoq.Parser.TextSpan"))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList([
                SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(span.Start))),
                SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(span.Length)))
            ])));
    }
}

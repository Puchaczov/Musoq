using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

internal static class CSharpReadModifierMetadata
{
    public static ObjectCreationExpressionSyntax CreateDictionaryCreation(
        IReadOnlyDictionary<string, string> readModifiers)
    {
        var entries = readModifiers
            .OrderBy(static modifier => modifier.Key, StringComparer.Ordinal)
            .Select(static modifier => SyntaxFactory.InitializerExpression(
                SyntaxKind.ComplexElementInitializerExpression,
                SyntaxFactory.SeparatedList<ExpressionSyntax>([
                    CreateStringLiteral(modifier.Key),
                    CreateStringLiteral(modifier.Value)
                ])));

        return SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.ParseTypeName("Dictionary<string, string>"))
            .WithArgumentList(SyntaxFactory.ArgumentList())
            .WithInitializer(SyntaxFactory.InitializerExpression(
                SyntaxKind.CollectionInitializerExpression,
                SyntaxFactory.SeparatedList<ExpressionSyntax>(entries)));
    }

    private static LiteralExpressionSyntax CreateStringLiteral(string value)
    {
        return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(value));
    }
}

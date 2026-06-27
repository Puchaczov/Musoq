using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Execution;

internal static class ReadModifierMetadata
{
    public static void AppendKey(StringBuilder builder, IReadOnlyDictionary<string, string> readModifiers)
    {
        builder.Append(':').Append(readModifiers.Count);

        foreach (var modifier in Sort(readModifiers))
        {
            AppendKeyPart(builder, modifier.Key);
            builder.Append(':');
            AppendKeyPart(builder, modifier.Value);
        }
    }

    public static ObjectCreationExpressionSyntax CreateDictionaryCreation(
        IReadOnlyDictionary<string, string> readModifiers)
    {
        var entries = Sort(readModifiers)
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

    private static IEnumerable<KeyValuePair<string, string>> Sort(IReadOnlyDictionary<string, string> readModifiers)
    {
        return readModifiers.OrderBy(static modifier => modifier.Key, StringComparer.Ordinal);
    }

    private static void AppendKeyPart(StringBuilder builder, string value)
    {
        builder
            .Append(':')
            .Append(value.Length)
            .Append(':')
            .Append(value);
    }

    private static LiteralExpressionSyntax CreateStringLiteral(string value)
    {
        return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(value));
    }
}

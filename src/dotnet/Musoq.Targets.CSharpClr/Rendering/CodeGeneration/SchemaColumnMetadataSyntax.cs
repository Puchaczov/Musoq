using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Tables;
using Musoq.Schema;

namespace Musoq.Targets.CSharpClr.Rendering.CodeGeneration;

internal static class SchemaColumnMetadataSyntax
{
    public static ExpressionSyntax CreateColumnCreation(ISchemaColumn column)
    {
        if (column.ReadModifiers.Count == 0)
        {
            return SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(nameof(Column)))
                .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new ArgumentSyntax[]
                {
                    SyntaxFactory.Argument(CreateStringLiteral(column.ColumnName)),
                    SyntaxFactory.Argument(CreateTypeOfExpression(column.ColumnType)),
                    SyntaxFactory.Argument(CreateIntLiteral(column.ColumnIndex))
                })));
        }

        return SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.ParseTypeName("global::Musoq.Schema.DataSources.SchemaColumn"))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new ArgumentSyntax[]
            {
                SyntaxFactory.Argument(CreateStringLiteral(column.ColumnName)),
                SyntaxFactory.Argument(CreateIntLiteral(column.ColumnIndex)),
                SyntaxFactory.Argument(CreateTypeOfExpression(column.ColumnType)),
                SyntaxFactory.Argument(CreateReadModifierDictionaryCreation(column.ReadModifiers))
            })));
    }

    private static ObjectCreationExpressionSyntax CreateReadModifierDictionaryCreation(
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

    private static TypeOfExpressionSyntax CreateTypeOfExpression(Type type)
    {
        return SyntaxFactory.TypeOfExpression(
            SyntaxFactory.ParseTypeName(EvaluationHelper.GetCastableType(type)));
    }

    private static LiteralExpressionSyntax CreateStringLiteral(string value)
    {
        return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(value));
    }

    private static LiteralExpressionSyntax CreateIntLiteral(int value)
    {
        return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(value));
    }
}

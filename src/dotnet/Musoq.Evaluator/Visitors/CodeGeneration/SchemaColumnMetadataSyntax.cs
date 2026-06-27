using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.Tables;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors.CodeGeneration;

internal static class SchemaColumnMetadataSyntax
{
    public static ExpressionSyntax CreateColumnCreation(ISchemaColumn column)
    {
        if (column.ReadModifiers.Count == 0)
        {
            return SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(nameof(Column)))
                .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList([
                    SyntaxFactory.Argument(CreateStringLiteral(column.ColumnName)),
                    SyntaxFactory.Argument(CreateTypeOfExpression(column.ColumnType)),
                    SyntaxFactory.Argument(CreateIntLiteral(column.ColumnIndex))
                ])));
        }

        return SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.ParseTypeName("global::Musoq.Schema.DataSources.SchemaColumn"))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList([
                SyntaxFactory.Argument(CreateStringLiteral(column.ColumnName)),
                SyntaxFactory.Argument(CreateIntLiteral(column.ColumnIndex)),
                SyntaxFactory.Argument(CreateTypeOfExpression(column.ColumnType)),
                SyntaxFactory.Argument(ReadModifierMetadata.CreateDictionaryCreation(column.ReadModifiers))
            ])));
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

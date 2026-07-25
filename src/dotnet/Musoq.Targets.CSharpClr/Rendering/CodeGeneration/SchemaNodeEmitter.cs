using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Schema;

namespace Musoq.Targets.CSharpClr.Rendering.CodeGeneration;

internal static class SchemaNodeEmitter
{
    public static VariableDeclarationSyntax CreateTableInfoDeclaration(
        string tableInfoVariableName,
        IEnumerable<ISchemaColumn> columns)
    {
        return SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
            .WithVariables(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.VariableDeclarator(tableInfoVariableName)
                    .WithInitializer(SyntaxFactory.EqualsValueClause(
                        CreateColumnMetadataArray(columns)))));
    }

    public static ObjectCreationExpressionSyntax CreateRuntimeContext(
        string nodeId,
        int schemaFromIndex,
        ExpressionSyntax originallyInferredColumns,
        ExpressionSyntax? diagnostics = null)
    {
        var arguments = new List<ArgumentSyntax>
        {
            SyntaxFactory.Argument(CreateStringLiteral(nodeId)),
            SyntaxFactory.Argument(
                CreateElementAccess("sourceExecutionPlans", nodeId)),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("token")),
            SyntaxFactory.Argument(originallyInferredColumns),
            SyntaxFactory.Argument(
                CreateElementAccess("sourceRuntimeSettingsBySourceContextId", nodeId)),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("logger")),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("OnDataSourceProgress"))
        };

        if (diagnostics != null)
            arguments.Add(SyntaxFactory.Argument(diagnostics));

        return SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.IdentifierName(nameof(SourceExecutionContext)))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(arguments)));
    }

    private static ArrayCreationExpressionSyntax CreateColumnMetadataArray(IEnumerable<ISchemaColumn> columns)
    {
        var columnExpressions = columns
            .Select(SchemaColumnMetadataSyntax.CreateColumnCreation)
            .ToArray();

        return SyntaxFactory.ArrayCreationExpression(
                SyntaxFactory.ArrayType(SyntaxFactory.IdentifierName(nameof(ISchemaColumn)))
                    .WithRankSpecifiers(SyntaxFactory.SingletonList(
                        SyntaxFactory.ArrayRankSpecifier(
                            SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                SyntaxFactory.OmittedArraySizeExpression())))))
            .WithInitializer(SyntaxFactory.InitializerExpression(
                SyntaxKind.ArrayInitializerExpression,
                SyntaxFactory.SeparatedList<ExpressionSyntax>(columnExpressions)));
    }

    private static ElementAccessExpressionSyntax CreateElementAccess(string identifier, string stringKey)
    {
        return CreateElementAccess(identifier, CreateStringLiteral(stringKey));
    }

    private static ElementAccessExpressionSyntax CreateElementAccess(string identifier, ExpressionSyntax indexExpression)
    {
        return SyntaxFactory.ElementAccessExpression(SyntaxFactory.IdentifierName(identifier))
            .WithArgumentList(SyntaxFactory.BracketedArgumentList(
                SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(indexExpression))));
    }

    private static LiteralExpressionSyntax CreateStringLiteral(string value)
    {
        return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(value));
    }

}

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Tables;
using Musoq.Parser.Nodes;
using Group = Musoq.Plugins.Group;

namespace Musoq.Evaluator.Visitors.CodeGeneration;

/// <summary>
///     Handles generation of transformation table creation code.
/// </summary>
public static class TransformationTableEmitter
{
    /// <summary>
    ///     Creates a transformation table statement for regular (non-grouping) tables.
    /// </summary>
    /// <param name="node">The transformation table node.</param>
    /// <param name="tableName">The variable name for the table from scope.</param>
    /// <param name="trackTypes">Action to track types for references.</param>
    /// <param name="trackNamespaces">Action to track namespaces.</param>
    /// <returns>The local declaration statement for the table.</returns>
    public static LocalDeclarationStatementSyntax CreateRegularTransformationTable(
        CreateTransformationTableNode node,
        string tableName,
        Action<Type[]> trackTypes,
        Action<Type[]> trackNamespaces)
    {
        var cols = new List<ExpressionSyntax>();

        foreach (var field in node.Fields)
        {
            var type = field.ReturnType;
            var types = EvaluationHelper.GetNestedTypes(type);

            trackNamespaces(types);
            trackTypes(types);

            cols.Add(CreateColumnExpression(field.FieldName, type, field.FieldOrder));
        }

        var createObject = SyntaxHelper.CreateAssignment(
            tableName,
            SyntaxHelper.CreateObjectOf(
                nameof(Table),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(
                    [
                        SyntaxFactory.Argument(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                SyntaxFactory.Literal(node.Name))),
                        SyntaxFactory.Argument(
                            SyntaxHelper.CreateArrayOf(
                                nameof(Column),
                                cols.ToArray()))
                    ]))));

        return SyntaxFactory.LocalDeclarationStatement(createObject);
    }

    /// <summary>
    ///     Creates a transformation table statement for grouping tables.
    /// </summary>
    /// <param name="tableName">The variable name for the table from scope.</param>
    /// <returns>The local declaration statement for the group list.</returns>
    public static LocalDeclarationStatementSyntax CreateGroupingTransformationTable(string tableName)
    {
        var createObject = SyntaxHelper.CreateAssignment(
            tableName,
            SyntaxHelper.CreateObjectOf(
                NamingHelper.ListOf<Group>(),
                SyntaxFactory.ArgumentList()));

        return SyntaxFactory.LocalDeclarationStatement(createObject);
    }

    private static ObjectCreationExpressionSyntax CreateColumnExpression(
        string fieldName,
        Type type,
        int fieldOrder)
    {
        var escapedFieldName = fieldName.Replace("\"", "'");

        return SyntaxHelper.CreateObjectOf(
            nameof(Column),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(
                [
                    SyntaxFactory.Argument(
                        SyntaxFactory.LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            SyntaxFactory.Literal($"@\"{escapedFieldName}\"", fieldName))),
                    SyntaxHelper.TypeLiteralArgument(
                        EvaluationHelper.GetCastableType(type)),
                    SyntaxHelper.IntLiteralArgument(fieldOrder)
                ])));
    }
}

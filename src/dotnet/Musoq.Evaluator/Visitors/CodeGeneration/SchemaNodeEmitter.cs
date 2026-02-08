using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Tables;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors.CodeGeneration;

/// <summary>
///     Emits C# syntax for schema-related operations.
/// </summary>
internal static class SchemaNodeEmitter
{
    /// <summary>
    ///     Creates the table info variable declaration containing column metadata.
    /// </summary>
    public static VariableDeclarationSyntax CreateTableInfoDeclaration(
        string tableInfoVariableName,
        IEnumerable<ISchemaColumn> columns)
    {
        var columnExpressions = columns.Select(column =>
                SyntaxHelper.CreateObjectOf(
                    nameof(Column),
                    SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList([
                        SyntaxFactory.Argument(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                SyntaxFactory.Literal(column.ColumnName))),
                        SyntaxHelper.TypeLiteralArgument(EvaluationHelper.GetCastableType(column.ColumnType)),
                        SyntaxHelper.IntLiteralArgument(column.ColumnIndex)
                    ]))))
            .Cast<ExpressionSyntax>()
            .ToArray();

        return SyntaxHelper.CreateAssignment(
            tableInfoVariableName,
            SyntaxHelper.CreateArrayOf(nameof(ISchemaColumn), columnExpressions));
    }

    /// <summary>
    ///     Creates the schema variable declaration.
    /// </summary>
    public static VariableDeclarationSyntax CreateSchemaDeclaration(string alias, string schemaName)
    {
        return SyntaxHelper.CreateAssignmentByMethodCall(
            alias,
            "provider",
            nameof(ISchemaProvider.GetSchema),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.Token(SyntaxKind.OpenParenToken),
                SyntaxFactory.SeparatedList([SyntaxHelper.StringLiteralArgument(schemaName)]),
                SyntaxFactory.Token(SyntaxKind.CloseParenToken)));
    }

    /// <summary>
    ///     Creates the schema rows variable declaration.
    /// </summary>
    public static VariableDeclarationSyntax CreateSchemaRowsDeclaration(
        string alias,
        string method,
        ExpressionSyntax runtimeContext,
        IEnumerable<ExpressionSyntax> args)
    {
        return SyntaxHelper.CreateAssignmentByMethodCall(
            $"{alias}Rows",
            alias,
            nameof(ISchema.GetRowSource),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList([
                    SyntaxHelper.StringLiteralArgument(method),
                    SyntaxFactory.Argument(runtimeContext),
                    SyntaxFactory.Argument(SyntaxHelper.CreateArrayOf(nameof(Object), args.ToArray()))
                ])));
    }

    /// <summary>
    ///     Creates a RuntimeContext object creation expression.
    /// </summary>
    public static ObjectCreationExpressionSyntax CreateRuntimeContext(
        string nodeId,
        int schemaFromIndex,
        ExpressionSyntax originallyInferredColumns)
    {
        return SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.IdentifierName(nameof(RuntimeContext)))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList([
                        SyntaxFactory.Argument(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                SyntaxFactory.Literal(nodeId))),
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("token")),
                        SyntaxFactory.Argument(originallyInferredColumns),
                        SyntaxFactory.Argument(
                            SyntaxFactory.ElementAccessExpression(
                                    SyntaxFactory.IdentifierName("positionalEnvironmentVariables"))
                                .WithArgumentList(
                                    SyntaxFactory.BracketedArgumentList(
                                        SyntaxFactory.SingletonSeparatedList(
                                            SyntaxFactory.Argument(
                                                SyntaxFactory.LiteralExpression(
                                                    SyntaxKind.NumericLiteralExpression,
                                                    SyntaxFactory.Literal(schemaFromIndex))))))),
                        SyntaxFactory.Argument(
                            SyntaxFactory.ElementAccessExpression(
                                    SyntaxFactory.IdentifierName("queriesInformation"))
                                .WithArgumentList(
                                    SyntaxFactory.BracketedArgumentList(
                                        SyntaxFactory.SingletonSeparatedList(
                                            SyntaxFactory.Argument(
                                                SyntaxFactory.LiteralExpression(
                                                    SyntaxKind.StringLiteralExpression,
                                                    SyntaxFactory.Literal(nodeId))))))),
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("logger")),
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("OnDataSourceProgress"))
                    ])));
    }

    /// <summary>
    ///     Creates a row source statement for in-memory tables.
    /// </summary>
    public static LocalDeclarationStatementSyntax CreateInMemoryTableRowsSource(
        string alias,
        int tableIndex)
    {
        var tableArgument = SyntaxFactory.Argument(
            SyntaxFactory.ElementAccessExpression(
                    SyntaxFactory.IdentifierName("_tableResults"))
                .WithArgumentList(
                    SyntaxFactory.BracketedArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(
                                SyntaxFactory.LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    SyntaxFactory.Literal(tableIndex)))))));

        var literalTrueArgument = SyntaxFactory.Argument(
            SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression));

        return SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(alias.ToRowsSource()))
                            .WithInitializer(
                                SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.InvocationExpression(
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                                                SyntaxFactory.IdentifierName(
                                                    nameof(EvaluationHelper.ConvertTableToSource))))
                                        .WithArgumentList(
                                            SyntaxFactory.ArgumentList(
                                                SyntaxFactory.SeparatedList([
                                                    tableArgument, literalTrueArgument
                                                ]))))))));
    }

    /// <summary>
    ///     Creates a row source statement for enumerable sources.
    /// </summary>
    public static LocalDeclarationStatementSyntax CreateEnumerableRowsSource(
        string alias,
        Type returnType,
        ExpressionSyntax sourceExpression)
    {
        return SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(alias.ToRowsSource()))
                            .WithInitializer(
                                SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.InvocationExpression(
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                                                SyntaxFactory.IdentifierName(
                                                    nameof(EvaluationHelper.ConvertEnumerableToSource))))
                                        .WithArgumentList(
                                            SyntaxFactory.ArgumentList(
                                                SyntaxFactory.SingletonSeparatedList(
                                                    SyntaxFactory.Argument(
                                                        SyntaxFactory.CastExpression(
                                                            SyntaxFactory.ParseTypeName(
                                                                EvaluationHelper.GetCastableType(returnType)),
                                                            sourceExpression))))))))));
    }

    /// <summary>
    ///     Creates a row source statement for interpret functions where the source expression
    ///     is already an IEnumerable (wrapped by CreateScalarToArrayWrapper).
    ///     Unlike CreateEnumerableRowsSource, this doesn't add an extra cast.
    /// </summary>
    public static LocalDeclarationStatementSyntax CreateInterpretRowsSource(
        string alias,
        ExpressionSyntax wrappedEnumerableExpression)
    {
        return SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(alias.ToRowsSource()))
                            .WithInitializer(
                                SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.InvocationExpression(
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                                                SyntaxFactory.IdentifierName(
                                                    nameof(EvaluationHelper.ConvertEnumerableToSource))))
                                        .WithArgumentList(
                                            SyntaxFactory.ArgumentList(
                                                SyntaxFactory.SingletonSeparatedList(
                                                    SyntaxFactory.Argument(wrappedEnumerableExpression)))))))));
    }

    /// <summary>
    ///     Creates a row source statement for property chain navigation.
    /// </summary>
    public static LocalDeclarationStatementSyntax CreatePropertyRowsSource(
        string alias,
        string sourceAlias,
        Type returnType,
        (string PropertyName, Type PropertyType, string? IntendedTypeName)[] propertiesChain)
    {
        
        
        
        var firstPropertyCastType = !string.IsNullOrEmpty(propertiesChain[0].IntendedTypeName)
            ? propertiesChain[0].IntendedTypeName
            : EvaluationHelper.GetCastableType(propertiesChain[0].PropertyType);

        ExpressionSyntax propertyAccess = SyntaxFactory.ParenthesizedExpression(
            SyntaxFactory.CastExpression(
                SyntaxFactory.ParseTypeName(firstPropertyCastType),
                SyntaxFactory.ElementAccessExpression(
                    SyntaxFactory.IdentifierName($"{sourceAlias}Row"),
                    SyntaxFactory.BracketedArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(
                                SyntaxFactory.LiteralExpression(
                                    SyntaxKind.StringLiteralExpression,
                                    SyntaxFactory.Literal(propertiesChain[0].PropertyName))))))));


        for (var i = 1; i < propertiesChain.Length; i++)
            propertyAccess = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                propertyAccess,
                SyntaxFactory.IdentifierName(propertiesChain[i].PropertyName));

        return SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.IdentifierName("var"))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(
                                SyntaxFactory.Identifier(alias.ToRowsSource()))
                            .WithInitializer(
                                SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.InvocationExpression(
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                                                SyntaxFactory.IdentifierName(
                                                    nameof(EvaluationHelper.ConvertEnumerableToSource))))
                                        .WithArgumentList(
                                            SyntaxFactory.ArgumentList(
                                                SyntaxFactory.SingletonSeparatedList(
                                                    SyntaxFactory.Argument(
                                                        SyntaxFactory.CastExpression(
                                                            SyntaxFactory.ParseTypeName(
                                                                EvaluationHelper.GetCastableType(returnType)),
                                                            propertyAccess))))))))));
    }

    /// <summary>
    ///     Creates an expression that wraps a scalar value in an array for CROSS APPLY compatibility.
    ///     Per spec: Single object T -> new[] { result }
    ///     null with CROSS -> Array.Empty&lt;T&gt;()
    ///     null with OUTER -> new T[] { null }
    /// </summary>
    public static ExpressionSyntax CreateScalarToArrayWrapper(
        ExpressionSyntax sourceExpression,
        Type returnType,
        bool isCrossApply)
    {
        var typeName = EvaluationHelper.GetCastableType(returnType);
        return CreateScalarToArrayWrapperByTypeName(sourceExpression, typeName, isCrossApply);
    }

    /// <summary>
    ///     Creates an expression that wraps a scalar value in an array for CROSS APPLY compatibility.
    ///     Uses a type name string instead of a Type object.
    /// </summary>
    public static ExpressionSyntax CreateScalarToArrayWrapperByTypeName(
        ExpressionSyntax sourceExpression,
        string typeName,
        bool isCrossApply)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                    SyntaxFactory.GenericName(
                            SyntaxFactory.Identifier(nameof(EvaluationHelper.WrapScalarForCrossApply)))
                        .WithTypeArgumentList(
                            SyntaxFactory.TypeArgumentList(
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.ParseTypeName(typeName))))))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Argument(sourceExpression),
                        SyntaxFactory.Argument(
                            SyntaxFactory.LiteralExpression(
                                isCrossApply ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression))
                    })));
    }

    /// <summary>
    ///     Processes a SchemaFromNode and returns all required statements.
    /// </summary>
    public static SchemaFromNodeResult ProcessSchemaFromNode(
        string nodeId,
        string alias,
        string schema,
        string method,
        int schemaFromIndex,
        IEnumerable<ISchemaColumn> columns,
        ArgumentListSyntax argList)
    {
        var tableInfoVariableName = alias.ToInfoTable();

        var tableInfoObject = CreateTableInfoDeclaration(tableInfoVariableName, columns);
        var createdSchema = CreateSchemaDeclaration(alias, schema);

        var args = argList.Arguments.Select(arg => arg.Expression);
        var runtimeContext =
            CreateRuntimeContext(nodeId, schemaFromIndex, SyntaxFactory.IdentifierName(tableInfoVariableName));
        var createdSchemaRows = CreateSchemaRowsDeclaration(alias, method, runtimeContext, args);

        return new SchemaFromNodeResult
        {
            TableInfoStatement = SyntaxFactory.LocalDeclarationStatement(tableInfoObject),
            SchemaStatement = SyntaxFactory.LocalDeclarationStatement(createdSchema),
            RowsStatement = SyntaxFactory.LocalDeclarationStatement(createdSchemaRows)
        };
    }

    /// <summary>
    ///     Result of processing a SchemaFromNode.
    /// </summary>
    public readonly struct SchemaFromNodeResult
    {
        public LocalDeclarationStatementSyntax TableInfoStatement { get; init; }
        public LocalDeclarationStatementSyntax SchemaStatement { get; init; }
        public LocalDeclarationStatementSyntax RowsStatement { get; init; }
    }
}

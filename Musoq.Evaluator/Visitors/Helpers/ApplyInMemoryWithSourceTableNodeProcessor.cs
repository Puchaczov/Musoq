using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Visitors.Helpers;

/// <summary>
/// Provides specialized processing for ApplyInMemoryWithSourceTableFromNode operations in the query syntax tree.
/// Handles the complex apply logic for Cross and Outer apply types with detailed syntax generation.
/// </summary>
public static class ApplyInMemoryWithSourceTableNodeProcessor
{
    /// <summary>
    /// Processes an apply memory table node and generates the corresponding computing block.
    /// </summary>
    /// <param name="node">The apply memory table node to process</param>
    /// <param name="generator">The syntax generator for creating expressions</param>
    /// <param name="scope">The current scope for symbol resolution</param>
    /// <param name="queryAlias">The current query alias</param>
    /// <param name="getRowsSourceOrEmpty">Function to get rows source or empty</param>
    /// <param name="block">Function to create block statements</param>
    /// <param name="generateCancellationExpression">Function to generate cancellation expression</param>
    /// <returns>A tuple containing the empty block and computing block</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
    /// <exception cref="ArgumentException">Thrown when unsupported apply type is provided</exception>
    public static (BlockSyntax EmptyBlock, BlockSyntax ComputingBlock) ProcessApplyInMemoryWithSourceTable(
        ApplyInMemoryWithSourceTableFromNode node,
        SyntaxGenerator generator,
        Scope scope,
        string queryAlias,
        Func<string, StatementSyntax> getRowsSourceOrEmpty,
        Func<StatementSyntax[], BlockSyntax> block,
        Func<StatementSyntax> generateCancellationExpression)
    {
        ValidateParameters(node, generator, scope, queryAlias, getRowsSourceOrEmpty, block, generateCancellationExpression);

        var emptyBlock = SyntaxFactory.Block();
        var computingBlock = SyntaxFactory.Block();

        computingBlock = node.ApplyType switch
        {
            ApplyType.Cross => ProcessCrossApply(node, emptyBlock, getRowsSourceOrEmpty, block, generateCancellationExpression),
            ApplyType.Outer => ProcessOuterApply(node, generator, scope, queryAlias, emptyBlock, getRowsSourceOrEmpty, block, generateCancellationExpression),
            _ => throw new ArgumentException($"Unsupported apply type: {node.ApplyType}")
        };

        return (emptyBlock, computingBlock);
    }

    /// <summary>
    /// Processes a Cross apply operation generating nested foreach statements.
    /// </summary>
    private static BlockSyntax ProcessCrossApply(
        ApplyInMemoryWithSourceTableFromNode node,
        BlockSyntax emptyBlock,
        Func<string, StatementSyntax> getRowsSourceOrEmpty,
        Func<StatementSyntax[], BlockSyntax> block,
        Func<StatementSyntax> generateCancellationExpression)
    {
        return SyntaxFactory.Block().AddStatements(
            SyntaxFactory.ForEachStatement(
                SyntaxFactory.IdentifierName("var"),
                SyntaxFactory.Identifier($"{node.InMemoryTableAlias}Row"),
                SyntaxFactory.IdentifierName(
                    $"{nameof(EvaluationHelper)}.{nameof(EvaluationHelper.ConvertTableToSource)}({node.InMemoryTableAlias}TransitionTable, false).{nameof(RowSource.Rows)}"),
                block([
                    getRowsSourceOrEmpty(node.SourceTable.Alias),
                    SyntaxFactory.ForEachStatement(
                        SyntaxFactory.IdentifierName("var"),
                        SyntaxFactory.Identifier($"{node.SourceTable.Alias}Row"),
                        SyntaxFactory.IdentifierName($"{node.SourceTable.Alias}Rows.Rows"),
                        SyntaxFactory.Block(
                            generateCancellationExpression(),
                            emptyBlock))])));
    }

    /// <summary>
    /// Processes an Outer apply operation with complex expression building and table manipulation.
    /// </summary>
    private static BlockSyntax ProcessOuterApply(
        ApplyInMemoryWithSourceTableFromNode node,
        SyntaxGenerator generator,
        Scope scope,
        string queryAlias,
        BlockSyntax emptyBlock,
        Func<string, StatementSyntax> getRowsSourceOrEmpty,
        Func<StatementSyntax[], BlockSyntax> block,
        Func<StatementSyntax> generateCancellationExpression)
    {
        var fullTransitionTable = scope.ScopeSymbolTable.GetSymbol<TableSymbol>(queryAlias);
        var fieldNames = scope.ScopeSymbolTable.GetSymbol<FieldsNamesSymbol>(MetaAttributes.OuterJoinSelect);
        
        var expressions = BuildOuterApplyExpressions(node, generator, fullTransitionTable, fieldNames);
        var rewriteSelect = CreateRewriteSelectVariable(expressions);
        var invocation = CreateTableAddInvocation(node, scope);

        return SyntaxFactory.Block().AddStatements(
            SyntaxFactory.ForEachStatement(
                SyntaxFactory.IdentifierName("var"),
                SyntaxFactory.Identifier($"{node.InMemoryTableAlias}Row"),
                SyntaxFactory.IdentifierName(
                    $"{nameof(EvaluationHelper)}.{nameof(EvaluationHelper.ConvertTableToSource)}({node.InMemoryTableAlias}TransitionTable, false).{nameof(RowSource.Rows)}"),
                block([
                    SyntaxFactory.LocalDeclarationStatement(
                        SyntaxHelper.CreateAssignment("hasAnyRowMatched",
                            SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))),
                    getRowsSourceOrEmpty(node.SourceTable.Alias),
                    SyntaxFactory.ForEachStatement(
                        SyntaxFactory.IdentifierName("var"),
                        SyntaxFactory.Identifier($"{node.SourceTable.Alias}Row"),
                        SyntaxFactory.IdentifierName($"{node.SourceTable.Alias}Rows.Rows"),
                        SyntaxFactory.Block(
                            generateCancellationExpression(),
                            emptyBlock,
                            SyntaxFactory.IfStatement(
                                (PrefixUnaryExpressionSyntax)generator.LogicalNotExpression(
                                    SyntaxFactory.IdentifierName("hasAnyRowMatched")),
                                SyntaxFactory.Block(
                                    SyntaxFactory.ExpressionStatement(
                                        SyntaxFactory.AssignmentExpression(
                                            SyntaxKind.SimpleAssignmentExpression,
                                            SyntaxFactory.IdentifierName("hasAnyRowMatched"),
                                            (LiteralExpressionSyntax)generator.TrueLiteralExpression())))))),
                    SyntaxFactory.IfStatement(
                        (PrefixUnaryExpressionSyntax)generator.LogicalNotExpression(
                            SyntaxFactory.IdentifierName("hasAnyRowMatched")),
                        SyntaxFactory.Block(
                            SyntaxFactory.LocalDeclarationStatement(rewriteSelect),
                            SyntaxFactory.ExpressionStatement(invocation)))])));
    }

    /// <summary>
    /// Builds the expressions list for outer apply operations by processing table columns.
    /// </summary>
    private static List<ExpressionSyntax> BuildOuterApplyExpressions(
        ApplyInMemoryWithSourceTableFromNode node,
        SyntaxGenerator generator,
        TableSymbol fullTransitionTable,
        FieldsNamesSymbol fieldNames)
    {
        var expressions = new List<ExpressionSyntax>();
        var j = 0;

        
        for (var i = 0; i < fullTransitionTable.CompoundTables.Length - 1; i++)
        {
            foreach (var column in fullTransitionTable.GetColumns(fullTransitionTable.CompoundTables[i]))
            {
                expressions.Add(
                    SyntaxFactory.ElementAccessExpression(
                        SyntaxFactory.IdentifierName($"{node.InMemoryTableAlias}Row"),
                        SyntaxFactory.BracketedArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(
                                    (LiteralExpressionSyntax)generator.LiteralExpression(
                                        fieldNames.Names[j]))))));
                j += 1;
            }
        }

        
        foreach (var column in fullTransitionTable.GetColumns(fullTransitionTable.CompoundTables[^1]))
        {
            expressions.Add(
                SyntaxFactory.CastExpression(
                    SyntaxFactory.IdentifierName(EvaluationHelper.GetCastableType(column.ColumnType)),
                    (LiteralExpressionSyntax)generator.NullLiteralExpression()));
        }

        return expressions;
    }

    /// <summary>
    /// Creates a variable declaration for the rewrite select operation.
    /// </summary>
    private static VariableDeclarationSyntax CreateRewriteSelectVariable(List<ExpressionSyntax> expressions)
    {
        var arrayType = SyntaxFactory.ArrayType(
            SyntaxFactory.IdentifierName("object"),
            new SyntaxList<ArrayRankSpecifierSyntax>(
                SyntaxFactory.ArrayRankSpecifier(
                    SyntaxFactory.SingletonSeparatedList(
                        (ExpressionSyntax)SyntaxFactory.OmittedArraySizeExpression()))));

        return SyntaxFactory.VariableDeclaration(
            SyntaxFactory.IdentifierName("var"),
            SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.VariableDeclarator(
                    SyntaxFactory.Identifier("select"),
                    null,
                    SyntaxFactory.EqualsValueClause(
                        SyntaxFactory.ArrayCreationExpression(
                            arrayType,
                            SyntaxFactory.InitializerExpression(
                                SyntaxKind.ArrayInitializerExpression,
                                SyntaxFactory.SeparatedList(expressions)))))));
    }

    /// <summary>
    /// Creates the table add invocation expression for outer apply operations.
    /// </summary>
    private static InvocationExpressionSyntax CreateTableAddInvocation(
        ApplyInMemoryWithSourceTableFromNode node,
        Scope scope)
    {
        return SyntaxHelper.CreateMethodInvocation(
            scope[MetaAttributes.SelectIntoVariableName],
            nameof(Table.Add),
            [
                SyntaxFactory.Argument(
                    SyntaxFactory.ObjectCreationExpression(
                        SyntaxFactory.Token(SyntaxKind.NewKeyword)
                            .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                        SyntaxHelper.ObjectsRowTypeSyntax,
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(
                            [
                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("select")),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName($"{node.InMemoryTableAlias}Row"),
                                        SyntaxFactory.IdentifierName($"{nameof(IObjectResolver.Contexts)}"))),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression))
                            ])
                        ),
                        SyntaxFactory.InitializerExpression(SyntaxKind.ComplexElementInitializerExpression))
                )
            ]);
    }

    /// <summary>
    /// Validates that all required parameters are not null.
    /// </summary>
    private static void ValidateParameters(
        ApplyInMemoryWithSourceTableFromNode node,
        SyntaxGenerator generator,
        Scope scope,
        string queryAlias,
        Func<string, StatementSyntax> getRowsSourceOrEmpty,
        Func<StatementSyntax[], BlockSyntax> block,
        Func<StatementSyntax> generateCancellationExpression)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));
        if (generator == null)
            throw new ArgumentNullException(nameof(generator));
        if (scope == null)
            throw new ArgumentNullException(nameof(scope));
        if (string.IsNullOrEmpty(queryAlias))
            throw new ArgumentException("Query alias cannot be null or empty", nameof(queryAlias));
        if (getRowsSourceOrEmpty == null)
            throw new ArgumentNullException(nameof(getRowsSourceOrEmpty));
        if (block == null)
            throw new ArgumentNullException(nameof(block));
        if (generateCancellationExpression == null)
            throw new ArgumentNullException(nameof(generateCancellationExpression));
    }
}
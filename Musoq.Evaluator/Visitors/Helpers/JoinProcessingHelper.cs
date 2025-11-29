using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Runtime;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Tokens;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Visitors.Helpers;

/// <summary>
/// Helper class for processing join operations in ToCSharpRewriteTreeVisitor.
/// Extracts complex join logic to improve maintainability and testability.
/// </summary>
public static class JoinProcessingHelper
{
    /// <summary>
    /// Processes an inner join operation.
    /// </summary>
    /// <param name="node">The join node containing source and memory table information</param>
    /// <param name="ifStatement">The conditional statement for join logic</param>
    /// <param name="emptyBlock">The empty block syntax</param>
    /// <param name="generator">The syntax generator</param>
    /// <param name="getRowsSourceOrEmpty">Function to get rows source or empty statement</param>
    /// <param name="generateCancellationExpression">Function to generate cancellation expression</param>
    /// <returns>A BlockSyntax containing the inner join logic</returns>
    public static BlockSyntax ProcessInnerJoin(
        JoinInMemoryWithSourceTableFromNode node,
        StatementSyntax ifStatement,
        BlockSyntax emptyBlock,
        SyntaxGenerator generator,
        System.Func<string, StatementSyntax> getRowsSourceOrEmpty,
        System.Func<StatementSyntax> generateCancellationExpression)
    {
        ValidateJoinParameters(node, ifStatement, emptyBlock, generator, getRowsSourceOrEmpty, generateCancellationExpression);

        var computingBlock = SyntaxFactory.Block();
        
        return computingBlock.AddStatements(
            SyntaxFactory.ForEachStatement(
                SyntaxFactory.IdentifierName("var"),
                SyntaxFactory.Identifier($"{node.InMemoryTableAlias}Row"),
                SyntaxFactory.IdentifierName(
                    $"{nameof(EvaluationHelper)}.{nameof(EvaluationHelper.ConvertTableToSource)}({node.InMemoryTableAlias}TransitionTable, false).{nameof(RowSource.Rows)}"),
                Block(
                    getRowsSourceOrEmpty(node.SourceTable.Alias),
                    SyntaxFactory.ForEachStatement(
                        SyntaxFactory.IdentifierName("var"),
                        SyntaxFactory.Identifier($"{node.SourceTable.Alias}Row"),
                        SyntaxFactory.IdentifierName($"{node.SourceTable.Alias}Rows.Rows"),
                        SyntaxFactory.Block(
                            generateCancellationExpression(),
                            ifStatement,
                            emptyBlock)))));
    }

    /// <summary>
    /// Processes an outer left join operation.
    /// </summary>
    /// <param name="node">The join node containing source and memory table information</param>
    /// <param name="ifStatement">The conditional statement for join logic</param>
    /// <param name="emptyBlock">The empty block syntax</param>
    /// <param name="generator">The syntax generator</param>
    /// <param name="scope">The current scope for symbol table access</param>
    /// <param name="queryAlias">The query alias</param>
    /// <param name="getRowsSourceOrEmpty">Function to get rows source or empty statement</param>
    /// <param name="generateCancellationExpression">Function to generate cancellation expression</param>
    /// <returns>A BlockSyntax containing the outer left join logic</returns>
    public static BlockSyntax ProcessOuterLeftJoin(
        JoinInMemoryWithSourceTableFromNode node,
        StatementSyntax ifStatement,
        BlockSyntax emptyBlock,
        SyntaxGenerator generator,
        Scope scope,
        string queryAlias,
        System.Func<string, StatementSyntax> getRowsSourceOrEmpty,
        System.Func<StatementSyntax> generateCancellationExpression)
    {
        ValidateOuterJoinParameters(node, ifStatement, emptyBlock, generator, scope, queryAlias, getRowsSourceOrEmpty, generateCancellationExpression);

        var computingBlock = SyntaxFactory.Block();
        var fullTransitionTable = scope.ScopeSymbolTable.GetSymbol<TableSymbol>(queryAlias);
        var fieldNames = scope.ScopeSymbolTable.GetSymbol<FieldsNamesSymbol>(MetaAttributes.OuterJoinSelect);
        
        var expressions = BuildLeftJoinExpressions(node, fullTransitionTable, fieldNames, generator);
        var rewriteSelect = CreateSelectVariableDeclaration(expressions);
        var invocation = CreateTableAddInvocation(node, scope);

        return computingBlock.AddStatements(
            getRowsSourceOrEmpty(node.InMemoryTableAlias),
            SyntaxFactory.ForEachStatement(
                SyntaxFactory.IdentifierName("var"),
                SyntaxFactory.Identifier($"{node.InMemoryTableAlias}Row"),
                SyntaxFactory.IdentifierName(
                    $"{nameof(EvaluationHelper)}.{nameof(EvaluationHelper.ConvertTableToSource)}({node.InMemoryTableAlias}TransitionTable, false).{nameof(RowSource.Rows)}"),
                Block(
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
                            ifStatement,
                            emptyBlock,
                            SyntaxFactory.IfStatement(
                                (PrefixUnaryExpressionSyntax) generator.LogicalNotExpression(
                                    SyntaxFactory.IdentifierName("hasAnyRowMatched")),
                                SyntaxFactory.Block(
                                    SyntaxFactory.ExpressionStatement(
                                        SyntaxFactory.AssignmentExpression(
                                            SyntaxKind.SimpleAssignmentExpression,
                                            SyntaxFactory.IdentifierName("hasAnyRowMatched"),
                                            (LiteralExpressionSyntax) generator.TrueLiteralExpression())))))),
                    SyntaxFactory.IfStatement(
                        (PrefixUnaryExpressionSyntax) generator.LogicalNotExpression(
                            SyntaxFactory.IdentifierName("hasAnyRowMatched")),
                        SyntaxFactory.Block(
                            SyntaxFactory.LocalDeclarationStatement(rewriteSelect),
                            SyntaxFactory.ExpressionStatement(invocation))))));
    }

    /// <summary>
    /// Processes an outer right join operation.
    /// </summary>
    /// <param name="node">The join node containing source and memory table information</param>
    /// <param name="ifStatement">The conditional statement for join logic</param>
    /// <param name="emptyBlock">The empty block syntax</param>
    /// <param name="generator">The syntax generator</param>
    /// <param name="scope">The current scope for symbol table access</param>
    /// <param name="queryAlias">The query alias</param>
    /// <param name="getRowsSourceOrEmpty">Function to get rows source or empty statement</param>
    /// <param name="generateCancellationExpression">Function to generate cancellation expression</param>
    /// <returns>A BlockSyntax containing the outer right join logic</returns>
    public static BlockSyntax ProcessOuterRightJoin(
        JoinInMemoryWithSourceTableFromNode node,
        StatementSyntax ifStatement,
        BlockSyntax emptyBlock,
        SyntaxGenerator generator,
        Scope scope,
        string queryAlias,
        System.Func<string, StatementSyntax> getRowsSourceOrEmpty,
        System.Func<StatementSyntax> generateCancellationExpression)
    {
        ValidateOuterJoinParameters(node, ifStatement, emptyBlock, generator, scope, queryAlias, getRowsSourceOrEmpty, generateCancellationExpression);

        var computingBlock = SyntaxFactory.Block();
        var fullTransitionTable = scope.ScopeSymbolTable.GetSymbol<TableSymbol>(queryAlias);
        var fieldNames = scope.ScopeSymbolTable.GetSymbol<FieldsNamesSymbol>(MetaAttributes.OuterJoinSelect);
        
        var expressions = BuildRightJoinExpressions(node, fullTransitionTable, fieldNames, generator);
        var rewriteSelect = CreateSelectVariableDeclaration(expressions);
        var invocation = CreateTableAddInvocationForRightJoin(node, scope);

        return computingBlock.AddStatements(
            getRowsSourceOrEmpty(node.SourceTable.Alias),
            SyntaxFactory.ForEachStatement(
                SyntaxFactory.IdentifierName("var"),
                SyntaxFactory.Identifier($"{node.SourceTable.Alias}Row"),
                SyntaxFactory.IdentifierName($"{node.SourceTable.Alias}Rows.Rows"),
                Block(
                    SyntaxFactory.LocalDeclarationStatement(
                        SyntaxHelper.CreateAssignment("hasAnyRowMatched",
                            SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))),
                    getRowsSourceOrEmpty(node.InMemoryTableAlias),
                    SyntaxFactory.ForEachStatement(
                        SyntaxFactory.IdentifierName("var"),
                        SyntaxFactory.Identifier($"{node.InMemoryTableAlias}Row"),
                        SyntaxFactory.IdentifierName(
                            $"{nameof(EvaluationHelper)}.{nameof(EvaluationHelper.ConvertTableToSource)}({node.InMemoryTableAlias}TransitionTable, false).{nameof(RowSource.Rows)}"),
                        SyntaxFactory.Block(
                            generateCancellationExpression(),
                            ifStatement,
                            emptyBlock,
                            SyntaxFactory.IfStatement(
                                (PrefixUnaryExpressionSyntax) generator.LogicalNotExpression(
                                    SyntaxFactory.IdentifierName("hasAnyRowMatched")),
                                SyntaxFactory.Block(
                                    SyntaxFactory.ExpressionStatement(
                                        SyntaxFactory.AssignmentExpression(
                                            SyntaxKind.SimpleAssignmentExpression,
                                            SyntaxFactory.IdentifierName("hasAnyRowMatched"),
                                            (LiteralExpressionSyntax) generator.TrueLiteralExpression())))))),
                    SyntaxFactory.IfStatement(
                        (PrefixUnaryExpressionSyntax) generator.LogicalNotExpression(
                            SyntaxFactory.IdentifierName("hasAnyRowMatched")),
                        SyntaxFactory.Block(
                            SyntaxFactory.LocalDeclarationStatement(rewriteSelect),
                            SyntaxFactory.ExpressionStatement(invocation))))));
    }

    /// <summary>
    /// Builds expressions for left join operations.
    /// </summary>
    private static List<ExpressionSyntax> BuildLeftJoinExpressions(
        JoinInMemoryWithSourceTableFromNode node,
        TableSymbol fullTransitionTable,
        FieldsNamesSymbol fieldNames,
        SyntaxGenerator generator)
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
                                    (LiteralExpressionSyntax) generator.LiteralExpression(
                                        fieldNames.Names[j]))))));
                j += 1;
            }
        }

        foreach (var column in fullTransitionTable.GetColumns(fullTransitionTable.CompoundTables[^1]))
        {
            expressions.Add(
                SyntaxFactory.CastExpression(
                    SyntaxFactory.IdentifierName(EvaluationHelper.GetCastableType(column.ColumnType)),
                    (LiteralExpressionSyntax) generator.NullLiteralExpression()));
        }

        return expressions;
    }

    /// <summary>
    /// Builds expressions for right join operations.
    /// </summary>
    private static List<ExpressionSyntax> BuildRightJoinExpressions(
        JoinInMemoryWithSourceTableFromNode node,
        TableSymbol fullTransitionTable,
        FieldsNamesSymbol fieldNames,
        SyntaxGenerator generator)
    {
        var expressions = new List<ExpressionSyntax>();
        var j = 0;

        for (var i = 0; i < fullTransitionTable.CompoundTables.Length - 1; i++)
        {
            foreach (var column in fullTransitionTable.GetColumns(fullTransitionTable.CompoundTables[i]))
            {
                expressions.Add(
                    SyntaxFactory.CastExpression(
                        SyntaxFactory.IdentifierName(EvaluationHelper.GetCastableType(column.ColumnType)),
                        (LiteralExpressionSyntax) generator.NullLiteralExpression()));
                j += 1;
            }
        }

        foreach (var column in fullTransitionTable.GetColumns(fullTransitionTable.CompoundTables[^1]))
        {
            expressions.Add(
                SyntaxFactory.ElementAccessExpression(
                    SyntaxFactory.IdentifierName($"{node.SourceTable.Alias}Row"),
                    SyntaxFactory.BracketedArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(
                                (LiteralExpressionSyntax) generator.LiteralExpression(
                                    fieldNames.Names[j++]))))));
        }

        return expressions;
    }

    /// <summary>
    /// Creates a variable declaration for the select statement.
    /// </summary>
    private static VariableDeclarationSyntax CreateSelectVariableDeclaration(List<ExpressionSyntax> expressions)
    {
        var arrayType = SyntaxFactory.ArrayType(
            SyntaxFactory.IdentifierName("object"),
            new SyntaxList<ArrayRankSpecifierSyntax>(
                SyntaxFactory.ArrayRankSpecifier(
                    SyntaxFactory.SingletonSeparatedList(
                        (ExpressionSyntax) SyntaxFactory.OmittedArraySizeExpression()))));

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
    /// Creates a table add invocation for left join operations.
    /// </summary>
    private static InvocationExpressionSyntax CreateTableAddInvocation(
        JoinInMemoryWithSourceTableFromNode node,
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
                                        SyntaxFactory.IdentifierName(
                                            $"{nameof(IObjectResolver.Contexts)}"))),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression))
                            ])
                        ),
                        SyntaxFactory.InitializerExpression(SyntaxKind.ComplexElementInitializerExpression))
                )
            ]);
    }

    /// <summary>
    /// Creates a table add invocation for right join operations.
    /// </summary>
    private static InvocationExpressionSyntax CreateTableAddInvocationForRightJoin(
        JoinInMemoryWithSourceTableFromNode node,
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
                                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName($"{node.SourceTable.Alias}Row"),
                                        SyntaxFactory.IdentifierName(
                                            $"{nameof(IObjectResolver.Contexts)}")))
                            ])
                        ),
                        SyntaxFactory.InitializerExpression(SyntaxKind.ComplexElementInitializerExpression))
                )
            ]);
    }

    /// <summary>
    /// Creates a block from statements, filtering out empty statements.
    /// </summary>
    private static BlockSyntax Block(params StatementSyntax[] statements)
    {
        return SyntaxFactory.Block(statements.Where(f => f is not EmptyStatementSyntax));
    }

    /// <summary>
    /// Validates parameters for basic join operations.
    /// </summary>
    private static void ValidateJoinParameters(
        JoinInMemoryWithSourceTableFromNode node,
        StatementSyntax ifStatement,
        BlockSyntax emptyBlock,
        SyntaxGenerator generator,
        System.Func<string, StatementSyntax> getRowsSourceOrEmpty,
        System.Func<StatementSyntax> generateCancellationExpression)
    {
        if (node == null) throw new System.ArgumentNullException(nameof(node));
        if (ifStatement == null) throw new System.ArgumentNullException(nameof(ifStatement));
        if (emptyBlock == null) throw new System.ArgumentNullException(nameof(emptyBlock));
        if (generator == null) throw new System.ArgumentNullException(nameof(generator));
        if (getRowsSourceOrEmpty == null) throw new System.ArgumentNullException(nameof(getRowsSourceOrEmpty));
        if (generateCancellationExpression == null) throw new System.ArgumentNullException(nameof(generateCancellationExpression));
    }

    /// <summary>
    /// Validates parameters for outer join operations.
    /// </summary>
    private static void ValidateOuterJoinParameters(
        JoinInMemoryWithSourceTableFromNode node,
        StatementSyntax ifStatement,
        BlockSyntax emptyBlock,
        SyntaxGenerator generator,
        Scope scope,
        string queryAlias,
        System.Func<string, StatementSyntax> getRowsSourceOrEmpty,
        System.Func<StatementSyntax> generateCancellationExpression)
    {
        ValidateJoinParameters(node, ifStatement, emptyBlock, generator, getRowsSourceOrEmpty, generateCancellationExpression);
        
        if (scope == null) throw new System.ArgumentNullException(nameof(scope));
        if (string.IsNullOrEmpty(queryAlias)) throw new System.ArgumentException("Query alias cannot be null or empty", nameof(queryAlias));
    }
}
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
using Musoq.Evaluator.Visitors.CodeGeneration;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Visitors.Helpers;

/// <summary>
/// Processes ApplySourcesTableFromNode to generate C# syntax for CROSS APPLY and OUTER APPLY operations.
/// </summary>
public static class ApplySourcesTableNodeProcessor
{
    public readonly struct ApplySourcesResult
    {
        public BlockSyntax EmptyBlock { get; init; }
        public BlockSyntax ComputingBlock { get; init; }
    }

    public static ApplySourcesResult ProcessApplySourcesTable(
        ApplySourcesTableFromNode node,
        SyntaxGenerator generator,
        Scope scope,
        string queryAlias,
        Func<string, StatementSyntax> getRowsSourceOrEmpty,
        Func<StatementSyntax[], BlockSyntax> block,
        Func<StatementSyntax> generateCancellationExpression)
    {
        var emptyBlock = StatementEmitter.CreateEmptyBlock();
        var computingBlock = StatementEmitter.CreateEmptyBlock();

        switch (node.ApplyType)
        {
            case ApplyType.Cross:
                computingBlock = ProcessCrossApply(node, getRowsSourceOrEmpty, block, generateCancellationExpression, emptyBlock);
                break;
            case ApplyType.Outer:
                computingBlock = ProcessOuterApply(node, generator, scope, queryAlias, getRowsSourceOrEmpty, block, generateCancellationExpression, emptyBlock);
                break;
        }

        return new ApplySourcesResult
        {
            EmptyBlock = emptyBlock,
            ComputingBlock = computingBlock
        };
    }

    private static BlockSyntax ProcessCrossApply(
        ApplySourcesTableFromNode node,
        Func<string, StatementSyntax> getRowsSourceOrEmpty,
        Func<StatementSyntax[], BlockSyntax> block,
        Func<StatementSyntax> generateCancellationExpression,
        BlockSyntax emptyBlock)
    {
        return StatementEmitter.CreateEmptyBlock()
            .AddStatements(
                getRowsSourceOrEmpty(node.First.Alias),
                StatementEmitter.CreateForeach($"{node.First.Alias}Row",
                    SyntaxFactory.IdentifierName($"{node.First.Alias}Rows.Rows"),
                    block([
                        getRowsSourceOrEmpty(node.Second.Alias),
                        StatementEmitter.CreateForeach($"{node.Second.Alias}Row",
                            SyntaxFactory.IdentifierName($"{node.Second.Alias}Rows.Rows"),
                            StatementEmitter.CreateBlock(
                                generateCancellationExpression(),
                                emptyBlock))
                    ])));
    }

    private static BlockSyntax ProcessOuterApply(
        ApplySourcesTableFromNode node,
        SyntaxGenerator generator,
        Scope scope,
        string queryAlias,
        Func<string, StatementSyntax> getRowsSourceOrEmpty,
        Func<StatementSyntax[], BlockSyntax> block,
        Func<StatementSyntax> generateCancellationExpression,
        BlockSyntax emptyBlock)
    {
        var fullTransitionTable = scope.ScopeSymbolTable.GetSymbol<TableSymbol>(queryAlias);
        
        var expressions = BuildColumnExpressions(node, generator, fullTransitionTable);
        var rewriteSelect = CreateSelectArrayDeclaration(expressions);
        var addInvocation = CreateTableAddInvocation(scope, node.First.Alias);

        return StatementEmitter.CreateEmptyBlock()
            .AddStatements(
                getRowsSourceOrEmpty(node.First.Alias),
                StatementEmitter.CreateForeach($"{node.First.Alias}Row",
                    SyntaxFactory.IdentifierName($"{node.First.Alias}Rows.Rows"),
                    block([
                        SyntaxFactory.LocalDeclarationStatement(
                            SyntaxHelper.CreateAssignment("hasAnyRowMatched",
                                (LiteralExpressionSyntax) generator.FalseLiteralExpression())),
                        getRowsSourceOrEmpty(node.Second.Alias),
                        StatementEmitter.CreateForeach($"{node.Second.Alias}Row",
                            SyntaxFactory.IdentifierName($"{node.Second.Alias}Rows.Rows"),
                            StatementEmitter.CreateBlock(
                                generateCancellationExpression(),
                                emptyBlock,
                                StatementEmitter.CreateIf(
                                    (PrefixUnaryExpressionSyntax) generator.LogicalNotExpression(
                                        SyntaxFactory.IdentifierName("hasAnyRowMatched")),
                                    StatementEmitter.CreateBlock(
                                        StatementEmitter.CreateAssignment("hasAnyRowMatched",
                                            (LiteralExpressionSyntax) generator.TrueLiteralExpression()))))),
                        StatementEmitter.CreateIf(
                            (PrefixUnaryExpressionSyntax) generator.LogicalNotExpression(
                                SyntaxFactory.IdentifierName("hasAnyRowMatched")),
                            StatementEmitter.CreateBlock(
                                SyntaxFactory.LocalDeclarationStatement(rewriteSelect),
                                SyntaxFactory.ExpressionStatement(addInvocation)))
                    ])));
    }

    private static List<ExpressionSyntax> BuildColumnExpressions(
        ApplySourcesTableFromNode node,
        SyntaxGenerator generator,
        TableSymbol fullTransitionTable)
    {
        var expressions = new List<ExpressionSyntax>();

        foreach (var column in fullTransitionTable.GetColumns(fullTransitionTable.CompoundTables[0]))
        {
            expressions.Add(
                SyntaxFactory.ElementAccessExpression(
                    SyntaxFactory.IdentifierName($"{node.First.Alias}Row"),
                    SyntaxFactory.BracketedArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(
                                (LiteralExpressionSyntax) generator.LiteralExpression(column.ColumnName))))));
        }

        foreach (var column in fullTransitionTable.GetColumns(fullTransitionTable.CompoundTables[1]))
        {
            expressions.Add(
                SyntaxFactory.CastExpression(
                    SyntaxFactory.IdentifierName(EvaluationHelper.GetCastableType(column.ColumnType)),
                    (LiteralExpressionSyntax) generator.NullLiteralExpression()));
        }

        return expressions;
    }

    private static VariableDeclarationSyntax CreateSelectArrayDeclaration(List<ExpressionSyntax> expressions)
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

    private static InvocationExpressionSyntax CreateTableAddInvocation(Scope scope, string firstAlias)
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
                                        SyntaxFactory.IdentifierName($"{firstAlias}Row"),
                                        SyntaxFactory.IdentifierName(nameof(IObjectResolver.Contexts)))),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression))
                            ])
                        ),
                        SyntaxFactory.InitializerExpression(SyntaxKind.ComplexElementInitializerExpression))
                )
            ]);
    }
}

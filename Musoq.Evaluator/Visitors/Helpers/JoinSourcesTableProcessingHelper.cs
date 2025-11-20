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
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Visitors.Helpers;

/// <summary>
/// Helper class for processing JoinSourcesTableFromNode operations.
/// Handles Inner, OuterLeft, and OuterRight join processing logic.
/// </summary>
public static class JoinSourcesTableProcessingHelper
{
    /// <summary>
    /// Processes a JoinSourcesTableFromNode and returns the appropriate computing block based on join type.
    /// </summary>
    /// <param name="node">The join node to process</param>
    /// <param name="generator">Syntax generator for creating syntax nodes</param>
    /// <param name="scope">Current evaluation scope</param>
    /// <param name="queryAlias">Alias for the query</param>
    /// <param name="ifStatement">The conditional statement for the join</param>
    /// <param name="emptyBlock">Empty block for join processing</param>
    /// <param name="getRowsSourceOrEmpty">Function to get rows source or empty statement</param>
    /// <param name="block">Function to create block syntax</param>
    /// <param name="generateCancellationExpression">Function to generate cancellation expression</param>
    /// <returns>BlockSyntax containing the join processing logic</returns>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is null</exception>
    /// <exception cref="ArgumentException">Thrown when unsupported join type is encountered</exception>
    public static BlockSyntax ProcessJoinSourcesTable(
        JoinSourcesTableFromNode node,
        SyntaxGenerator generator,
        Scope scope,
        string queryAlias,
        SyntaxNode ifStatement,
        BlockSyntax emptyBlock,
        Func<string, StatementSyntax> getRowsSourceOrEmpty,
        Func<StatementSyntax[], BlockSyntax> block,
        Func<StatementSyntax> generateCancellationExpression,
        CompilationOptions compilationOptions = null)
    {
        // Validate parameters
        ValidateParameter(nameof(node), node);
        ValidateParameter(nameof(generator), generator);
        ValidateParameter(nameof(scope), scope);
        ValidateParameter(nameof(queryAlias), queryAlias);
        ValidateParameter(nameof(ifStatement), ifStatement);
        ValidateParameter(nameof(emptyBlock), emptyBlock);
        ValidateParameter(nameof(getRowsSourceOrEmpty), getRowsSourceOrEmpty);
        ValidateParameter(nameof(block), block);
        ValidateParameter(nameof(generateCancellationExpression), generateCancellationExpression);

        var computingBlock = SyntaxFactory.Block();
        
        if (node.JoinType == JoinType.Inner && compilationOptions?.UseHashJoin == true)
        {
            if (TryGetHashJoinKeys(node, out var leftKey, out var rightKey, out var keyType))
            {
                return ProcessHashJoin(node, generator, scope, queryAlias, leftKey, rightKey, keyType, emptyBlock, getRowsSourceOrEmpty, block, generateCancellationExpression);
            }
        }
        
        switch (node.JoinType)
        {
            case JoinType.Inner:
                return ProcessInnerJoin(node, generator, ifStatement, emptyBlock, getRowsSourceOrEmpty, block, generateCancellationExpression);
                
            case JoinType.OuterLeft:
                return ProcessOuterLeftJoin(node, generator, scope, queryAlias, ifStatement, emptyBlock, getRowsSourceOrEmpty, block, generateCancellationExpression);
                
            case JoinType.OuterRight:
                return ProcessOuterRightJoin(node, generator, scope, queryAlias, ifStatement, emptyBlock, getRowsSourceOrEmpty, block, generateCancellationExpression);
                
            case JoinType.Hash:
                // This case might be unreachable if we handle it above, but good to keep for explicit JoinType.Hash
                // However, we need keys for ProcessHashJoin.
                // If JoinType is explicitly Hash, we assume keys are extractable or we fallback?
                // For now, let's try to extract keys.
                if (TryGetHashJoinKeys(node, out var leftKey, out var rightKey, out var keyType))
                {
                    return ProcessHashJoin(node, generator, scope, queryAlias, leftKey, rightKey, keyType, emptyBlock, getRowsSourceOrEmpty, block, generateCancellationExpression);
                }
                // Fallback to Inner if keys cannot be extracted (should not happen for valid Hash Join)
                return ProcessInnerJoin(node, generator, ifStatement, emptyBlock, getRowsSourceOrEmpty, block, generateCancellationExpression);
                
            default:
                throw new ArgumentException($"Unsupported join type: {node.JoinType}");
        }
    }

    private static BlockSyntax ProcessInnerJoin(
        JoinSourcesTableFromNode node,
        SyntaxGenerator generator,
        SyntaxNode ifStatement,
        BlockSyntax emptyBlock,
        Func<string, StatementSyntax> getRowsSourceOrEmpty,
        Func<StatementSyntax[], BlockSyntax> block,
        Func<StatementSyntax> generateCancellationExpression)
    {
        var computingBlock = SyntaxFactory.Block();
        
        return computingBlock.AddStatements(
            getRowsSourceOrEmpty(node.First.Alias),
            SyntaxFactory.ForEachStatement(
                SyntaxFactory.IdentifierName("var"),
                SyntaxFactory.Identifier($"{node.First.Alias}Row"),
                SyntaxFactory.IdentifierName($"{node.First.Alias}Rows.Rows"),
                block([
                    getRowsSourceOrEmpty(node.Second.Alias),
                    SyntaxFactory.ForEachStatement(
                        SyntaxFactory.IdentifierName("var"),
                        SyntaxFactory.Identifier($"{node.Second.Alias}Row"),
                        SyntaxFactory.IdentifierName($"{node.Second.Alias}Rows.Rows"),
                        SyntaxFactory.Block(
                            generateCancellationExpression(),
                            (StatementSyntax)ifStatement,
                            emptyBlock))
                ])));
    }

    private static BlockSyntax ProcessOuterLeftJoin(
        JoinSourcesTableFromNode node,
        SyntaxGenerator generator,
        Scope scope,
        string queryAlias,
        SyntaxNode ifStatement,
        BlockSyntax emptyBlock,
        Func<string, StatementSyntax> getRowsSourceOrEmpty,
        Func<StatementSyntax[], BlockSyntax> block,
        Func<StatementSyntax> generateCancellationExpression)
    {
        var computingBlock = SyntaxFactory.Block();
        var fullTransitionTable = scope.ScopeSymbolTable.GetSymbol<TableSymbol>(queryAlias);
        var expressions = new List<ExpressionSyntax>();

        // Create expressions for first table columns
        foreach (var column in fullTransitionTable.GetColumns(fullTransitionTable.CompoundTables[0]))
        {
            expressions.Add(
                SyntaxFactory.ElementAccessExpression(
                    SyntaxFactory.IdentifierName($"{node.First.Alias}Row"),
                    SyntaxFactory.BracketedArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(
                                (LiteralExpressionSyntax)generator.LiteralExpression(
                                    column.ColumnName))))));
        }

        // Create null expressions for second table columns
        foreach (var column in fullTransitionTable.GetColumns(fullTransitionTable.CompoundTables[1]))
        {
            expressions.Add(
                SyntaxFactory.CastExpression(
                    SyntaxFactory.IdentifierName(
                        EvaluationHelper.GetCastableType(column.ColumnType)),
                    (LiteralExpressionSyntax)generator.NullLiteralExpression()));
        }

        var (arrayType, rewriteSelect, invocation) = CreateSelectAndInvocationForOuterLeft(expressions, generator, scope, node.First.Alias);

        return computingBlock.AddStatements(
            getRowsSourceOrEmpty(node.First.Alias),
            SyntaxFactory.ForEachStatement(
                SyntaxFactory.IdentifierName("var"),
                SyntaxFactory.Identifier($"{node.First.Alias}Row"),
                SyntaxFactory.IdentifierName($"{node.First.Alias}Rows.Rows"),
                block([
                    SyntaxFactory.LocalDeclarationStatement(
                        SyntaxHelper.CreateAssignment("hasAnyRowMatched",
                            (LiteralExpressionSyntax)generator.FalseLiteralExpression())),
                    getRowsSourceOrEmpty(node.Second.Alias),
                    SyntaxFactory.ForEachStatement(
                        SyntaxFactory.IdentifierName("var"),
                        SyntaxFactory.Identifier($"{node.Second.Alias}Row"),
                        SyntaxFactory.IdentifierName($"{node.Second.Alias}Rows.Rows"),
                        SyntaxFactory.Block(
                            generateCancellationExpression(),
                            (StatementSyntax)ifStatement,
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
                            SyntaxFactory.ExpressionStatement(invocation)))
                ])));
    }

    private static BlockSyntax ProcessOuterRightJoin(
        JoinSourcesTableFromNode node,
        SyntaxGenerator generator,
        Scope scope,
        string queryAlias,
        SyntaxNode ifStatement,
        BlockSyntax emptyBlock,
        Func<string, StatementSyntax> getRowsSourceOrEmpty,
        Func<StatementSyntax[], BlockSyntax> block,
        Func<StatementSyntax> generateCancellationExpression)
    {
        var computingBlock = SyntaxFactory.Block();
        var fullTransitionTable = scope.ScopeSymbolTable.GetSymbol<TableSymbol>(queryAlias);
        var expressions = new List<ExpressionSyntax>();

        // Create null expressions for first table columns
        foreach (var column in fullTransitionTable.GetColumns(fullTransitionTable.CompoundTables[0]))
        {
            expressions.Add(
                SyntaxFactory.CastExpression(
                    SyntaxFactory.IdentifierName(
                        EvaluationHelper.GetCastableType(column.ColumnType)),
                    (LiteralExpressionSyntax)generator.NullLiteralExpression()));
        }

        // Create expressions for second table columns
        foreach (var column in fullTransitionTable.GetColumns(fullTransitionTable.CompoundTables[1]))
        {
            expressions.Add(
                SyntaxFactory.ElementAccessExpression(
                    SyntaxFactory.IdentifierName($"{node.Second.Alias}Row"),
                    SyntaxFactory.BracketedArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(
                                (LiteralExpressionSyntax)generator.LiteralExpression(
                                    column.ColumnName))))));
        }

        var (arrayType, rewriteSelect, invocation) = CreateSelectAndInvocationForOuterRight(expressions, generator, scope, node.Second.Alias);

        return computingBlock.AddStatements(
            getRowsSourceOrEmpty(node.Second.Alias),
            SyntaxFactory.ForEachStatement(
                SyntaxFactory.IdentifierName("var"),
                SyntaxFactory.Identifier($"{node.Second.Alias}Row"),
                SyntaxFactory.IdentifierName($"{node.Second.Alias}Rows.Rows"),
                block([
                    SyntaxFactory.LocalDeclarationStatement(
                        SyntaxHelper.CreateAssignment("hasAnyRowMatched",
                            (LiteralExpressionSyntax)generator.FalseLiteralExpression())),
                    getRowsSourceOrEmpty(node.First.Alias),
                    SyntaxFactory.ForEachStatement(
                        SyntaxFactory.IdentifierName("var"),
                        SyntaxFactory.Identifier($"{node.First.Alias}Row"),
                        SyntaxFactory.IdentifierName($"{node.First.Alias}Rows.Rows"),
                        SyntaxFactory.Block(
                            generateCancellationExpression(),
                            (StatementSyntax)ifStatement,
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
                            SyntaxFactory.ExpressionStatement(invocation)))
                ])));
    }

    public static BlockSyntax ProcessHashJoin(
        JoinSourcesTableFromNode node,
        SyntaxGenerator generator,
        Scope scope,
        string queryAlias,
        ExpressionSyntax leftKey,
        ExpressionSyntax rightKey,
        Type keyType,
        BlockSyntax emptyBlock,
        Func<string, StatementSyntax> getRowsSourceOrEmpty,
        Func<StatementSyntax[], BlockSyntax> block,
        Func<StatementSyntax> generateCancellationExpression)
    {
        var computingBlock = SyntaxFactory.Block();
        
        var keyTypeName = EvaluationHelper.GetCastableType(keyType);
        var dictionaryType = SyntaxFactory.ParseTypeName($"System.Collections.Generic.Dictionary<{keyTypeName}, System.Collections.Generic.List<Musoq.Schema.DataSources.IObjectResolver>>");
        
        var dictionaryName = $"{node.Second.Alias}Hashed";
        var dictionaryCreation = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(
                SyntaxFactory.IdentifierName("var"),
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator(
                        SyntaxFactory.Identifier(dictionaryName),
                        null,
                        SyntaxFactory.EqualsValueClause(
                            SyntaxFactory.ObjectCreationExpression(dictionaryType)
                                .WithArgumentList(SyntaxFactory.ArgumentList())
                        )
                    )
                )
            )
        );

        // Build Phase (Right Table)
        var buildPhase = block([
            getRowsSourceOrEmpty(node.Second.Alias),
            SyntaxFactory.ForEachStatement(
                SyntaxFactory.IdentifierName("var"),
                SyntaxFactory.Identifier($"{node.Second.Alias}Row"),
                SyntaxFactory.IdentifierName($"{node.Second.Alias}Rows.Rows"),
                block([
                    generateCancellationExpression(),
                    SyntaxFactory.LocalDeclarationStatement(
                        SyntaxFactory.VariableDeclaration(
                            SyntaxFactory.IdentifierName("var"),
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.VariableDeclarator(
                                    SyntaxFactory.Identifier("key"),
                                    null,
                                    SyntaxFactory.EqualsValueClause(rightKey)
                                )
                            )
                        )
                    ),
                    SyntaxFactory.IfStatement(
                        SyntaxFactory.PrefixUnaryExpression(
                            SyntaxKind.LogicalNotExpression,
                            SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName(dictionaryName),
                                    SyntaxFactory.IdentifierName("ContainsKey")
                                ),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("key"))
                                    )
                                )
                            )
                        ),
                        SyntaxFactory.Block(
                            SyntaxFactory.ExpressionStatement(
                                SyntaxFactory.AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    SyntaxFactory.ElementAccessExpression(
                                        SyntaxFactory.IdentifierName(dictionaryName),
                                        SyntaxFactory.BracketedArgumentList(
                                            SyntaxFactory.SingletonSeparatedList(
                                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("key"))
                                            )
                                        )
                                    ),
                                    SyntaxFactory.ObjectCreationExpression(
                                        SyntaxFactory.ParseTypeName("System.Collections.Generic.List<Musoq.Schema.DataSources.IObjectResolver>")
                                    ).WithArgumentList(SyntaxFactory.ArgumentList())
                                )
                            )
                        )
                    ),
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.ElementAccessExpression(
                                    SyntaxFactory.IdentifierName(dictionaryName),
                                    SyntaxFactory.BracketedArgumentList(
                                        SyntaxFactory.SingletonSeparatedList(
                                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("key"))
                                        )
                                    )
                                ),
                                SyntaxFactory.IdentifierName("Add")
                            ),
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName($"{node.Second.Alias}Row"))
                                )
                            )
                        )
                    )
                ])
            )
        ]);

        // Probe Phase (Left Table)
        var probePhase = block([
            getRowsSourceOrEmpty(node.First.Alias),
            SyntaxFactory.ForEachStatement(
                SyntaxFactory.IdentifierName("var"),
                SyntaxFactory.Identifier($"{node.First.Alias}Row"),
                SyntaxFactory.IdentifierName($"{node.First.Alias}Rows.Rows"),
                block([
                    generateCancellationExpression(),
                    SyntaxFactory.LocalDeclarationStatement(
                        SyntaxFactory.VariableDeclaration(
                            SyntaxFactory.IdentifierName("var"),
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.VariableDeclarator(
                                    SyntaxFactory.Identifier("key"),
                                    null,
                                    SyntaxFactory.EqualsValueClause(leftKey)
                                )
                            )
                        )
                    ),
                    SyntaxFactory.IfStatement(
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName(dictionaryName),
                                SyntaxFactory.IdentifierName("TryGetValue")
                            ),
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SeparatedList(new [] {
                                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName("key")),
                                    SyntaxFactory.Argument(
                                        null,
                                        SyntaxFactory.Token(SyntaxKind.OutKeyword),
                                        SyntaxFactory.DeclarationExpression(
                                            SyntaxFactory.IdentifierName("var"),
                                            SyntaxFactory.SingleVariableDesignation(
                                                SyntaxFactory.Identifier("matches")
                                            )
                                        )
                                    )
                                })
                            )
                        ),
                        block([
                            SyntaxFactory.ForEachStatement(
                                SyntaxFactory.IdentifierName("var"),
                                SyntaxFactory.Identifier($"{node.Second.Alias}Row"),
                                SyntaxFactory.IdentifierName("matches"),
                                block([
                                    generateCancellationExpression(),
                                    emptyBlock
                                ])
                            )
                        ])
                    )
                ])
            )
        ]);

        return computingBlock.AddStatements(dictionaryCreation, buildPhase, probePhase);
    }

    private static (ArrayTypeSyntax arrayType, VariableDeclarationSyntax rewriteSelect, InvocationExpressionSyntax invocation) 
        CreateSelectAndInvocationForOuterLeft(
            List<ExpressionSyntax> expressions, 
            SyntaxGenerator generator, 
            Scope scope, 
            string firstAlias)
    {
        var arrayType = SyntaxFactory.ArrayType(
            SyntaxFactory.IdentifierName("object"),
            new SyntaxList<ArrayRankSpecifierSyntax>(
                SyntaxFactory.ArrayRankSpecifier(
                    SyntaxFactory.SingletonSeparatedList(
                        (ExpressionSyntax)SyntaxFactory.OmittedArraySizeExpression()))));

        var rewriteSelect = SyntaxFactory.VariableDeclaration(
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

        var invocation = SyntaxHelper.CreateMethodInvocation(
            scope[MetaAttributes.SelectIntoVariableName],
            nameof(Table.Add),
            [
                SyntaxFactory.Argument(
                    SyntaxFactory.ObjectCreationExpression(
                        SyntaxFactory.Token(SyntaxKind.NewKeyword)
                            .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                        SyntaxFactory.ParseTypeName(nameof(ObjectsRow)),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(
                            [
                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("select")),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName($"{firstAlias}Row"),
                                        SyntaxFactory.IdentifierName($"{nameof(IObjectResolver.Contexts)}"))),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression))
                            ])
                        ),
                        SyntaxFactory.InitializerExpression(SyntaxKind.ComplexElementInitializerExpression))
                )
            ]);

        return (arrayType, rewriteSelect, invocation);
    }

    private static (ArrayTypeSyntax arrayType, VariableDeclarationSyntax rewriteSelect, InvocationExpressionSyntax invocation) 
        CreateSelectAndInvocationForOuterRight(
            List<ExpressionSyntax> expressions, 
            SyntaxGenerator generator, 
            Scope scope, 
            string secondAlias)
    {
        var arrayType = SyntaxFactory.ArrayType(
            SyntaxFactory.IdentifierName("object"),
            new SyntaxList<ArrayRankSpecifierSyntax>(
                SyntaxFactory.ArrayRankSpecifier(
                    SyntaxFactory.SingletonSeparatedList(
                        (ExpressionSyntax)SyntaxFactory.OmittedArraySizeExpression()))));

        var rewriteSelect = SyntaxFactory.VariableDeclaration(
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

        var invocation = SyntaxHelper.CreateMethodInvocation(
            scope[MetaAttributes.SelectIntoVariableName],
            nameof(Table.Add),
            [
                SyntaxFactory.Argument(
                    SyntaxFactory.ObjectCreationExpression(
                        SyntaxFactory.Token(SyntaxKind.NewKeyword)
                            .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                        SyntaxFactory.ParseTypeName(nameof(ObjectsRow)),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(
                            [
                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("select")),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName($"{secondAlias}Row"),
                                        SyntaxFactory.IdentifierName($"{nameof(IObjectResolver.Contexts)}")))
                            ])
                        ),
                        SyntaxFactory.InitializerExpression(SyntaxKind.ComplexElementInitializerExpression))
                )
            ]);

        return (arrayType, rewriteSelect, invocation);
    }

    private static bool TryGetHashJoinKeys(
        JoinSourcesTableFromNode node, 
        out ExpressionSyntax leftKey, 
        out ExpressionSyntax rightKey, 
        out Type keyType)
    {
        leftKey = null;
        rightKey = null;
        keyType = null;

        if (node.Expression is EqualityNode binary)
        {
            if (binary.Left is AccessColumnNode leftCol && binary.Right is AccessColumnNode rightCol)
            {
                AccessColumnNode firstCol = null;
                AccessColumnNode secondCol = null;
                
                if (leftCol.Alias == node.First.Alias && rightCol.Alias == node.Second.Alias)
                {
                    firstCol = leftCol;
                    secondCol = rightCol;
                }
                else if (leftCol.Alias == node.Second.Alias && rightCol.Alias == node.First.Alias)
                {
                    firstCol = rightCol;
                    secondCol = leftCol;
                }
                
                if (firstCol != null && secondCol != null)
                {
                    if (firstCol.ReturnType == secondCol.ReturnType)
                    {
                        keyType = firstCol.ReturnType;
                        
                        leftKey = CreateColumnAccessExpression(node.First.Alias, firstCol.Name, firstCol.ReturnType);
                        rightKey = CreateColumnAccessExpression(node.Second.Alias, secondCol.Name, secondCol.ReturnType);
                        
                        return true;
                    }
                }
            }
        }
        
        return false;
    }

    private static ExpressionSyntax CreateColumnAccessExpression(string alias, string columnName, Type type)
    {
        var rowVar = SyntaxFactory.IdentifierName($"{alias}Row");
        var indexer = SyntaxFactory.ElementAccessExpression(
            rowVar,
            SyntaxFactory.BracketedArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(
                        SyntaxFactory.LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            SyntaxFactory.Literal(columnName)
                        )
                    )
                )
            )
        );
        
        var castType = EvaluationHelper.GetCastableType(type);
        var castExpression = SyntaxFactory.CastExpression(
            SyntaxFactory.ParseTypeName(castType),
            indexer
        );
        
        return castExpression;
    }

    private static void ValidateParameter<T>(string parameterName, T parameter) where T : class
    {
        if (parameter == null)
            throw new ArgumentNullException(parameterName);
    }
}
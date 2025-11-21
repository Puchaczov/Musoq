using System;
using System.Collections.Generic;
using System.Linq;
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
        
        if (compilationOptions?.UseHashJoin == true && 
            (node.JoinType == JoinType.Inner || node.JoinType == JoinType.OuterLeft || node.JoinType == JoinType.OuterRight))
        {
            if (TryGetHashJoinKeys(node, out var leftKeys, out var rightKeys, out var keyTypes))
            {
                return ProcessHashJoin(node, generator, scope, queryAlias, leftKeys, rightKeys, keyTypes, ifStatement, emptyBlock, getRowsSourceOrEmpty, block, generateCancellationExpression);
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
                if (TryGetHashJoinKeys(node, out var leftKey, out var rightKey, out var keyType))
                {
                    return ProcessHashJoin(node, generator, scope, queryAlias, leftKey, rightKey, keyType, ifStatement, emptyBlock, getRowsSourceOrEmpty, block, generateCancellationExpression);
                }
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

        foreach (var column in fullTransitionTable.GetColumns(fullTransitionTable.CompoundTables[0]))
        {
            expressions.Add(
                SyntaxFactory.CastExpression(
                    SyntaxFactory.IdentifierName(
                        EvaluationHelper.GetCastableType(column.ColumnType)),
                    (LiteralExpressionSyntax)generator.NullLiteralExpression()));
        }

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
        List<ExpressionSyntax> leftKeys,
        List<ExpressionSyntax> rightKeys,
        List<Type> keyTypes,
        SyntaxNode ifStatement,
        BlockSyntax emptyBlock,
        Func<string, StatementSyntax> getRowsSourceOrEmpty,
        Func<StatementSyntax[], BlockSyntax> block,
        Func<StatementSyntax> generateCancellationExpression)
    {
        var computingBlock = SyntaxFactory.Block();
        
        string keyTypeName;
        if (keyTypes.Count == 1)
        {
            keyTypeName = EvaluationHelper.GetCastableType(keyTypes[0]);
        }
        else
        {
            var typeNames = keyTypes.Select(t => EvaluationHelper.GetCastableType(t));
            keyTypeName = $"({string.Join(", ", typeNames)})";
        }

        var dictionaryType = SyntaxFactory.ParseTypeName($"System.Collections.Generic.Dictionary<{keyTypeName}, System.Collections.Generic.List<Musoq.Schema.DataSources.IObjectResolver>>");
        
        var isRightOuter = node.JoinType == JoinType.OuterRight;
        
        var buildAlias = isRightOuter ? node.First.Alias : node.Second.Alias;
        var probeAlias = isRightOuter ? node.Second.Alias : node.First.Alias;
        var buildKeys = isRightOuter ? leftKeys : rightKeys;
        var probeKeys = isRightOuter ? rightKeys : leftKeys;
        
        var dictionaryName = $"{buildAlias}Hashed";
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

        var buildPhaseStatements = new List<StatementSyntax>
        {
            generateCancellationExpression()
        };

        var buildKeyVars = new List<string>();
        for (int i = 0; i < buildKeys.Count; i++)
        {
            var varName = $"key{i}";
            buildKeyVars.Add(varName);
            buildPhaseStatements.Add(
                SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(
                        SyntaxFactory.IdentifierName("var"),
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(
                                SyntaxFactory.Identifier(varName),
                                null,
                                SyntaxFactory.EqualsValueClause(buildKeys[i])
                            )
                        )
                    )
                )
            );

            if (!keyTypes[i].IsValueType || Nullable.GetUnderlyingType(keyTypes[i]) != null)
            {
                buildPhaseStatements.Add(
                    SyntaxFactory.IfStatement(
                        SyntaxFactory.BinaryExpression(
                            SyntaxKind.EqualsExpression,
                            SyntaxFactory.IdentifierName(varName),
                            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
                        ),
                        SyntaxFactory.ContinueStatement()
                    )
                );
            }
        }

        ExpressionSyntax buildKeyExpr;
        if (buildKeyVars.Count == 1)
        {
            buildKeyExpr = SyntaxFactory.IdentifierName(buildKeyVars[0]);
        }
        else
        {
            buildKeyExpr = SyntaxFactory.TupleExpression(
                SyntaxFactory.SeparatedList(
                    buildKeyVars.Select(v => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(v)))
                )
            );
        }

        buildPhaseStatements.Add(
            SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.IdentifierName("var"),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(
                            SyntaxFactory.Identifier("key"),
                            null,
                            SyntaxFactory.EqualsValueClause(buildKeyExpr)
                        )
                    )
                )
            )
        );

        buildPhaseStatements.Add(
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
            )
        );

        buildPhaseStatements.Add(
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
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName($"{buildAlias}Row"))
                        )
                    )
                )
            )
        );

        var buildLoop = SyntaxFactory.ForEachStatement(
            SyntaxFactory.IdentifierName("var"),
            SyntaxFactory.Identifier($"{buildAlias}Row"),
            SyntaxFactory.IdentifierName($"{buildAlias}Rows.Rows"),
            SyntaxFactory.Block(buildPhaseStatements)
        );

        StatementSyntax outerJoinFallback = SyntaxFactory.Block();
        if (node.JoinType == JoinType.OuterLeft)
        {
            var fullTransitionTable = scope.ScopeSymbolTable.GetSymbol<TableSymbol>(queryAlias);
            var expressions = new List<ExpressionSyntax>();

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

            foreach (var column in fullTransitionTable.GetColumns(fullTransitionTable.CompoundTables[1]))
            {
                expressions.Add(
                    SyntaxFactory.CastExpression(
                        SyntaxFactory.IdentifierName(
                            EvaluationHelper.GetCastableType(column.ColumnType)),
                        (LiteralExpressionSyntax)generator.NullLiteralExpression()));
            }

            var (arrayType, rewriteSelect, invocation) = CreateSelectAndInvocationForOuterLeft(expressions, generator, scope, node.First.Alias);
            outerJoinFallback = SyntaxFactory.Block(
                SyntaxFactory.LocalDeclarationStatement(rewriteSelect),
                SyntaxFactory.ExpressionStatement(invocation)
            );
        }
        else if (node.JoinType == JoinType.OuterRight)
        {
            var fullTransitionTable = scope.ScopeSymbolTable.GetSymbol<TableSymbol>(queryAlias);
            var expressions = new List<ExpressionSyntax>();

            foreach (var column in fullTransitionTable.GetColumns(fullTransitionTable.CompoundTables[0]))
            {
                expressions.Add(
                    SyntaxFactory.CastExpression(
                        SyntaxFactory.IdentifierName(
                            EvaluationHelper.GetCastableType(column.ColumnType)),
                        (LiteralExpressionSyntax)generator.NullLiteralExpression()));
            }

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
            outerJoinFallback = SyntaxFactory.Block(
                SyntaxFactory.LocalDeclarationStatement(rewriteSelect),
                SyntaxFactory.ExpressionStatement(invocation)
            );
        }

        var probePhaseStatements = new List<StatementSyntax>
        {
            generateCancellationExpression()
        };

        var probeKeyVars = new List<string>();
        for (int i = 0; i < probeKeys.Count; i++)
        {
            var varName = $"key{i}";
            probeKeyVars.Add(varName);
            probePhaseStatements.Add(
                SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(
                        SyntaxFactory.IdentifierName("var"),
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(
                                SyntaxFactory.Identifier(varName),
                                null,
                                SyntaxFactory.EqualsValueClause(probeKeys[i])
                            )
                        )
                    )
                )
            );
        }

        ExpressionSyntax probeKeyExpr;
        if (probeKeyVars.Count == 1)
        {
            probeKeyExpr = SyntaxFactory.IdentifierName(probeKeyVars[0]);
        }
        else
        {
            probeKeyExpr = SyntaxFactory.TupleExpression(
                SyntaxFactory.SeparatedList(
                    probeKeyVars.Select(v => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(v)))
                )
            );
        }

        probePhaseStatements.Add(
            SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.IdentifierName("var"),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(
                            SyntaxFactory.Identifier("key"),
                            null,
                            SyntaxFactory.EqualsValueClause(probeKeyExpr)
                        )
                    )
                )
            )
        );

        var matchVarName = $"{buildAlias}Row";
        
        if (node.JoinType == JoinType.OuterLeft || node.JoinType == JoinType.OuterRight)
        {
            var matchFoundVar = SyntaxFactory.IdentifierName("matchFound");
            
            probePhaseStatements.Add(
                SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(
                        SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)),
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(
                                SyntaxFactory.Identifier("matchFound"),
                                null,
                                SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))
                            )
                        )
                    )
                )
            );
            
            var matchLoop = SyntaxFactory.ForEachStatement(
                SyntaxFactory.IdentifierName("var"),
                SyntaxFactory.Identifier(matchVarName),
                SyntaxFactory.IdentifierName("matches"),
                SyntaxFactory.Block(
                    generateCancellationExpression(),
                    (StatementSyntax)ifStatement,
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            matchFoundVar,
                            SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)
                        )
                    ),
                    emptyBlock
                )
            );
            
            var tryGetValue = SyntaxFactory.IfStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(dictionaryName),
                        SyntaxFactory.IdentifierName("TryGetValue")
                    ),
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(
                            new[]
                            {
                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("key")),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.DeclarationExpression(
                                        SyntaxFactory.IdentifierName("var"),
                                        SyntaxFactory.SingleVariableDesignation(
                                            SyntaxFactory.Identifier("matches")
                                        )
                                    )
                                ).WithRefOrOutKeyword(SyntaxFactory.Token(SyntaxKind.OutKeyword))
                            }
                        )
                    )
                ),
                SyntaxFactory.Block(matchLoop)
            );
            
            probePhaseStatements.Add(tryGetValue);
            
            probePhaseStatements.Add(
                SyntaxFactory.IfStatement(
                    SyntaxFactory.PrefixUnaryExpression(
                        SyntaxKind.LogicalNotExpression,
                        matchFoundVar
                    ),
                    outerJoinFallback
                )
            );
        }
        else
        {
            var matchLoop = SyntaxFactory.ForEachStatement(
                SyntaxFactory.IdentifierName("var"),
                SyntaxFactory.Identifier(matchVarName),
                SyntaxFactory.IdentifierName("matches"),
                SyntaxFactory.Block(
                    generateCancellationExpression(),
                    (StatementSyntax)ifStatement,
                    emptyBlock
                )
            );
            
            var tryGetValue = SyntaxFactory.IfStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(dictionaryName),
                        SyntaxFactory.IdentifierName("TryGetValue")
                    ),
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(
                            new[]
                            {
                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("key")),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.DeclarationExpression(
                                        SyntaxFactory.IdentifierName("var"),
                                        SyntaxFactory.SingleVariableDesignation(
                                            SyntaxFactory.Identifier("matches")
                                        )
                                    )
                                ).WithRefOrOutKeyword(SyntaxFactory.Token(SyntaxKind.OutKeyword))
                            }
                        )
                    )
                ),
                SyntaxFactory.Block(matchLoop)
            );
            
            probePhaseStatements.Add(tryGetValue);
        }

        var probeLoop = SyntaxFactory.ForEachStatement(
            SyntaxFactory.IdentifierName("var"),
            SyntaxFactory.Identifier($"{probeAlias}Row"),
            SyntaxFactory.IdentifierName($"{probeAlias}Rows.Rows"),
            SyntaxFactory.Block(probePhaseStatements)
        );

        computingBlock = computingBlock.AddStatements(
            dictionaryCreation,
            getRowsSourceOrEmpty(buildAlias),
            buildLoop,
            getRowsSourceOrEmpty(probeAlias),
            probeLoop
        );

        return computingBlock;
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
        out List<ExpressionSyntax> leftKeys, 
        out List<ExpressionSyntax> rightKeys, 
        out List<Type> keyTypes)
    {
        leftKeys = new List<ExpressionSyntax>();
        rightKeys = new List<ExpressionSyntax>();
        keyTypes = new List<Type>();

        var conditions = new List<EqualityNode>();
        
        if (node.Expression is EqualityNode eq)
        {
            conditions.Add(eq);
        }
        else if (node.Expression is AndNode and)
        {
            var stack = new Stack<Node>();
            stack.Push(and);
            
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current is AndNode a)
                {
                    stack.Push(a.Right);
                    stack.Push(a.Left);
                }
                else if (current is EqualityNode e)
                {
                    conditions.Add(e);
                }
                else
                {
                    return false;
                }
            }
        }
        else
        {
            return false;
        }

        foreach (var binary in conditions)
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
                    var type1 = firstCol.ReturnType;
                    var type2 = secondCol.ReturnType;
                    
                    var type1Underlying = Nullable.GetUnderlyingType(type1) ?? type1;
                    var type2Underlying = Nullable.GetUnderlyingType(type2) ?? type2;

                    if (type1Underlying == type2Underlying)
                    {
                        Type keyType;
                        if (type1 != type2)
                        {
                             keyType = typeof(Nullable<>).MakeGenericType(type1Underlying);
                        }
                        else
                        {
                             keyType = type1;
                        }
                        
                        leftKeys.Add(CreateColumnAccessExpression(node.First.Alias, firstCol.Name, keyType));
                        rightKeys.Add(CreateColumnAccessExpression(node.Second.Alias, secondCol.Name, keyType));
                        keyTypes.Add(keyType);
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        
        return leftKeys.Count > 0;
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
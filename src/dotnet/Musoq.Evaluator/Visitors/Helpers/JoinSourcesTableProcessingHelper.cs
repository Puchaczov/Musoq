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
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Visitors.Helpers;

/// <summary>
///     Helper class for processing JoinSourcesTableFromNode operations.
///     Handles Inner, OuterLeft, and OuterRight join processing logic.
/// </summary>
public static class JoinSourcesTableProcessingHelper
{
    /// <summary>
    ///     Processes a JoinSourcesTableFromNode and returns the appropriate computing block based on join type.
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
        Func<Node, ExpressionSyntax> nodeTranslator,
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
        ValidateParameter(nameof(nodeTranslator), nodeTranslator);

        var computingBlock = SyntaxFactory.Block();

        if (compilationOptions?.UseHashJoin == true &&
            (node.JoinType == JoinType.Inner || node.JoinType == JoinType.OuterLeft ||
             node.JoinType == JoinType.OuterRight) &&
            TryGetHashJoinKeys(node, scope, nodeTranslator, out var leftKeys, out var rightKeys, out var keyTypes))
            return ProcessHashJoin(node, generator, scope, queryAlias, leftKeys, rightKeys, keyTypes, ifStatement,
                emptyBlock, getRowsSourceOrEmpty, block, generateCancellationExpression);

        if (compilationOptions?.UseSortMergeJoin == true &&
            (node.JoinType == JoinType.Inner || node.JoinType == JoinType.OuterLeft ||
             node.JoinType == JoinType.OuterRight) &&
            TryGetSortMergeJoinKeys(node, scope, nodeTranslator, out var smLeftKey, out var smRightKey,
                out var smKeyType, out var smCondition, out var smComparisonKind))
            return ProcessSortMergeJoin(node, generator, scope, queryAlias, smLeftKey, smRightKey, smKeyType,
                smCondition, smComparisonKind, ifStatement, emptyBlock, getRowsSourceOrEmpty, block,
                generateCancellationExpression);

        switch (node.JoinType)
        {
            case JoinType.Inner:
                return ProcessInnerJoin(node, generator, ifStatement, emptyBlock, getRowsSourceOrEmpty, block,
                    generateCancellationExpression);

            case JoinType.OuterLeft:
                return ProcessOuterLeftJoin(node, generator, scope, queryAlias, ifStatement, emptyBlock,
                    getRowsSourceOrEmpty, block, generateCancellationExpression);

            case JoinType.OuterRight:
                return ProcessOuterRightJoin(node, generator, scope, queryAlias, ifStatement, emptyBlock,
                    getRowsSourceOrEmpty, block, generateCancellationExpression);

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
        var secondRowsCacheStatements = CreateRowsCacheStatements(node.Second.Alias);

        return computingBlock.AddStatements(
            getRowsSourceOrEmpty(node.First.Alias),
            getRowsSourceOrEmpty(node.Second.Alias),
            secondRowsCacheStatements[0],
            secondRowsCacheStatements[1],
            SyntaxFactory.ForEachStatement(
                SyntaxFactory.IdentifierName("var"),
                SyntaxFactory.Identifier($"{node.First.Alias}Row"),
                SyntaxFactory.IdentifierName($"{node.First.Alias}Rows.Rows"),
                block([
                    SyntaxFactory.ForEachStatement(
                        SyntaxFactory.IdentifierName("var"),
                        SyntaxFactory.Identifier($"{node.Second.Alias}Row"),
                        SyntaxFactory.IdentifierName($"{node.Second.Alias}RowsCached"),
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
        var secondRowsCacheStatements = CreateRowsCacheStatements(node.Second.Alias);
        var fullTransitionTable = scope.ScopeSymbolTable.GetSymbol<TableSymbol>(queryAlias);
        var expressions = new List<ExpressionSyntax>();

        foreach (var column in fullTransitionTable.GetColumns(fullTransitionTable.CompoundTables[0]))
            expressions.Add(
                SyntaxFactory.ElementAccessExpression(
                    SyntaxFactory.IdentifierName($"{node.First.Alias}Row"),
                    SyntaxFactory.BracketedArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(
                                (LiteralExpressionSyntax)generator.LiteralExpression(
                                    column.ColumnName))))));

        foreach (var column in fullTransitionTable.GetColumns(fullTransitionTable.CompoundTables[1]))
            expressions.Add(
                SyntaxFactory.CastExpression(
                    SyntaxFactory.IdentifierName(
                        EvaluationHelper.GetCastableType(column.ColumnType)),
                    (LiteralExpressionSyntax)generator.NullLiteralExpression()));

        var (arrayType, rewriteSelect, invocation) =
            CreateSelectAndInvocationForOuterLeft(expressions, generator, scope, node.First.Alias);

        return computingBlock.AddStatements(
            getRowsSourceOrEmpty(node.First.Alias),
            getRowsSourceOrEmpty(node.Second.Alias),
            secondRowsCacheStatements[0],
            secondRowsCacheStatements[1],
            SyntaxFactory.ForEachStatement(
                SyntaxFactory.IdentifierName("var"),
                SyntaxFactory.Identifier($"{node.First.Alias}Row"),
                SyntaxFactory.IdentifierName($"{node.First.Alias}Rows.Rows"),
                block([
                    SyntaxFactory.LocalDeclarationStatement(
                        SyntaxHelper.CreateAssignment("hasAnyRowMatched",
                            (LiteralExpressionSyntax)generator.FalseLiteralExpression())),
                    SyntaxFactory.ForEachStatement(
                        SyntaxFactory.IdentifierName("var"),
                        SyntaxFactory.Identifier($"{node.Second.Alias}Row"),
                        SyntaxFactory.IdentifierName($"{node.Second.Alias}RowsCached"),
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
        var firstRowsCacheStatements = CreateRowsCacheStatements(node.First.Alias);
        var fullTransitionTable = scope.ScopeSymbolTable.GetSymbol<TableSymbol>(queryAlias);
        var expressions = new List<ExpressionSyntax>();

        foreach (var column in fullTransitionTable.GetColumns(fullTransitionTable.CompoundTables[0]))
            expressions.Add(
                SyntaxFactory.CastExpression(
                    SyntaxFactory.IdentifierName(
                        EvaluationHelper.GetCastableType(column.ColumnType)),
                    (LiteralExpressionSyntax)generator.NullLiteralExpression()));

        foreach (var column in fullTransitionTable.GetColumns(fullTransitionTable.CompoundTables[1]))
            expressions.Add(
                SyntaxFactory.ElementAccessExpression(
                    SyntaxFactory.IdentifierName($"{node.Second.Alias}Row"),
                    SyntaxFactory.BracketedArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(
                                (LiteralExpressionSyntax)generator.LiteralExpression(
                                    column.ColumnName))))));

        var (arrayType, rewriteSelect, invocation) =
            CreateSelectAndInvocationForOuterRight(expressions, generator, scope, node.Second.Alias);

        return computingBlock.AddStatements(
            getRowsSourceOrEmpty(node.Second.Alias),
            getRowsSourceOrEmpty(node.First.Alias),
            firstRowsCacheStatements[0],
            firstRowsCacheStatements[1],
            SyntaxFactory.ForEachStatement(
                SyntaxFactory.IdentifierName("var"),
                SyntaxFactory.Identifier($"{node.Second.Alias}Row"),
                SyntaxFactory.IdentifierName($"{node.Second.Alias}Rows.Rows"),
                block([
                    SyntaxFactory.LocalDeclarationStatement(
                        SyntaxHelper.CreateAssignment("hasAnyRowMatched",
                            (LiteralExpressionSyntax)generator.FalseLiteralExpression())),
                    SyntaxFactory.ForEachStatement(
                        SyntaxFactory.IdentifierName("var"),
                        SyntaxFactory.Identifier($"{node.First.Alias}Row"),
                        SyntaxFactory.IdentifierName($"{node.First.Alias}RowsCached"),
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

    private static StatementSyntax[] CreateRowsCacheStatements(string alias)
    {
        return
        [
            SyntaxFactory.ParseStatement($"var {alias}RowsEnumerable = {alias}Rows.Rows;"),
            SyntaxFactory.ParseStatement(
                $"var {alias}RowsCached = {alias}RowsEnumerable as Musoq.Schema.DataSources.IObjectResolver[] ?? System.Linq.Enumerable.ToArray({alias}RowsEnumerable);")
        ];
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

        var dictionaryType = SyntaxFactory.ParseTypeName(
            $"System.Collections.Generic.Dictionary<{keyTypeName}, System.Collections.Generic.List<Musoq.Schema.DataSources.IObjectResolver>>");

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
        for (var i = 0; i < buildKeys.Count; i++)
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

        ExpressionSyntax buildKeyExpr = buildKeyVars.Count == 1
            ? SyntaxFactory.IdentifierName(buildKeyVars[0])
            : SyntaxFactory.TupleExpression(
                SyntaxFactory.SeparatedList(
                    buildKeyVars.Select(v => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(v)))
                )
            );

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
                                SyntaxHelper.ListOfIObjectResolverTypeSyntax
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
                expressions.Add(
                    SyntaxFactory.ElementAccessExpression(
                        SyntaxFactory.IdentifierName($"{node.First.Alias}Row"),
                        SyntaxFactory.BracketedArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(
                                    (LiteralExpressionSyntax)generator.LiteralExpression(
                                        column.ColumnName))))));

            foreach (var column in fullTransitionTable.GetColumns(fullTransitionTable.CompoundTables[1]))
                expressions.Add(
                    SyntaxFactory.CastExpression(
                        SyntaxFactory.IdentifierName(
                            EvaluationHelper.GetCastableType(column.ColumnType)),
                        (LiteralExpressionSyntax)generator.NullLiteralExpression()));

            var (_, rewriteSelect, invocation) =
                CreateSelectAndInvocationForOuterLeft(expressions, generator, scope, node.First.Alias);
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
                expressions.Add(
                    SyntaxFactory.CastExpression(
                        SyntaxFactory.IdentifierName(
                            EvaluationHelper.GetCastableType(column.ColumnType)),
                        (LiteralExpressionSyntax)generator.NullLiteralExpression()));

            foreach (var column in fullTransitionTable.GetColumns(fullTransitionTable.CompoundTables[1]))
                expressions.Add(
                    SyntaxFactory.ElementAccessExpression(
                        SyntaxFactory.IdentifierName($"{node.Second.Alias}Row"),
                        SyntaxFactory.BracketedArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(
                                    (LiteralExpressionSyntax)generator.LiteralExpression(
                                        column.ColumnName))))));

            var (_, rewriteSelect, invocation) =
                CreateSelectAndInvocationForOuterRight(expressions, generator, scope, node.Second.Alias);
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
        for (var i = 0; i < probeKeys.Count; i++)
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

        var probeKeyExpr = (ExpressionSyntax)(probeKeyVars.Count == 1
            ? SyntaxFactory.IdentifierName(probeKeyVars[0])
            : SyntaxFactory.TupleExpression(
                SyntaxFactory.SeparatedList(
                    probeKeyVars.Select(v => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(v)))
                )
            ));

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
                                SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))
                            )
                        )
                    )
                ));

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
                            [
                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("key")),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.DeclarationExpression(
                                        SyntaxFactory.IdentifierName("var"),
                                        SyntaxFactory.SingleVariableDesignation(
                                            SyntaxFactory.Identifier("matches")
                                        )
                                    )
                                ).WithRefOrOutKeyword(SyntaxFactory.Token(SyntaxKind.OutKeyword))
                            ]
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
                            [
                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("key")),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.DeclarationExpression(
                                        SyntaxFactory.IdentifierName("var"),
                                        SyntaxFactory.SingleVariableDesignation(
                                            SyntaxFactory.Identifier("matches")
                                        )
                                    )
                                ).WithRefOrOutKeyword(SyntaxFactory.Token(SyntaxKind.OutKeyword))
                            ]
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


    private static (ArrayTypeSyntax arrayType, VariableDeclarationSyntax rewriteSelect, InvocationExpressionSyntax
        invocation)
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
                        SyntaxHelper.ObjectsRowTypeSyntax,
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

    private static (ArrayTypeSyntax arrayType, VariableDeclarationSyntax rewriteSelect, InvocationExpressionSyntax
        invocation)
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
        Scope scope,
        Func<Node, ExpressionSyntax> nodeTranslator,
        out List<ExpressionSyntax> leftKeys,
        out List<ExpressionSyntax> rightKeys,
        out List<Type> keyTypes)
    {
        leftKeys = [];
        rightKeys = [];
        keyTypes = [];

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


        var firstAliases = GetComponentAliases(node.First);
        var secondAliases = GetComponentAliases(node.Second);
        var allAliases = firstAliases.Concat(secondAliases).ToArray();

        foreach (var binary in conditions)
        {
            var leftVisitor = new ExtractAccessColumnFromQueryVisitor();
            var leftTraverser = new ExtractAccessColumnFromQueryTraverseVisitor(leftVisitor);
            binary.Left.Accept(leftTraverser);
            var leftColumns = leftVisitor.GetForAliases(allAliases);

            var rightVisitor = new ExtractAccessColumnFromQueryVisitor();
            var rightTraverser = new ExtractAccessColumnFromQueryTraverseVisitor(rightVisitor);
            binary.Right.Accept(rightTraverser);
            var rightColumns = rightVisitor.GetForAliases(allAliases);


            var leftHasFirst = leftColumns.Any(c => firstAliases.Contains(c.Alias));
            var leftHasSecond = leftColumns.Any(c => secondAliases.Contains(c.Alias));
            var rightHasFirst = rightColumns.Any(c => firstAliases.Contains(c.Alias));
            var rightHasSecond = rightColumns.Any(c => secondAliases.Contains(c.Alias));

            var leftIsFirst = leftHasFirst && !leftHasSecond;
            var leftIsSecond = leftHasSecond && !leftHasFirst;
            var leftIsConstant = !leftHasFirst && !leftHasSecond;

            var rightIsFirst = rightHasFirst && !rightHasSecond;
            var rightIsSecond = rightHasSecond && !rightHasFirst;
            var rightIsConstant = !rightHasFirst && !rightHasSecond;

            if (leftIsConstant && rightIsConstant) continue;

            Node firstNode = null;
            Node secondNode = null;

            if ((leftIsFirst || leftIsConstant) && (rightIsSecond || rightIsConstant))
            {
                firstNode = binary.Left;
                secondNode = binary.Right;
            }
            else if ((leftIsSecond || leftIsConstant) && (rightIsFirst || rightIsConstant))
            {
                firstNode = binary.Right;
                secondNode = binary.Left;
            }

            if (firstNode != null && secondNode != null)
            {
                var type1 = firstNode.ReturnType;
                var type2 = secondNode.ReturnType;

                var type1Underlying = Nullable.GetUnderlyingType(type1) ?? type1;
                var type2Underlying = Nullable.GetUnderlyingType(type2) ?? type2;

                if (type1Underlying == type2Underlying)
                {
                    var keyType = type1 != type2
                        ? typeof(Nullable<>).MakeGenericType(type1Underlying)
                        : type1;

                    var leftExpr = nodeTranslator(firstNode);
                    var rightExpr = nodeTranslator(secondNode);

                    if (firstNode.ReturnType != keyType)
                        leftExpr = SyntaxFactory.CastExpression(SyntaxFactory.ParseTypeName(GetTypeName(keyType)),
                            leftExpr);

                    if (secondNode.ReturnType != keyType)
                        rightExpr = SyntaxFactory.CastExpression(SyntaxFactory.ParseTypeName(GetTypeName(keyType)),
                            rightExpr);

                    leftKeys.Add(leftExpr);
                    rightKeys.Add(rightExpr);
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

    private static BlockSyntax ProcessSortMergeJoin(
        JoinSourcesTableFromNode node,
        SyntaxGenerator generator,
        Scope scope,
        string queryAlias,
        ExpressionSyntax leftKey,
        ExpressionSyntax rightKey,
        Type keyType,
        BinaryNode condition,
        SyntaxKind comparisonKind,
        SyntaxNode ifStatement,
        BlockSyntax emptyBlock,
        Func<string, StatementSyntax> getRowsSourceOrEmpty,
        Func<StatementSyntax[], BlockSyntax> block,
        Func<StatementSyntax> generateCancellationExpression)
    {
        var computingBlock = SyntaxFactory.Block();

        var isRightJoin = node.JoinType == JoinType.OuterRight;
        var probeAlias = isRightJoin ? node.Second.Alias : node.First.Alias;
        var buildAlias = isRightJoin ? node.First.Alias : node.Second.Alias;

        var buildKeyExpr = isRightJoin ? leftKey : rightKey;
        var probeKeyExpr = isRightJoin ? rightKey : leftKey;

        if (isRightJoin) comparisonKind = SwapComparisonKind(comparisonKind);

        var innerRowsVar = $"{buildAlias}RowsArray";
        var innerKeysVar = $"{buildAlias}KeysArray";
        var outerRowsVar = $"{probeAlias}RowsArray";
        var outerKeysVar = $"{probeAlias}KeysArray";

        var rowType = SyntaxHelper.IObjectResolverTypeSyntax;
        var keyTypeSyntax = EvaluationHelper.GetCastableType(keyType);

        computingBlock = computingBlock.AddStatements(getRowsSourceOrEmpty(buildAlias));
        computingBlock = computingBlock.AddStatements(
            SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(
                        SyntaxFactory.ArrayType(rowType)
                            .WithRankSpecifiers(SyntaxFactory.SingletonList(SyntaxFactory.ArrayRankSpecifier()))
                    )
                    .WithVariables(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(
                                    SyntaxFactory.Identifier(innerRowsVar)
                                )
                                .WithInitializer(
                                    SyntaxFactory.EqualsValueClause(
                                        SyntaxFactory.InvocationExpression(
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    SyntaxFactory.IdentifierName($"{buildAlias}Rows"),
                                                    SyntaxFactory.IdentifierName("Rows")
                                                ),
                                                SyntaxFactory.IdentifierName("ToArray")
                                            )
                                        )
                                    )
                                )
                        )
                    )
            )
        );

        computingBlock = computingBlock.AddStatements(
            SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                    .WithVariables(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(innerKeysVar))
                            .WithInitializer(SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.ObjectCreationExpression(
                                    SyntaxFactory.ArrayType(SyntaxFactory.ParseTypeName(keyTypeSyntax))
                                        .WithRankSpecifiers(SyntaxFactory.SingletonList(
                                            SyntaxFactory.ArrayRankSpecifier(
                                                SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                                    SyntaxFactory.MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        SyntaxFactory.IdentifierName(innerRowsVar),
                                                        SyntaxFactory.IdentifierName("Length")
                                                    )
                                                )
                                            )
                                        ))
                                )
                            ))
                    ))
            ),
            SyntaxFactory.ForStatement(
                    SyntaxFactory.Block(
                        SyntaxFactory.ExpressionStatement(
                            SyntaxFactory.AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                SyntaxFactory.ElementAccessExpression(
                                    SyntaxFactory.IdentifierName(innerKeysVar),
                                    SyntaxFactory.BracketedArgumentList(
                                        SyntaxFactory.SingletonSeparatedList(
                                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("i"))))
                                ),
                                ReplaceIdentifier(buildKeyExpr, $"{buildAlias}Row", "r")
                            )
                        )
                    )
                )
                .WithDeclaration(
                    SyntaxFactory
                        .VariableDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)))
                        .WithVariables(SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator("i").WithInitializer(
                                SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                        SyntaxFactory.Literal(0))))
                        ))
                )
                .WithCondition(
                    SyntaxFactory.BinaryExpression(
                        SyntaxKind.LessThanExpression,
                        SyntaxFactory.IdentifierName("i"),
                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(innerRowsVar), SyntaxFactory.IdentifierName("Length"))
                    )
                )
                .WithIncrementors(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                    SyntaxFactory.PostfixUnaryExpression(SyntaxKind.PostIncrementExpression,
                        SyntaxFactory.IdentifierName("i"))))
        );

        var keyExtractionLoop = (ForStatementSyntax)computingBlock.Statements.Last();
        var loopBlock = (BlockSyntax)keyExtractionLoop.Statement;
        var newLoopBlock = loopBlock.WithStatements(
            SyntaxFactory.List(
                new StatementSyntax[]
                {
                    SyntaxFactory.LocalDeclarationStatement(
                        SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                            .WithVariables(SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.VariableDeclarator("r")
                                    .WithInitializer(SyntaxFactory.EqualsValueClause(
                                        SyntaxFactory.ElementAccessExpression(
                                            SyntaxFactory.IdentifierName(innerRowsVar),
                                            SyntaxFactory.BracketedArgumentList(
                                                SyntaxFactory.SingletonSeparatedList(
                                                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName("i"))))
                                        )
                                    ))
                            ))
                    )
                }.Concat(loopBlock.Statements)
            )
        );
        computingBlock = computingBlock.ReplaceNode(keyExtractionLoop, keyExtractionLoop.WithStatement(newLoopBlock));

        computingBlock = computingBlock.AddStatements(
            SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("Array"),
                        SyntaxFactory.IdentifierName("Sort")
                    ),
                    SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList([
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName(innerKeysVar)),
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName(innerRowsVar))
                    ]))
                )
            )
        );

        var sortAscending = comparisonKind == SyntaxKind.GreaterThanExpression ||
                            comparisonKind == SyntaxKind.GreaterThanOrEqualExpression;

        if (!sortAscending)
            computingBlock = computingBlock.AddStatements(
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("Array"),
                            SyntaxFactory.IdentifierName("Reverse")
                        ),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName(innerKeysVar))))
                    )
                ),
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("Array"),
                            SyntaxFactory.IdentifierName("Reverse")
                        ),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName(innerRowsVar))))
                    )
                )
            );

        computingBlock = computingBlock.AddStatements(getRowsSourceOrEmpty(probeAlias));

        computingBlock = computingBlock.AddStatements(
            SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(
                        SyntaxFactory.ArrayType(rowType)
                            .WithRankSpecifiers(SyntaxFactory.SingletonList(SyntaxFactory.ArrayRankSpecifier()))
                    )
                    .WithVariables(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(
                                    SyntaxFactory.Identifier(outerRowsVar)
                                )
                                .WithInitializer(
                                    SyntaxFactory.EqualsValueClause(
                                        SyntaxFactory.InvocationExpression(
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    SyntaxFactory.IdentifierName($"{probeAlias}Rows"),
                                                    SyntaxFactory.IdentifierName("Rows")
                                                ),
                                                SyntaxFactory.IdentifierName("ToArray")
                                            )
                                        )
                                    )
                                )
                        )
                    )
            ));

        computingBlock = computingBlock.AddStatements(
            SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                    .WithVariables(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(outerKeysVar))
                            .WithInitializer(SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.ObjectCreationExpression(
                                    SyntaxFactory.ArrayType(SyntaxFactory.ParseTypeName(keyTypeSyntax))
                                        .WithRankSpecifiers(SyntaxFactory.SingletonList(
                                            SyntaxFactory.ArrayRankSpecifier(
                                                SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                                    SyntaxFactory.MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        SyntaxFactory.IdentifierName(outerRowsVar),
                                                        SyntaxFactory.IdentifierName("Length")
                                                    )
                                                )
                                            )
                                        ))
                                )
                            ))
                    ))
            ),
            SyntaxFactory.ForStatement(
                    SyntaxFactory.Block(
                        SyntaxFactory.LocalDeclarationStatement(
                            SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                                .WithVariables(SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.VariableDeclarator("r")
                                        .WithInitializer(SyntaxFactory.EqualsValueClause(
                                            SyntaxFactory.ElementAccessExpression(
                                                SyntaxFactory.IdentifierName(outerRowsVar),
                                                SyntaxFactory.BracketedArgumentList(
                                                    SyntaxFactory.SingletonSeparatedList(
                                                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("i"))))
                                            )
                                        ))
                                ))
                        ),
                        SyntaxFactory.ExpressionStatement(
                            SyntaxFactory.AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                SyntaxFactory.ElementAccessExpression(
                                    SyntaxFactory.IdentifierName(outerKeysVar),
                                    SyntaxFactory.BracketedArgumentList(
                                        SyntaxFactory.SingletonSeparatedList(
                                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("i"))))
                                ),
                                ReplaceIdentifier(probeKeyExpr, $"{probeAlias}Row", "r")
                            )
                        )
                    )
                )
                .WithDeclaration(
                    SyntaxFactory
                        .VariableDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)))
                        .WithVariables(SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator("i").WithInitializer(
                                SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                        SyntaxFactory.Literal(0))))
                        ))
                )
                .WithCondition(
                    SyntaxFactory.BinaryExpression(
                        SyntaxKind.LessThanExpression,
                        SyntaxFactory.IdentifierName("i"),
                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(outerRowsVar), SyntaxFactory.IdentifierName("Length"))
                    )
                )
                .WithIncrementors(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                    SyntaxFactory.PostfixUnaryExpression(SyntaxKind.PostIncrementExpression,
                        SyntaxFactory.IdentifierName("i"))))
        );

        computingBlock = computingBlock.AddStatements(
            SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("Array"),
                        SyntaxFactory.IdentifierName("Sort")
                    ),
                    SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList([
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName(outerKeysVar)),
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName(outerRowsVar))
                    ]))
                )
            )
        );

        if (!sortAscending)
            computingBlock = computingBlock.AddStatements(
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("Array"),
                            SyntaxFactory.IdentifierName("Reverse")
                        ),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName(outerKeysVar))))
                    )
                ),
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("Array"),
                            SyntaxFactory.IdentifierName("Reverse")
                        ),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName(outerRowsVar))))
                    )
                )
            );


        var rowTypeConcrete = SyntaxHelper.RowConcreteTypeSyntax;

        computingBlock = computingBlock.AddStatements(
            SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                    .WithVariables(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator("resultBag")
                            .WithInitializer(SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.ObjectCreationExpression(
                                    SyntaxFactory.GenericName(
                                        SyntaxFactory.Identifier("System.Collections.Concurrent.ConcurrentBag"),
                                        SyntaxFactory.TypeArgumentList(
                                            SyntaxFactory.SingletonSeparatedList(rowTypeConcrete))
                                    )
                                ).WithArgumentList(SyntaxFactory.ArgumentList())
                            ))
                    ))
            )
        );

        var rewriter = new TableAddRewriter(queryAlias, "resultBag");
        var rewrittenIf = rewriter.Visit(ifStatement);

        StatementSyntax outerJoinFallback = SyntaxFactory.Block();
        if (node.JoinType == JoinType.OuterLeft)
        {
            var fullTransitionTable = scope.ScopeSymbolTable.GetSymbol<TableSymbol>(queryAlias);
            var expressions = new List<ExpressionSyntax>();

            foreach (var column in fullTransitionTable.GetColumns(fullTransitionTable.CompoundTables[0]))
                expressions.Add(
                    SyntaxFactory.ElementAccessExpression(
                        SyntaxFactory.IdentifierName($"{node.First.Alias}Row"),
                        SyntaxFactory.BracketedArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(
                                    (LiteralExpressionSyntax)generator.LiteralExpression(
                                        column.ColumnName))))));

            foreach (var column in fullTransitionTable.GetColumns(fullTransitionTable.CompoundTables[1]))
                expressions.Add(
                    SyntaxFactory.CastExpression(
                        SyntaxFactory.IdentifierName(
                            EvaluationHelper.GetCastableType(column.ColumnType)),
                        (LiteralExpressionSyntax)generator.NullLiteralExpression()));

            var (_, rewriteSelect, invocation) =
                CreateSelectAndInvocationForOuterLeft(expressions, generator, scope, node.First.Alias);
            outerJoinFallback = SyntaxFactory.Block(
                SyntaxFactory.LocalDeclarationStatement(rewriteSelect),
                SyntaxFactory.ExpressionStatement(invocation)
            );

            outerJoinFallback = (StatementSyntax)rewriter.Visit(outerJoinFallback);
        }
        else if (node.JoinType == JoinType.OuterRight)
        {
            var fullTransitionTable = scope.ScopeSymbolTable.GetSymbol<TableSymbol>(queryAlias);
            var expressions = new List<ExpressionSyntax>();

            foreach (var column in fullTransitionTable.GetColumns(fullTransitionTable.CompoundTables[0]))
                expressions.Add(
                    SyntaxFactory.CastExpression(
                        SyntaxFactory.IdentifierName(
                            EvaluationHelper.GetCastableType(column.ColumnType)),
                        (LiteralExpressionSyntax)generator.NullLiteralExpression()));

            foreach (var column in fullTransitionTable.GetColumns(fullTransitionTable.CompoundTables[1]))
                expressions.Add(
                    SyntaxFactory.ElementAccessExpression(
                        SyntaxFactory.IdentifierName($"{node.Second.Alias}Row"),
                        SyntaxFactory.BracketedArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(
                                    (LiteralExpressionSyntax)generator.LiteralExpression(
                                        column.ColumnName))))));

            var (_, rewriteSelect, invocation) =
                CreateSelectAndInvocationForOuterRight(expressions, generator, scope, node.Second.Alias);
            outerJoinFallback = SyntaxFactory.Block(
                SyntaxFactory.LocalDeclarationStatement(rewriteSelect),
                SyntaxFactory.ExpressionStatement(invocation)
            );

            outerJoinFallback = (StatementSyntax)rewriter.Visit(outerJoinFallback);
        }

        var parallelLoop = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Parallel"),
                SyntaxFactory.IdentifierName("ForEach")
            ),
            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList([
                SyntaxFactory.Argument(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("System.Collections.Concurrent.Partitioner"),
                            SyntaxFactory.IdentifierName("Create")
                        ),
                        SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList([
                            SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                SyntaxFactory.Literal(0))),
                            SyntaxFactory.Argument(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName(outerRowsVar),
                                    SyntaxFactory.IdentifierName("Length")
                                )
                            )
                        ]))
                    )
                ),
                SyntaxFactory.Argument(
                    SyntaxFactory.ParenthesizedLambdaExpression(
                        SyntaxFactory.ParameterList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Parameter(SyntaxFactory.Identifier("range")))),
                        SyntaxFactory.Block(
                            SyntaxFactory.LocalDeclarationStatement(
                                SyntaxFactory
                                    .VariableDeclaration(
                                        SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)))
                                    .WithVariables(SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.VariableDeclarator("limit")
                                            .WithInitializer(SyntaxFactory.EqualsValueClause(
                                                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                                    SyntaxFactory.Literal(0))))
                                    ))
                            ),
                            SyntaxFactory.WhileStatement(
                                SyntaxFactory.BinaryExpression(
                                    SyntaxKind.LogicalAndExpression,
                                    SyntaxFactory.BinaryExpression(
                                        SyntaxKind.LessThanExpression,
                                        SyntaxFactory.IdentifierName("limit"),
                                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.IdentifierName(innerKeysVar),
                                            SyntaxFactory.IdentifierName("Length"))
                                    ),
                                    SyntaxFactory.BinaryExpression(
                                        comparisonKind,
                                        SyntaxFactory.ElementAccessExpression(
                                            SyntaxFactory.IdentifierName(outerKeysVar),
                                            SyntaxFactory.BracketedArgumentList(SyntaxFactory.SingletonSeparatedList(
                                                SyntaxFactory.Argument(
                                                    SyntaxFactory.MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        SyntaxFactory.IdentifierName("range"),
                                                        SyntaxFactory.IdentifierName("Item1"))
                                                )))
                                        ),
                                        SyntaxFactory.ElementAccessExpression(
                                            SyntaxFactory.IdentifierName(innerKeysVar),
                                            SyntaxFactory.BracketedArgumentList(
                                                SyntaxFactory.SingletonSeparatedList(
                                                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName("limit"))))
                                        )
                                    )
                                ),
                                SyntaxFactory.ExpressionStatement(
                                    SyntaxFactory.PostfixUnaryExpression(SyntaxKind.PostIncrementExpression,
                                        SyntaxFactory.IdentifierName("limit"))
                                )
                            ),
                            SyntaxFactory.ForStatement(
                                    SyntaxFactory.Block(
                                        SyntaxFactory.LocalDeclarationStatement(
                                            SyntaxFactory
                                                .VariableDeclaration(
                                                    SyntaxFactory.PredefinedType(
                                                        SyntaxFactory.Token(SyntaxKind.BoolKeyword)))
                                                .WithVariables(SyntaxFactory.SingletonSeparatedList(
                                                    SyntaxFactory.VariableDeclarator("joined")
                                                        .WithInitializer(SyntaxFactory.EqualsValueClause(
                                                            SyntaxFactory.LiteralExpression(SyntaxKind
                                                                .FalseLiteralExpression)))
                                                ))
                                        ),
                                        SyntaxFactory.WhileStatement(
                                            SyntaxFactory.BinaryExpression(
                                                SyntaxKind.LogicalAndExpression,
                                                SyntaxFactory.BinaryExpression(
                                                    SyntaxKind.LessThanExpression,
                                                    SyntaxFactory.IdentifierName("limit"),
                                                    SyntaxFactory.MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        SyntaxFactory.IdentifierName(innerKeysVar),
                                                        SyntaxFactory.IdentifierName("Length"))
                                                ),
                                                SyntaxFactory.BinaryExpression(
                                                    comparisonKind,
                                                    SyntaxFactory.ElementAccessExpression(
                                                        SyntaxFactory.IdentifierName(outerKeysVar),
                                                        SyntaxFactory.BracketedArgumentList(
                                                            SyntaxFactory.SingletonSeparatedList(
                                                                SyntaxFactory.Argument(
                                                                    SyntaxFactory.IdentifierName("i"))))
                                                    ),
                                                    SyntaxFactory.ElementAccessExpression(
                                                        SyntaxFactory.IdentifierName(innerKeysVar),
                                                        SyntaxFactory.BracketedArgumentList(
                                                            SyntaxFactory.SingletonSeparatedList(
                                                                SyntaxFactory.Argument(
                                                                    SyntaxFactory.IdentifierName("limit"))))
                                                    )
                                                )
                                            ),
                                            SyntaxFactory.ExpressionStatement(
                                                SyntaxFactory.PostfixUnaryExpression(SyntaxKind.PostIncrementExpression,
                                                    SyntaxFactory.IdentifierName("limit"))
                                            )
                                        ),
                                        SyntaxFactory.ForStatement(
                                                SyntaxFactory.Block(
                                                    SyntaxFactory.ExpressionStatement(
                                                        SyntaxFactory.AssignmentExpression(
                                                            SyntaxKind.SimpleAssignmentExpression,
                                                            SyntaxFactory.IdentifierName("joined"),
                                                            SyntaxFactory.LiteralExpression(SyntaxKind
                                                                .TrueLiteralExpression)
                                                        )
                                                    ),
                                                    SyntaxFactory.LocalDeclarationStatement(
                                                        SyntaxFactory
                                                            .VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                                                            .WithVariables(SyntaxFactory.SingletonSeparatedList(
                                                                SyntaxFactory.VariableDeclarator($"{probeAlias}Row")
                                                                    .WithInitializer(SyntaxFactory.EqualsValueClause(
                                                                        SyntaxFactory.ElementAccessExpression(
                                                                            SyntaxFactory.IdentifierName(outerRowsVar),
                                                                            SyntaxFactory.BracketedArgumentList(
                                                                                SyntaxFactory.SingletonSeparatedList(
                                                                                    SyntaxFactory.Argument(
                                                                                        SyntaxFactory.IdentifierName(
                                                                                            "i"))))
                                                                        )
                                                                    ))
                                                            ))
                                                    ),
                                                    SyntaxFactory.LocalDeclarationStatement(
                                                        SyntaxFactory
                                                            .VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                                                            .WithVariables(SyntaxFactory.SingletonSeparatedList(
                                                                SyntaxFactory.VariableDeclarator($"{buildAlias}Row")
                                                                    .WithInitializer(SyntaxFactory.EqualsValueClause(
                                                                        SyntaxFactory.ElementAccessExpression(
                                                                            SyntaxFactory.IdentifierName(innerRowsVar),
                                                                            SyntaxFactory.BracketedArgumentList(
                                                                                SyntaxFactory.SingletonSeparatedList(
                                                                                    SyntaxFactory.Argument(
                                                                                        SyntaxFactory.IdentifierName(
                                                                                            "j"))))
                                                                        )
                                                                    ))
                                                            ))
                                                    ),
                                                    (StatementSyntax)rewrittenIf,
                                                    emptyBlock
                                                )
                                            )
                                            .WithDeclaration(
                                                SyntaxFactory
                                                    .VariableDeclaration(
                                                        SyntaxFactory.PredefinedType(
                                                            SyntaxFactory.Token(SyntaxKind.IntKeyword)))
                                                    .WithVariables(SyntaxFactory.SingletonSeparatedList(
                                                        SyntaxFactory.VariableDeclarator("j")
                                                            .WithInitializer(
                                                                SyntaxFactory.EqualsValueClause(
                                                                    SyntaxFactory.LiteralExpression(
                                                                        SyntaxKind.NumericLiteralExpression,
                                                                        SyntaxFactory.Literal(0))))
                                                    ))
                                            )
                                            .WithCondition(
                                                SyntaxFactory.BinaryExpression(
                                                    SyntaxKind.LessThanExpression,
                                                    SyntaxFactory.IdentifierName("j"),
                                                    SyntaxFactory.IdentifierName("limit")
                                                )
                                            )
                                            .WithIncrementors(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                                SyntaxFactory.PostfixUnaryExpression(SyntaxKind.PostIncrementExpression,
                                                    SyntaxFactory.IdentifierName("j")))),
                                        node.JoinType == JoinType.OuterLeft || node.JoinType == JoinType.OuterRight
                                            ? SyntaxFactory.IfStatement(
                                                SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression,
                                                    SyntaxFactory.IdentifierName("joined")),
                                                SyntaxFactory.Block(
                                                    SyntaxFactory.LocalDeclarationStatement(
                                                        SyntaxFactory
                                                            .VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                                                            .WithVariables(SyntaxFactory.SingletonSeparatedList(
                                                                SyntaxFactory.VariableDeclarator($"{probeAlias}Row")
                                                                    .WithInitializer(SyntaxFactory.EqualsValueClause(
                                                                        SyntaxFactory.ElementAccessExpression(
                                                                            SyntaxFactory.IdentifierName(outerRowsVar),
                                                                            SyntaxFactory.BracketedArgumentList(
                                                                                SyntaxFactory.SingletonSeparatedList(
                                                                                    SyntaxFactory.Argument(
                                                                                        SyntaxFactory.IdentifierName(
                                                                                            "i"))))
                                                                        )
                                                                    ))
                                                            ))
                                                    ),
                                                    outerJoinFallback
                                                )
                                            )
                                            : SyntaxFactory.EmptyStatement()
                                    )
                                )
                                .WithDeclaration(
                                    SyntaxFactory
                                        .VariableDeclaration(
                                            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)))
                                        .WithVariables(SyntaxFactory.SingletonSeparatedList(
                                            SyntaxFactory.VariableDeclarator("i").WithInitializer(
                                                SyntaxFactory.EqualsValueClause(
                                                    SyntaxFactory.MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        SyntaxFactory.IdentifierName("range"),
                                                        SyntaxFactory.IdentifierName("Item1"))
                                                ))
                                        ))
                                )
                                .WithCondition(
                                    SyntaxFactory.BinaryExpression(
                                        SyntaxKind.LessThanExpression,
                                        SyntaxFactory.IdentifierName("i"),
                                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.IdentifierName("range"),
                                            SyntaxFactory.IdentifierName("Item2"))
                                    )
                                )
                                .WithIncrementors(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                    SyntaxFactory.PostfixUnaryExpression(SyntaxKind.PostIncrementExpression,
                                        SyntaxFactory.IdentifierName("i"))))
                        )
                    )
                )
            ]))
        );

        computingBlock = computingBlock.AddStatements(SyntaxFactory.ExpressionStatement(parallelLoop));

        computingBlock = computingBlock.AddStatements(
            SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(scope[MetaAttributes.SelectIntoVariableName]),
                        SyntaxFactory.IdentifierName("AddRange")
                    ),
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("resultBag"))))
                )
            )
        );

        return computingBlock;
    }

    private static StatementSyntax StripGuardClauses(StatementSyntax statement)
    {
        if (statement is BlockSyntax block)
        {
            var newStatements = block.Statements.Where(s => !IsGuardClause(s));
            return SyntaxFactory.Block(newStatements);
        }

        return statement;
    }

    private static bool IsGuardClause(StatementSyntax s)
    {
        if (s is IfStatementSyntax ifStmt)
        {
            if (ifStmt.Statement is BlockSyntax b && b.Statements.Any(st => st is ContinueStatementSyntax))
                return true;
            if (ifStmt.Statement is ContinueStatementSyntax)
                return true;
        }

        return false;
    }

    private static string GenerateRowResolverCreation(Scope scope, string alias)
    {
        var columns = scope.ScopeSymbolTable.GetSymbol<TableSymbol>(alias).GetColumns(alias);
        var mapCreation = "new System.Collections.Generic.Dictionary<string, int> { " +
                          string.Join(", ", columns.Select((c, i) => $"{{\"{c.ColumnName}\", {i}}}")) +
                          " }";

        return
            $"(Musoq.Schema.DataSources.IObjectResolver)new Musoq.Evaluator.Tables.RowResolver(new Musoq.Evaluator.Tables.ObjectsRow(new object[{columns.Length}]), {mapCreation})";
    }

    private static ExpressionSyntax ReplaceIdentifier(ExpressionSyntax expression, string oldName, string newName)
    {
        var rewriter = new IdentifierReplacementRewriter(oldName, newName);
        return (ExpressionSyntax)rewriter.Visit(expression);
    }

    private static bool TryGetSortMergeJoinKeys(
        JoinSourcesTableFromNode node,
        Scope scope,
        Func<Node, ExpressionSyntax> nodeTranslator,
        out ExpressionSyntax leftKey,
        out ExpressionSyntax rightKey,
        out Type keyType,
        out BinaryNode condition,
        out SyntaxKind comparisonKind)
    {
        leftKey = null;
        rightKey = null;
        keyType = null;
        condition = null;
        comparisonKind = SyntaxKind.None;

        var conditions = new List<BinaryNode>();

        if (node.Expression is BinaryNode binary && IsInequality(binary))
        {
            conditions.Add(binary);
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
                else if (current is BinaryNode b && IsInequality(b))
                {
                    conditions.Add(b);
                }
            }
        }

        foreach (var bin in conditions)
        {
            var leftVisitor = new ExtractAccessColumnFromQueryVisitor();
            var leftTraverser = new ExtractAccessColumnFromQueryTraverseVisitor(leftVisitor);
            bin.Left.Accept(leftTraverser);
            var leftColumns = leftVisitor.GetForAliases(node.First.Alias, node.Second.Alias);

            var rightVisitor = new ExtractAccessColumnFromQueryVisitor();
            var rightTraverser = new ExtractAccessColumnFromQueryTraverseVisitor(rightVisitor);
            bin.Right.Accept(rightTraverser);
            var rightColumns = rightVisitor.GetForAliases(node.First.Alias, node.Second.Alias);

            var leftHasFirst = leftColumns.Any(c => c.Alias == node.First.Alias);
            var leftHasSecond = leftColumns.Any(c => c.Alias == node.Second.Alias);
            var rightHasFirst = rightColumns.Any(c => c.Alias == node.First.Alias);
            var rightHasSecond = rightColumns.Any(c => c.Alias == node.Second.Alias);

            var leftIsFirst = leftHasFirst && !leftHasSecond;
            var leftIsSecond = leftHasSecond && !leftHasFirst;
            var leftIsConstant = !leftHasFirst && !leftHasSecond;

            var rightIsFirst = rightHasFirst && !rightHasSecond;
            var rightIsSecond = rightHasSecond && !rightHasFirst;
            var rightIsConstant = !rightHasFirst && !rightHasSecond;

            if (leftIsConstant && rightIsConstant) continue;

            Node outerNode = null;
            Node innerNode = null;
            var kind = SyntaxKind.None;

            if ((leftIsFirst || leftIsConstant) && (rightIsSecond || rightIsConstant))
            {
                outerNode = bin.Left;
                innerNode = bin.Right;
                kind = GetSyntaxKind(bin);
            }
            else if ((leftIsSecond || leftIsConstant) && (rightIsFirst || rightIsConstant))
            {
                outerNode = bin.Right;
                innerNode = bin.Left;
                kind = GetSwappedSyntaxKind(bin);
            }

            if (outerNode != null && innerNode != null)
            {
                var type1 = outerNode.ReturnType;
                var type2 = innerNode.ReturnType;

                var type1Underlying = Nullable.GetUnderlyingType(type1) ?? type1;
                var type2Underlying = Nullable.GetUnderlyingType(type2) ?? type2;

                if (type1Underlying == type2Underlying && IsComparable(type1Underlying))
                {
                    var kType = type1 != type2
                        ? typeof(Nullable<>).MakeGenericType(type1Underlying)
                        : type1;

                    leftKey = nodeTranslator(outerNode);
                    rightKey = nodeTranslator(innerNode);
                    keyType = kType;
                    condition = bin;
                    comparisonKind = kind;

                    return true;
                }
            }
        }

        return false;
    }

    private static int GetColumnIndex(string name, TableSymbol table, string alias)
    {
        var columns = table.GetColumns(alias);
        for (var i = 0; i < columns.Length; i++)
            if (string.Equals(columns[i].ColumnName, name, StringComparison.OrdinalIgnoreCase))
                return columns[i].ColumnIndex;
        return -1;
    }

    private static ExpressionSyntax CreateColumnAccessExpression(string alias, int index, Type type)
    {
        var rowVar = SyntaxFactory.IdentifierName($"{alias}Row");
        var indexLiteral =
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(index));

        var iObjectResolverType = SyntaxHelper.IObjectResolverTypeSyntax;

        var castToResolver = SyntaxFactory.CastExpression(iObjectResolverType, rowVar);
        var accessResolver = SyntaxFactory.ElementAccessExpression(
            SyntaxFactory.ParenthesizedExpression(castToResolver),
            SyntaxFactory.BracketedArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(indexLiteral)
                )
            )
        );

        return SyntaxFactory.CastExpression(
            SyntaxFactory.ParseTypeName(EvaluationHelper.GetCastableType(type)),
            accessResolver
        );
    }

    private static bool IsInequality(BinaryNode node)
    {
        return node is GreaterNode || node is LessNode || node is GreaterOrEqualNode || node is LessOrEqualNode;
    }

    private static bool IsComparable(Type type)
    {
        return typeof(IComparable).IsAssignableFrom(type) || type.IsPrimitive || type == typeof(string) ||
               type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan) ||
               type == typeof(decimal);
    }

    private static SyntaxKind GetSyntaxKind(BinaryNode node)
    {
        if (node is GreaterNode) return SyntaxKind.GreaterThanExpression;
        if (node is GreaterOrEqualNode) return SyntaxKind.GreaterThanOrEqualExpression;
        if (node is LessNode) return SyntaxKind.LessThanExpression;
        if (node is LessOrEqualNode) return SyntaxKind.LessThanOrEqualExpression;
        if (node is DiffNode) return SyntaxKind.NotEqualsExpression;
        if (node is EqualityNode) return SyntaxKind.EqualsExpression;
        return SyntaxKind.None;
    }

    private static SyntaxKind GetSwappedSyntaxKind(BinaryNode node)
    {
        if (node is GreaterNode) return SyntaxKind.LessThanExpression;
        if (node is GreaterOrEqualNode) return SyntaxKind.LessThanOrEqualExpression;
        if (node is LessNode) return SyntaxKind.GreaterThanExpression;
        if (node is LessOrEqualNode) return SyntaxKind.GreaterThanOrEqualExpression;
        if (node is DiffNode) return SyntaxKind.NotEqualsExpression;
        if (node is EqualityNode) return SyntaxKind.EqualsExpression;
        return SyntaxKind.None;
    }

    private static string GetTypeName(Type type)
    {
        if (type == typeof(void)) return "void";
        if (!type.IsGenericType) return type.FullName.Replace('+', '.');

        var genericTypeName = type.GetGenericTypeDefinition().FullName.Replace('+', '.');
        genericTypeName = genericTypeName.Substring(0, genericTypeName.IndexOf('`'));
        var genericArgs = string.Join(",", type.GetGenericArguments().Select(GetTypeName));
        return $"{genericTypeName}<{genericArgs}>";
    }

    private static SyntaxKind SwapComparisonKind(SyntaxKind kind)
    {
        switch (kind)
        {
            case SyntaxKind.GreaterThanExpression: return SyntaxKind.LessThanExpression;
            case SyntaxKind.GreaterThanOrEqualExpression: return SyntaxKind.LessThanOrEqualExpression;
            case SyntaxKind.LessThanExpression: return SyntaxKind.GreaterThanExpression;
            case SyntaxKind.LessThanOrEqualExpression: return SyntaxKind.GreaterThanOrEqualExpression;
            default: return kind;
        }
    }

    private static HashSet<string> GetComponentAliases(FromNode node)
    {
        var aliases = new HashSet<string>();
        CollectComponentAliases(node, aliases);
        return aliases;
    }

    private static void CollectComponentAliases(FromNode node, HashSet<string> aliases)
    {
        if (node == null) return;


        aliases.Add(node.Alias);


        if (node is JoinSourcesTableFromNode joinNode)
        {
            CollectComponentAliases(joinNode.First, aliases);
            CollectComponentAliases(joinNode.Second, aliases);
        }
    }

    private class IdentifierReplacementRewriter : CSharpSyntaxRewriter
    {
        private readonly string _newName;
        private readonly string _oldName;

        public IdentifierReplacementRewriter(string oldName, string newName)
        {
            _oldName = oldName;
            _newName = newName;
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (node.Identifier.Text == _oldName) return SyntaxFactory.IdentifierName(_newName);
            return base.VisitIdentifierName(node);
        }
    }

    private class TableAddRewriter : CSharpSyntaxRewriter
    {
        private readonly string _listName;
        private readonly string _tableName;

        public TableAddRewriter(string tableName, string listName)
        {
            _tableName = tableName;
            _listName = listName;
        }

        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (node.Expression is MemberAccessExpressionSyntax memberAccess)
                if (memberAccess.Expression is IdentifierNameSyntax identifier &&
                    identifier.Identifier.Text == _tableName &&
                    memberAccess.Name.Identifier.Text == "Add")
                    return node.WithExpression(
                        memberAccess.WithExpression(
                            SyntaxFactory.IdentifierName(_listName)
                        )
                    );

            return base.VisitInvocationExpression(node);
        }
    }
}

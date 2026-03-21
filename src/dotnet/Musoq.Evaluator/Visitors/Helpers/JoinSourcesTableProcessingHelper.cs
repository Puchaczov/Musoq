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

        if ((node.JoinType == JoinType.AsOf || node.JoinType == JoinType.AsOfLeft) &&
            TryGetAsOfJoinKeys(node, scope, nodeTranslator, out var asofEqLeftKeys, out var asofEqRightKeys,
                out var asofEqKeyTypes, out var asofLeftIneqKey, out var asofRightIneqKey, out var asofIneqKeyType,
                out var asofComparisonKind))
            return ProcessAsOfJoin(node, generator, scope, queryAlias, asofEqLeftKeys, asofEqRightKeys,
                asofEqKeyTypes, asofLeftIneqKey, asofRightIneqKey, asofIneqKeyType, asofComparisonKind,
                ifStatement, emptyBlock, getRowsSourceOrEmpty, block, generateCancellationExpression);

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

            case JoinType.AsOf:
            case JoinType.AsOfLeft:
                throw new InvalidOperationException(
                    "ASOF JOIN could not extract required keys. Ensure ON clause has exactly one inequality condition.");

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
            SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier($"{alias}RowsEnumerable"), null,
                            SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName($"{alias}Rows"),
                                    SyntaxFactory.IdentifierName("Rows"))))))),
            SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier($"{alias}RowsCached"), null,
                            SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.BinaryExpression(SyntaxKind.CoalesceExpression,
                                    SyntaxFactory.BinaryExpression(SyntaxKind.AsExpression,
                                        SyntaxFactory.IdentifierName($"{alias}RowsEnumerable"),
                                        SyntaxFactory.ArrayType(
                                            SyntaxFactory.ParseTypeName("Musoq.Schema.DataSources.IObjectResolver"),
                                            SyntaxFactory.SingletonList(
                                                SyntaxFactory.ArrayRankSpecifier(
                                                    SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                                        SyntaxFactory.OmittedArraySizeExpression()))))),
                                    SyntaxFactory.InvocationExpression(
                                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                    SyntaxFactory.IdentifierName("System"),
                                                    SyntaxFactory.IdentifierName("Linq")),
                                                SyntaxFactory.IdentifierName("Enumerable")),
                                            SyntaxFactory.IdentifierName("ToArray")),
                                        SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName($"{alias}RowsEnumerable")))))))))))
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

    private static bool TryGetAsOfJoinKeys(
        JoinSourcesTableFromNode node,
        Scope scope,
        Func<Node, ExpressionSyntax> nodeTranslator,
        out List<ExpressionSyntax> eqLeftKeys,
        out List<ExpressionSyntax> eqRightKeys,
        out List<Type> eqKeyTypes,
        out ExpressionSyntax ineqLeftKey,
        out ExpressionSyntax ineqRightKey,
        out Type ineqKeyType,
        out SyntaxKind comparisonKind)
    {
        eqLeftKeys = [];
        eqRightKeys = [];
        eqKeyTypes = [];
        ineqLeftKey = null;
        ineqRightKey = null;
        ineqKeyType = null;
        comparisonKind = SyntaxKind.None;

        var equalities = new List<EqualityNode>();
        var inequalities = new List<BinaryNode>();

        var stack = new Stack<Node>();
        stack.Push(node.Expression);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            if (current is AndNode and)
            {
                stack.Push(and.Right);
                stack.Push(and.Left);
            }
            else if (current is EqualityNode eq)
            {
                equalities.Add(eq);
            }
            else if (current is BinaryNode bin && IsInequality(bin))
            {
                inequalities.Add(bin);
            }
            else
            {
                return false;
            }
        }

        if (inequalities.Count != 1)
            return false;

        var firstAliases = GetComponentAliases(node.First);
        var secondAliases = GetComponentAliases(node.Second);
        var allAliases = firstAliases.Concat(secondAliases).ToArray();

        foreach (var eq in equalities)
        {
            var leftVisitor = new ExtractAccessColumnFromQueryVisitor();
            var leftTraverser = new ExtractAccessColumnFromQueryTraverseVisitor(leftVisitor);
            eq.Left.Accept(leftTraverser);
            var leftColumns = leftVisitor.GetForAliases(allAliases);

            var rightVisitor = new ExtractAccessColumnFromQueryVisitor();
            var rightTraverser = new ExtractAccessColumnFromQueryTraverseVisitor(rightVisitor);
            eq.Right.Accept(rightTraverser);
            var rightColumns = rightVisitor.GetForAliases(allAliases);

            var leftHasFirst = leftColumns.Any(c => firstAliases.Contains(c.Alias));
            var leftHasSecond = leftColumns.Any(c => secondAliases.Contains(c.Alias));
            var rightHasFirst = rightColumns.Any(c => firstAliases.Contains(c.Alias));
            var rightHasSecond = rightColumns.Any(c => secondAliases.Contains(c.Alias));

            Node firstNode = null;
            Node secondNode = null;

            if (leftHasFirst && !leftHasSecond && rightHasSecond && !rightHasFirst)
            {
                firstNode = eq.Left;
                secondNode = eq.Right;
            }
            else if (leftHasSecond && !leftHasFirst && rightHasFirst && !rightHasSecond)
            {
                firstNode = eq.Right;
                secondNode = eq.Left;
            }

            if (firstNode == null || secondNode == null)
                return false;

            var type1 = Nullable.GetUnderlyingType(firstNode.ReturnType) ?? firstNode.ReturnType;
            var type2 = Nullable.GetUnderlyingType(secondNode.ReturnType) ?? secondNode.ReturnType;

            if (type1 != type2)
                return false;

            var keyType = firstNode.ReturnType != secondNode.ReturnType
                ? typeof(Nullable<>).MakeGenericType(type1)
                : firstNode.ReturnType;

            var leftExpr = nodeTranslator(firstNode);
            var rightExpr = nodeTranslator(secondNode);

            if (firstNode.ReturnType != keyType)
                leftExpr = SyntaxFactory.CastExpression(SyntaxFactory.ParseTypeName(GetTypeName(keyType)), leftExpr);
            if (secondNode.ReturnType != keyType)
                rightExpr = SyntaxFactory.CastExpression(SyntaxFactory.ParseTypeName(GetTypeName(keyType)), rightExpr);

            eqLeftKeys.Add(leftExpr);
            eqRightKeys.Add(rightExpr);
            eqKeyTypes.Add(keyType);
        }

        var ineq = inequalities[0];

        var ineqLVisitor = new ExtractAccessColumnFromQueryVisitor();
        var ineqLTraverser = new ExtractAccessColumnFromQueryTraverseVisitor(ineqLVisitor);
        ineq.Left.Accept(ineqLTraverser);
        var ineqLeftCols = ineqLVisitor.GetForAliases(allAliases);

        var ineqRVisitor = new ExtractAccessColumnFromQueryVisitor();
        var ineqRTraverser = new ExtractAccessColumnFromQueryTraverseVisitor(ineqRVisitor);
        ineq.Right.Accept(ineqRTraverser);
        var ineqRightCols = ineqRVisitor.GetForAliases(allAliases);

        var ineqLeftIsFirst = ineqLeftCols.Any(c => firstAliases.Contains(c.Alias)) &&
                              !ineqLeftCols.Any(c => secondAliases.Contains(c.Alias));
        var ineqRightIsSecond = ineqRightCols.Any(c => secondAliases.Contains(c.Alias)) &&
                                !ineqRightCols.Any(c => firstAliases.Contains(c.Alias));
        var ineqLeftIsSecond = ineqLeftCols.Any(c => secondAliases.Contains(c.Alias)) &&
                               !ineqLeftCols.Any(c => firstAliases.Contains(c.Alias));
        var ineqRightIsFirst = ineqRightCols.Any(c => firstAliases.Contains(c.Alias)) &&
                               !ineqRightCols.Any(c => secondAliases.Contains(c.Alias));

        Node ineqFirstNode;
        Node ineqSecondNode;

        if (ineqLeftIsFirst && ineqRightIsSecond)
        {
            ineqFirstNode = ineq.Left;
            ineqSecondNode = ineq.Right;
            comparisonKind = GetSyntaxKind(ineq);
        }
        else if (ineqLeftIsSecond && ineqRightIsFirst)
        {
            ineqFirstNode = ineq.Right;
            ineqSecondNode = ineq.Left;
            comparisonKind = GetSwappedSyntaxKind(ineq);
        }
        else
        {
            return false;
        }

        var ineqType1 = Nullable.GetUnderlyingType(ineqFirstNode.ReturnType) ?? ineqFirstNode.ReturnType;
        var ineqType2 = Nullable.GetUnderlyingType(ineqSecondNode.ReturnType) ?? ineqSecondNode.ReturnType;

        if (ineqType1 != ineqType2 || !IsComparable(ineqType1))
            return false;

        ineqKeyType = ineqFirstNode.ReturnType != ineqSecondNode.ReturnType
            ? typeof(Nullable<>).MakeGenericType(ineqType1)
            : ineqFirstNode.ReturnType;

        ineqLeftKey = nodeTranslator(ineqFirstNode);
        ineqRightKey = nodeTranslator(ineqSecondNode);

        return true;
    }

    private static BlockSyntax ProcessAsOfJoin(
        JoinSourcesTableFromNode node,
        SyntaxGenerator generator,
        Scope scope,
        string queryAlias,
        List<ExpressionSyntax> eqLeftKeys,
        List<ExpressionSyntax> eqRightKeys,
        List<Type> eqKeyTypes,
        ExpressionSyntax ineqLeftKey,
        ExpressionSyntax ineqRightKey,
        Type ineqKeyType,
        SyntaxKind comparisonKind,
        SyntaxNode ifStatement,
        BlockSyntax emptyBlock,
        Func<string, StatementSyntax> getRowsSourceOrEmpty,
        Func<StatementSyntax[], BlockSyntax> block,
        Func<StatementSyntax> generateCancellationExpression)
    {
        var computingBlock = SyntaxFactory.Block();

        var isLeftJoin = node.JoinType == JoinType.AsOfLeft;
        var leftAlias = node.First.Alias;
        var rightAlias = node.Second.Alias;

        var rightRowsVar = $"{rightAlias}RowsArray";
        var rightKeysVar = $"{rightAlias}IneqKeys";
        var ineqKeyTypeSyntax = EvaluationHelper.GetCastableType(ineqKeyType);

        computingBlock = computingBlock.AddStatements(getRowsSourceOrEmpty(rightAlias));

        computingBlock = computingBlock.AddStatements(
            SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(rightRowsVar), null,
                            SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.IdentifierName("System"),
                                                SyntaxFactory.IdentifierName("Linq")),
                                            SyntaxFactory.IdentifierName("Enumerable")),
                                        SyntaxFactory.IdentifierName("ToArray")),
                                    SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(
                                            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.IdentifierName($"{rightAlias}Rows"),
                                                SyntaxFactory.IdentifierName("Rows"))))))))))));

        if (eqLeftKeys.Count > 0)
        {
            computingBlock = GenerateAsOfJoinWithEqualityKeys(
                computingBlock, node, generator, scope, queryAlias,
                eqLeftKeys, eqRightKeys, eqKeyTypes,
                ineqLeftKey, ineqRightKey, ineqKeyType, ineqKeyTypeSyntax,
                comparisonKind, ifStatement, emptyBlock,
                getRowsSourceOrEmpty, block, generateCancellationExpression,
                leftAlias, rightAlias, rightRowsVar, rightKeysVar, isLeftJoin);
        }
        else
        {
            computingBlock = GenerateAsOfJoinWithoutEqualityKeys(
                computingBlock, node, generator, scope, queryAlias,
                ineqLeftKey, ineqRightKey, ineqKeyType, ineqKeyTypeSyntax,
                comparisonKind, ifStatement, emptyBlock,
                getRowsSourceOrEmpty, block, generateCancellationExpression,
                leftAlias, rightAlias, rightRowsVar, rightKeysVar, isLeftJoin);
        }

        return computingBlock;
    }

    private static BlockSyntax GenerateAsOfJoinWithoutEqualityKeys(
        BlockSyntax computingBlock,
        JoinSourcesTableFromNode node,
        SyntaxGenerator generator,
        Scope scope,
        string queryAlias,
        ExpressionSyntax ineqLeftKey,
        ExpressionSyntax ineqRightKey,
        Type ineqKeyType,
        string ineqKeyTypeSyntax,
        SyntaxKind comparisonKind,
        SyntaxNode ifStatement,
        BlockSyntax emptyBlock,
        Func<string, StatementSyntax> getRowsSourceOrEmpty,
        Func<StatementSyntax[], BlockSyntax> block,
        Func<StatementSyntax> generateCancellationExpression,
        string leftAlias,
        string rightAlias,
        string rightRowsVar,
        string rightKeysVar,
        bool isLeftJoin)
    {
        computingBlock = computingBlock.AddStatements(
            SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(rightKeysVar), null,
                            SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.ArrayCreationExpression(
                                    SyntaxFactory.ArrayType(SyntaxFactory.ParseTypeName(ineqKeyTypeSyntax))
                                        .WithRankSpecifiers(SyntaxFactory.SingletonList(
                                            SyntaxFactory.ArrayRankSpecifier(
                                                SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                        SyntaxFactory.IdentifierName(rightRowsVar),
                                                        SyntaxFactory.IdentifierName("Length")))))))))))));

        computingBlock = computingBlock.AddStatements(
            GenerateKeyExtractionLoop(rightRowsVar, rightKeysVar, rightAlias, ineqRightKey));

        computingBlock = computingBlock.AddStatements(
            SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("System"),
                            SyntaxFactory.IdentifierName("Array")),
                        SyntaxFactory.IdentifierName("Sort")),
                    SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
                        [
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName(rightKeysVar)),
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName(rightRowsVar))
                        ])))));

        computingBlock = computingBlock.AddStatements(getRowsSourceOrEmpty(leftAlias));

        var binarySearchBlock = GenerateBinarySearchAndEmit(
            generator, scope, queryAlias, node,
            ineqLeftKey, ineqKeyTypeSyntax, comparisonKind,
            ifStatement, emptyBlock, generateCancellationExpression,
            leftAlias, rightAlias, rightRowsVar, rightKeysVar, isLeftJoin);

        computingBlock = computingBlock.AddStatements(
            SyntaxFactory.ForEachStatement(
                SyntaxFactory.IdentifierName("var"),
                SyntaxFactory.Identifier($"{leftAlias}Row"),
                SyntaxFactory.IdentifierName($"{leftAlias}Rows.Rows"),
                binarySearchBlock));

        return computingBlock;
    }

    private static BlockSyntax GenerateAsOfJoinWithEqualityKeys(
        BlockSyntax computingBlock,
        JoinSourcesTableFromNode node,
        SyntaxGenerator generator,
        Scope scope,
        string queryAlias,
        List<ExpressionSyntax> eqLeftKeys,
        List<ExpressionSyntax> eqRightKeys,
        List<Type> eqKeyTypes,
        ExpressionSyntax ineqLeftKey,
        ExpressionSyntax ineqRightKey,
        Type ineqKeyType,
        string ineqKeyTypeSyntax,
        SyntaxKind comparisonKind,
        SyntaxNode ifStatement,
        BlockSyntax emptyBlock,
        Func<string, StatementSyntax> getRowsSourceOrEmpty,
        Func<StatementSyntax[], BlockSyntax> block,
        Func<StatementSyntax> generateCancellationExpression,
        string leftAlias,
        string rightAlias,
        string rightRowsVar,
        string rightKeysVar,
        bool isLeftJoin)
    {
        string eqKeyTypeName;
        if (eqKeyTypes.Count == 1)
        {
            eqKeyTypeName = EvaluationHelper.GetCastableType(eqKeyTypes[0]);
        }
        else
        {
            var typeNames = eqKeyTypes.Select(t => EvaluationHelper.GetCastableType(t));
            eqKeyTypeName = $"({string.Join(", ", typeNames)})";
        }

        var bucketType =
            $"System.Collections.Generic.Dictionary<{eqKeyTypeName}, (Musoq.Schema.DataSources.IObjectResolver[] Rows, {ineqKeyTypeSyntax}[] Keys)>";
        var bucketVar = $"{rightAlias}Buckets";

        computingBlock = computingBlock.AddStatements(
            SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(bucketVar), null,
                            SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(bucketType))
                                    .WithArgumentList(SyntaxFactory.ArgumentList())))))));

        var groupListType =
            $"System.Collections.Generic.Dictionary<{eqKeyTypeName}, System.Collections.Generic.List<Musoq.Schema.DataSources.IObjectResolver>>";
        var groupListVar = $"{rightAlias}Groups";

        computingBlock = computingBlock.AddStatements(
            SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(groupListVar), null,
                            SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(groupListType))
                                    .WithArgumentList(SyntaxFactory.ArgumentList())))))));

        var buildStatements = new List<StatementSyntax> { generateCancellationExpression() };

        var buildKeyVars = new List<string>();
        for (var i = 0; i < eqRightKeys.Count; i++)
        {
            var varName = $"eqKey{i}";
            buildKeyVars.Add(varName);
            buildStatements.Add(
                SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"),
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(varName), null,
                                SyntaxFactory.EqualsValueClause(eqRightKeys[i]))))));

            if (!eqKeyTypes[i].IsValueType || Nullable.GetUnderlyingType(eqKeyTypes[i]) != null)
                buildStatements.Add(
                    SyntaxFactory.IfStatement(
                        SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression,
                            SyntaxFactory.IdentifierName(varName),
                            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                        SyntaxFactory.ContinueStatement()));
        }

        ExpressionSyntax buildKeyExpr = buildKeyVars.Count == 1
            ? SyntaxFactory.IdentifierName(buildKeyVars[0])
            : SyntaxFactory.TupleExpression(
                SyntaxFactory.SeparatedList(
                    buildKeyVars.Select(v => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(v)))));

        buildStatements.Add(
            SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("eqKey"), null,
                            SyntaxFactory.EqualsValueClause(buildKeyExpr))))));

        buildStatements.Add(
            SyntaxFactory.IfStatement(
                SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression,
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(groupListVar),
                            SyntaxFactory.IdentifierName("TryGetValue")),
                        SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
                            [
                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("eqKey")),
                                SyntaxFactory.Argument(
                                        SyntaxFactory.DeclarationExpression(
                                            SyntaxFactory.IdentifierName("var"),
                                            SyntaxFactory.SingleVariableDesignation(SyntaxFactory.Identifier("existingList"))))
                                    .WithRefOrOutKeyword(SyntaxFactory.Token(SyntaxKind.OutKeyword))
                            ])))),
                SyntaxFactory.Block(
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.IdentifierName("existingList"),
                            SyntaxFactory.ObjectCreationExpression(
                                    SyntaxFactory.ParseTypeName("System.Collections.Generic.List<Musoq.Schema.DataSources.IObjectResolver>"))
                                .WithArgumentList(SyntaxFactory.ArgumentList()))),
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.ElementAccessExpression(
                                SyntaxFactory.IdentifierName(groupListVar),
                                SyntaxFactory.BracketedArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("eqKey"))))),
                            SyntaxFactory.IdentifierName("existingList"))))));

        buildStatements.Add(
            SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("existingList"),
                        SyntaxFactory.IdentifierName("Add")),
                    SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName($"{rightAlias}Row")))))));

        computingBlock = computingBlock.AddStatements(
            SyntaxFactory.ForEachStatement(
                SyntaxFactory.IdentifierName("var"),
                SyntaxFactory.Identifier($"{rightAlias}Row"),
                SyntaxFactory.IdentifierName(rightRowsVar),
                SyntaxFactory.Block(buildStatements)));

        var sortStatements = new List<StatementSyntax>
        {
            SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("bucketRows"), null,
                            SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.IdentifierName("kvp"),
                                            SyntaxFactory.IdentifierName("Value")),
                                        SyntaxFactory.IdentifierName("ToArray")))))))),
            SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("bucketKeys"), null,
                            SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.ArrayCreationExpression(
                                    SyntaxFactory.ArrayType(SyntaxFactory.ParseTypeName(ineqKeyTypeSyntax))
                                        .WithRankSpecifiers(SyntaxFactory.SingletonList(
                                            SyntaxFactory.ArrayRankSpecifier(
                                                SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                        SyntaxFactory.IdentifierName("bucketRows"),
                                                        SyntaxFactory.IdentifierName("Length")))))))))))),
            GenerateKeyExtractionLoop("bucketRows", "bucketKeys", rightAlias, ineqRightKey),
            SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("System"),
                            SyntaxFactory.IdentifierName("Array")),
                        SyntaxFactory.IdentifierName("Sort")),
                    SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
                        [
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("bucketKeys")),
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("bucketRows"))
                        ])))),
            SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.ElementAccessExpression(
                        SyntaxFactory.IdentifierName(bucketVar),
                        SyntaxFactory.BracketedArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(
                                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName("kvp"),
                                        SyntaxFactory.IdentifierName("Key")))))),
                    SyntaxFactory.TupleExpression(SyntaxFactory.SeparatedList(
                        [
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("bucketRows")),
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("bucketKeys"))
                        ]))))
        };

        computingBlock = computingBlock.AddStatements(
            SyntaxFactory.ForEachStatement(
                SyntaxFactory.IdentifierName("var"),
                SyntaxFactory.Identifier("kvp"),
                SyntaxFactory.IdentifierName(groupListVar),
                SyntaxFactory.Block(sortStatements)));

        computingBlock = computingBlock.AddStatements(getRowsSourceOrEmpty(leftAlias));

        var probeStatements = new List<StatementSyntax> { generateCancellationExpression() };

        var probeKeyVars = new List<string>();
        for (var i = 0; i < eqLeftKeys.Count; i++)
        {
            var varName = $"eqKey{i}";
            probeKeyVars.Add(varName);
            probeStatements.Add(
                SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"),
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(varName), null,
                                SyntaxFactory.EqualsValueClause(eqLeftKeys[i]))))));
        }

        ExpressionSyntax probeKeyExpr = probeKeyVars.Count == 1
            ? SyntaxFactory.IdentifierName(probeKeyVars[0])
            : SyntaxFactory.TupleExpression(
                SyntaxFactory.SeparatedList(
                    probeKeyVars.Select(v => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(v)))));

        probeStatements.Add(
            SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("probeKey"), null,
                            SyntaxFactory.EqualsValueClause(probeKeyExpr))))));

        var bucketFoundBody = GenerateBinarySearchAndEmit(
            generator, scope, queryAlias, node,
            ineqLeftKey, ineqKeyTypeSyntax, comparisonKind,
            ifStatement, emptyBlock, generateCancellationExpression,
            leftAlias, rightAlias, "bucket.Rows", "bucket.Keys", isLeftJoin);

        var tryGetBucket = SyntaxFactory.IfStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(bucketVar),
                    SyntaxFactory.IdentifierName("TryGetValue")),
                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList([
                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName("probeKey")),
                    SyntaxFactory.Argument(
                            SyntaxFactory.DeclarationExpression(
                                SyntaxFactory.IdentifierName("var"),
                                SyntaxFactory.SingleVariableDesignation(SyntaxFactory.Identifier("bucket"))))
                        .WithRefOrOutKeyword(SyntaxFactory.Token(SyntaxKind.OutKeyword))
                ]))),
            bucketFoundBody,
            isLeftJoin
                ? SyntaxFactory.ElseClause(GenerateNullFallback(node, generator, scope, queryAlias, leftAlias))
                : null);

        probeStatements.Add(tryGetBucket);

        computingBlock = computingBlock.AddStatements(
            SyntaxFactory.ForEachStatement(
                SyntaxFactory.IdentifierName("var"),
                SyntaxFactory.Identifier($"{leftAlias}Row"),
                SyntaxFactory.IdentifierName($"{leftAlias}Rows.Rows"),
                SyntaxFactory.Block(probeStatements)));

        return computingBlock;
    }

    private static BlockSyntax GenerateBinarySearchAndEmit(
        SyntaxGenerator generator,
        Scope scope,
        string queryAlias,
        JoinSourcesTableFromNode node,
        ExpressionSyntax ineqLeftKey,
        string ineqKeyTypeSyntax,
        SyntaxKind comparisonKind,
        SyntaxNode ifStatement,
        BlockSyntax emptyBlock,
        Func<StatementSyntax> generateCancellationExpression,
        string leftAlias,
        string rightAlias,
        string rightRowsExpr,
        string rightKeysExpr,
        bool isLeftJoin)
    {
        var statements = new List<StatementSyntax>();

        statements.Add(
            SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("probeValue"), null,
                            SyntaxFactory.EqualsValueClause(ineqLeftKey))))));

        statements.Add(
            SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("searchIdx"), null,
                            SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.IdentifierName("System"),
                                            SyntaxFactory.IdentifierName("Array")),
                                        SyntaxFactory.GenericName("BinarySearch")
                                            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                                                SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                                    SyntaxFactory.IdentifierName(ineqKeyTypeSyntax))))),
                                    SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
                                        [
                                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName(rightKeysExpr)),
                                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("probeValue"))
                                        ])))))))));

        statements.Add(
            SyntaxFactory.IfStatement(
                SyntaxFactory.BinaryExpression(SyntaxKind.LessThanExpression,
                    SyntaxFactory.IdentifierName("searchIdx"),
                    SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0))),
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName("searchIdx"),
                        SyntaxFactory.PrefixUnaryExpression(SyntaxKind.BitwiseNotExpression,
                            SyntaxFactory.IdentifierName("searchIdx"))))));

        var bestIdxStatements = GenerateBestIndexLogic(comparisonKind, rightKeysExpr, ineqKeyTypeSyntax);
        statements.AddRange(bestIdxStatements);

        var matchBlock = new List<StatementSyntax>
        {
            SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.IdentifierName("var"),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(
                            SyntaxFactory.Identifier($"{rightAlias}Row"),
                            null,
                            SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.ElementAccessExpression(
                                    SyntaxFactory.IdentifierName(rightRowsExpr),
                                    SyntaxFactory.BracketedArgumentList(
                                        SyntaxFactory.SingletonSeparatedList(
                                            SyntaxFactory.Argument(
                                                SyntaxFactory.IdentifierName("bestIdx"))))))))))
        };

        matchBlock.Add(generateCancellationExpression());
        matchBlock.Add((StatementSyntax)ifStatement);
        matchBlock.Add(emptyBlock);

        StatementSyntax elseClause = null;
        if (isLeftJoin)
            elseClause = GenerateNullFallback(node, generator, scope, queryAlias, leftAlias);

        statements.Add(
            SyntaxFactory.IfStatement(
                SyntaxFactory.BinaryExpression(SyntaxKind.GreaterThanOrEqualExpression,
                    SyntaxFactory.IdentifierName("bestIdx"),
                    SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0))),
                SyntaxFactory.Block(matchBlock),
                elseClause != null ? SyntaxFactory.ElseClause(elseClause) : null));

        return SyntaxFactory.Block(statements);
    }

    private static List<StatementSyntax> GenerateBestIndexLogic(
        SyntaxKind comparisonKind,
        string rightKeysExpr,
        string ineqKeyTypeSyntax)
    {
        var rightKeysLength = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(rightKeysExpr),
            SyntaxFactory.IdentifierName("Length"));

        var comparerCompare = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("System"),
                        SyntaxFactory.IdentifierName("Collections")),
                    SyntaxFactory.IdentifierName("Generic")),
                SyntaxFactory.GenericName("Comparer")
                    .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                            SyntaxFactory.IdentifierName(ineqKeyTypeSyntax))))),
            SyntaxFactory.IdentifierName("Default"));

        var searchIdx = SyntaxFactory.IdentifierName("searchIdx");
        var bestIdx = SyntaxFactory.IdentifierName("bestIdx");
        var probeValue = SyntaxFactory.IdentifierName("probeValue");
        var zero = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0));
        var one = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1));
        var negOne = SyntaxFactory.PrefixUnaryExpression(SyntaxKind.UnaryMinusExpression,
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)));

        InvocationExpressionSyntax ComparerCall(ExpressionSyntax left, ExpressionSyntax right) =>
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    comparerCompare, SyntaxFactory.IdentifierName("Compare")),
                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList([
                    SyntaxFactory.Argument(left), SyntaxFactory.Argument(right)])));

        ExpressionSyntax RightKeyAt(ExpressionSyntax index) =>
            SyntaxFactory.ElementAccessExpression(
                SyntaxFactory.IdentifierName(rightKeysExpr),
                SyntaxFactory.BracketedArgumentList(
                    SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(index))));

        LocalDeclarationStatementSyntax BestIdxDeclaration(ExpressionSyntax initializer) =>
            SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("bestIdx"), null,
                            SyntaxFactory.EqualsValueClause(initializer)))));

        var statements = new List<StatementSyntax>();

        switch (comparisonKind)
        {
            case SyntaxKind.GreaterThanOrEqualExpression:
                // left >= right: find largest right key <= probeValue
                // var bestIdx = (searchIdx < keys.Length && Comparer<T>.Default.Compare(keys[searchIdx], probeValue) == 0)
                //     ? searchIdx : searchIdx - 1;
                statements.Add(BestIdxDeclaration(
                    SyntaxFactory.ConditionalExpression(
                        SyntaxFactory.ParenthesizedExpression(
                            SyntaxFactory.BinaryExpression(SyntaxKind.LogicalAndExpression,
                                SyntaxFactory.BinaryExpression(SyntaxKind.LessThanExpression, searchIdx, rightKeysLength),
                                SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression,
                                    ComparerCall(RightKeyAt(searchIdx), probeValue), zero))),
                        searchIdx,
                        SyntaxFactory.BinaryExpression(SyntaxKind.SubtractExpression, searchIdx, one))));
                break;

            case SyntaxKind.GreaterThanExpression:
                // left > right: find largest right key < probeValue
                // Must scan left past any duplicates equal to probeValue
                statements.Add(BestIdxDeclaration(
                    SyntaxFactory.BinaryExpression(SyntaxKind.SubtractExpression, searchIdx, one)));
                // while (bestIdx >= 0 && Comparer<T>.Default.Compare(keys[bestIdx], probeValue) >= 0) bestIdx--;
                statements.Add(SyntaxFactory.WhileStatement(
                    SyntaxFactory.BinaryExpression(SyntaxKind.LogicalAndExpression,
                        SyntaxFactory.BinaryExpression(SyntaxKind.GreaterThanOrEqualExpression, bestIdx, zero),
                        SyntaxFactory.BinaryExpression(SyntaxKind.GreaterThanOrEqualExpression,
                            ComparerCall(RightKeyAt(bestIdx), probeValue), zero)),
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.PostfixUnaryExpression(SyntaxKind.PostDecrementExpression, bestIdx))));
                break;

            case SyntaxKind.LessThanOrEqualExpression:
                // left <= right: find smallest right key >= probeValue
                // var bestIdx = (searchIdx < keys.Length) ? searchIdx : -1;
                statements.Add(BestIdxDeclaration(
                    SyntaxFactory.ConditionalExpression(
                        SyntaxFactory.ParenthesizedExpression(
                            SyntaxFactory.BinaryExpression(SyntaxKind.LessThanExpression, searchIdx, rightKeysLength)),
                        searchIdx,
                        negOne)));
                break;

            case SyntaxKind.LessThanExpression:
                // left < right: find smallest right key > probeValue
                // Must scan right past any duplicates equal to probeValue
                statements.Add(BestIdxDeclaration(searchIdx));
                // while (bestIdx < keys.Length && Comparer<T>.Default.Compare(keys[bestIdx], probeValue) <= 0) bestIdx++;
                statements.Add(SyntaxFactory.WhileStatement(
                    SyntaxFactory.BinaryExpression(SyntaxKind.LogicalAndExpression,
                        SyntaxFactory.BinaryExpression(SyntaxKind.LessThanExpression, bestIdx, rightKeysLength),
                        SyntaxFactory.BinaryExpression(SyntaxKind.LessThanOrEqualExpression,
                            ComparerCall(RightKeyAt(bestIdx), probeValue), zero)),
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.PostfixUnaryExpression(SyntaxKind.PostIncrementExpression, bestIdx))));
                // if (bestIdx >= keys.Length) bestIdx = -1;
                statements.Add(SyntaxFactory.IfStatement(
                    SyntaxFactory.BinaryExpression(SyntaxKind.GreaterThanOrEqualExpression, bestIdx, rightKeysLength),
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            bestIdx, negOne))));
                break;

            default:
                throw new ArgumentException($"Unsupported comparison kind for ASOF JOIN: {comparisonKind}");
        }

        return statements;
    }

    private static StatementSyntax GenerateNullFallback(
        JoinSourcesTableFromNode node,
        SyntaxGenerator generator,
        Scope scope,
        string queryAlias,
        string leftAlias)
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
                                (LiteralExpressionSyntax)generator.LiteralExpression(column.ColumnName))))));

        foreach (var column in fullTransitionTable.GetColumns(fullTransitionTable.CompoundTables[1]))
            expressions.Add(
                SyntaxFactory.CastExpression(
                    SyntaxFactory.IdentifierName(EvaluationHelper.GetCastableType(column.ColumnType)),
                    (LiteralExpressionSyntax)generator.NullLiteralExpression()));

        var (_, rewriteSelect, invocation) =
            CreateSelectAndInvocationForOuterLeft(expressions, generator, scope, leftAlias);

        return SyntaxFactory.Block(
            SyntaxFactory.LocalDeclarationStatement(rewriteSelect),
            SyntaxFactory.ExpressionStatement(invocation));
    }

    private static ForStatementSyntax GenerateKeyExtractionLoop(
        string rowsVar,
        string keysVar,
        string alias,
        ExpressionSyntax keyExpr)
    {
        var rewriter = new IdentifierReplacementRewriter($"{alias}Row", "r");
        var rewrittenKey = (ExpressionSyntax)rewriter.Visit(keyExpr);

        return SyntaxFactory.ForStatement(
                SyntaxFactory.Block(
                    SyntaxFactory.LocalDeclarationStatement(
                        SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                            .WithVariables(SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.VariableDeclarator("r")
                                    .WithInitializer(SyntaxFactory.EqualsValueClause(
                                        SyntaxFactory.ElementAccessExpression(
                                            SyntaxFactory.IdentifierName(rowsVar),
                                            SyntaxFactory.BracketedArgumentList(
                                                SyntaxFactory.SingletonSeparatedList(
                                                    SyntaxFactory.Argument(
                                                        SyntaxFactory.IdentifierName("i")))))))))),
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.ElementAccessExpression(
                                SyntaxFactory.IdentifierName(keysVar),
                                SyntaxFactory.BracketedArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("i"))))),
                            rewrittenKey))))
            .WithDeclaration(
                SyntaxFactory
                    .VariableDeclaration(
                        SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)))
                    .WithVariables(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator("i").WithInitializer(
                            SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                    SyntaxFactory.Literal(0)))))))
            .WithCondition(
                SyntaxFactory.BinaryExpression(SyntaxKind.LessThanExpression,
                    SyntaxFactory.IdentifierName("i"),
                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(rowsVar),
                        SyntaxFactory.IdentifierName("Length"))))
            .WithIncrementors(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                SyntaxFactory.PostfixUnaryExpression(SyntaxKind.PostIncrementExpression,
                    SyntaxFactory.IdentifierName("i"))));
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

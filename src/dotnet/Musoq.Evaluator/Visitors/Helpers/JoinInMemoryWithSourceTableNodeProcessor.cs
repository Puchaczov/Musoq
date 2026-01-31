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
using Musoq.Evaluator.Visitors.CodeGeneration;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Visitors.Helpers;

/// <summary>
///     Processor for JoinInMemoryWithSourceTableFromNode that handles join type dispatch.
/// </summary>
public static class JoinInMemoryWithSourceTableNodeProcessor
{
    /// <summary>
    ///     Processes a JoinInMemoryWithSourceTableFromNode with optional hash/merge join optimization.
    /// </summary>
    /// <param name="node">The join node.</param>
    /// <param name="conditionExpression">The condition expression from the stack.</param>
    /// <param name="generator">The Roslyn syntax generator.</param>
    /// <param name="scope">The current scope.</param>
    /// <param name="queryAlias">The query alias.</param>
    /// <param name="getRowsSourceOrEmpty">Function to get rows source.</param>
    /// <param name="generateCancellationExpression">Function to generate cancellation check.</param>
    /// <param name="nodeTranslator">Function to translate nodes to expressions (optional, for hash/merge join).</param>
    /// <param name="compilationOptions">Compilation options (optional, for hash/merge join).</param>
    /// <returns>The result containing empty and computing blocks.</returns>
    public static ProcessResult Process(
        JoinInMemoryWithSourceTableFromNode node,
        SyntaxNode conditionExpression,
        SyntaxGenerator generator,
        Scope scope,
        string queryAlias,
        Func<string, StatementSyntax> getRowsSourceOrEmpty,
        Func<StatementSyntax> generateCancellationExpression,
        Func<Node, ExpressionSyntax> nodeTranslator = null,
        CompilationOptions compilationOptions = null)
    {
        var ifStatement = JoinEmitter.CreateJoinConditionCheck(conditionExpression, generator);
        var emptyBlock = StatementEmitter.CreateEmptyBlock();


        if (compilationOptions?.UseHashJoin == true &&
            nodeTranslator != null &&
            (node.JoinType == JoinType.Inner || node.JoinType == JoinType.OuterLeft ||
             node.JoinType == JoinType.OuterRight) &&
            TryGetHashJoinKeys(node, scope, nodeTranslator, out var leftKeys, out var rightKeys, out var keyTypes))
        {
            var computingBlock = ProcessHashJoin(
                node, generator, scope, queryAlias, leftKeys, rightKeys, keyTypes,
                ifStatement, emptyBlock, getRowsSourceOrEmpty, generateCancellationExpression);

            return new ProcessResult
            {
                EmptyBlock = emptyBlock,
                ComputingBlock = computingBlock
            };
        }


        var nestedLoopBlock = node.JoinType switch
        {
            JoinType.Inner => JoinProcessingHelper.ProcessInnerJoin(
                node, ifStatement, emptyBlock, generator, getRowsSourceOrEmpty, generateCancellationExpression),
            JoinType.OuterLeft => JoinProcessingHelper.ProcessOuterLeftJoin(
                node, ifStatement, emptyBlock, generator, scope, queryAlias, getRowsSourceOrEmpty,
                generateCancellationExpression),
            JoinType.OuterRight => JoinProcessingHelper.ProcessOuterRightJoin(
                node, ifStatement, emptyBlock, generator, scope, queryAlias, getRowsSourceOrEmpty,
                generateCancellationExpression),
            _ => throw new ArgumentException($"Unsupported join type: {node.JoinType}")
        };

        return new ProcessResult
        {
            EmptyBlock = emptyBlock,
            ComputingBlock = nestedLoopBlock
        };
    }

    private static BlockSyntax ProcessHashJoin(
        JoinInMemoryWithSourceTableFromNode node,
        SyntaxGenerator generator,
        Scope scope,
        string queryAlias,
        List<ExpressionSyntax> leftKeys,
        List<ExpressionSyntax> rightKeys,
        List<Type> keyTypes,
        StatementSyntax ifStatement,
        BlockSyntax emptyBlock,
        Func<string, StatementSyntax> getRowsSourceOrEmpty,
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


        var isLeftOuter = node.JoinType == JoinType.OuterLeft;

        string buildAlias;
        string probeAlias;
        List<ExpressionSyntax> buildKeys;
        List<ExpressionSyntax> probeKeys;

        if (isLeftOuter)
        {
            buildAlias = node.SourceTable.Alias;
            probeAlias = node.InMemoryTableAlias;
            buildKeys = rightKeys;
            probeKeys = leftKeys;
        }
        else
        {
            buildAlias = node.InMemoryTableAlias;
            probeAlias = node.SourceTable.Alias;
            buildKeys = leftKeys;
            probeKeys = rightKeys;
        }

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
                                .WithArgumentList(SyntaxFactory.ArgumentList()))))));


        var buildPhaseStatements = new List<StatementSyntax> { generateCancellationExpression() };

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
                                SyntaxFactory.EqualsValueClause(buildKeys[i]))))));

            if (!keyTypes[i].IsValueType || Nullable.GetUnderlyingType(keyTypes[i]) != null)
                buildPhaseStatements.Add(
                    SyntaxFactory.IfStatement(
                        SyntaxFactory.BinaryExpression(
                            SyntaxKind.EqualsExpression,
                            SyntaxFactory.IdentifierName(varName),
                            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                        SyntaxFactory.ContinueStatement()));
        }

        ExpressionSyntax buildKeyExpr = buildKeyVars.Count == 1
            ? SyntaxFactory.IdentifierName(buildKeyVars[0])
            : SyntaxFactory.TupleExpression(
                SyntaxFactory.SeparatedList(
                    buildKeyVars.Select(v => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(v)))));

        buildPhaseStatements.Add(
            SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.IdentifierName("var"),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(
                            SyntaxFactory.Identifier("key"),
                            null,
                            SyntaxFactory.EqualsValueClause(buildKeyExpr))))));

        buildPhaseStatements.Add(
            SyntaxFactory.IfStatement(
                SyntaxFactory.PrefixUnaryExpression(
                    SyntaxKind.LogicalNotExpression,
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(dictionaryName),
                            SyntaxFactory.IdentifierName("ContainsKey")),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("key")))))),
                SyntaxFactory.Block(
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.ElementAccessExpression(
                                SyntaxFactory.IdentifierName(dictionaryName),
                                SyntaxFactory.BracketedArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("key"))))),
                            SyntaxFactory.ObjectCreationExpression(
                                    SyntaxHelper.ListOfIObjectResolverTypeSyntax)
                                .WithArgumentList(SyntaxFactory.ArgumentList()))))));

        buildPhaseStatements.Add(
            SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.ElementAccessExpression(
                            SyntaxFactory.IdentifierName(dictionaryName),
                            SyntaxFactory.BracketedArgumentList(
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName("key"))))),
                        SyntaxFactory.IdentifierName("Add")),
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName($"{buildAlias}Row")))))));


        ForEachStatementSyntax buildLoop;
        StatementSyntax buildTableSetup = SyntaxFactory.EmptyStatement();

        if (isLeftOuter)
        {
            buildTableSetup = getRowsSourceOrEmpty(buildAlias);
            buildLoop = SyntaxFactory.ForEachStatement(
                SyntaxFactory.IdentifierName("var"),
                SyntaxFactory.Identifier($"{buildAlias}Row"),
                SyntaxFactory.IdentifierName($"{buildAlias}Rows.Rows"),
                SyntaxFactory.Block(buildPhaseStatements));
        }
        else
        {
            buildLoop = SyntaxFactory.ForEachStatement(
                SyntaxFactory.IdentifierName("var"),
                SyntaxFactory.Identifier($"{buildAlias}Row"),
                SyntaxFactory.IdentifierName(
                    $"{nameof(EvaluationHelper)}.{nameof(EvaluationHelper.ConvertTableToSource)}({buildAlias}TransitionTable, false).{nameof(RowSource.Rows)}"),
                SyntaxFactory.Block(buildPhaseStatements));
        }


        StatementSyntax outerJoinFallback = SyntaxFactory.Block();
        if (node.JoinType == JoinType.OuterLeft)
        {
            var fullTransitionTable = scope.ScopeSymbolTable.GetSymbol<TableSymbol>(queryAlias);
            var fieldNames = scope.ScopeSymbolTable.GetSymbol<FieldsNamesSymbol>(MetaAttributes.OuterJoinSelect);
            var expressions = BuildLeftJoinExpressions(node, fullTransitionTable, fieldNames, generator);
            var rewriteSelect = CreateSelectVariableDeclaration(expressions);
            var invocation = CreateTableAddInvocation(node, scope);
            outerJoinFallback = SyntaxFactory.Block(
                SyntaxFactory.LocalDeclarationStatement(rewriteSelect),
                SyntaxFactory.ExpressionStatement(invocation));
        }
        else if (node.JoinType == JoinType.OuterRight)
        {
            var fullTransitionTable = scope.ScopeSymbolTable.GetSymbol<TableSymbol>(queryAlias);
            var fieldNames = scope.ScopeSymbolTable.GetSymbol<FieldsNamesSymbol>(MetaAttributes.OuterJoinSelect);
            var expressions = BuildRightJoinExpressions(node, fullTransitionTable, fieldNames, generator);
            var rewriteSelect = CreateSelectVariableDeclaration(expressions);
            var invocation = CreateTableAddInvocationForRightJoin(node, scope);
            outerJoinFallback = SyntaxFactory.Block(
                SyntaxFactory.LocalDeclarationStatement(rewriteSelect),
                SyntaxFactory.ExpressionStatement(invocation));
        }


        var probePhaseStatements = new List<StatementSyntax> { generateCancellationExpression() };

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
                                SyntaxFactory.EqualsValueClause(probeKeys[i]))))));
        }

        var probeKeyExpr = (ExpressionSyntax)(probeKeyVars.Count == 1
            ? SyntaxFactory.IdentifierName(probeKeyVars[0])
            : SyntaxFactory.TupleExpression(
                SyntaxFactory.SeparatedList(
                    probeKeyVars.Select(v => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(v))))));

        probePhaseStatements.Add(
            SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.IdentifierName("var"),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(
                            SyntaxFactory.Identifier("key"),
                            null,
                            SyntaxFactory.EqualsValueClause(probeKeyExpr))))));

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
                                    SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression)))))));

            var matchLoop = SyntaxFactory.ForEachStatement(
                SyntaxFactory.IdentifierName("var"),
                SyntaxFactory.Identifier(matchVarName),
                SyntaxFactory.IdentifierName("matches"),
                SyntaxFactory.Block(
                    generateCancellationExpression(),
                    ifStatement,
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            matchFoundVar,
                            SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression))),
                    emptyBlock));

            var tryGetValue = SyntaxFactory.IfStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(dictionaryName),
                        SyntaxFactory.IdentifierName("TryGetValue")),
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(new[]
                        {
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("key")),
                            SyntaxFactory.Argument(
                                    SyntaxFactory.DeclarationExpression(
                                        SyntaxFactory.IdentifierName("var"),
                                        SyntaxFactory.SingleVariableDesignation(
                                            SyntaxFactory.Identifier("matches"))))
                                .WithRefOrOutKeyword(SyntaxFactory.Token(SyntaxKind.OutKeyword))
                        }))),
                SyntaxFactory.Block(matchLoop));


            var tryGetValueWithNullCheck = SyntaxFactory.IfStatement(
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.NotEqualsExpression,
                    SyntaxFactory.IdentifierName("key"),
                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                SyntaxFactory.Block(tryGetValue));

            probePhaseStatements.Add(tryGetValueWithNullCheck);

            probePhaseStatements.Add(
                SyntaxFactory.IfStatement(
                    SyntaxFactory.PrefixUnaryExpression(
                        SyntaxKind.LogicalNotExpression,
                        matchFoundVar),
                    outerJoinFallback));
        }
        else
        {
            var matchLoop = SyntaxFactory.ForEachStatement(
                SyntaxFactory.IdentifierName("var"),
                SyntaxFactory.Identifier(matchVarName),
                SyntaxFactory.IdentifierName("matches"),
                SyntaxFactory.Block(
                    generateCancellationExpression(),
                    ifStatement,
                    emptyBlock));

            var tryGetValue = SyntaxFactory.IfStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(dictionaryName),
                        SyntaxFactory.IdentifierName("TryGetValue")),
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(new[]
                        {
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("key")),
                            SyntaxFactory.Argument(
                                    SyntaxFactory.DeclarationExpression(
                                        SyntaxFactory.IdentifierName("var"),
                                        SyntaxFactory.SingleVariableDesignation(
                                            SyntaxFactory.Identifier("matches"))))
                                .WithRefOrOutKeyword(SyntaxFactory.Token(SyntaxKind.OutKeyword))
                        }))),
                SyntaxFactory.Block(matchLoop));

            probePhaseStatements.Add(tryGetValue);
        }


        ForEachStatementSyntax probeLoop;
        StatementSyntax probeTableSetup;

        if (isLeftOuter)
        {
            probeTableSetup = SyntaxFactory.EmptyStatement();
            probeLoop = SyntaxFactory.ForEachStatement(
                SyntaxFactory.IdentifierName("var"),
                SyntaxFactory.Identifier($"{probeAlias}Row"),
                SyntaxFactory.IdentifierName(
                    $"{nameof(EvaluationHelper)}.{nameof(EvaluationHelper.ConvertTableToSource)}({probeAlias}TransitionTable, false).{nameof(RowSource.Rows)}"),
                SyntaxFactory.Block(probePhaseStatements));
        }
        else
        {
            probeTableSetup = getRowsSourceOrEmpty(probeAlias);
            probeLoop = SyntaxFactory.ForEachStatement(
                SyntaxFactory.IdentifierName("var"),
                SyntaxFactory.Identifier($"{probeAlias}Row"),
                SyntaxFactory.IdentifierName($"{probeAlias}Rows.Rows"),
                SyntaxFactory.Block(probePhaseStatements));
        }

        computingBlock = computingBlock.AddStatements(
            dictionaryCreation,
            buildTableSetup,
            buildLoop,
            probeTableSetup,
            probeLoop);

        return computingBlock;
    }

    private static bool TryGetHashJoinKeys(
        JoinInMemoryWithSourceTableFromNode node,
        Scope scope,
        Func<Node, ExpressionSyntax> nodeTranslator,
        out List<ExpressionSyntax> leftKeys,
        out List<ExpressionSyntax> rightKeys,
        out List<Type> keyTypes)
    {
        leftKeys = [];
        rightKeys = [];
        keyTypes = [];


        var expressionVisitor = new ExtractAccessColumnFromQueryVisitor();
        var expressionTraverser = new ExtractAccessColumnFromQueryTraverseVisitor(expressionVisitor);
        node.Expression.Accept(expressionTraverser);
        var allColumns = expressionVisitor.GetAll();


        if (allColumns.Length == 0) return false;


        var inMemoryColumns = allColumns.Where(c => c.Alias == node.InMemoryTableAlias).ToArray();
        if (inMemoryColumns.Length == 0) return false;


        // Note: Chained joins with OUTER joins are now supported. For LEFT OUTER joins, we swap


        var expectedAliases = new HashSet<string>(StringComparer.Ordinal)
        {
            node.InMemoryTableAlias,
            node.SourceTable.Alias
        };

        foreach (var column in allColumns)
            if (!expectedAliases.Contains(column.Alias))
                return false;

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
            var leftVisitor = new ExtractAccessColumnFromQueryVisitor();
            var leftTraverser = new ExtractAccessColumnFromQueryTraverseVisitor(leftVisitor);
            binary.Left.Accept(leftTraverser);

            var rightVisitor = new ExtractAccessColumnFromQueryVisitor();
            var rightTraverser = new ExtractAccessColumnFromQueryTraverseVisitor(rightVisitor);
            binary.Right.Accept(rightTraverser);


            var leftAllColumns = leftVisitor.GetForAliases(node.InMemoryTableAlias, node.SourceTable.Alias);
            var rightAllColumns = rightVisitor.GetForAliases(node.InMemoryTableAlias, node.SourceTable.Alias);

            var leftHasInMemory = leftAllColumns.Any(c => c.Alias == node.InMemoryTableAlias);
            var leftHasSource = leftAllColumns.Any(c => c.Alias == node.SourceTable.Alias);
            var rightHasInMemory = rightAllColumns.Any(c => c.Alias == node.InMemoryTableAlias);
            var rightHasSource = rightAllColumns.Any(c => c.Alias == node.SourceTable.Alias);

            var leftIsInMemory = leftHasInMemory && !leftHasSource;
            var leftIsSource = leftHasSource && !leftHasInMemory;
            var leftIsConstant = !leftHasInMemory && !leftHasSource;

            var rightIsInMemory = rightHasInMemory && !rightHasSource;
            var rightIsSource = rightHasSource && !rightHasInMemory;
            var rightIsConstant = !rightHasInMemory && !rightHasSource;

            if (leftIsConstant && rightIsConstant) continue;

            Node inMemoryNode = null;
            Node sourceNode = null;

            if ((leftIsInMemory || leftIsConstant) && (rightIsSource || rightIsConstant))
            {
                inMemoryNode = binary.Left;
                sourceNode = binary.Right;
            }
            else if ((leftIsSource || leftIsConstant) && (rightIsInMemory || rightIsConstant))
            {
                inMemoryNode = binary.Right;
                sourceNode = binary.Left;
            }

            if (inMemoryNode != null && sourceNode != null)
            {
                var type1 = inMemoryNode.ReturnType;
                var type2 = sourceNode.ReturnType;

                var type1Underlying = Nullable.GetUnderlyingType(type1) ?? type1;
                var type2Underlying = Nullable.GetUnderlyingType(type2) ?? type2;

                if (type1Underlying == type2Underlying)
                {
                    var keyType = type1 != type2
                        ? typeof(Nullable<>).MakeGenericType(type1Underlying)
                        : type1;

                    leftKeys.Add(nodeTranslator(inMemoryNode));
                    rightKeys.Add(nodeTranslator(sourceNode));
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

    private static List<ExpressionSyntax> BuildLeftJoinExpressions(
        JoinInMemoryWithSourceTableFromNode node,
        TableSymbol fullTransitionTable,
        FieldsNamesSymbol fieldNames,
        SyntaxGenerator generator)
    {
        var expressions = new List<ExpressionSyntax>();
        var j = 0;


        for (var i = 0; i < fullTransitionTable.CompoundTables.Length - 1; i++)
            foreach (var column in fullTransitionTable.GetColumns(fullTransitionTable.CompoundTables[i]))
            {
                expressions.Add(
                    SyntaxFactory.ElementAccessExpression(
                        SyntaxFactory.IdentifierName($"{node.InMemoryTableAlias}Row"),
                        SyntaxFactory.BracketedArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(
                                    (LiteralExpressionSyntax)generator.LiteralExpression(fieldNames.Names[j]))))));
                j += 1;
            }

        foreach (var column in fullTransitionTable.GetColumns(fullTransitionTable.CompoundTables[^1]))
            expressions.Add(
                SyntaxFactory.CastExpression(
                    SyntaxFactory.IdentifierName(EvaluationHelper.GetCastableType(column.ColumnType)),
                    (LiteralExpressionSyntax)generator.NullLiteralExpression()));

        return expressions;
    }

    private static List<ExpressionSyntax> BuildRightJoinExpressions(
        JoinInMemoryWithSourceTableFromNode node,
        TableSymbol fullTransitionTable,
        FieldsNamesSymbol fieldNames,
        SyntaxGenerator generator)
    {
        var expressions = new List<ExpressionSyntax>();
        var j = 0;


        for (var i = 0; i < fullTransitionTable.CompoundTables.Length - 1; i++)
            foreach (var column in fullTransitionTable.GetColumns(fullTransitionTable.CompoundTables[i]))
            {
                expressions.Add(
                    SyntaxFactory.CastExpression(
                        SyntaxFactory.IdentifierName(EvaluationHelper.GetCastableType(column.ColumnType)),
                        (LiteralExpressionSyntax)generator.NullLiteralExpression()));
                j += 1;
            }

        foreach (var column in fullTransitionTable.GetColumns(fullTransitionTable.CompoundTables[^1]))
            expressions.Add(
                SyntaxFactory.ElementAccessExpression(
                    SyntaxFactory.IdentifierName($"{node.SourceTable.Alias}Row"),
                    SyntaxFactory.BracketedArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(
                                (LiteralExpressionSyntax)generator.LiteralExpression(fieldNames.Names[j++]))))));

        return expressions;
    }

    private static VariableDeclarationSyntax CreateSelectVariableDeclaration(List<ExpressionSyntax> expressions)
    {
        return SyntaxFactory.VariableDeclaration(
            SyntaxFactory.IdentifierName("var"),
            SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.VariableDeclarator(
                    SyntaxFactory.Identifier("select"),
                    null,
                    SyntaxFactory.EqualsValueClause(
                        SyntaxFactory.ArrayCreationExpression(
                            SyntaxFactory.ArrayType(
                                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)),
                                SyntaxFactory.SingletonList(
                                    SyntaxFactory.ArrayRankSpecifier(
                                        SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                            SyntaxFactory.OmittedArraySizeExpression())))),
                            SyntaxFactory.InitializerExpression(
                                SyntaxKind.ArrayInitializerExpression,
                                SyntaxFactory.SeparatedList(expressions)))))));
    }

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
    ///     Result of processing a JoinInMemoryWithSourceTableFromNode.
    /// </summary>
    public readonly struct ProcessResult
    {
        /// <summary>
        ///     The empty block for the join.
        /// </summary>
        public BlockSyntax EmptyBlock { get; init; }

        /// <summary>
        ///     The computing block containing join logic.
        /// </summary>
        public BlockSyntax ComputingBlock { get; init; }
    }
}

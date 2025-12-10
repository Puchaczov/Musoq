using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Visitors.Helpers;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.CodeGeneration;

/// <summary>
/// Consolidated emitter for SQL query clauses (SELECT, WHERE, GROUP BY, HAVING, ORDER BY).
/// Provides a unified interface for query clause code generation.
/// </summary>
public sealed class QueryClauseEmitter(SyntaxGenerator generator)
{
    private readonly SyntaxGenerator _generator = generator ?? throw new ArgumentNullException(nameof(generator));

    #region SELECT Clause

    /// <summary>
    /// Result of processing a SELECT clause.
    /// </summary>
    public readonly struct SelectClauseResult
    {
        public MemberDeclarationSyntax RowClass { get; init; }
        public BlockSyntax SelectBlock { get; init; }
    }

    /// <summary>
    /// Processes a SELECT node and generates the row class and select block.
    /// </summary>
    public SelectClauseResult ProcessSelect(
        SelectNode node,
        Stack<SyntaxNode> nodes,
        Scope scope,
        MethodAccessType accessType,
        string queryAlias,
        ref int rowClassCounter)
    {
        var className = $"{queryAlias}Row{rowClassCounter++}";
        var rowClass = RowClassEmitter.GenerateRowClass(className, node, scope);
        var selectBlock = SelectNodeProcessor.ProcessSelectNode(node, nodes, scope, accessType, className);

        return new SelectClauseResult
        {
            RowClass = rowClass,
            SelectBlock = selectBlock
        };
    }

    #endregion

    #region WHERE Clause

    /// <summary>
    /// Processes a WHERE node and generates the condition check statement.
    /// </summary>
    public SyntaxNode ProcessWhere(
        SyntaxNode conditionExpression,
        bool isParallelizationImpossible,
        bool isResultQuery)
    {
        return WhereEmitter.CreateWhereCondition(
            conditionExpression,
            isParallelizationImpossible,
            isResultQuery,
            _generator);
    }

    #endregion

    #region GROUP BY Clause

    /// <summary>
    /// Result of processing a GROUP BY clause.
    /// </summary>
    public readonly struct GroupByClauseResult
    {
        public VariableDeclarationSyntax GroupKeys { get; init; }
        public VariableDeclarationSyntax GroupValues { get; init; }
        public SyntaxNode GroupHaving { get; init; }
        public StatementSyntax GroupFieldsStatement { get; init; }
        public string RequiredNamespace { get; init; }
    }

    /// <summary>
    /// Processes a GROUP BY node and generates the grouping structures.
    /// </summary>
    public GroupByClauseResult ProcessGroupBy(
        GroupByNode node,
        Stack<SyntaxNode> nodes,
        Scope scope)
    {
        var result = GroupByNodeProcessor.ProcessGroupByNode(node, nodes, scope);

        return new GroupByClauseResult
        {
            GroupKeys = result.GroupKeys,
            GroupValues = result.GroupValues,
            GroupHaving = result.GroupHaving,
            GroupFieldsStatement = result.GroupFieldsStatement,
            RequiredNamespace = typeof(GroupKey).Namespace
        };
    }

    #endregion

    #region HAVING Clause

    /// <summary>
    /// Processes a HAVING node and generates the having condition.
    /// </summary>
    public SyntaxNode ProcessHaving(SyntaxNode conditionExpression)
    {
        return GroupByEmitter.CreateHavingCondition(conditionExpression, _generator);
    }

    #endregion

    #region SKIP/TAKE (Pagination)

    /// <summary>
    /// Result of processing a SKIP clause.
    /// </summary>
    public readonly struct SkipClauseResult
    {
        public StatementSyntax Declaration { get; init; }
        public SyntaxNode IfStatement { get; init; }
    }

    /// <summary>
    /// Processes a SKIP node and generates the skip logic.
    /// </summary>
    public SkipClauseResult ProcessSkip(long skipValue)
    {
        var result = PaginationEmitter.GenerateSkipCode(skipValue, _generator);
        return new SkipClauseResult
        {
            Declaration = result.Declaration,
            IfStatement = result.IfStatement
        };
    }

    /// <summary>
    /// Result of processing a TAKE clause.
    /// </summary>
    public readonly struct TakeClauseResult
    {
        public StatementSyntax Declaration { get; init; }
        public BlockSyntax Block { get; init; }
    }

    /// <summary>
    /// Processes a TAKE node and generates the take logic.
    /// </summary>
    public TakeClauseResult ProcessTake(long takeValue)
    {
        var (declaration, block) = PaginationEmitter.GenerateTakeCode(takeValue, _generator);
        return new TakeClauseResult
        {
            Declaration = declaration,
            Block = block
        };
    }

    #endregion

    #region ORDER BY Clause

    /// <summary>
    /// Gets the required namespace for ORDER BY operations.
    /// </summary>
    public static string GetOrderByNamespace() => "Musoq.Evaluator";

    #endregion
}

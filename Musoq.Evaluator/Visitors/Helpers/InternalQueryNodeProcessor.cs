using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Visitors.CodeGeneration;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers;

/// <summary>
/// Processes InternalQueryNode to generate C# code for internal query execution (joins/applies).
/// </summary>
public static class InternalQueryNodeProcessor
{
    /// <summary>
    /// Result of processing an InternalQueryNode with GroupBy.
    /// </summary>
    public readonly struct GroupByQueryResult
    {
        /// <summary>
        /// The statements to add to the method body.
        /// </summary>
        public IReadOnlyList<StatementSyntax> Statements { get; init; }
    }

    /// <summary>
    /// Processes the GroupBy path of an InternalQueryNode.
    /// </summary>
    public static GroupByQueryResult ProcessGroupByPath(
        InternalQueryNode node,
        Stack<SyntaxNode> nodes,
        BlockSyntax block,
        StatementSyntax[] cseDeclarations,
        StatementSyntax where,
        VariableDeclarationSyntax groupKeys,
        VariableDeclarationSyntax groupValues,
        SyntaxNode groupHaving,
        Func<string, StatementSyntax> getRowsSourceFunc,
        string sourceName,
        Func<int, Func<int, string>, InitializerExpressionSyntax[]> createIndexToColumnMap,
        string? queryId = null)
    {
        var statements = new List<StatementSyntax>();
        
        if (!string.IsNullOrEmpty(queryId))
        {
            statements.Add(QueryEmitter.GeneratePhaseChangeStatement(queryId, QueryPhase.Begin));
        }
        
        if (!string.IsNullOrEmpty(queryId))
        {
            statements.Add(QueryEmitter.GeneratePhaseChangeStatement(queryId, QueryPhase.GroupBy));
        }
        
        statements.AddRange(QueryEmitter.GenerateGroupInitStatements());

        var refreshBlock = node.Refresh.Nodes.Length > 0
            ? (BlockSyntax)nodes.Pop()
            : null;

        block = GroupByEmitter.BuildGroupByExecutionBlock(
            block,
            cseDeclarations,
            QueryEmitter.GenerateCancellationCheck(),
            where,
            groupKeys,
            groupValues,
            refreshBlock,
            node.GroupBy.Having != null ? groupHaving : null,
            node.From.Alias.ToGroupingTable());

        var indexToColumnMapCode = createIndexToColumnMap(
            node.Select.Fields.Length,
            i => node.Select.Fields[i].FieldName);

        statements.Add(QueryEmitter.CreateIndexToValueDictDeclaration(indexToColumnMapCode));

        var foreachBlock = GroupByEmitter.CreateGroupByForeach(block, node.From.Alias.ToRowItem(), sourceName);
        var fullBlock = StatementEmitter.CreateBlock(getRowsSourceFunc(node.From.Alias), foreachBlock);
        statements.AddRange(fullBlock.Statements);
        
        if (!string.IsNullOrEmpty(queryId))
        {
            statements.Add(QueryEmitter.GeneratePhaseChangeStatement(queryId, QueryPhase.End));
        }

        return new GroupByQueryResult { Statements = statements };
    }

    /// <summary>
    /// Processes the Join/Apply path of an InternalQueryNode.
    /// </summary>
    public static IEnumerable<StatementSyntax> ProcessJoinApplyPath(
        BlockSyntax selectBlock,
        BlockSyntax joinOrApplyBlock)
    {
        var emptyBlock = joinOrApplyBlock.DescendantNodes().OfType<BlockSyntax>()
            .First(f => f.Statements.Count == 0);
        var updatedBlock = joinOrApplyBlock.ReplaceNode(emptyBlock, selectBlock.Statements);
        return updatedBlock.Statements;
    }
}

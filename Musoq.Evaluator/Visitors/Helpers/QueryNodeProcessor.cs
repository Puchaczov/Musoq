using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Musoq.Evaluator.Visitors.CodeGeneration;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers;

/// <summary>
/// Processes QueryNode to generate C# code for query execution.
/// </summary>
public static class QueryNodeProcessor
{
    /// <summary>
    /// Result of processing a QueryNode.
    /// </summary>
    public readonly struct QueryNodeResult
    {
        /// <summary>
        /// The statements to add to the method body.
        /// </summary>
        public IReadOnlyList<StatementSyntax> Statements { get; init; }
    }

    /// <summary>
    /// Processes a QueryNode and generates the corresponding C# statements.
    /// </summary>
    /// <param name="node">The QueryNode to process.</param>
    /// <param name="nodes">The syntax node stack.</param>
    /// <param name="selectBlock">The pre-generated select block.</param>
    /// <param name="cseVariableDeclarations">CSE variable declarations.</param>
    /// <param name="getRowsSourceFunc">Function to get rows source statement.</param>
    /// <param name="sourceName">The source name from scope.</param>
    /// <param name="isResultParallelizationPossible">Whether parallelization is possible.</param>
    /// <param name="generator">The syntax generator.</param>
    /// <returns>The result containing statements to add.</returns>
    public static QueryNodeResult ProcessQueryNode(
        QueryNode node,
        Stack<SyntaxNode> nodes,
        BlockSyntax selectBlock,
        StatementSyntax[] cseVariableDeclarations,
        System.Func<string, StatementSyntax> getRowsSourceFunc,
        string sourceName,
        bool isResultParallelizationPossible,
        SyntaxGenerator generator)
    {
        var detailedQuery = (DetailedQueryNode)node;

        var orderByFields = detailedQuery.OrderBy is not null
            ? new (FieldOrderedNode Field, ExpressionSyntax Syntax)[detailedQuery.OrderBy.Fields.Length]
            : [];

        for (var i = orderByFields.Length - 1; i >= 0; i--)
        {
            var orderBy = detailedQuery.OrderBy!;
            var field = orderBy.Fields[i];
            var syntax = (ExpressionSyntax)nodes.Pop();
            orderByFields[i] = (field, syntax);
        }

        var skip = node.Skip != null ? nodes.Pop() as StatementSyntax : null;
        var take = node.Take != null ? nodes.Pop() as BlockSyntax : null;
        var where = node.Where != null ? nodes.Pop() as StatementSyntax : null;
        var block = (BlockSyntax)nodes.Pop();

        var executionBlock = QueryEmitter.BuildQueryExecutionBlock(
            block,
            cseVariableDeclarations,
            where,
            skip,
            take,
            selectBlock);

        var fullBlockStatements = QueryEmitter.CreateFullQueryBlock(
            getRowsSourceFunc(node.From.Alias),
            sourceName,
            executionBlock,
            orderByFields,
            detailedQuery.ReturnVariableName,
            isResultParallelizationPossible,
            generator);

        return new QueryNodeResult
        {
            Statements = fullBlockStatements.ToList()
        };
    }
}

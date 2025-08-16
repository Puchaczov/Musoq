using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers;

/// <summary>
/// Processor for QueryNode that handles order by, skip, take, where clause handling and block generation.
/// </summary>
public static class QueryNodeProcessor
{
    /// <summary>
    /// Result of processing a QueryNode.
    /// </summary>
    public sealed class ProcessResult
    {
        /// <summary>
        /// The generated statements for the query block.
        /// </summary>
        public required StatementSyntax[] Statements { get; init; }
    }

    /// <summary>
    /// Processes a QueryNode to generate query block syntax with order by, skip, take, and where clause handling.
    /// </summary>
    /// <param name="node">The QueryNode to process.</param>
    /// <param name="nodes">Stack of syntax nodes for popping expressions.</param>
    /// <param name="selectBlock">The select block syntax to include in the query.</param>
    /// <param name="scope">The scope containing metadata and variable information.</param>
    /// <param name="generator">The syntax generator for creating statements.</param>
    /// <param name="isResultParallelizationImpossible">Whether result parallelization is impossible.</param>
    /// <param name="getRowsSourceStatement">Dictionary for managing rows source statements.</param>
    /// <param name="generateCancellationExpression">Function to generate cancellation expression.</param>
    /// <param name="generateStatsUpdateStatements">Function to generate stats update statements.</param>
    /// <param name="getRowsSourceOrEmpty">Function to get rows source or empty statement.</param>
    /// <returns>ProcessResult containing the generated statements.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
    public static ProcessResult ProcessQueryNode(
        QueryNode node, 
        Stack<SyntaxNode> nodes, 
        BlockSyntax selectBlock,
        Scope scope,
        SyntaxGenerator generator,
        bool isResultParallelizationImpossible,
        Dictionary<string, LocalDeclarationStatementSyntax> getRowsSourceStatement,
        Func<StatementSyntax> generateCancellationExpression,
        Func<StatementSyntax> generateStatsUpdateStatements,
        Func<string, StatementSyntax> getRowsSourceOrEmpty)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));
        
        if (nodes == null)
            throw new ArgumentNullException(nameof(nodes));
        
        if (selectBlock == null)
            throw new ArgumentNullException(nameof(selectBlock));
        
        if (scope == null)
            throw new ArgumentNullException(nameof(scope));
        
        if (generator == null)
            throw new ArgumentNullException(nameof(generator));
        
        if (getRowsSourceStatement == null)
            throw new ArgumentNullException(nameof(getRowsSourceStatement));
        
        if (generateCancellationExpression == null)
            throw new ArgumentNullException(nameof(generateCancellationExpression));
        
        if (generateStatsUpdateStatements == null)
            throw new ArgumentNullException(nameof(generateStatsUpdateStatements));
        
        if (getRowsSourceOrEmpty == null)
            throw new ArgumentNullException(nameof(getRowsSourceOrEmpty));

        var detailedQuery = (DetailedQueryNode) node;

        // Handle order by fields
        var orderByFields = ProcessOrderByFields(detailedQuery, nodes);

        // Handle skip, take, where clauses
        var skip = node.Skip != null ? nodes.Pop() as StatementSyntax : null;
        var take = node.Take != null ? nodes.Pop() as BlockSyntax : null;
        var where = node.Where != null ? nodes.Pop() as StatementSyntax : null;

        // Build the query block
        var block = (BlockSyntax) nodes.Pop();
        block = BuildQueryBlock(
            block, 
            selectBlock, 
            where, 
            skip, 
            take, 
            generateCancellationExpression, 
            generateStatsUpdateStatements);

        // Create the full block with foreach and return statements
        var fullBlock = CreateFullQueryBlock(
            detailedQuery, 
            block, 
            orderByFields, 
            scope, 
            generator, 
            isResultParallelizationImpossible,
            getRowsSourceOrEmpty);

        return new ProcessResult
        {
            Statements = fullBlock.Statements.ToArray()
        };
    }

    /// <summary>
    /// Processes order by fields from the detailed query.
    /// </summary>
    private static (FieldOrderedNode Field, ExpressionSyntax Syntax)[] ProcessOrderByFields(
        DetailedQueryNode detailedQuery, 
        Stack<SyntaxNode> nodes)
    {
        var orderByFields = detailedQuery.OrderBy is not null
            ? new (FieldOrderedNode Field, ExpressionSyntax Syntax)[detailedQuery.OrderBy.Fields.Length]
            : [];

        for (var i = orderByFields.Length - 1; i >= 0; i--)
        {
            var orderBy = detailedQuery.OrderBy!;
            var field = orderBy.Fields[i];
            var syntax = (ExpressionSyntax) nodes.Pop();
            orderByFields[i] = (field, syntax);
        }

        return orderByFields;
    }

    /// <summary>
    /// Builds the query block with cancellation, where, stats, skip, take, and select statements.
    /// </summary>
    private static BlockSyntax BuildQueryBlock(
        BlockSyntax block, 
        BlockSyntax selectBlock, 
        StatementSyntax where, 
        StatementSyntax skip, 
        BlockSyntax take,
        Func<StatementSyntax> generateCancellationExpression,
        Func<StatementSyntax> generateStatsUpdateStatements)
    {
        block = block.AddStatements(generateCancellationExpression());

        if (where != null)
            block = block.AddStatements(where);

        block = block.AddStatements(generateStatsUpdateStatements());

        if (skip != null)
            block = block.AddStatements(skip);

        if (take != null)
            block = block.AddStatements(take.Statements.ToArray());
        
        block = block.AddStatements(selectBlock.Statements.ToArray());

        return block;
    }

    /// <summary>
    /// Creates the full query block with foreach and return statements.
    /// </summary>
    private static BlockSyntax CreateFullQueryBlock(
        DetailedQueryNode detailedQuery,
        BlockSyntax block,
        (FieldOrderedNode Field, ExpressionSyntax Syntax)[] orderByFields,
        Scope scope,
        SyntaxGenerator generator,
        bool isResultParallelizationImpossible,
        Func<string, StatementSyntax> getRowsSourceOrEmpty)
    {
        var fullBlock = SyntaxFactory.Block();

        fullBlock = fullBlock.AddStatements(
            getRowsSourceOrEmpty(detailedQuery.From.Alias),
            isResultParallelizationImpossible
                ? SyntaxHelper.Foreach("score", scope[MetaAttributes.SourceName], block, orderByFields)
                : SyntaxHelper.ParallelForeach("score", scope[MetaAttributes.SourceName], block));

        fullBlock = fullBlock.AddStatements(
            (StatementSyntax) generator.ReturnStatement(
                SyntaxFactory.IdentifierName(detailedQuery.ReturnVariableName)));

        return fullBlock;
    }
}
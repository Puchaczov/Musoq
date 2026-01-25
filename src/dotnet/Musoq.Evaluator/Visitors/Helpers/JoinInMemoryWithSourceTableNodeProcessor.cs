using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Visitors.CodeGeneration;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors.Helpers;

/// <summary>
///     Processor for JoinInMemoryWithSourceTableFromNode that handles join type dispatch.
/// </summary>
public static class JoinInMemoryWithSourceTableNodeProcessor
{
    /// <summary>
    ///     Processes a JoinInMemoryWithSourceTableFromNode.
    /// </summary>
    /// <param name="node">The join node.</param>
    /// <param name="conditionExpression">The condition expression from the stack.</param>
    /// <param name="generator">The Roslyn syntax generator.</param>
    /// <param name="scope">The current scope.</param>
    /// <param name="queryAlias">The query alias.</param>
    /// <param name="getRowsSourceOrEmpty">Function to get rows source.</param>
    /// <param name="generateCancellationExpression">Function to generate cancellation check.</param>
    /// <returns>The result containing empty and computing blocks.</returns>
    public static ProcessResult Process(
        JoinInMemoryWithSourceTableFromNode node,
        SyntaxNode conditionExpression,
        SyntaxGenerator generator,
        Scope scope,
        string queryAlias,
        Func<string, StatementSyntax> getRowsSourceOrEmpty,
        Func<StatementSyntax> generateCancellationExpression)
    {
        var ifStatement = JoinEmitter.CreateJoinConditionCheck(conditionExpression, generator);
        var emptyBlock = StatementEmitter.CreateEmptyBlock();

        var computingBlock = node.JoinType switch
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
            ComputingBlock = computingBlock
        };
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

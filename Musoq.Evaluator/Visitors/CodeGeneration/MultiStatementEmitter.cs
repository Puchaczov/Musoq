#nullable enable

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Visitors.Helpers;

namespace Musoq.Evaluator.Visitors.CodeGeneration;

/// <summary>
///     Handles code generation for multi-statement and refresh nodes.
/// </summary>
public static class MultiStatementEmitter
{
    /// <summary>
    ///     Creates the stats declaration statement.
    /// </summary>
    public static LocalDeclarationStatementSyntax CreateStatsDeclaration()
    {
        return SyntaxFactory.LocalDeclarationStatement(
            SyntaxHelper.CreateAssignment(
                "stats",
                SyntaxHelper.CreateObjectOf(
                    nameof(AmendableQueryStats),
                    SyntaxFactory.ArgumentList())));
    }

    /// <summary>
    ///     Generates the method name for multi-statement processing.
    /// </summary>
    /// <param name="scope">The current scope.</param>
    /// <param name="methodIdentifier">The set operator method identifier.</param>
    /// <returns>The generated method name.</returns>
    public static string GenerateMethodName(Scope scope, int methodIdentifier)
    {
        var methodName = $"{scope[MetaAttributes.MethodName]}_{methodIdentifier}";
        if (scope.IsInsideNamedScope("CTE Inner Expression"))
            methodName = $"{methodName}_Inner_Cte";
        return methodName;
    }

    /// <summary>
    ///     Creates the method declaration for multi-statement processing.
    /// </summary>
    /// <param name="methodName">The method name.</param>
    /// <param name="statements">The statements to include in the method body.</param>
    /// <returns>The method declaration.</returns>
    public static MemberDeclarationSyntax CreateMethod(string methodName, List<StatementSyntax> statements)
    {
        return MethodDeclarationHelper.CreateStandardPrivateMethod(
            methodName,
            StatementEmitter.CreateBlock(statements));
    }

    /// <summary>
    ///     Processes RefreshNode to create a block of expression statements.
    /// </summary>
    /// <param name="nodeCount">The number of nodes to process.</param>
    /// <param name="nodes">The stack of syntax nodes.</param>
    /// <returns>A block containing expression statements, or null if empty.</returns>
    public static BlockSyntax? ProcessRefreshNode(int nodeCount, Stack<SyntaxNode> nodes)
    {
        if (nodeCount == 0)
            return null;

        var block = StatementEmitter.CreateEmptyBlock();
        for (var i = 0; i < nodeCount; i++)
            block = block.AddStatements(
                SyntaxFactory.ExpressionStatement((ExpressionSyntax)nodes.Pop()));

        return block;
    }

    /// <summary>
    ///     Creates a PutTrueNode expression (1 == 1).
    /// </summary>
    /// <param name="generator">The Roslyn syntax generator.</param>
    /// <returns>An expression representing true (1 == 1).</returns>
    public static SyntaxNode CreatePutTrueExpression(SyntaxGenerator generator)
    {
        return generator.ValueEqualsExpression(
            generator.LiteralExpression(1),
            generator.LiteralExpression(1));
    }

    /// <summary>
    ///     Result of processing a MultiStatementNode.
    /// </summary>
    public readonly struct MultiStatementResult
    {
        /// <summary>
        ///     The statement to insert at the beginning.
        /// </summary>
        public LocalDeclarationStatementSyntax StatsDeclaration { get; init; }

        /// <summary>
        ///     The generated method name.
        /// </summary>
        public string MethodName { get; init; }

        /// <summary>
        ///     The generated method declaration.
        /// </summary>
        public MemberDeclarationSyntax Method { get; init; }
    }
}

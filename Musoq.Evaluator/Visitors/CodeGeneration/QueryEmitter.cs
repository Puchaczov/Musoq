#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Musoq.Evaluator.Helpers;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.CodeGeneration;

/// <summary>
/// Emitter for query-related code generation.
/// Handles SELECT, WHERE, GROUP BY, ORDER BY, SKIP, TAKE statements.
/// </summary>
public class QueryEmitter(SyntaxGenerator generator)
{
    /// <summary>
    /// Generates a phase change invocation statement: OnPhaseChanged(queryId, QueryPhase.{phase}).
    /// </summary>
    /// <param name="queryId">The unique query identifier.</param>
    /// <param name="phase">The query phase.</param>
    /// <returns>An expression statement that invokes OnPhaseChanged.</returns>
    public static StatementSyntax GeneratePhaseChangeStatement(string queryId, QueryPhase phase)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.IdentifierName("OnPhaseChanged"))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Argument(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                SyntaxFactory.Literal(queryId))),
                        SyntaxFactory.Argument(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName(nameof(QueryPhase)),
                                SyntaxFactory.IdentifierName(phase.ToString())))
                    }))));
    }
    
    /// <summary>
    /// Generates a cancellation token check expression.
    /// </summary>
    /// <returns>An expression statement that throws if cancellation is requested</returns>
    public static StatementSyntax GenerateCancellationCheck()
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("token"),
                    SyntaxFactory.IdentifierName(nameof(CancellationToken.ThrowIfCancellationRequested)))));
    }
    
    /// <summary>
    /// Generates group initialization statements for GROUP BY queries.
    /// </summary>
    /// <returns>The initialization statements for grouping variables</returns>
    public static IEnumerable<StatementSyntax> GenerateGroupInitStatements()
    {
        yield return SyntaxFactory
            .ParseStatement("var rootGroup = new Group(null, new string[0], new string[0]);")
            .WithTrailingTrivia(SyntaxTriviaList.Create(SyntaxFactory.CarriageReturn))
            .NormalizeWhitespace();
            
        yield return SyntaxFactory
            .ParseStatement("var usedGroups = new HashSet<Group>();")
            .WithTrailingTrivia(SyntaxTriviaList.Create(SyntaxFactory.CarriageReturn))
            .NormalizeWhitespace();
            
        yield return SyntaxFactory
            .ParseStatement("var groups = new Dictionary<GroupKey, Group>();")
            .WithTrailingTrivia(SyntaxTriviaList.Create(SyntaxFactory.CarriageReturn))
            .NormalizeWhitespace();
    }
    
    /// <summary>
    /// Generates parent group variable declaration.
    /// </summary>
    public static StatementSyntax GenerateParentGroupDeclaration()
    {
        return SyntaxFactory
            .ParseStatement("var parent = rootGroup;")
            .WithTrailingTrivia(SyntaxTriviaList.Create(SyntaxFactory.CarriageReturn))
            .NormalizeWhitespace();
    }
    
    /// <summary>
    /// Generates group variable declaration.
    /// </summary>
    public static StatementSyntax GenerateGroupDeclaration()
    {
        return SyntaxFactory
            .ParseStatement("Group group = null;")
            .WithTrailingTrivia(SyntaxTriviaList.Create(SyntaxFactory.CarriageReturn))
            .NormalizeWhitespace();
    }
    
    /// <summary>
    /// Creates an index-to-column mapping initializer for SELECT fields.
    /// </summary>
    /// <param name="fieldCount">Number of fields in the SELECT clause</param>
    /// <param name="fieldNameGetter">Function to get field name by index</param>
    /// <returns>Array of initializer expressions for the mapping</returns>
    public InitializerExpressionSyntax[] CreateIndexToColumnMap(
        int fieldCount, 
        Func<int, string> fieldNameGetter)
    {
        var result = new InitializerExpressionSyntax[fieldCount];
        
        for (int i = 0, j = fieldCount - 1; i < fieldCount; i++, --j)
        {
            result[i] = SyntaxFactory.InitializerExpression(
                SyntaxKind.ComplexElementInitializerExpression,
                SyntaxFactory.SeparatedList<ExpressionSyntax>()
                    .Add((LiteralExpressionSyntax)generator.LiteralExpression(j))
                    .Add((LiteralExpressionSyntax)generator.LiteralExpression(
                        fieldNameGetter(i).Replace("\"", "'"))));
        }
        
        return result;
    }
    
    /// <summary>
    /// Creates the index-to-value dictionary variable declaration.
    /// </summary>
    /// <param name="initializerExpressions">The index-to-column mapping initializers</param>
    /// <param name="variableName">The name of the dictionary variable</param>
    /// <returns>A local declaration statement for the dictionary</returns>
    public static LocalDeclarationStatementSyntax CreateIndexToValueDictDeclaration(
        InitializerExpressionSyntax[] initializerExpressions,
        string variableName = "indexToValueDict")
    {
        var columnToValueDict = SyntaxHelper.CreateAssignment(
            variableName, 
            SyntaxHelper.CreateObjectOf(
                "Dictionary<int, string>",
                SyntaxFactory.ArgumentList(),
                SyntaxFactory.InitializerExpression(
                    SyntaxKind.ObjectInitializerExpression,
                    SyntaxFactory.SeparatedList<ExpressionSyntax>()
                        .AddRange(initializerExpressions))));

        return SyntaxFactory.LocalDeclarationStatement(columnToValueDict);
    }
    
    /// <summary>
    /// Creates a return statement for the query result.
    /// </summary>
    /// <param name="returnVariableName">The name of the variable to return</param>
    /// <returns>A return statement syntax</returns>
    public StatementSyntax CreateReturnStatement(string returnVariableName)
    {
        return (StatementSyntax)generator.ReturnStatement(
            SyntaxFactory.IdentifierName(returnVariableName));
    }
    
    /// <summary>
    /// Generates a foreach statement for iterating through query results.
    /// </summary>
    /// <param name="variableName">The loop variable name</param>
    /// <param name="sourceName">The source collection name</param>
    /// <param name="body">The loop body</param>
    /// <param name="useParallel">Whether to use parallel iteration</param>
    /// <param name="orderByFields">Order by fields for non-parallel iteration</param>
    /// <returns>A foreach or parallel foreach statement</returns>
    public static StatementSyntax CreateIterationStatement(
        string variableName,
        string sourceName,
        BlockSyntax body,
        bool useParallel,
        (FieldOrderedNode Field, ExpressionSyntax Syntax)[]? orderByFields = null)
    {
        if (useParallel)
        {
            return SyntaxHelper.ParallelForeach(variableName, sourceName, body);
        }
        
        return SyntaxHelper.Foreach(variableName, sourceName, body, orderByFields ?? []);
    }
    
    /// <summary>
    /// Generates a stats update statement that increments the row number.
    /// </summary>
    /// <returns>A local declaration statement for currentRowStats</returns>
    public static StatementSyntax GenerateStatsUpdateStatement()
    {
        return SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("currentRowStats"))
                            .WithInitializer(
                                SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.InvocationExpression(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.IdentifierName("stats"),
                                            SyntaxFactory.IdentifierName("IncrementRowNumber"))))))));
    }

    /// <summary>
    /// Builds the query execution block for QueryNode by assembling all parts.
    /// </summary>
    /// <param name="block">The initial block from the stack.</param>
    /// <param name="cseDeclarations">CSE variable declarations.</param>
    /// <param name="where">Optional WHERE clause statement.</param>
    /// <param name="skip">Optional SKIP clause statement.</param>
    /// <param name="take">Optional TAKE clause block.</param>
    /// <param name="select">The SELECT block.</param>
    /// <param name="queryId">The unique query identifier for phase tracking.</param>
    /// <returns>The assembled query execution block.</returns>
    public static BlockSyntax BuildQueryExecutionBlock(
        BlockSyntax block,
        StatementSyntax[] cseDeclarations,
        StatementSyntax? where,
        StatementSyntax? skip,
        BlockSyntax? take,
        BlockSyntax select,
        string? queryId = null)
    {
        if (cseDeclarations.Length > 0)
        {
            block = StatementEmitter.CreateBlock(cseDeclarations.Concat(block.Statements));
        }

        block = block.AddStatements(GenerateCancellationCheck());

        if (where != null)
        {
            if (!string.IsNullOrEmpty(queryId))
            {
                block = block.AddStatements(GeneratePhaseChangeStatement(queryId, QueryPhase.Where));
            }
            block = block.AddStatements(where);
        }

        block = block.AddStatements(GenerateStatsUpdateStatement());

        if (skip != null)
            block = block.AddStatements(skip);

        if (take != null)
            block = block.AddStatements(take.Statements.ToArray());

        if (!string.IsNullOrEmpty(queryId))
        {
            block = block.AddStatements(GeneratePhaseChangeStatement(queryId, QueryPhase.Select));
        }
        
        block = block.AddStatements(select.Statements.ToArray());
        
        return block;
    }

    /// <summary>
    /// Creates the full query block with iteration and return statement.
    /// </summary>
    /// <param name="rowsSource">The rows source statement.</param>
    /// <param name="sourceName">The source collection name.</param>
    /// <param name="executionBlock">The query execution block.</param>
    /// <param name="orderByFields">Order by fields for sorting.</param>
    /// <param name="returnVariableName">The variable name to return.</param>
    /// <param name="useParallel">Whether to use parallel iteration.</param>
    /// <param name="generator">The syntax generator.</param>
    /// <param name="queryId">The unique query identifier for phase tracking.</param>
    /// <returns>The full query block statements.</returns>
    public static IEnumerable<StatementSyntax> CreateFullQueryBlock(
        StatementSyntax rowsSource,
        string sourceName,
        BlockSyntax executionBlock,
        (FieldOrderedNode Field, ExpressionSyntax Syntax)[] orderByFields,
        string returnVariableName,
        bool useParallel,
        SyntaxGenerator generator,
        string? queryId = null)
    {
        var fullBlock = StatementEmitter.CreateEmptyBlock();

        // Add Begin phase tracking if queryId is provided
        if (!string.IsNullOrEmpty(queryId))
        {
            fullBlock = fullBlock.AddStatements(GeneratePhaseChangeStatement(queryId, QueryPhase.Begin));
        }

        // Add From phase tracking if queryId is provided (before row source initialization)
        if (!string.IsNullOrEmpty(queryId))
        {
            fullBlock = fullBlock.AddStatements(GeneratePhaseChangeStatement(queryId, QueryPhase.From));
        }

        var iterationStatement = useParallel
            ? SyntaxHelper.ParallelForeach("score", sourceName, executionBlock)
            : SyntaxHelper.Foreach("score", sourceName, executionBlock, orderByFields);

        fullBlock = fullBlock.AddStatements(rowsSource, iterationStatement);

        // Add End phase tracking if queryId is provided
        if (!string.IsNullOrEmpty(queryId))
        {
            fullBlock = fullBlock.AddStatements(GeneratePhaseChangeStatement(queryId, QueryPhase.End));
        }

        fullBlock = fullBlock.AddStatements(
            (StatementSyntax)generator.ReturnStatement(
                SyntaxFactory.IdentifierName(returnVariableName)));

        return fullBlock.Statements;
    }
}

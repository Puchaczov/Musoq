#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors.Helpers;

/// <summary>
///     Provides specialized processing for CTE (Common Table Expression) nodes in the query syntax tree.
///     Handles the complex method declaration generation for CTE result queries.
/// </summary>
public static class CteExpressionNodeProcessor
{
    /// <summary>
    ///     Processes a CTE expression node and generates the corresponding method declaration with proper parameter lists.
    /// </summary>
    /// <param name="node">The CTE expression node to process</param>
    /// <param name="methodNames">Stack of method names for processing</param>
    /// <param name="nodes">Stack of syntax nodes for processing</param>
    /// <returns>A tuple containing the method declaration and the method name</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
    public static (MethodDeclarationSyntax Method, string MethodName) ProcessCteExpressionNode(
        CteExpressionNode node,
        Stack<string> methodNames,
        Stack<SyntaxNode> nodes)
    {
        return ProcessCteExpressionNode(node, methodNames, nodes, null!, null!);
    }

    public static (MethodDeclarationSyntax Method, string MethodName) ProcessCteExpressionNode(
        CteExpressionNode node,
        Stack<string> methodNames,
        Stack<SyntaxNode> nodes,
        CompilationOptions compilationOptions)
    {
        return ProcessCteExpressionNode(node, methodNames, nodes, compilationOptions, null!);
    }

    /// <summary>
    ///     Processes a CTE expression node and generates the corresponding method declaration with proper parameter lists.
    ///     Supports parallel CTE execution when CompilationOptions.UseCteParallelization is true.
    /// </summary>
    /// <param name="node">The CTE expression node to process</param>
    /// <param name="methodNames">Stack of method names for processing</param>
    /// <param name="nodes">Stack of syntax nodes for processing</param>
    /// <param name="compilationOptions">Optional compilation options to control parallelization</param>
    /// <param name="preComputedPlan">Optional pre-computed execution plan (computed before AST rewriting)</param>
    /// <returns>A tuple containing the method declaration and the method name</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
    public static (MethodDeclarationSyntax Method, string MethodName) ProcessCteExpressionNode(
        CteExpressionNode node,
        Stack<string> methodNames,
        Stack<SyntaxNode> nodes,
        CompilationOptions compilationOptions,
        CteExecutionPlan preComputedPlan)
    {
        ValidateParameters(node, methodNames, nodes);

        var resultCteMethodName = methodNames.Pop();


        var cteNameStatementPairs = new List<(string Name, StatementSyntax Statement)>();
        var innerExpressions = node.InnerExpression.Reverse().ToArray();
        foreach (var inner in innerExpressions)
        {
            methodNames.Pop();
            var statement = (StatementSyntax)nodes.Pop();
            cteNameStatementPairs.Add((inner.Name, statement));
        }


        cteNameStatementPairs.Reverse();


        var cteStatements = cteNameStatementPairs.Select(p => p.Statement).ToList();
        var cteNames = cteNameStatementPairs.Select(p => p.Name).ToList();


        var useParallelization = false;
        if (compilationOptions?.UseCteParallelization == true)
        {
            if (preComputedPlan != null)
                useParallelization = preComputedPlan.CanParallelize;
            else
                try
                {
                    useParallelization = CteParallelizationAnalyzer.CanBenefitFromParallelization(node);
                }
                catch (NotSupportedException)
                {
                    useParallelization = false;
                }
        }

        List<StatementSyntax> statements;
        if (useParallelization)
            statements = GenerateParallelCteStatements(cteStatements, cteNames, preComputedPlan!);
        else
            statements = cteStatements;

        const string methodName = "CteResultQuery";

        statements.Add(CreateReturnStatement(resultCteMethodName));

        var method = CreateCteMethodDeclaration(methodName, statements);

        return (method, methodName);
    }

    private static List<StatementSyntax> GenerateParallelCteStatements(
        List<StatementSyntax> cteStatements,
        List<string> cteNames,
        CteExecutionPlan preComputedPlan)
    {
        var cteToStatement = new Dictionary<string, StatementSyntax>();
        for (var i = 0; i < cteNames.Count; i++) cteToStatement[cteNames[i]] = cteStatements[i];


        if (preComputedPlan == null || preComputedPlan.IsEmpty || !preComputedPlan.CanParallelize) return cteStatements;

        var result = new List<StatementSyntax>();


        foreach (var level in preComputedPlan.Levels)
            if (level.Count == 1)
            {
                var cteName = level.Ctes[0].Name;
                if (cteToStatement.TryGetValue(cteName, out var statement)) result.Add(statement);
            }
            else
            {
                var parallelStatement = CreateParallelForEachStatement(level, cteToStatement);
                result.Add(parallelStatement);
            }

        return result;
    }

    private static StatementSyntax CreateParallelForEachStatement(
        CteExecutionLevel level,
        Dictionary<string, StatementSyntax> cteToStatement)
    {
        var arrayElements = level.Ctes
            .Select(cte => (ExpressionSyntax)SyntaxFactory.LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal(cte.Name)))
            .ToArray();

        var arrayCreation = SyntaxFactory.ImplicitArrayCreationExpression(
            SyntaxFactory.InitializerExpression(
                SyntaxKind.ArrayInitializerExpression,
                SyntaxFactory.SeparatedList(arrayElements)));


        var switchSections = new List<SwitchSectionSyntax>();
        foreach (var cte in level.Ctes)
            if (cteToStatement.TryGetValue(cte.Name, out var statement))
            {
                var caseLabel = SyntaxFactory.CaseSwitchLabel(
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal(cte.Name)));

                var section = SyntaxFactory.SwitchSection(
                    SyntaxFactory.SingletonList<SwitchLabelSyntax>(caseLabel),
                    SyntaxFactory.List(new[]
                    {
                        statement,
                        SyntaxFactory.BreakStatement()
                    }));

                switchSections.Add(section);
            }


        var switchStatement = SyntaxFactory.SwitchStatement(
            SyntaxFactory.IdentifierName("cteName"),
            SyntaxFactory.List(switchSections));


        var lambda = SyntaxFactory.ParenthesizedLambdaExpression(
            SyntaxFactory.ParameterList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier("cteName")))),
            SyntaxFactory.Block(switchStatement));


        var parallelForEach = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("Parallel"),
                    SyntaxFactory.IdentifierName("ForEach")))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Argument(arrayCreation),
                        SyntaxFactory.Argument(lambda)
                    })));

        return SyntaxFactory.ExpressionStatement(parallelForEach);
    }

    private static ReturnStatementSyntax CreateReturnStatement(string resultCteMethodName)
    {
        return SyntaxFactory.ReturnStatement(
            SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(resultCteMethodName))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList([
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("provider")),
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("positionalEnvironmentVariables")),
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("queriesInformation")),
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("logger")),
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("token"))
                        ]))));
    }

    private static MethodDeclarationSyntax CreateCteMethodDeclaration(string methodName,
        List<StatementSyntax> statements)
    {
        return SyntaxFactory.MethodDeclaration(
            [],
            SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword).WithTrailingTrivia(SyntaxHelper.WhiteSpace)),
            SyntaxFactory.IdentifierName(nameof(Table)).WithTrailingTrivia(SyntaxHelper.WhiteSpace),
            null,
            SyntaxFactory.Identifier(methodName),
            null,
            CreateCteParameterList(),
            [],
            SyntaxFactory.Block(statements),
            null);
    }

    private static ParameterListSyntax CreateCteParameterList()
    {
        return SyntaxFactory.ParameterList(
            SyntaxFactory.SeparatedList([
                CreateProviderParameter(),
                CreatePositionalEnvironmentVariablesParameter(),
                CreateQueriesInformationParameter(),
                CreateLoggerParameter(),
                CreateTokenParameter()
            ]));
    }

    private static ParameterSyntax CreateProviderParameter()
    {
        return SyntaxFactory.Parameter(
            [],
            SyntaxTokenList.Create(new SyntaxToken()),
            SyntaxFactory.IdentifierName(nameof(ISchemaProvider))
                .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
            SyntaxFactory.Identifier("provider"),
            null);
    }

    private static ParameterSyntax CreatePositionalEnvironmentVariablesParameter()
    {
        return SyntaxFactory.Parameter(
            [],
            SyntaxTokenList.Create(new SyntaxToken()),
            SyntaxFactory.GenericName(SyntaxFactory.Identifier("IReadOnlyDictionary"))
                .WithTypeArgumentList(
                    SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SeparatedList<TypeSyntax>(
                            new SyntaxNodeOrToken[]
                            {
                                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.UIntKeyword)),
                                SyntaxFactory.Token(SyntaxKind.CommaToken),
                                CreateStringDictionaryType()
                            })))
                .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
            SyntaxFactory.Identifier("positionalEnvironmentVariables"),
            null);
    }

    private static TypeSyntax CreateStringDictionaryType()
    {
        return SyntaxFactory.GenericName(SyntaxFactory.Identifier("IReadOnlyDictionary"))
            .WithTypeArgumentList(
                SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SeparatedList<TypeSyntax>(
                        new SyntaxNodeOrToken[]
                        {
                            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)),
                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword))
                        })));
    }

    private static ParameterSyntax CreateQueriesInformationParameter()
    {
        return SyntaxFactory.Parameter(SyntaxFactory.Identifier("queriesInformation"))
            .WithType(CreateQueriesInformationType());
    }

    private static TypeSyntax CreateQueriesInformationType()
    {
        return SyntaxFactory.GenericName(SyntaxFactory.Identifier("IReadOnlyDictionary"))
            .WithTypeArgumentList(
                SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SeparatedList<TypeSyntax>(
                        new SyntaxNodeOrToken[]
                        {
                            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)),
                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                            SyntaxFactory.IdentifierName("QuerySourceInfo")
                        })));
    }

    private static ParameterSyntax CreateLoggerParameter()
    {
        return SyntaxFactory.Parameter(
            [],
            SyntaxTokenList.Create(new SyntaxToken()),
            SyntaxFactory.IdentifierName(nameof(ILogger))
                .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
            SyntaxFactory.Identifier("logger"),
            null);
    }

    private static ParameterSyntax CreateTokenParameter()
    {
        return SyntaxFactory.Parameter(
            [],
            SyntaxTokenList.Create(new SyntaxToken()),
            SyntaxFactory.IdentifierName(nameof(CancellationToken))
                .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
            SyntaxFactory.Identifier("token"),
            null);
    }

    private static void ValidateParameters(CteExpressionNode node, Stack<string> methodNames, Stack<SyntaxNode> nodes)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));
        if (methodNames == null)
            throw new ArgumentNullException(nameof(methodNames));
        if (nodes == null)
            throw new ArgumentNullException(nameof(nodes));
    }
}

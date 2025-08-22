using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Utils;
using Musoq.Parser.Nodes;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Visitors.Helpers;

/// <summary>
/// Provides specialized processing for CTE (Common Table Expression) nodes in the query syntax tree.
/// Handles the complex method declaration generation for CTE result queries.
/// </summary>
public static class CteExpressionNodeProcessor
{
    /// <summary>
    /// Processes a CTE expression node and generates the corresponding method declaration with proper parameter lists.
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
        ValidateParameters(node, methodNames, nodes);

        var statements = new List<StatementSyntax>();
        var resultCteMethodName = methodNames.Pop();

        // Collect statements from inner expressions
        foreach (var _ in node.InnerExpression)
        {
            methodNames.Pop();
            statements.Add((StatementSyntax)nodes.Pop());
        }

        statements.Reverse();

        const string methodName = "CteResultQuery";

        // Add return statement with method invocation
        statements.Add(CreateReturnStatement(resultCteMethodName));

        // Create the method declaration
        var method = CreateCteMethodDeclaration(methodName, statements);

        return (method, methodName);
    }

    /// <summary>
    /// Creates a return statement that invokes the CTE result method with standard parameters.
    /// </summary>
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

    /// <summary>
    /// Creates a complete method declaration for a CTE result query with standard parameters.
    /// </summary>
    private static MethodDeclarationSyntax CreateCteMethodDeclaration(string methodName, List<StatementSyntax> statements)
    {
        return SyntaxFactory.MethodDeclaration(
            [],
            SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword).WithTrailingTrivia(SyntaxHelper.WhiteSpace)),
            SyntaxFactory.IdentifierName(nameof(Tables.Table)).WithTrailingTrivia(SyntaxHelper.WhiteSpace),
            null,
            SyntaxFactory.Identifier(methodName),
            null,
            CreateCteParameterList(),
            [],
            SyntaxFactory.Block(statements),
            null);
    }

    /// <summary>
    /// Creates the parameter list for CTE method declarations with all required parameters.
    /// </summary>
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

    /// <summary>
    /// Creates the provider parameter for CTE methods.
    /// </summary>
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

    /// <summary>
    /// Creates the positional environment variables parameter with complex generic type.
    /// </summary>
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

    /// <summary>
    /// Creates a generic string-to-string dictionary type syntax.
    /// </summary>
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

    /// <summary>
    /// Creates the queries information parameter with complex tuple type.
    /// </summary>
    private static ParameterSyntax CreateQueriesInformationParameter()
    {
        return SyntaxFactory.Parameter(SyntaxFactory.Identifier("queriesInformation"))
            .WithType(CreateQueriesInformationType());
    }

    /// <summary>
    /// Creates the complex queries information type with tuple elements.
    /// </summary>
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
                            CreateQueryInfoTupleType()
                        })));
    }

    /// <summary>
    /// Creates the tuple type for query information containing multiple elements.
    /// </summary>
    private static TupleTypeSyntax CreateQueryInfoTupleType()
    {
        return SyntaxFactory.TupleType(
            SyntaxFactory.SeparatedList<TupleElementSyntax>(
                new SyntaxNodeOrToken[]
                {
                    SyntaxFactory.TupleElement(SyntaxFactory.IdentifierName("SchemaFromNode"))
                        .WithIdentifier(SyntaxFactory.Identifier("FromNode")),
                    SyntaxFactory.Token(SyntaxKind.CommaToken),
                    CreateUsedColumnsTupleElement(),
                    SyntaxFactory.Token(SyntaxKind.CommaToken),
                    SyntaxFactory.TupleElement(SyntaxFactory.IdentifierName("WhereNode"))
                        .WithIdentifier(SyntaxFactory.Identifier("WhereNode")),
                    SyntaxFactory.Token(SyntaxKind.CommaToken),
                    SyntaxFactory.TupleElement(SyntaxFactory.IdentifierName("bool"))
                        .WithIdentifier(SyntaxFactory.Identifier("HasExternallyProvidedTypes"))
                }));
    }

    /// <summary>
    /// Creates the UsedColumns tuple element with IReadOnlyCollection type.
    /// </summary>
    private static TupleElementSyntax CreateUsedColumnsTupleElement()
    {
        return SyntaxFactory.TupleElement(
                SyntaxFactory.GenericName(SyntaxFactory.Identifier("IReadOnlyCollection"))
                    .WithTypeArgumentList(
                        SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                SyntaxFactory.IdentifierName("ISchemaColumn")))))
            .WithIdentifier(SyntaxFactory.Identifier("UsedColumns"));
    }

    /// <summary>
    /// Creates the logger parameter for CTE methods.
    /// </summary>
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

    /// <summary>
    /// Creates the cancellation token parameter for CTE methods.
    /// </summary>
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

    /// <summary>
    /// Validates that all required parameters are not null.
    /// </summary>
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
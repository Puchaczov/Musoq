#nullable enable

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Visitors.Helpers;

namespace Musoq.Evaluator.Visitors.CodeGeneration;

/// <summary>
/// Emitter for set operations (UNION, UNION ALL, EXCEPT, INTERSECT).
/// Consolidates the code generation logic for all set-based SQL operations.
/// </summary>
public class SetOperationEmitter(Dictionary<string, int[]> setOperatorFieldIndexes)
{
    /// <summary>
    /// Result of processing a set operation.
    /// </summary>
    public readonly struct SetOperationResult
    {
        public string CombinedMethodName { get; init; }
        public MethodDeclarationSyntax Method { get; init; }
    }

    /// <summary>
    /// Processes a set operation by combining two method names and generating the operation method.
    /// </summary>
    /// <param name="methodNames">Stack of method names (will pop two)</param>
    /// <param name="operationSuffix">The operation suffix (e.g., "Union", "Except")</param>
    /// <param name="baseOperationMethodName">The base operation method name (e.g., "Union")</param>
    /// <param name="setOperatorKey">The key for field index lookup</param>
    /// <returns>The result containing the combined method name and generated method</returns>
    public SetOperationResult ProcessSetOperation(
        Stack<string> methodNames,
        string operationSuffix,
        string baseOperationMethodName,
        string setOperatorKey)
    {
        var b = methodNames.Pop();
        var a = methodNames.Pop();
        var combinedName = CombineMethodNames(a, b, operationSuffix);

        var aInvocation = CreateSetOperationInvocation(a);
        var bInvocation = CreateSetOperationInvocation(b);

        var method = GenerateSetOperationMethod(
            combinedName,
            baseOperationMethodName,
            setOperatorKey,
            aInvocation,
            bInvocation);

        return new SetOperationResult
        {
            CombinedMethodName = combinedName,
            Method = method
        };
    }
    
    /// <summary>
    /// Creates a method invocation expression for a set operation.
    /// </summary>
    /// <param name="methodName">The name of the method to invoke (e.g., combined query method name)</param>
    /// <param name="providerIdentifier">The identifier name for the provider parameter</param>
    /// <param name="positionalEnvironmentVariablesIdentifier">The identifier for positional environment variables</param>
    /// <param name="queriesInformationIdentifier">The identifier for queries information</param>
    /// <param name="loggerIdentifier">The identifier for the logger</param>
    /// <param name="tokenIdentifier">The identifier for the cancellation token</param>
    /// <returns>An invocation expression for the set operation method</returns>
    private InvocationExpressionSyntax CreateSetOperationInvocation(
        string methodName,
        string providerIdentifier = "provider",
        string positionalEnvironmentVariablesIdentifier = "positionalEnvironmentVariables",
        string queriesInformationIdentifier = "queriesInformation",
        string loggerIdentifier = "logger",
        string tokenIdentifier = "token")
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.IdentifierName(methodName))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList<ArgumentSyntax>(
                        new SyntaxNodeOrToken[]
                        {
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName(providerIdentifier)),
                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName(positionalEnvironmentVariablesIdentifier)),
                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName(queriesInformationIdentifier)),
                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName(loggerIdentifier)),
                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName(tokenIdentifier))
                        })));
    }
    
    /// <summary>
    /// Generates a set operation method that combines two table expressions using the specified operator.
    /// </summary>
    /// <param name="methodName">The name of the generated method</param>
    /// <param name="setOperator">The set operator name (e.g., "Union", "Except", "Intersect")</param>
    /// <param name="key">The key used to look up field indexes for comparison</param>
    /// <param name="firstTableExpression">The expression for the first (left) table</param>
    /// <param name="secondTableExpression">The expression for the second (right) table</param>
    /// <returns>A method declaration for the set operation</returns>
    private MethodDeclarationSyntax GenerateSetOperationMethod(
        string methodName, 
        string setOperator, 
        string key,
        ExpressionSyntax firstTableExpression, 
        ExpressionSyntax secondTableExpression)
    {
        var body = SyntaxFactory.Block(
            SyntaxFactory.SingletonList<StatementSyntax>(
                SyntaxFactory.ReturnStatement(
                    SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(setOperator))
                        .WithArgumentList(
                            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                new SyntaxNodeOrToken[]
                                {
                                    SyntaxFactory.Argument(firstTableExpression),
                                    SyntaxFactory.Token(SyntaxKind.CommaToken),
                                    SyntaxFactory.Argument(secondTableExpression),
                                    SyntaxFactory.Token(SyntaxKind.CommaToken),
                                    SyntaxFactory.Argument(CreateComparisonLambda(key))
                                }))))));

        return MethodDeclarationHelper.CreateStandardPrivateMethod(methodName, body);
    }
    
    /// <summary>
    /// Creates a lambda expression for comparing two rows based on field indexes.
    /// </summary>
    /// <param name="key">The key used to look up field indexes</param>
    /// <returns>A parenthesized lambda expression for row comparison</returns>
    private ParenthesizedLambdaExpressionSyntax CreateComparisonLambda(string key)
    {
        return SyntaxFactory
            .ParenthesizedLambdaExpression(GenerateLambdaBody("first", "second", key))
            .WithParameterList(SyntaxFactory.ParameterList(
                SyntaxFactory.SeparatedList<ParameterSyntax>(
                    new SyntaxNodeOrToken[]
                    {
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("first")),
                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("second"))
                    })));
    }
    
    /// <summary>
    /// Generates the body of a comparison lambda that checks equality of field values.
    /// </summary>
    /// <param name="first">The identifier for the first row parameter</param>
    /// <param name="second">The identifier for the second row parameter</param>
    /// <param name="key">The key used to look up field indexes</param>
    /// <returns>The syntax node for the lambda body</returns>
    private CSharpSyntaxNode GenerateLambdaBody(string first, string second, string key)
    {
        var indexes = setOperatorFieldIndexes[key];
        var equality = CreateFieldEquality(first, second, indexes[0]);

        var subExpressions = new Stack<ExpressionSyntax>();
        subExpressions.Push(equality);

        for (var i = 1; i < indexes.Length; i++)
        {
            equality = CreateFieldEquality(first, second, indexes[i]);
            subExpressions.Push(
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.LogicalAndExpression,
                    subExpressions.Pop(),
                    equality));
        }

        return subExpressions.Pop();
    }
    
    /// <summary>
    /// Creates an equality check expression for a specific field index.
    /// </summary>
    /// <param name="first">The identifier for the first row</param>
    /// <param name="second">The identifier for the second row</param>
    /// <param name="fieldIndex">The index of the field to compare</param>
    /// <returns>An invocation expression for the equality check</returns>
    private static InvocationExpressionSyntax CreateFieldEquality(string first, string second, int fieldIndex)
    {
        return SyntaxFactory
            .InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    CreateElementAccess(first, fieldIndex),
                    SyntaxFactory.IdentifierName("Equals")))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(CreateElementAccess(second, fieldIndex)))));
    }
    
    /// <summary>
    /// Creates an element access expression (e.g., first[0]).
    /// </summary>
    /// <param name="identifier">The array/indexer identifier</param>
    /// <param name="index">The index to access</param>
    /// <returns>An element access expression</returns>
    private static ElementAccessExpressionSyntax CreateElementAccess(string identifier, int index)
    {
        return SyntaxFactory
            .ElementAccessExpression(SyntaxFactory.IdentifierName(identifier))
            .WithArgumentList(
                SyntaxFactory.BracketedArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                SyntaxFactory.Literal(index))))));
    }

    /// <summary>
    /// Combines two method names for set operation naming.
    /// </summary>
    /// <param name="leftMethodName">The left method name</param>
    /// <param name="rightMethodName">The right method name</param>
    /// <param name="operationSuffix">Optional suffix to add (e.g., "Union", "Except")</param>
    /// <returns>The combined method name</returns>
    private static string CombineMethodNames(string leftMethodName, string rightMethodName, string? operationSuffix = null)
    {
        var baseName = $"{leftMethodName}_{rightMethodName}";
        return operationSuffix != null ? $"{baseName}_{operationSuffix}" : baseName;
    }
}

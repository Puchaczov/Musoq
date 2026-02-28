#nullable enable

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Visitors.Helpers;

namespace Musoq.Evaluator.Visitors.CodeGeneration;

/// <summary>
///     Emitter for set operations (UNION, UNION ALL, EXCEPT, INTERSECT).
///     Consolidates the code generation logic for all set-based SQL operations.
/// </summary>
public class SetOperationEmitter(Dictionary<string, int[]> setOperatorFieldIndexes)
{
    /// <summary>
    ///     Processes a set operation by combining two method names and generating the operation method.
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
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName(positionalEnvironmentVariablesIdentifier)),
                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName(queriesInformationIdentifier)),
                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName(loggerIdentifier)),
                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName(tokenIdentifier))
                        })));
    }

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

    private static InvocationExpressionSyntax CreateFieldEquality(string first, string second, int fieldIndex)
    {
        return SyntaxFactory
            .InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)),
                    SyntaxFactory.IdentifierName("Equals")))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList<ArgumentSyntax>(
                        new SyntaxNodeOrToken[]
                        {
                            SyntaxFactory.Argument(CreateElementAccess(first, fieldIndex)),
                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                            SyntaxFactory.Argument(CreateElementAccess(second, fieldIndex))
                        })));
    }

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

    private static string CombineMethodNames(string leftMethodName, string rightMethodName,
        string? operationSuffix = null)
    {
        var baseName = $"{leftMethodName}_{rightMethodName}";
        return operationSuffix != null ? $"{baseName}_{operationSuffix}" : baseName;
    }

    /// <summary>
    ///     Result of processing a set operation.
    /// </summary>
    public readonly struct SetOperationResult
    {
        public string CombinedMethodName { get; init; }
        public MethodDeclarationSyntax Method { get; init; }
    }
}

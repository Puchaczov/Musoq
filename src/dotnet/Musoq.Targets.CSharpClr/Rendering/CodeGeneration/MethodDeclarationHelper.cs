using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Diagnostics;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Tables;

namespace Musoq.Targets.CSharpClr.Rendering.CodeGeneration;

/// <summary>
///     Helper class for creating complex method and property declarations with standardized parameter lists
/// </summary>
public static partial class MethodDeclarationHelper
{
    /// <summary>
    ///     Creates a standard parameter list for query execution methods
    /// </summary>
    /// <returns>Parameter list with Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, and Token parameters</returns>
    public static ParameterListSyntax CreateStandardParameterList()
    {
        return SyntaxFactory.ParameterList(
            SyntaxFactory.SeparatedList([
                CreateProviderParameter(),
                CreateSourceRuntimeSettingsBySourceContextIdParameter(),
                CreateSourceExecutionPlansParameter(),
                CreateLoggerParameter(),
                CreateTokenParameter()
            ]));
    }

    /// <summary>
    ///     Creates a private method declaration with standard parameters and return type of Table
    /// </summary>
    /// <param name="methodName">Name of the method</param>
    /// <param name="body">Method body statements</param>
    /// <returns>Complete method declaration</returns>
    public static MethodDeclarationSyntax CreateStandardPrivateMethod(string methodName, BlockSyntax body)
    {
        if (string.IsNullOrWhiteSpace(methodName))
            throw new ArgumentException("Method name cannot be null or whitespace", nameof(methodName));
        ArgumentNullException.ThrowIfNull(body);

        return SyntaxFactory.MethodDeclaration(
            [],
            SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword).WithTrailingTrivia(SyntaxHelper.WhiteSpace)),
            SyntaxFactory.IdentifierName(nameof(Table)).WithTrailingTrivia(SyntaxHelper.WhiteSpace),
            null,
            SyntaxFactory.Identifier(methodName),
            null,
            CreateStandardParameterList(),
            [],
            body,
            null);
    }

    /// <summary>
    ///     Creates a public property declaration with get/set accessors
    /// </summary>
    /// <param name="typeName">Property type name</param>
    /// <param name="propertyName">Property name</param>
    /// <returns>Complete property declaration</returns>
    public static PropertyDeclarationSyntax CreatePublicProperty(string typeName, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            throw new ArgumentException("Type name cannot be null or whitespace", nameof(typeName));
        if (string.IsNullOrWhiteSpace(propertyName))
            throw new ArgumentException("Property name cannot be null or whitespace", nameof(propertyName));

        return SyntaxFactory.PropertyDeclaration(
            [],
            SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword).WithTrailingTrivia(SyntaxHelper.WhiteSpace)),
            SyntaxFactory.IdentifierName(typeName).WithTrailingTrivia(SyntaxHelper.WhiteSpace),
            null,
            SyntaxFactory.Identifier(propertyName),
            SyntaxFactory.AccessorList(
                SyntaxFactory.List([
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                ])),
            null,
            null);
    }

    /// <summary>
    ///     Creates the complex generic property for SourceExecutionPlans
    /// </summary>
    /// <returns>SourceExecutionPlans property declaration</returns>
    public static PropertyDeclarationSyntax CreateSourceExecutionPlansProperty()
    {
        return SyntaxFactory.PropertyDeclaration(
                CreateSourceExecutionPlansType(),
                SyntaxFactory.Identifier(nameof(IQueryRunnable.SourceExecutionPlans)))
            .WithModifiers(
                SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithAccessorList(
                SyntaxFactory.AccessorList(
                    SyntaxFactory.List([
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                    ])));
    }

    /// <summary>
    ///     Creates a public Run method that returns Table and takes CancellationToken
    /// </summary>
    /// <param name="methodCallExpression">The method call expression to return</param>
    /// <returns>Complete Run method declaration</returns>
    public static MethodDeclarationSyntax CreateRunMethod(string methodCallExpression)
    {
        if (string.IsNullOrWhiteSpace(methodCallExpression))
            throw new ArgumentException("Method call expression cannot be null or whitespace",
                nameof(methodCallExpression));

        return CreateRunMethodWithBody(
            SyntaxFactory.Block(SyntaxFactory.ParseStatement($"return {methodCallExpression};")));
    }

    public static MethodDeclarationSyntax CreateRunMethodWithBody(BlockSyntax body)
    {
        ArgumentNullException.ThrowIfNull(body);
        return SyntaxFactory.MethodDeclaration(
            [],
            SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword).WithTrailingTrivia(SyntaxHelper.WhiteSpace)),
            SyntaxFactory.IdentifierName(nameof(Table)).WithTrailingTrivia(SyntaxHelper.WhiteSpace),
            null,
            SyntaxFactory.Identifier(nameof(ITableRunnable.Run)),
            null,
            SyntaxFactory.ParameterList(
                SyntaxFactory.SeparatedList([
                    SyntaxFactory.Parameter(
                        [],
                        SyntaxTokenList.Create(new SyntaxToken()),
                        SyntaxFactory.IdentifierName(nameof(CancellationToken))
                            .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                        SyntaxFactory.Identifier("token"), null)
                ])),
            [],
            body,
            null);
    }

    internal static MethodDeclarationSyntax CreateProfiledRunMethod(string methodCallExpression)
    {
        if (string.IsNullOrWhiteSpace(methodCallExpression))
            throw new ArgumentException("Method call expression cannot be null or whitespace", nameof(methodCallExpression));

        var body = SyntaxFactory.Block(
            SyntaxFactory.ParseStatement("ArgumentNullException.ThrowIfNull(profileRecorder);"),
            SyntaxFactory.ParseStatement($"return {methodCallExpression};"));

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.IdentifierName(nameof(Table)),
                SyntaxFactory.Identifier(nameof(IProfiledRunnable.RunWithProfile)))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(
                SyntaxFactory.ParameterList(
                    SyntaxFactory.SeparatedList(
                    [
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("token"))
                            .WithType(SyntaxFactory.IdentifierName(nameof(CancellationToken))),
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("profileRecorder"))
                            .WithType(SyntaxFactory.IdentifierName(nameof(QueryProfileRecorder)))
                    ])))
            .WithBody(body);
    }
}

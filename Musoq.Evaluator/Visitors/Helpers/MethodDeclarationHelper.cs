using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Tables;
using Musoq.Schema;
using System.Threading;

namespace Musoq.Evaluator.Visitors.Helpers
{
    /// <summary>
    /// Helper class for creating complex method and property declarations with standardized parameter lists
    /// </summary>
    public static class MethodDeclarationHelper
    {
        /// <summary>
        /// Creates a standard parameter list for query execution methods
        /// </summary>
        /// <returns>Parameter list with Provider, PositionalEnvironmentVariables, QueriesInformation, Logger, and Token parameters</returns>
        public static ParameterListSyntax CreateStandardParameterList()
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
        /// Creates a private method declaration with standard parameters and return type of Table
        /// </summary>
        /// <param name="methodName">Name of the method</param>
        /// <param name="body">Method body statements</param>
        /// <returns>Complete method declaration</returns>
        public static MethodDeclarationSyntax CreateStandardPrivateMethod(string methodName, BlockSyntax body)
        {
            if (string.IsNullOrWhiteSpace(methodName))
                throw new ArgumentException("Method name cannot be null or whitespace", nameof(methodName));
            if (body == null)
                throw new ArgumentNullException(nameof(body));

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
        /// Creates a public property declaration with get/set accessors
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
        /// Creates the complex generic property for PositionalEnvironmentVariables
        /// </summary>
        /// <returns>PositionalEnvironmentVariables property declaration</returns>
        public static PropertyDeclarationSyntax CreatePositionalEnvironmentVariablesProperty()
        {
            return SyntaxFactory.PropertyDeclaration(
                [],
                SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword).WithTrailingTrivia(SyntaxHelper.WhiteSpace)),
                CreatePositionalEnvironmentVariablesType()
                    .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                null,
                SyntaxFactory.Identifier(nameof(IRunnable.PositionalEnvironmentVariables)),
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
        /// Creates the complex generic property for QueriesInformation
        /// </summary>
        /// <returns>QueriesInformation property declaration</returns>
        public static PropertyDeclarationSyntax CreateQueriesInformationProperty()
        {
            return SyntaxFactory.PropertyDeclaration(
                CreateQueriesInformationType(),
                SyntaxFactory.Identifier("QueriesInformation"))
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
                    ])))
            .NormalizeWhitespace();
        }

        /// <summary>
        /// Creates a public Run method that returns Table and takes CancellationToken
        /// </summary>
        /// <param name="methodCallExpression">The method call expression to return</param>
        /// <returns>Complete Run method declaration</returns>
        public static MethodDeclarationSyntax CreateRunMethod(string methodCallExpression)
        {
            if (string.IsNullOrWhiteSpace(methodCallExpression))
                throw new ArgumentException("Method call expression cannot be null or whitespace", nameof(methodCallExpression));

            return SyntaxFactory.MethodDeclaration(
                [],
                SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword).WithTrailingTrivia(SyntaxHelper.WhiteSpace)),
                SyntaxFactory.IdentifierName(nameof(Table)).WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                null,
                SyntaxFactory.Identifier(nameof(IRunnable.Run)),
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
                SyntaxFactory.Block(SyntaxFactory.ParseStatement($"return {methodCallExpression};")),
                null);
        }

        #region Private Helper Methods

        private static ParameterSyntax CreateProviderParameter()
        {
            return SyntaxFactory.Parameter(
                [],
                SyntaxTokenList.Create(new SyntaxToken()),
                SyntaxFactory.IdentifierName(nameof(ISchemaProvider))
                    .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                SyntaxFactory.Identifier("provider"), null);
        }

        private static ParameterSyntax CreatePositionalEnvironmentVariablesParameter()
        {
            return SyntaxFactory.Parameter(
                [],
                SyntaxTokenList.Create(new SyntaxToken()),
                CreatePositionalEnvironmentVariablesType()
                    .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                SyntaxFactory.Identifier("positionalEnvironmentVariables"), null);
        }

        private static ParameterSyntax CreateQueriesInformationParameter()
        {
            return SyntaxFactory.Parameter(
                    SyntaxFactory.Identifier("queriesInformation"))
                .WithType(CreateQueriesInformationType());
        }

        private static ParameterSyntax CreateLoggerParameter()
        {
            return SyntaxFactory.Parameter(
                [],
                SyntaxTokenList.Create(new SyntaxToken()),
                SyntaxFactory.IdentifierName(nameof(ILogger))
                    .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                SyntaxFactory.Identifier("logger"), null);
        }

        private static ParameterSyntax CreateTokenParameter()
        {
            return SyntaxFactory.Parameter(
                [],
                SyntaxTokenList.Create(new SyntaxToken()),
                SyntaxFactory.IdentifierName(nameof(CancellationToken))
                    .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                SyntaxFactory.Identifier("token"), null);
        }

        private static TypeSyntax CreatePositionalEnvironmentVariablesType()
        {
            return SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier("IReadOnlyDictionary"))
                .WithTypeArgumentList(
                    SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SeparatedList<TypeSyntax>(
                            new SyntaxNodeOrToken[]
                            {
                                SyntaxFactory.PredefinedType(
                                    SyntaxFactory.Token(SyntaxKind.UIntKeyword)),
                                SyntaxFactory.Token(SyntaxKind.CommaToken),
                                SyntaxFactory.GenericName(
                                        SyntaxFactory.Identifier("IReadOnlyDictionary"))
                                    .WithTypeArgumentList(
                                        SyntaxFactory.TypeArgumentList(
                                            SyntaxFactory.SeparatedList<TypeSyntax>(
                                                new SyntaxNodeOrToken[]
                                                {
                                                    SyntaxFactory.PredefinedType(
                                                        SyntaxFactory.Token(SyntaxKind.StringKeyword)),
                                                    SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                    SyntaxFactory.PredefinedType(
                                                        SyntaxFactory.Token(SyntaxKind.StringKeyword))
                                                })))
                            })));
        }

        private static TypeSyntax CreateQueriesInformationType()
        {
            return SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier("IReadOnlyDictionary"))
                .WithTypeArgumentList(
                    SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SeparatedList<TypeSyntax>(
                            new SyntaxNodeOrToken[]
                            {
                                SyntaxFactory.PredefinedType(
                                    SyntaxFactory.Token(SyntaxKind.StringKeyword)),
                                SyntaxFactory.Token(SyntaxKind.CommaToken),
                                SyntaxFactory.TupleType(
                                    SyntaxFactory.SeparatedList<TupleElementSyntax>(
                                        new SyntaxNodeOrToken[]
                                        {
                                            SyntaxFactory.TupleElement(
                                                    SyntaxFactory.IdentifierName("SchemaFromNode"))
                                                .WithIdentifier(
                                                    SyntaxFactory.Identifier("FromNode")),
                                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                                            SyntaxFactory.TupleElement(
                                                    SyntaxFactory.GenericName(
                                                            SyntaxFactory.Identifier("IReadOnlyCollection"))
                                                        .WithTypeArgumentList(
                                                            SyntaxFactory.TypeArgumentList(
                                                                SyntaxFactory
                                                                    .SingletonSeparatedList<TypeSyntax>(
                                                                        SyntaxFactory.IdentifierName(
                                                                            "ISchemaColumn")))))
                                                .WithIdentifier(
                                                    SyntaxFactory.Identifier("UsedColumns")),
                                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                                            SyntaxFactory.TupleElement(
                                                    SyntaxFactory.IdentifierName("WhereNode"))
                                                .WithIdentifier(
                                                    SyntaxFactory.Identifier("WhereNode")),
                                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                                            SyntaxFactory.TupleElement(
                                                    SyntaxFactory.IdentifierName("bool"))
                                                .WithIdentifier(
                                                    SyntaxFactory.Identifier("HasExternallyProvidedTypes"))
                                        }))
                            })));
        }

        #endregion
    }
}
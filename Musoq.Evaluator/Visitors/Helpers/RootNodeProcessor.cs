using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.Extensions.Logging;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Runtime;
using Musoq.Evaluator.Tables;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Plugins;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors.Helpers;

/// <summary>
/// Processor for RootNode that handles class generation and compilation unit creation.
/// </summary>
public static class RootNodeProcessor
{
    /// <summary>
    /// Result of processing a RootNode.
    /// </summary>
    public sealed class ProcessResult
    {
        /// <summary>
        /// The updated compilation with the generated syntax tree.
        /// </summary>
        public required CSharpCompilation Compilation { get; init; }
    }

    /// <summary>
    /// Processes a RootNode to generate a complete class with compilation unit.
    /// </summary>
    /// <param name="node">The RootNode to process.</param>
    /// <param name="methodNames">Stack of method names.</param>
    /// <param name="members">List of class members.</param>
    /// <param name="namespaces">List of namespaces to include.</param>
    /// <param name="inMemoryTableIndex">Index for in-memory tables.</param>
    /// <param name="className">Name of the class to generate.</param>
    /// <param name="namespace">Namespace for the generated class.</param>
    /// <param name="compilation">Current compilation.</param>
    /// <param name="workspace">Workspace for formatting.</param>
    /// <param name="generator">Syntax generator for creating declarations.</param>
    /// <returns>ProcessResult containing the updated compilation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
    public static ProcessResult ProcessRootNode(
        RootNode node,
        Stack<string> methodNames,
        List<SyntaxNode> members,
        List<string> namespaces,
        int inMemoryTableIndex,
        string className,
        string @namespace,
        CSharpCompilation compilation,
        AdhocWorkspace workspace,
        SyntaxGenerator generator)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));
        
        if (methodNames == null)
            throw new ArgumentNullException(nameof(methodNames));
        
        if (members == null)
            throw new ArgumentNullException(nameof(members));
        
        if (namespaces == null)
            throw new ArgumentNullException(nameof(namespaces));
        
        if (string.IsNullOrEmpty(className))
            throw new ArgumentNullException(nameof(className));
        
        if (string.IsNullOrEmpty(@namespace))
            throw new ArgumentNullException(nameof(@namespace));
        
        if (compilation == null)
            throw new ArgumentNullException(nameof(compilation));
        
        if (workspace == null)
            throw new ArgumentNullException(nameof(workspace));
        
        if (generator == null)
            throw new ArgumentNullException(nameof(generator));

        // Create Run method
        var method = CreateRunMethod(methodNames);
        
        // Create properties
        var providerParam = CreateProviderProperty();
        var positionalEnvironmentVariablesParam = CreatePositionalEnvironmentVariablesProperty();
        var queriesInformationParam = CreateQueriesInformationProperty();
        var loggerParam = CreateLoggerProperty();

        // Add members
        members.Add(method);
        members.Add(providerParam);
        members.Add(positionalEnvironmentVariablesParam);
        members.Add(queriesInformationParam);
        members.Add(loggerParam);

        // Create in-memory tables field
        var inMemoryTables = CreateInMemoryTablesField(inMemoryTableIndex);
        members.Insert(0, inMemoryTables);

        // Create class declaration
        var classDeclaration = generator.ClassDeclaration(className, Array.Empty<string>(), Accessibility.Public,
            DeclarationModifiers.None,
            null,
            [
                SyntaxFactory.IdentifierName(nameof(BaseOperations)),
                SyntaxFactory.IdentifierName(nameof(IRunnable))
            ], members);

        // Create namespace and compilation unit
        var ns = CreateNamespaceDeclaration(@namespace, namespaces, classDeclaration);
        var compilationUnit = CreateCompilationUnit(ns);

        // Format and add to compilation
        var formatted = FormatCompilationUnit(compilationUnit, workspace);
        var updatedCompilation = compilation.AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(formatted.ToFullString(),
            new CSharpParseOptions(LanguageVersion.CSharp8), null, Encoding.ASCII));

        return new ProcessResult
        {
            Compilation = updatedCompilation
        };
    }

    /// <summary>
    /// Creates the Run method declaration.
    /// </summary>
    private static MethodDeclarationSyntax CreateRunMethod(Stack<string> methodNames)
    {
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
            SyntaxFactory.Block(SyntaxFactory.ParseStatement(
                $"return {methodNames.Pop()}(Provider, PositionalEnvironmentVariables, QueriesInformation, Logger, token);")),
            null);
    }

    /// <summary>
    /// Creates the Provider property declaration.
    /// </summary>
    private static PropertyDeclarationSyntax CreateProviderProperty()
    {
        return SyntaxFactory.PropertyDeclaration(
            [],
            SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword).WithTrailingTrivia(SyntaxHelper.WhiteSpace)),
            SyntaxFactory.IdentifierName(nameof(ISchemaProvider)).WithTrailingTrivia(SyntaxHelper.WhiteSpace),
            null,
            SyntaxFactory.Identifier(nameof(IRunnable.Provider)),
            SyntaxFactory.AccessorList(
                SyntaxFactory.List<AccessorDeclarationSyntax>()
                    .Add(SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))
                    .Add(SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))),
            null,
            null);
    }

    /// <summary>
    /// Creates the PositionalEnvironmentVariables property declaration.
    /// </summary>
    private static PropertyDeclarationSyntax CreatePositionalEnvironmentVariablesProperty()
    {
        return SyntaxFactory.PropertyDeclaration(
            [],
            SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword).WithTrailingTrivia(SyntaxHelper.WhiteSpace)),
            SyntaxFactory.GenericName(
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
                            })))
                .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
            null,
            SyntaxFactory.Identifier(nameof(IRunnable.PositionalEnvironmentVariables)),
            SyntaxFactory.AccessorList(
                SyntaxFactory.List<AccessorDeclarationSyntax>()
                    .Add(SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))
                    .Add(SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))),
            null,
            null);
    }

    /// <summary>
    /// Creates the QueriesInformation property declaration.
    /// </summary>
    private static PropertyDeclarationSyntax CreateQueriesInformationProperty()
    {
        return SyntaxFactory.PropertyDeclaration(
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
                                }))),
                SyntaxFactory.Identifier("QueriesInformation"))
            .WithModifiers(
                SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithAccessorList(
                SyntaxFactory.AccessorList(
                    SyntaxFactory.List(
                    [
                        SyntaxFactory.AccessorDeclaration(
                                SyntaxKind.GetAccessorDeclaration)
                            .WithSemicolonToken(
                                SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                        SyntaxFactory.AccessorDeclaration(
                                SyntaxKind.SetAccessorDeclaration)
                            .WithSemicolonToken(
                                SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                    ])))
            .NormalizeWhitespace();
    }

    /// <summary>
    /// Creates the Logger property declaration.
    /// </summary>
    private static PropertyDeclarationSyntax CreateLoggerProperty()
    {
        return SyntaxFactory.PropertyDeclaration(
            [],
            SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword).WithTrailingTrivia(SyntaxHelper.WhiteSpace)),
            SyntaxFactory.IdentifierName(nameof(ILogger)).WithTrailingTrivia(SyntaxHelper.WhiteSpace),
            null,
            SyntaxFactory.Identifier(nameof(IRunnable.Logger)),
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
    /// Creates the in-memory tables field declaration.
    /// </summary>
    private static FieldDeclarationSyntax CreateInMemoryTablesField(int inMemoryTableIndex)
    {
        return SyntaxFactory
            .FieldDeclaration(SyntaxFactory
                .VariableDeclaration(SyntaxFactory.ArrayType(SyntaxFactory.IdentifierName(nameof(Table)))
                    .WithRankSpecifiers(SyntaxFactory.SingletonList(
                        SyntaxFactory.ArrayRankSpecifier(
                            SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                SyntaxFactory.OmittedArraySizeExpression()))))).WithVariables(
                    SyntaxFactory.SingletonSeparatedList(SyntaxFactory
                        .VariableDeclarator(SyntaxFactory.Identifier("_tableResults")).WithInitializer(
                            SyntaxFactory.EqualsValueClause(SyntaxFactory.ArrayCreationExpression(SyntaxFactory
                                .ArrayType(SyntaxFactory.IdentifierName(nameof(Table))).WithRankSpecifiers(
                                    SyntaxFactory.SingletonList(
                                        SyntaxFactory.ArrayRankSpecifier(
                                            SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                                    SyntaxFactory.Literal(inMemoryTableIndex))))))))))))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));
    }

    /// <summary>
    /// Creates the namespace declaration.
    /// </summary>
    private static NamespaceDeclarationSyntax CreateNamespaceDeclaration(
        string @namespace, 
        List<string> namespaces, 
        SyntaxNode classDeclaration)
    {
        return SyntaxFactory.NamespaceDeclaration(
            SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(@namespace)),
            SyntaxFactory.List<ExternAliasDirectiveSyntax>(),
            SyntaxFactory.List(
                namespaces.Select(
                    n => SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName(n)))),
            SyntaxFactory.List<MemberDeclarationSyntax>([(ClassDeclarationSyntax) classDeclaration]));
    }

    /// <summary>
    /// Creates the compilation unit.
    /// </summary>
    private static CompilationUnitSyntax CreateCompilationUnit(NamespaceDeclarationSyntax @namespace)
    {
        return SyntaxFactory.CompilationUnit(
            SyntaxFactory.List<ExternAliasDirectiveSyntax>(),
            SyntaxFactory.List<UsingDirectiveSyntax>(),
            SyntaxFactory.List<AttributeListSyntax>(),
            SyntaxFactory.List<MemberDeclarationSyntax>([@namespace]));
    }

    /// <summary>
    /// Formats the compilation unit using the workspace.
    /// </summary>
    private static SyntaxNode FormatCompilationUnit(CompilationUnitSyntax compilationUnit, AdhocWorkspace workspace)
    {
        var options = workspace.Options;
        options = options.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInMethods, true);
        options.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInProperties, true);

        return Formatter.Format(compilationUnit, workspace);
    }
}
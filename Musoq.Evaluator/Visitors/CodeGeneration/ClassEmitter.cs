using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.Extensions.Logging;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Visitors.Helpers;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors.CodeGeneration;

/// <summary>
/// Emitter for generating the final C# class that implements IRunnable.
/// </summary>
public static class ClassEmitter
{
    /// <summary>
    /// Creates the in-memory table results field declaration.
    /// </summary>
    /// <param name="tableCount">The number of in-memory tables.</param>
    /// <returns>A field declaration for the _tableResults array.</returns>
    public static FieldDeclarationSyntax CreateInMemoryTablesField(int tableCount)
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
                                                    SyntaxFactory.Literal(tableCount))))))))))))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));
    }

    /// <summary>
    /// Creates the class declaration with all members and base types.
    /// </summary>
    public static SyntaxNode CreateClassDeclaration(
        SyntaxGenerator generator,
        string className,
        IList<SyntaxNode> members)
    {
        return generator.ClassDeclaration(
            className, 
            [], 
            Accessibility.Public,
            DeclarationModifiers.None,
            null,
            [
                SyntaxFactory.IdentifierName(nameof(BaseOperations)),
                SyntaxFactory.IdentifierName(nameof(IRunnable))
            ], 
            members);
    }

    /// <summary>
    /// Creates the namespace declaration containing the class.
    /// </summary>
    public static NamespaceDeclarationSyntax CreateNamespaceDeclaration(
        string namespaceName,
        IEnumerable<string> namespaces,
        ClassDeclarationSyntax classDeclaration)
    {
        return SyntaxFactory.NamespaceDeclaration(
            SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(namespaceName)),
            SyntaxFactory.List<ExternAliasDirectiveSyntax>(),
            SyntaxFactory.List(
                namespaces.Select(n => SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName(n)))),
            SyntaxFactory.List<MemberDeclarationSyntax>([classDeclaration]));
    }

    /// <summary>
    /// Creates the compilation unit containing the namespace.
    /// </summary>
    public static CompilationUnitSyntax CreateCompilationUnit(NamespaceDeclarationSyntax ns)
    {
        return SyntaxFactory.CompilationUnit(
            SyntaxFactory.List<ExternAliasDirectiveSyntax>(),
            SyntaxFactory.List<UsingDirectiveSyntax>(),
            SyntaxFactory.List<AttributeListSyntax>(),
            SyntaxFactory.List<MemberDeclarationSyntax>([ns]));
    }

    /// <summary>
    /// Formats the compilation unit with standard C# formatting options.
    /// </summary>
    public static SyntaxNode FormatCompilationUnit(CompilationUnitSyntax compilationUnit, Workspace workspace)
    {
        var options = workspace.Options;
        options = options.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInMethods, true);
        options = options.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInProperties, true);
        options = options.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInControlBlocks, true);
        options = options.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInAnonymousMethods, true);
        options = options.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInLambdaExpressionBody, true);

        return Formatter.Format(compilationUnit, workspace, options);
    }

    /// <summary>
    /// Creates a syntax tree from the formatted code.
    /// </summary>
    public static SyntaxTree CreateSyntaxTree(SyntaxNode formattedCode)
    {
        return SyntaxFactory.ParseSyntaxTree(
            formattedCode.ToFullString(),
            new CSharpParseOptions(LanguageVersion.CSharp8), 
            null, 
            Encoding.ASCII);
    }

    /// <summary>
    /// Adds the standard IRunnable members (properties and Run method).
    /// </summary>
    public static void AddRunnableMembers(
        IList<SyntaxNode> members,
        string methodCallExpression)
    {
        var method = MethodDeclarationHelper.CreateRunMethod(methodCallExpression);
        var providerParam = MethodDeclarationHelper.CreatePublicProperty(nameof(ISchemaProvider), nameof(IRunnable.Provider));
        var positionalEnvironmentVariablesParam = MethodDeclarationHelper.CreatePositionalEnvironmentVariablesProperty();
        var queriesInformationParam = MethodDeclarationHelper.CreateQueriesInformationProperty();
        var loggerParam = MethodDeclarationHelper.CreatePublicProperty(nameof(ILogger), nameof(IRunnable.Logger));
        var phaseChangedEvent = MethodDeclarationHelper.CreatePhaseChangedEvent();
        var onPhaseChangedMethod = MethodDeclarationHelper.CreateOnPhaseChangedMethod();
        var dataSourceProgressEvent = MethodDeclarationHelper.CreateDataSourceProgressEvent();
        var onDataSourceProgressMethod = MethodDeclarationHelper.CreateOnDataSourceProgressMethod();

        members.Add(method);
        members.Add(providerParam);
        members.Add(positionalEnvironmentVariablesParam);
        members.Add(queriesInformationParam);
        members.Add(loggerParam);
        members.Add(phaseChangedEvent);
        members.Add(onPhaseChangedMethod);
        members.Add(dataSourceProgressEvent);
        members.Add(onDataSourceProgressMethod);
    }
}

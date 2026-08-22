using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.Extensions.Logging;
using Musoq.Evaluator.Runtime;
using Musoq.Evaluator.Tables;
using Musoq.Schema;

namespace Musoq.Targets.CSharpClr.Rendering.CodeGeneration;

/// <summary>
///     Emitter for generating the final C# class that implements ITableRunnable.
/// </summary>
public static class ClassEmitter
{
    /// <summary>
    ///     Creates the in-memory table results field declaration.
    /// </summary>
    /// <param name="tableCount">The number of in-memory tables.</param>
    /// <returns>A field declaration for the _tableResults array.</returns>
    public static FieldDeclarationSyntax CreateInMemoryTablesField(int tableCount)
    {
        ExpressionSyntax initializer;

        if (tableCount == 0)
        {
            initializer = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("Array"),
                    SyntaxFactory.GenericName(SyntaxFactory.Identifier("Empty"))
                        .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                SyntaxFactory.IdentifierName(nameof(Table)))))));
        }
        else
        {
            initializer = SyntaxFactory.ArrayCreationExpression(SyntaxFactory
                .ArrayType(SyntaxFactory.IdentifierName(nameof(Table))).WithRankSpecifiers(
                    SyntaxFactory.SingletonList(
                        SyntaxFactory.ArrayRankSpecifier(
                            SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                    SyntaxFactory.Literal(tableCount)))))));
        }

        return SyntaxFactory
            .FieldDeclaration(SyntaxFactory
                .VariableDeclaration(SyntaxFactory.ArrayType(SyntaxFactory.IdentifierName(nameof(Table)))
                    .WithRankSpecifiers(SyntaxFactory.SingletonList(
                        SyntaxFactory.ArrayRankSpecifier(
                            SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                SyntaxFactory.OmittedArraySizeExpression()))))).WithVariables(
                    SyntaxFactory.SingletonSeparatedList(SyntaxFactory
                        .VariableDeclarator(SyntaxFactory.Identifier("_tableResults")).WithInitializer(
                            SyntaxFactory.EqualsValueClause(initializer)))))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));
    }

    /// <summary>
    ///     Creates the CTE sidecar index results field declaration.
    /// </summary>
    public static FieldDeclarationSyntax CreateCteIndexResultsField(int indexCount)
    {
        ExpressionSyntax initializer;

        if (indexCount == 0)
        {
            initializer = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("Array"),
                    SyntaxFactory.GenericName(SyntaxFactory.Identifier("Empty"))
                        .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)))))));
        }
        else
        {
            initializer = SyntaxFactory.ArrayCreationExpression(SyntaxFactory
                .ArrayType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword))).WithRankSpecifiers(
                    SyntaxFactory.SingletonList(
                        SyntaxFactory.ArrayRankSpecifier(
                            SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                    SyntaxFactory.Literal(indexCount)))))));
        }

        return SyntaxFactory
            .FieldDeclaration(SyntaxFactory
                .VariableDeclaration(SyntaxFactory.ArrayType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)))
                    .WithRankSpecifiers(SyntaxFactory.SingletonList(
                        SyntaxFactory.ArrayRankSpecifier(
                            SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                SyntaxFactory.OmittedArraySizeExpression()))))).WithVariables(
                    SyntaxFactory.SingletonSeparatedList(SyntaxFactory
                        .VariableDeclarator(SyntaxFactory.Identifier("_cteIndexResults")).WithInitializer(
                            SyntaxFactory.EqualsValueClause(initializer)))))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));
    }

    /// <summary>
    ///     Creates the class declaration with all members and base types.
    ///     Members are reordered by generated-code readability groups.
    /// </summary>
    public static SyntaxNode CreateClassDeclaration(
        SyntaxGenerator generator,
        string className,
        IList<SyntaxNode> members,
        bool implementsProfiledRunnable = false,
        bool implementsContextualRunnable = false,
        bool implementsContextualProfiledRunnable = false)
    {
        ArgumentNullException.ThrowIfNull(generator);
        var orderedMembers = ReorderMembers(members);
        var baseTypes = new List<SyntaxNode>
        {
            SyntaxFactory.IdentifierName(nameof(BaseOperations)),
            SyntaxFactory.IdentifierName(nameof(ITableRunnable)),
            SyntaxFactory.IdentifierName(nameof(IQueryProgressSource)),
            SyntaxFactory.IdentifierName(nameof(IParameterizedRunnable))
        };

        if (implementsContextualRunnable)
            baseTypes.Insert(2, SyntaxFactory.IdentifierName(nameof(IContextTableRunnable)));

        if (implementsProfiledRunnable)
            baseTypes.Add(SyntaxFactory.IdentifierName(nameof(IProfiledRunnable)));

        if (implementsContextualProfiledRunnable)
            baseTypes.Add(SyntaxFactory.IdentifierName(nameof(IContextProfiledRunnable)));

        return generator.ClassDeclaration(
            className,
            [],
            Accessibility.Public,
            DeclarationModifiers.Sealed,
            null,
            baseTypes,
            orderedMembers);
    }

    public static SyntaxNode CreateTypedClassDeclaration(
        SyntaxGenerator generator,
        string className,
        Type outputType,
        IList<SyntaxNode> members)
    {
        ArgumentNullException.ThrowIfNull(generator);
        ArgumentNullException.ThrowIfNull(outputType);
        var orderedMembers = ReorderMembers(members);
        var baseTypes = new List<SyntaxNode>
        {
            SyntaxFactory.IdentifierName(nameof(BaseOperations)),
            SyntaxFactory.GenericName(nameof(ITypedRunnable<object>))
                .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                        LegacyCodeGenerationSyntaxFactory.CreateTypeSyntax(outputType)))),
            SyntaxFactory.IdentifierName(nameof(IQueryProgressSource)),
            SyntaxFactory.IdentifierName(nameof(IParameterizedRunnable))
        };

        return generator.ClassDeclaration(
            className,
            [],
            Accessibility.Public,
            DeclarationModifiers.Sealed,
            null,
            baseTypes,
            orderedMembers);
    }

    private static List<SyntaxNode> ReorderMembers(IList<SyntaxNode> members)
    {
        return members.OrderBy(GetMemberSortOrder).ToList();
    }

    private static int GetMemberSortOrder(SyntaxNode member)
    {
        return member switch
        {
            FieldDeclarationSyntax => 0,
            ConstructorDeclarationSyntax => 1,
            PropertyDeclarationSyntax property when HasModifier(property.Modifiers, SyntaxKind.PublicKeyword) => 2,
            EventFieldDeclarationSyntax eventField when HasModifier(eventField.Modifiers, SyntaxKind.PublicKeyword) => 3,
            EventDeclarationSyntax eventDeclaration when HasModifier(eventDeclaration.Modifiers, SyntaxKind.PublicKeyword) => 3,
            MethodDeclarationSyntax method => GetMethodSortOrder(method.Modifiers),
            PropertyDeclarationSyntax property => GetNonPublicMemberSortOrder(property.Modifiers),
            EventFieldDeclarationSyntax eventField => GetNonPublicMemberSortOrder(eventField.Modifiers),
            EventDeclarationSyntax eventDeclaration => GetNonPublicMemberSortOrder(eventDeclaration.Modifiers),
            ClassDeclarationSyntax => 8,
            StructDeclarationSyntax => 8,
            RecordDeclarationSyntax => 8,
            _ => 9
        };
    }

    private static int GetMethodSortOrder(SyntaxTokenList modifiers)
    {
        if (HasModifier(modifiers, SyntaxKind.PublicKeyword))
            return 4;

        if (HasModifier(modifiers, SyntaxKind.ProtectedKeyword))
            return 5;

        if (HasModifier(modifiers, SyntaxKind.StaticKeyword))
            return 7;

        return 6;
    }

    private static int GetNonPublicMemberSortOrder(SyntaxTokenList modifiers)
    {
        if (HasModifier(modifiers, SyntaxKind.ProtectedKeyword))
            return 5;

        if (HasModifier(modifiers, SyntaxKind.StaticKeyword))
            return 7;

        return 6;
    }

    private static bool HasModifier(SyntaxTokenList modifiers, SyntaxKind kind)
    {
        return modifiers.Any(kind);
    }

    /// <summary>
    ///     Creates the namespace declaration containing the class.
    /// </summary>
    public static NamespaceDeclarationSyntax CreateNamespaceDeclaration(
        string namespaceName,
        IEnumerable<string> namespaces,
        ClassDeclarationSyntax classDeclaration)
    {
        return SyntaxFactory.NamespaceDeclaration(
            SyntaxFactory.ParseName(namespaceName),
            SyntaxFactory.List<ExternAliasDirectiveSyntax>(),
            SyntaxFactory.List(
                namespaces.Select(n => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(n)))),
            SyntaxFactory.List<MemberDeclarationSyntax>([classDeclaration]));
    }

    /// <summary>
    ///     Creates the compilation unit containing the namespace.
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
    ///     Creates a syntax tree directly from the compilation unit without the expensive Formatter.Format() step.
    ///     Uses NormalizeWhitespace() instead, which is an O(n) pass that adds standard whitespace trivia —
    ///     much faster than the full Roslyn Formatter which requires a Workspace and formatting options.
    ///     The tree is then re-parsed to ensure proper structure for Roslyn compilation.
    /// </summary>
    public static SyntaxTree CreateSyntaxTreeDirect(CompilationUnitSyntax compilationUnit)
    {
        return CreateSyntaxTreeDirect(compilationUnit, TargetRenderProfile.StableArtifact);
    }

    internal static SyntaxTree CreateSyntaxTreeDirect(
        CompilationUnitSyntax compilationUnit,
        TargetRenderProfile profile,
        string? path = null)
    {
        ArgumentNullException.ThrowIfNull(compilationUnit);
        if (profile == TargetRenderProfile.ExecutionFast)
        {
            using (TargetRenderTelemetry.BeginPhase("render.syntax-tree-direct"))
            {
                // The generated tree contains a few contextual syntax nodes
                // (for example `var` and `ref var`) created by SyntaxFactory.
                // Re-parsing normalized text restores the parser's
                // contextual-keyword classification. We deliberately omit
                // the workspace formatter and redundant-parenthesis pass.
                return CSharpSyntaxTree.ParseText(
                    compilationUnit.NormalizeWhitespace().ToFullString(),
                    new CSharpParseOptions(LanguageVersion.CSharp13),
                    path ?? string.Empty,
                    Encoding.UTF8);
            }
        }

        CompilationUnitSyntax cleaned;
        using (TargetRenderTelemetry.BeginPhase("render.syntax-cleanup"))
            cleaned = (CompilationUnitSyntax)new RedundantParenthesisRewriter().Visit(compilationUnit)!;

        string source;
        using (TargetRenderTelemetry.BeginPhase("render.formatting"))
            source = GeneratedCSharpCodeFormatter.Normalize(cleaned.NormalizeWhitespace().ToFullString());

        using (TargetRenderTelemetry.BeginPhase("render.reparse"))
            return SyntaxFactory.ParseSyntaxTree(
                source,
                new CSharpParseOptions(LanguageVersion.CSharp13),
                path ?? string.Empty,
                Encoding.UTF8);
    }

    /// <summary>
    ///     Adds the standard table runnable members (properties and Run method).
    /// </summary>
    public static void AddRunnableMembers(
        IList<SyntaxNode> members,
        string methodCallExpression,
        IReadOnlyList<ScriptParameterDefinition>? parameterDefinitions = null,
        string? contextMethodCallExpression = null,
        bool enableContextualExecution = false)
    {
        ArgumentNullException.ThrowIfNull(members);
        var method = MethodDeclarationHelper.CreateRunMethod(methodCallExpression);
        AddRunnableMembersCore(members, method, parameterDefinitions);
        if (enableContextualExecution && contextMethodCallExpression != null)
            members.Add(MethodDeclarationHelper.CreateContextRunMethodWithBody(
                SyntaxFactory.Block(SyntaxFactory.ParseStatement($"return {contextMethodCallExpression};"))));
    }

    public static void AddProfiledRunnableMembers(
        IList<SyntaxNode> members,
        string methodCallExpression,
        string profiledMethodCallExpression,
        IReadOnlyList<ScriptParameterDefinition>? parameterDefinitions = null,
        string? contextMethodCallExpression = null,
        bool enableContextualExecution = false,
        string? contextProfiledMethodCallExpression = null)
    {
        ArgumentNullException.ThrowIfNull(members);
        var method = MethodDeclarationHelper.CreateRunMethod(methodCallExpression);
        AddRunnableMembersCore(members, method, parameterDefinitions);
        if (enableContextualExecution && contextMethodCallExpression != null)
            members.Add(MethodDeclarationHelper.CreateContextRunMethodWithBody(
                SyntaxFactory.Block(SyntaxFactory.ParseStatement($"return {contextMethodCallExpression};"))));
        members.Add(MethodDeclarationHelper.CreateProfiledRunMethod(profiledMethodCallExpression));
        if (enableContextualExecution && contextProfiledMethodCallExpression != null)
            members.Add(MethodDeclarationHelper.CreateContextProfiledRunMethodWithBody(
                SyntaxFactory.Block(SyntaxFactory.ParseStatement($"return {contextProfiledMethodCallExpression};"))));
    }

    /// <summary>
    ///     Adds the standard table runnable members (properties and Run method) with a custom run body.
    /// </summary>
    public static void AddRunnableMembers(
        IList<SyntaxNode> members,
        BlockSyntax runBody,
        IReadOnlyList<ScriptParameterDefinition>? parameterDefinitions = null)
    {
        ArgumentNullException.ThrowIfNull(members);
        var method = MethodDeclarationHelper.CreateRunMethodWithBody(runBody);
        AddRunnableMembersCore(members, method, parameterDefinitions);
    }

    public static void AddTableViaRowsRunnableMembers(
        IList<SyntaxNode> members,
        string rowsMethodName,
        TableViaRowsResultInfo resultInfo,
        IReadOnlyList<ScriptParameterDefinition>? parameterDefinitions = null,
        bool useLifecycleWrapper = true,
        bool forceTableResultMaterialization = false,
        bool enableContextualExecution = false)
    {
        ArgumentNullException.ThrowIfNull(members);
        ArgumentException.ThrowIfNullOrWhiteSpace(rowsMethodName);
        ArgumentNullException.ThrowIfNull(resultInfo);

        var method = MethodDeclarationHelper.CreateRunMethodWithBody(
            CreateTableViaRowsRunBody(
                rowsMethodName,
                resultInfo,
                null,
                forceTableResultMaterialization,
                useContext: enableContextualExecution,
                createContext: enableContextualExecution));

        AddRunnableMembersCore(members, method, parameterDefinitions);
        if (enableContextualExecution)
            members.Add(MethodDeclarationHelper.CreateContextRunMethodWithBody(
                CreateTableViaRowsRunBody(
                    rowsMethodName,
                    resultInfo,
                    null,
                    forceTableResultMaterialization,
                    useContext: true,
                    createContext: false)));
    }

    public static void AddProfiledTableViaRowsRunnableMembers(
        IList<SyntaxNode> members,
        string rowsMethodName,
        string profiledRowsMethodName,
        TableViaRowsResultInfo resultInfo,
        IReadOnlyList<ScriptParameterDefinition>? parameterDefinitions = null,
        bool forceTableResultMaterialization = false,
        bool enableContextualExecution = false)
    {
        ArgumentNullException.ThrowIfNull(members);
        ArgumentException.ThrowIfNullOrWhiteSpace(rowsMethodName);
        ArgumentNullException.ThrowIfNull(resultInfo);

        var method = MethodDeclarationHelper.CreateRunMethodWithBody(
            CreateTableViaRowsRunBody(
                rowsMethodName,
                resultInfo,
                null,
                forceTableResultMaterialization,
                useContext: enableContextualExecution,
                createContext: enableContextualExecution));
        AddRunnableMembersCore(members, method, parameterDefinitions);
        if (enableContextualExecution)
            members.Add(MethodDeclarationHelper.CreateContextRunMethodWithBody(
                CreateTableViaRowsRunBody(
                    rowsMethodName,
                    resultInfo,
                    null,
                    forceTableResultMaterialization,
                    useContext: true,
                    createContext: false)));

        members.Add(CreateProfiledTableViaRowsRunMethod(
            profiledRowsMethodName,
            resultInfo,
            forceTableResultMaterialization,
            useContext: enableContextualExecution,
            createContext: enableContextualExecution,
            methodUsesContext: false));
        if (enableContextualExecution)
            members.Add(CreateProfiledTableViaRowsRunMethod(
                profiledRowsMethodName,
                resultInfo,
                forceTableResultMaterialization,
                useContext: true,
                createContext: false,
                methodUsesContext: true));
    }

    private static MethodDeclarationSyntax CreateProfiledTableViaRowsRunMethod(
        string rowsMethodName,
        TableViaRowsResultInfo resultInfo,
        bool forceTableResultMaterialization,
        bool useContext,
        bool createContext,
        bool methodUsesContext)
    {
        var statements = new List<StatementSyntax>
        {
            SyntaxFactory.ParseStatement("ArgumentNullException.ThrowIfNull(profileRecorder);")
        };
        statements.AddRange(CreateTableViaRowsRunBody(
            rowsMethodName,
            resultInfo,
            SyntaxFactory.IdentifierName("profileRecorder"),
            forceTableResultMaterialization,
            useContext,
            createContext).Statements);

        var parameterList = methodUsesContext
            ? SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(
            [
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("queryContext"))
                    .WithType(SyntaxFactory.IdentifierName(nameof(QueryRunContext))),
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("profileRecorder"))
                    .WithType(SyntaxFactory.IdentifierName("QueryProfileRecorder"))
            ]))
            : SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(
            [
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("token"))
                    .WithType(SyntaxFactory.IdentifierName(nameof(CancellationToken))),
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("profileRecorder"))
                    .WithType(SyntaxFactory.IdentifierName("QueryProfileRecorder"))
            ]));

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.IdentifierName(nameof(Table)),
                SyntaxFactory.Identifier(nameof(IProfiledRunnable.RunWithProfile)))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(parameterList)
            .WithBody(SyntaxFactory.Block(statements));
    }

    private static BlockSyntax CreateTableViaRowsRunBody(
        string rowsMethodName,
        TableViaRowsResultInfo resultInfo,
        ExpressionSyntax? profileRecorderArgument,
        bool forceTableResultMaterialization,
        bool useContext,
        bool createContext)
    {
        var tableExpression = CreateDeferredTableExpression(rowsMethodName, resultInfo, profileRecorderArgument, useContext);
        var statements = new List<StatementSyntax>();
        if (createContext)
        {
            statements.Add(SyntaxFactory.ParseStatement(
                "var queryContext = new QueryRunContext(token, Parameters, PhaseChanged, DataSourceProgress, this, queryId: \"compiled\", provider: Provider, sourceRuntimeSettingsBySourceContextId: SourceRuntimeSettingsBySourceContextId, sourceRuntimeSettingDescriptionsBySourceContextId: SourceRuntimeSettingDescriptionsBySourceContextId, sourceExecutionPlans: SourceExecutionPlans, logger: Logger, queryProgress: QueryProgress);"));
        }

        if (!forceTableResultMaterialization)
        {
            statements.Add(SyntaxFactory.ReturnStatement(tableExpression));
            return SyntaxFactory.Block(statements);
        }

        const string materializedTableName = "__musoqMaterializedTable";
        statements.Add(SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                    .WithVariables(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(materializedTableName))
                            .WithInitializer(SyntaxFactory.EqualsValueClause(tableExpression))))));
        statements.Add(SyntaxFactory.ParseStatement($"_ = {materializedTableName}.Count;"));
        statements.Add(SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(materializedTableName)));
        return SyntaxFactory.Block(statements);
    }

    private static InvocationExpressionSyntax CreateDeferredTableExpression(
        string rowsMethodName,
        TableViaRowsResultInfo resultInfo,
        ExpressionSyntax? profileRecorderArgument,
        bool useContext)
    {
        ExpressionSyntax columns = resultInfo.ColumnsFieldName != null
            ? SyntaxFactory.IdentifierName(resultInfo.ColumnsFieldName)
            : LegacyCodeGenerationSyntaxFactory.CreateArrayCreation(
                nameof(Column),
                resultInfo.Columns.Select(LegacyCodeGenerationSyntaxFactory.CreateColumnCreation));
        var rowsArguments = new List<ArgumentSyntax>
        {
            SyntaxFactory.Argument(useContext
                ? SyntaxFactory.ParseExpression("queryContext.Provider!")
                : SyntaxFactory.IdentifierName(nameof(IQueryRunnable.Provider))),
            SyntaxFactory.Argument(useContext
                ? SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("queryContext"),
                    SyntaxFactory.IdentifierName(nameof(QueryRunContext.SourceRuntimeSettingsBySourceContextId)))
                : SyntaxFactory.IdentifierName(nameof(IQueryRunnable.SourceRuntimeSettingsBySourceContextId))),
            SyntaxFactory.Argument(useContext
                ? SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("queryContext"),
                    SyntaxFactory.IdentifierName(nameof(QueryRunContext.SourceExecutionPlans)))
                : SyntaxFactory.IdentifierName(nameof(IQueryRunnable.SourceExecutionPlans))),
            SyntaxFactory.Argument(useContext
                ? SyntaxFactory.ParseExpression("queryContext.Logger!")
                : SyntaxFactory.IdentifierName(nameof(IQueryRunnable.Logger))),
            SyntaxFactory.Argument(useContext
                ? SyntaxFactory.IdentifierName("queryContext")
                : SyntaxFactory.IdentifierName("queryToken"))
        };
        if (profileRecorderArgument != null)
            rowsArguments.Add(SyntaxFactory.Argument(profileRecorderArgument));

        var rowsCall = SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(rowsMethodName))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(rowsArguments)));
        var rowsFactory = SyntaxFactory.ParenthesizedLambdaExpression()
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("queryToken")))))
            .WithExpressionBody(rowsCall);

        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(QueryRows)),
                    SyntaxFactory.GenericName(nameof(QueryRows.DeferredTable))
                        .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                SyntaxFactory.ParseTypeName(resultInfo.RowTypeName))))))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
            [
                SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(resultInfo.TableName))),
                SyntaxFactory.Argument(columns),
                SyntaxFactory.Argument(rowsFactory),
                SyntaxFactory.Argument(useContext
                    ? SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("queryContext"),
                        SyntaxFactory.IdentifierName(nameof(QueryRunContext.CancellationToken)))
                    : SyntaxFactory.IdentifierName("token"))
            ])));
    }

    public static void AddTypedRunnableMembers(
        IList<SyntaxNode> members,
        string rowsMethodName,
        Type outputType,
        IReadOnlyList<ScriptParameterDefinition>? parameterDefinitions = null)
    {
        ArgumentNullException.ThrowIfNull(members);
        ArgumentException.ThrowIfNullOrWhiteSpace(rowsMethodName);
        ArgumentNullException.ThrowIfNull(outputType);

        const string initialContextName = "__musoqInitialRunContext";
        const string enumerationContextName = "__musoqEnumerationRunContext";
        const string localContextName = "queryContext";
        var rowsFactoryBody = SyntaxFactory.Block(
            CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                localContextName,
                CreatePerEnumerationRunContext(initialContextName)),
            SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(enumerationContextName),
                    SyntaxFactory.IdentifierName(localContextName))),
            SyntaxFactory.ReturnStatement(
                SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(rowsMethodName))
                    .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
                    [
                        SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(initialContextName),
                            SyntaxFactory.IdentifierName(nameof(QueryRunContext.Provider)))),
                        SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(initialContextName),
                            SyntaxFactory.IdentifierName(nameof(QueryRunContext.SourceRuntimeSettingsBySourceContextId)))),
                        SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(initialContextName),
                            SyntaxFactory.IdentifierName(nameof(QueryRunContext.SourceExecutionPlans)))),
                        SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(initialContextName),
                            SyntaxFactory.IdentifierName(nameof(QueryRunContext.Logger)))),
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName(localContextName))
                    ])))));
        var rowsFactory = SyntaxFactory.ParenthesizedLambdaExpression()
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("queryToken")))))
            .WithBody(rowsFactoryBody);
        var rowEnumerable = SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.GenericName(nameof(QueryEnumerable<object>))
                    .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                            LegacyCodeGenerationSyntaxFactory.CreateTypeSyntax(outputType)))))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
            [
                SyntaxFactory.Argument(rowsFactory),
                SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("options"),
                    SyntaxFactory.IdentifierName(nameof(TypedQueryRunOptions.CancellationToken)))),
                SyntaxFactory.Argument(SyntaxFactory.ParseExpression(
                        $"{initialContextName}.{nameof(QueryRunContext.QueryProgress)} == null ? null : " +
                        $"(Action)(() => {enumerationContextName}?.{nameof(QueryRunContext.CompleteQueryProgress)}())"))
                    .WithNameColon(SyntaxFactory.NameColon("onCompleted")),
                SyntaxFactory.Argument(SyntaxFactory.ParseExpression(
                        $"{initialContextName}.{nameof(QueryRunContext.QueryProgress)} == null ? null : " +
                        $"(Action<Exception>)(_ => {enumerationContextName}?.{nameof(QueryRunContext.CompleteQueryProgress)}())"))
                    .WithNameColon(SyntaxFactory.NameColon("onException")),
                SyntaxFactory.Argument(SyntaxFactory.ParseExpression(
                        $"{initialContextName}.{nameof(QueryRunContext.QueryProgress)} == null ? null : " +
                        $"(Action)(() => {enumerationContextName}?.{nameof(QueryRunContext.CompleteQueryProgress)}())"))
                    .WithNameColon(SyntaxFactory.NameColon("onDisposed"))
            ])));
        var body = SyntaxFactory.Block(
            SyntaxFactory.ParseStatement("ArgumentNullException.ThrowIfNull(options);"),
            CreateLocalDeclaration(
                SyntaxFactory.ParseTypeName("QueryRunContext?"),
                enumerationContextName,
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
            CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                initialContextName,
                SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(nameof(QueryRunContext)),
                            SyntaxFactory.IdentifierName(nameof(QueryRunContext.Capture))))
                    .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
                    [
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("options")),
                        SyntaxFactory.Argument(SyntaxFactory.ThisExpression())
                    ])))),
            SyntaxFactory.ReturnStatement(rowEnumerable));
        var runOptionsMethod = MethodDeclarationHelper.CreateTypedRunOptionsMethodWithBody(outputType, body);
        var runTokenMethod = MethodDeclarationHelper.CreateTypedRunTokenShim(outputType);

        AddRunnableMembersCore(members, [runOptionsMethod, runTokenMethod], parameterDefinitions);
    }

    private static void AddRunnableMembersCore(
        IList<SyntaxNode> members,
        MethodDeclarationSyntax runMethod,
        IReadOnlyList<ScriptParameterDefinition>? parameterDefinitions)
    {
        AddRunnableMembersCore(members, [runMethod], parameterDefinitions);
    }

    private static void AddRunnableMembersCore(
        IList<SyntaxNode> members,
        IReadOnlyList<MethodDeclarationSyntax> runMethods,
        IReadOnlyList<ScriptParameterDefinition>? parameterDefinitions)
    {
        var providerParam =
            MethodDeclarationHelper.CreatePublicProperty(nameof(ISchemaProvider), nameof(IQueryRunnable.Provider));
        var sourceRuntimeSettingsParam =
            MethodDeclarationHelper.CreateSourceRuntimeSettingsBySourceContextIdProperty();
        var sourceRuntimeSettingDescriptionsParam =
            MethodDeclarationHelper.CreateSourceRuntimeSettingDescriptionsBySourceContextIdProperty();
        var sourceExecutionPlansParam = MethodDeclarationHelper.CreateSourceExecutionPlansProperty();
        var loggerParam = MethodDeclarationHelper.CreatePublicProperty(nameof(ILogger), nameof(IQueryRunnable.Logger));
        var parametersParam = CreateParametersProperty();
        var parameterDefinitionsParam = CreateParameterDefinitionsProperty(parameterDefinitions);
        var parameterContractsParam = CreateParameterContractsProperty(parameterDefinitions);
        var phaseChangedEvent = MethodDeclarationHelper.CreatePhaseChangedEvent();
        var onPhaseChangedMethod = MethodDeclarationHelper.CreateOnPhaseChangedMethod();
        var dataSourceProgressEvent = MethodDeclarationHelper.CreateDataSourceProgressEvent();
        var onDataSourceProgressMethod = MethodDeclarationHelper.CreateOnDataSourceProgressMethod();
        var queryProgressEvent = MethodDeclarationHelper.CreateQueryProgressEvent();

        foreach (var runMethod in runMethods)
            members.Add(runMethod);
        members.Add(providerParam);
        members.Add(sourceRuntimeSettingsParam);
        members.Add(sourceRuntimeSettingDescriptionsParam);
        members.Add(sourceExecutionPlansParam);
        members.Add(loggerParam);
        members.Add(parametersParam);
        members.Add(parameterDefinitionsParam);
        members.Add(parameterContractsParam);
        members.Add(phaseChangedEvent);
        members.Add(onPhaseChangedMethod);
        members.Add(dataSourceProgressEvent);
        members.Add(onDataSourceProgressMethod);
        members.Add(queryProgressEvent);
    }

    private static ObjectCreationExpressionSyntax CreatePerEnumerationRunContext(string initialContextName)
    {
        return SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(nameof(QueryRunContext)))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
            [
                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("queryToken")),
                SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(initialContextName),
                    SyntaxFactory.IdentifierName(nameof(QueryRunContext.RuntimeParameters)))),
                SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(initialContextName),
                    SyntaxFactory.IdentifierName(nameof(QueryRunContext.PhaseChanged)))),
                SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(initialContextName),
                    SyntaxFactory.IdentifierName(nameof(QueryRunContext.DataSourceProgress)))),
                SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(initialContextName),
                    SyntaxFactory.IdentifierName(nameof(QueryRunContext.Sender)))),
                SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(initialContextName),
                    SyntaxFactory.IdentifierName(nameof(QueryRunContext.QueryId))))
                    .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("queryId"))),
                SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(initialContextName),
                    SyntaxFactory.IdentifierName(nameof(QueryRunContext.QueryProgress))))
                    .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("queryProgress"))),
                SyntaxFactory.Argument(SyntaxFactory.ParseExpression(
                        $"{initialContextName}.{nameof(QueryRunContext.QueryProgress)} == null ? null : " +
                        $"{initialContextName}.{nameof(QueryRunContext.QueryProgressOptions)}"))
                    .WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("queryProgressOptions")))
            ])));
    }

    private static LocalDeclarationStatementSyntax CreateLocalDeclaration(
        TypeSyntax type,
        string name,
        ExpressionSyntax initializer)
    {
        return SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(type)
                .WithVariables(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(name))
                        .WithInitializer(SyntaxFactory.EqualsValueClause(initializer)))));
    }

    private static PropertyDeclarationSyntax CreateParametersProperty()
    {
        return SyntaxFactory.PropertyDeclaration(
                SyntaxFactory.ParseTypeName("IDictionary<string, System.Object>"),
                nameof(IParameterizedRunnable.Parameters))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithAccessorList(
                SyntaxFactory.AccessorList(
                    SyntaxFactory.SingletonList(
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))))
            .WithInitializer(
                SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.ParseExpression("new Dictionary<string, System.Object>(StringComparer.Ordinal)")))
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
    }

    private static PropertyDeclarationSyntax CreateParameterDefinitionsProperty(
        IReadOnlyList<ScriptParameterDefinition>? parameterDefinitions)
    {
        return SyntaxFactory.PropertyDeclaration(
                SyntaxFactory.ParseTypeName("IReadOnlyList<ScriptParameterDefinition>"),
                nameof(IParameterizedRunnable.ParameterDefinitions))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithAccessorList(
                SyntaxFactory.AccessorList(
                    SyntaxFactory.SingletonList(
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))))
            .WithInitializer(
                SyntaxFactory.EqualsValueClause(
                    ScriptParameterSyntaxFactory.CreateDefinitionsInitializer(parameterDefinitions)))
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
    }

    private static PropertyDeclarationSyntax CreateParameterContractsProperty(
        IReadOnlyList<ScriptParameterDefinition>? parameterDefinitions)
    {
        return SyntaxFactory.PropertyDeclaration(
                SyntaxFactory.ParseTypeName("IReadOnlyList<ScriptParameterContract>"),
                nameof(IParameterizedRunnable.ParameterContracts))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithAccessorList(
                SyntaxFactory.AccessorList(
                    SyntaxFactory.SingletonList(
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))))
            .WithInitializer(
                SyntaxFactory.EqualsValueClause(
                    ScriptParameterSyntaxFactory.CreateContractsInitializer(parameterDefinitions)))
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
    }
}

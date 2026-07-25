using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Utils;
using Musoq.Targets.CSharpClr.Rendering.CodeGeneration;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class CompiledQueryClassRendererTests : IDisposable
{
    private AdhocWorkspace? _workspace;
    private SyntaxGenerator _generator = null!;

    [TestInitialize]
    public void Initialize()
    {
        _workspace = new AdhocWorkspace();
        _generator = SyntaxGenerator.GetGenerator(_workspace, LanguageNames.CSharp);
    }

    [TestCleanup]
    public void Cleanup()
    {
        DisposeWorkspace();
    }

    public void Dispose()
    {
        DisposeWorkspace();
        GC.SuppressFinalize(this);
    }

    private void DisposeWorkspace()
    {
        _workspace?.Dispose();
        _workspace = null;
    }

    [TestMethod]
    public void WhenRenderingClassShell_ShouldContainCompiledQueryBoilerplate()
    {
        var context = new RenderContext(
            _generator,
            new RenderContextOptions(AssemblyName: "Query.Compiled_Test"));
        var renderer = new CompiledQueryClassRenderer(context);

        var result = renderer.Render("ab");
        var code = Normalize(result);

        StringAssert.Contains(code, "namespace Query.Compiled_Test");
        StringAssert.Contains(code, "using System.Threading;");
        StringAssert.Contains(code, "using Musoq.Evaluator.Tables;");
        StringAssert.Contains(code, "public sealed class CompiledQuery : BaseOperations, ITableRunnable, IParameterizedRunnable");
        Assert.IsFalse(code.Contains("_tableResults", StringComparison.Ordinal));
        StringAssert.Contains(code, "public IDictionary<string, System.Object> Parameters { get; } = new Dictionary<string, System.Object>(StringComparer.Ordinal);");
        StringAssert.Contains(code, "public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = Array.Empty<ScriptParameterDefinition>();");
        StringAssert.Contains(code, "public event QueryPhaseEventHandler PhaseChanged;");
        StringAssert.Contains(code, "public event DataSourceEventHandler DataSourceProgress;");
        StringAssert.Contains(code, "public Table Run(CancellationToken token)");
        StringAssert.Contains(code, "return ComputeTable_ab_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, token);");
        StringAssert.Contains(code, "private Table ComputeTable_ab_0(");
        StringAssert.Contains(code, "throw new NotImplementedException();");
    }

    [TestMethod]
    public void WhenContextualExecutionIsEnabled_ShouldEmitPerRunContract()
    {
        var context = new RenderContext(
            _generator,
            new RenderContextOptions(
                AssemblyName: "Query.Compiled_Test",
                EnableContextualExecution: true));
        var renderer = new CompiledQueryClassRenderer(context);

        var code = Normalize(renderer.Render("ab"));

        StringAssert.Contains(code, "ITableRunnable, IContextTableRunnable, IParameterizedRunnable");
        StringAssert.Contains(code, "public Table Run(QueryRunContext queryContext)");
    }

    [TestMethod]
    public void WhenRenderingClassShell_ShouldOrderMembersByVisibilityGroup()
    {
        var context = new RenderContext(
            _generator,
            new RenderContextOptions(AssemblyName: "Query.Compiled_Test"));
        context.AddClassMember(SyntaxFactory.ParseMemberDeclaration("private static void StaticHelper() { }")!);
        context.AddClassMember(SyntaxFactory.ParseMemberDeclaration("protected void ProtectedHook() { }")!);
        context.AddClassMember(SyntaxFactory.ParseMemberDeclaration("private sealed class Nested { }")!);
        var renderer = new CompiledQueryClassRenderer(context);

        var result = renderer.Render("ab");
        var code = Normalize(result);

        AssertFragmentsInOrder(
            code,
            "public ISchemaProvider Provider",
            "public event QueryPhaseEventHandler PhaseChanged",
            "public Table Run(CancellationToken token)",
            "protected void ProtectedHook()",
            "private Table ComputeTable_ab_0(",
            "private static void StaticHelper()",
            "private sealed class Nested");
    }

    [TestMethod]
    public void WhenRenderingClassShellWithScriptParameters_ShouldContainParameterDefinitions()
    {
        var context = new RenderContext(
            _generator,
            new RenderContextOptions(
                AssemblyName: "Query.Compiled_Test",
                ScriptParameterDefinitions:
                [
                    new ScriptParameterDefinition("author", typeof(string), false, null),
                    new ScriptParameterDefinition("limit", typeof(int), true, 100)
                ]));
        var renderer = new CompiledQueryClassRenderer(context);

        var result = renderer.Render("ab");
        var code = Normalize(result);

        StringAssert.Contains(code, "public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = new ScriptParameterDefinition[]");
        StringAssert.Contains(code, "public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = new ScriptParameterContract[]");
        StringAssert.Contains(code, "new ScriptParameterDefinition(new ScriptParameterContract(\"author\", \"string\", \"string\", typeof(string), false, false, null, null, false, ScriptParameterDefaultKind.None, null))");
        StringAssert.Contains(code, "new ScriptParameterDefinition(new ScriptParameterContract(\"limit\", \"int\", \"int\", typeof(int), false, false, null, null, true, ScriptParameterDefaultKind.Literal, 100))");
        StringAssert.Contains(code, "new ScriptParameterContract(\"author\", \"string\", \"string\", typeof(string), false, false, null, null, false, ScriptParameterDefaultKind.None, null)");
        StringAssert.Contains(code, "new ScriptParameterContract(\"limit\", \"int\", \"int\", typeof(int), false, false, null, null, true, ScriptParameterDefaultKind.Literal, 100)");
    }

    [TestMethod]
    public void WhenRenderingClassShellWithPrimitiveScriptParameterDefaults_ShouldEmitStableLiterals()
    {
        var guid = Guid.Parse("2ffcf6fa-3369-4300-946a-bb131a037985");
        var dateTime = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var dateTimeOffset = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.FromHours(2));
        var timeSpan = TimeSpan.FromMinutes(90);
        var context = new RenderContext(
            _generator,
            new RenderContextOptions(
                AssemblyName: "Query.Compiled_Test",
                ScriptParameterDefinitions:
                [
                    new ScriptParameterDefinition("code", typeof(char), true, 'x'),
                    new ScriptParameterDefinition("id", typeof(Guid), true, guid),
                    new ScriptParameterDefinition("created", typeof(DateTime), true, dateTime),
                    new ScriptParameterDefinition("seen", typeof(DateTimeOffset), true, dateTimeOffset),
                    new ScriptParameterDefinition("elapsed", typeof(TimeSpan), true, timeSpan)
                ]));
        var renderer = new CompiledQueryClassRenderer(context);

        var result = renderer.Render("ab");
        var code = Normalize(result);

        StringAssert.Contains(code, "new ScriptParameterDefinition(new ScriptParameterContract(\"code\", \"char\", \"char\", typeof(char), false, false, null, null, true, ScriptParameterDefaultKind.Literal, 'x'))");
        StringAssert.Contains(code, "new ScriptParameterDefinition(new ScriptParameterContract(\"id\", \"guid\", \"guid\", typeof(Guid), false, false, null, null, true, ScriptParameterDefaultKind.Literal, new Guid(\"2ffcf6fa-3369-4300-946a-bb131a037985\")))");
        StringAssert.Contains(code, $"new ScriptParameterDefinition(new ScriptParameterContract(\"created\", \"datetime\", \"datetime\", typeof(DateTime), false, false, null, null, true, ScriptParameterDefaultKind.Literal, new DateTime({dateTime.Ticks}L, DateTimeKind.Utc)))");
        StringAssert.Contains(code, $"new ScriptParameterDefinition(new ScriptParameterContract(\"seen\", \"datetimeoffset\", \"datetimeoffset\", typeof(DateTimeOffset), false, false, null, null, true, ScriptParameterDefaultKind.Literal, new DateTimeOffset({dateTimeOffset.Ticks}L, new TimeSpan({dateTimeOffset.Offset.Ticks}L))))");
        StringAssert.Contains(code, $"new ScriptParameterDefinition(new ScriptParameterContract(\"elapsed\", \"timespan\", \"timespan\", typeof(TimeSpan), false, false, null, null, true, ScriptParameterDefaultKind.Literal, new TimeSpan({timeSpan.Ticks}L)))");
    }

    [TestMethod]
    public void WhenScopeContainsMethodName_ShouldReuseScopeMethodName()
    {
        var scope = new Scope(null, 0, "query") { [MetaAttributes.MethodName] = "ComputeTable_scope_7" };

        var context = new RenderContext(
            _generator,
            new RenderContextOptions(
                Scope: scope,
                AssemblyName: "Query.Compiled_Test"));
        var renderer = new CompiledQueryClassRenderer(context);

        var result = renderer.Render("ab");
        var code = Normalize(result);

        StringAssert.Contains(code, "return ComputeTable_scope_7(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, token);");
        StringAssert.Contains(code, "private Table ComputeTable_scope_7(");
        Assert.IsFalse(code.Contains("ComputeTable_ab_0", StringComparison.Ordinal));
    }

    [TestMethod]
    public void WhenComputeMethodAlreadyExists_ShouldNotAddPlaceholderMethod()
    {
        var scope = new Scope(null, 0, "query") { [MetaAttributes.MethodName] = "ComputeTable_existing_2" };

        var context = new RenderContext(
            _generator,
            new RenderContextOptions(
                Scope: scope,
                AssemblyName: "Query.Compiled_Test"));
        context.AddClassMember(CreateExistingComputeMethod("ComputeTable_existing_2"));

        var renderer = new CompiledQueryClassRenderer(context);

        var result = renderer.Render("ab");
        var code = Normalize(result);

        Assert.AreEqual(1, CountOccurrences(code, "private Table ComputeTable_existing_2("));
        StringAssert.Contains(code, "throw new InvalidOperationException();");
    }

    [TestMethod]
    public void WhenUsingCSharpRendererFacade_ShouldRenderCompilationUnit()
    {
        var context = new RenderContext(
            _generator,
            new RenderContextOptions(AssemblyName: "Query.Compiled_Test"));
        var renderer = new CSharpRenderer(context);

        var result = renderer.RenderCompilationUnit("facade");
        var code = Normalize(result);

        StringAssert.Contains(code, "namespace Query.Compiled_Test");
        StringAssert.Contains(code, "ComputeTable_facade_0");
    }

    private static MethodDeclarationSyntax CreateExistingComputeMethod(string methodName)
    {
        var exception = SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.IdentifierName(nameof(InvalidOperationException)))
            .WithArgumentList(SyntaxFactory.ArgumentList());
        var body = StatementEmitter.CreateBlock(StatementEmitter.CreateThrow(exception));

        return MethodDeclarationHelper.CreateStandardPrivateMethod(methodName, body);
    }

    private static string Normalize(SyntaxNode syntax)
    {
        return syntax.NormalizeWhitespace().ToFullString();
    }

    private static int CountOccurrences(string value, string fragment)
    {
        var count = 0;
        var index = 0;

        while ((index = value.IndexOf(fragment, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += fragment.Length;
        }

        return count;
    }

    private static void AssertFragmentsInOrder(string value, params string[] fragments)
    {
        var previousIndex = -1;

        foreach (var fragment in fragments)
        {
            var index = value.IndexOf(fragment, StringComparison.Ordinal);
            Assert.IsGreaterThanOrEqualTo(0, index, $"Missing fragment '{fragment}'.");
            Assert.IsGreaterThan(previousIndex, index, $"Fragment '{fragment}' was not in the expected order.");
            previousIndex = index;
        }
    }
}

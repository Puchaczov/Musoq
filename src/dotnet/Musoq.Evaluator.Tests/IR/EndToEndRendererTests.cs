using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Build;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.Runtime;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Targets.CSharpClr.Rendering.CodeGeneration;
using Musoq.Schema;
using Microsoft.CodeAnalysis.Editing;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public class EndToEndRendererTests
{
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);
    private readonly ILoggerResolver _loggerResolver = new TestsLoggerResolver();

    [TestMethod]
    public void WhenSimpleSelectWhere_IrRendererShouldProduceSameResultsAsExistingPipeline()
    {
        const string query = "select Name from #A.Entities() where Population > 100";
        var sources = CreateSources(
            new BasicEntity("NYC", 200),
            new BasicEntity("LA", 50),
            new BasicEntity("CHI", 150));

        AssertIrRendererProducesSameResults(query, sources);
    }

    [TestMethod]
    public void WhenSelectWithArithmetic_IrRendererShouldProduceSameResultsAsExistingPipeline()
    {
        const string query = "select Name, Population + 10 from #A.Entities()";
        var sources = CreateSources(
            new BasicEntity("NYC", 200),
            new BasicEntity("LA", 50));

        AssertIrRendererProducesSameResults(query, sources);
    }

    [TestMethod]
    public void WhenSelectWithLiterals_IrRendererShouldProduceSameResultsAsExistingPipeline()
    {
        const string query = "select 1, 'hello' from #A.Entities()";
        var sources = CreateSources(new BasicEntity("X", 1));

        AssertIrRendererProducesSameResults(query, sources);
    }

    [TestMethod]
    public void WhenGroupByWithCount_IrRendererShouldProduceSameResultsAsExistingPipeline()
    {
        const string query = "select Country, Count(Country) from #A.Entities() group by Country";
        var sources = CreateSources(
            new BasicEntity("Poland", "Warsaw", 100),
            new BasicEntity("Poland", "Krakow", 50),
            new BasicEntity("Germany", "Berlin", 200));

        AssertIrRendererProducesSameResults(query, sources);
    }

    [TestMethod]
    public void WhenInnerJoin_IrRendererShouldProduceSameResultsAsExistingPipeline()
    {
        const string query = "select a.Name, b.Name from #A.Entities() a inner join #B.Entities() b on a.Country = b.Country";
        var sourcesA = new[] {
            new BasicEntity { Name = "Alice", Country = "PL" },
            new BasicEntity { Name = "Bob", Country = "DE" }
        };
        var sourcesB = new[] {
            new BasicEntity { Name = "Warsaw", Country = "PL" },
            new BasicEntity { Name = "Berlin", Country = "DE" }
        };
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", sourcesA },
            { "#B", sourcesB }
        };

        AssertIrRendererProducesSameResults(query, sources);
    }

    [TestMethod]
    public void WhenUnionAll_IrRendererShouldProduceSameResultsAsExistingPipeline()
    {
        const string query = "select Name from #A.Entities() union all (Name) select Name from #B.Entities()";
        var sourcesA = new[] { new BasicEntity("Alice") };
        var sourcesB = new[] { new BasicEntity("Bob") };
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", sourcesA },
            { "#B", sourcesB }
        };

        AssertIrRendererProducesSameResults(query, sources);
    }

    [TestMethod]
    public void WhenCte_IrRendererShouldProduceSameResultsAsExistingPipeline()
    {
        const string query = "with cte as (select Name from #A.Entities()) select Name from cte";
        var sources = CreateSources(new BasicEntity("Alice"), new BasicEntity("Bob"));

        AssertIrRendererProducesSameResults(query, sources);
    }

    [TestMethod]
    public void WhenWindowFunction_IrRendererShouldProduceSameResultsAsExistingPipeline()
    {
        const string query = "select Name, RowNumber() over (order by Name asc) from #A.Entities()";
        var sources = CreateSources(
            new BasicEntity("Charlie"),
            new BasicEntity("Alice"),
            new BasicEntity("Bob"));

        AssertIrRendererProducesSameResults(query, sources);
    }

    private void AssertIrRendererProducesSameResults(
        string query,
        IDictionary<string, IEnumerable<BasicEntity>> sources)
    {
        var provider = new BasicSchemaProvider<BasicEntity>(sources);

        var baselineTable = RunExistingPipeline(query, provider);

        var irTable = RunIrRendererPipeline(query, provider);

        AssertTablesEqual(baselineTable, irTable);
    }

    private Table RunExistingPipeline(string query, ISchemaProvider provider)
    {
        var compiled = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            provider,
            _loggerResolver,
            TestCompilationOptions);

        return compiled.Run(CancellationToken.None);
    }

    private Table RunIrRendererPipeline(string query, ISchemaProvider provider)
    {
        var assemblyName = $"Query.Compiled_{Guid.NewGuid():N}";

        var buildItems = InstanceCreator.CreateForAnalyze(
            query, assemblyName, provider, _loggerResolver);

        Assert.IsNotNull(buildItems.ExecutionPlan, "ExecutionPlan must not be null for Execution IR rendering.");
        Assert.IsNotNull(buildItems.PipelineScope, "PipelineScope must not be null for IR rendering.");

        using var workspace = new AdhocWorkspace();
        var generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);

        var context = new RenderContext(
            generator,
            new RenderContextOptions(
                Scope: buildItems.PipelineScope,
                AssemblyName: assemblyName));

        var renderer = new CSharpRenderer(context);

        var queryIdentifier = "compiled";

        var queryResult = renderer.TryRenderExecutionQueryMethod(buildItems.ExecutionPlan, queryIdentifier);

        Assert.IsTrue(
            queryResult.IsSupported,
            string.IsNullOrWhiteSpace(queryResult.UnsupportedReason)
                ? "Execution IR renderer did not produce a query method."
                : queryResult.UnsupportedReason);

        context.AddClassMember(queryResult.Method!.Value.MethodDeclaration);

        var unit = renderer.RenderCompilationUnit(
            queryIdentifier,
            CountExecutionTableSlots(buildItems.ExecutionPlan));

        return CompileAndRun(unit, assemblyName, provider, buildItems);
    }

    private Table CompileAndRun(
        CompilationUnitSyntax compilationUnit,
        string assemblyName,
        ISchemaProvider provider,
        BuildItems buildItems)
    {
        var compilation = CreateCompilation(compilationUnit, assemblyName);
        var (dllBytes, _) = EmitAssembly(compilation);

        var irAccessToClassPath = $"{assemblyName}.CompiledQuery";
        var runnable = LoadAndCreateRunnable(
            dllBytes,
            irAccessToClassPath,
            provider,
            buildItems);

        return runnable.Run(CancellationToken.None);
    }

    private static Microsoft.CodeAnalysis.CSharp.CSharpCompilation CreateCompilation(
        CompilationUnitSyntax compilationUnit,
        string assemblyName)
    {
        RuntimeLibraries.CreateReferences();

        var compilationContext = new CompilationContextManager(
            RoslynSharedFactory.CreateCompilation(assemblyName));
        compilationContext.InitializeDefaults();
        compilationContext.InitializeCoreReferences([typeof(EndToEndRendererTests).Assembly]);
        compilationContext.AddSyntaxTree(ClassEmitter.CreateSyntaxTreeDirect(compilationUnit));

        return compilationContext.GetCompilation();
    }

    private static (byte[] Dll, byte[] Pdb) EmitAssembly(
        Microsoft.CodeAnalysis.CSharp.CSharpCompilation compilation)
    {
        using var dllStream = new MemoryStream();

        var result = compilation.Emit(dllStream);

        if (!result.Success)
        {
            var errors = result.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.ToString())
                .ToArray();

            var generatedCode = string.Join("\n",
                compilation.SyntaxTrees.Select(t => t.ToString()));

            Assert.Fail($"IR-rendered code failed to compile:\n{string.Join("\n", errors)}\n\nGenerated code:\n{generatedCode}");
        }

        if (!dllStream.TryGetBuffer(out var dllBuffer))
            dllBuffer = new ArraySegment<byte>(dllStream.ToArray());

        return (dllBuffer.ToArray(), []);
    }

    private static ITableRunnable LoadAndCreateRunnable(
        byte[] dllBytes,
        string accessToClassPath,
        ISchemaProvider provider,
        BuildItems buildItems)
    {
        var assembly = Assembly.Load(dllBytes);
        var type = assembly.GetType(accessToClassPath)
            ?? throw new InvalidOperationException(
                $"Type {accessToClassPath} not found in IR-rendered assembly.");

        var runnable = (ITableRunnable)(Activator.CreateInstance(type)
            ?? throw new InvalidOperationException(
                $"Could not create instance of {type.FullName}."));

        runnable.Provider = provider;
        runnable.SourceRuntimeSettingsBySourceContextId = buildItems.SourceRuntimeSettingsBySourceContextId;
        runnable.SourceRuntimeSettingDescriptionsBySourceContextId =
            buildItems.SourceRuntimeSettingDescriptionsBySourceContextId;

        runnable.SourceExecutionPlans = buildItems.SourcePlanRequestsPerSchema.ToDictionary(
            f => f.Key.Id,
            f => SourceExecutionPlan.Empty(f.Value.Identity));

        return runnable;
    }

    private static void AssertTablesEqual(Table expected, Table actual)
    {
        Assert.AreEqual(expected.Count, actual.Count,
            $"Row count mismatch: expected {expected.Count}, got {actual.Count}");

        var expectedColumns = expected.Columns.ToList();
        var actualColumns = actual.Columns.ToList();

        Assert.HasCount(expectedColumns.Count, actualColumns,
            $"Column count mismatch: expected {expectedColumns.Count}, got {actualColumns.Count}");

        for (var col = 0; col < expectedColumns.Count; col++)
        {
            Assert.AreEqual(expectedColumns[col].ColumnName, actualColumns[col].ColumnName,
                $"Column {col} name mismatch");
            Assert.AreEqual(expectedColumns[col].ColumnType, actualColumns[col].ColumnType,
                $"Column {col} type mismatch for '{expectedColumns[col].ColumnName}'");
        }

        var expectedRows = expected
            .Select(row => Enumerable.Range(0, expectedColumns.Count).Select(i => row[i]).ToArray())
            .OrderBy(row => string.Join("|", row.Select(v => v.ToString() ?? "NULL")))
            .ToList();

        var actualRows = actual
            .Select(row => Enumerable.Range(0, actualColumns.Count).Select(i => row[i]).ToArray())
            .OrderBy(row => string.Join("|", row.Select(v => v.ToString() ?? "NULL")))
            .ToList();

        for (var row = 0; row < expectedRows.Count; row++)
        {
            for (var col = 0; col < expectedColumns.Count; col++)
            {
                Assert.AreEqual(expectedRows[row][col], actualRows[row][col],
                    $"Value mismatch at sorted row {row}, column '{expectedColumns[col].ColumnName}': " +
                    $"expected '{expectedRows[row][col]}', got '{actualRows[row][col]}'");
            }
        }
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateSources(
        params BasicEntity[] entities)
    {
        return new Dictionary<string, IEnumerable<BasicEntity>> { { "#A", entities } };
    }

    private static int CountExecutionTableSlots(ExecutionPlan executionPlan)
    {
        return FindMaxExecutionTableIndex(executionPlan.Body) + 1;
    }

    private static int FindMaxExecutionTableIndex(ExecutionBlock block)
    {
        var maxIndex = -1;

        foreach (var node in block.Nodes)
            maxIndex = Math.Max(maxIndex, FindMaxExecutionTableIndex(node));

        return maxIndex;
    }

    private static int FindMaxExecutionTableIndex(ExecutionNode node)
    {
        return node switch
        {
            ExecutionStoreTable storeTable => storeTable.TableIndex,
            ExecutionForEach forEach => FindMaxExecutionTableIndex(forEach.Body),
            ExecutionForEachIndexed forEach => FindMaxExecutionTableIndex(forEach.Body),
            ExecutionIf branch => FindMaxExecutionTableIndex(branch.Body),
            _ => -1
        };
    }
}

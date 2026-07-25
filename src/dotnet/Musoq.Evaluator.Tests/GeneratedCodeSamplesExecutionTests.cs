using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.RuntimeV2;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class GeneratedCodeSamplesExecutionTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    public static IEnumerable<object[]> RecursiveSampleData => RecursiveCteSupportedCaseCatalog.Cases
        .Where(static item => item.GeneratedSampleName != null)
        .Select(static item => new object[] { item });

    [TestMethod]
    public void OrdinaryCteColumnListSample_WhenExecuted_ShouldReturnExportedColumnsAndRows()
    {
        var provider = CreateBasicProvider(
        [
            new BasicEntity { City = "Warsaw", Country = "PL" },
            new BasicEntity { City = "Berlin", Country = "DE" }
        ], []);
        var table = CompileSample("Q187_CteColumnListOrdinary.cs", provider)
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Nation", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Warsaw", "PL"],
            ["Berlin", "DE"]);
    }

    [TestMethod]
    [DynamicData(nameof(RecursiveSampleData))]
    public void RecursiveSample_WhenExecuted_ShouldReturnItsCatalogResult(RecursiveCteSupportedCase testCase)
    {
        var vm = CompileSample(
            $"{testCase.GeneratedSampleName}.cs",
            testCase.CreateSchemaProvider?.Invoke() ?? CreateBasicProvider([], []));
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            testCase.ExpectedColumns.Select(static column => (column.Name, column.ClrType)).ToArray());
        if (testCase.Ordered)
            TableMaterializationTestHelper.AssertRowsInOrder(table, testCase.ExpectedRows.ToArray());
        else
            TableMaterializationTestHelper.AssertRowsUnordered(table, testCase.ExpectedRows.ToArray());
    }

    [TestMethod]
    public void ScriptParameterGroupByHelperCaptureSample_WhenExecutedWithLargeSource_ShouldUseRuntimeParameters()
    {
        var vm = CompileSample(
            "Q123_ScriptParameterGroupByHelperCapture.cs",
            CreateBasicProvider(CreateLargeCountryRows(), []));
        vm.Parameters["suffix"] = "-large";
        vm.Parameters["minCount"] = 2500;

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("CountryKey", typeof(string)),
            ("NameCount", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["PL-large", 2500L],
            ["DE-large", 2500L]);
    }

    [TestMethod]
    public void ScriptParameterJoinHelperCaptureSample_WhenExecuted_ShouldUseRuntimeParameters()
    {
        var vm = CompileSample(
            "Q124_ScriptParameterJoinHelperCapture.cs",
            CreateBasicProvider(CreateBasicRows(), CreateTargetRows()));
        vm.Parameters["suffix"] = "-match";
        vm.Parameters["fallback"] = "missing";

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("MatchedName", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "TargetWarsaw-match"],
            ["Bob", "TargetBerlin-match"],
            ["Cara", "-match"],
            ["Dora", "-match"]);
    }

    [TestMethod]
    public void InnerJoinSample_WhenTokenIsCanceledBeforeHashHelperRuns_ShouldThrow()
    {
        var vm = CompileSample(
            "Q03_InnerJoin.cs",
            CreateBasicProvider(CreateLargeCountryRows(), CreateLargeCountryRows()));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsExactly<OperationCanceledException>(() => vm.Run(cts.Token));
    }

    [TestMethod]
    public void ParallelIndependentCtesSample_WhenExecuted_ShouldReturnExpectedJoinRows()
    {
        var vm = CompileSample(
            "Q82_ParallelIndependentCtes.cs",
            CreateBasicProvider(
                [
                    new BasicEntity { Name = "Alice" },
                    new BasicEntity { Name = "Bob" },
                    new BasicEntity { Name = "Cara" }
                ],
                [
                    new BasicEntity { Name = "Bob" },
                    new BasicEntity { Name = "Cara" },
                    new BasicEntity { Name = "Dora" }
                ]));

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("p.Name", typeof(string)),
            ("q.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Bob", "Bob"],
            ["Cara", "Cara"]);
    }

    [TestMethod]
    public void ScriptParameterCteHelperCaptureSample_WhenExecuted_ShouldUseRuntimeParameters()
    {
        var vm = CompileSample(
            "Q125_ScriptParameterCteHelperCapture.cs",
            CreateBasicProvider(CreateBasicRows(), CreateTargetRows()));
        vm.Parameters["country"] = "PL";

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("l.Name", typeof(string)),
            ("r.RequestedCountry", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "PL"],
            ["Cara", "PL"]);
    }

    [TestMethod]
    public void ScriptParameterWindowHelperCaptureSample_WhenExecuted_ShouldUseRuntimeParameters()
    {
        var vm = CompileSample(
            "Q126_ScriptParameterWindowHelperCapture.cs",
            CreateBasicProvider(CreateBasicRows(), CreateTargetRows()));
        vm.Parameters["country"] = "PL";
        vm.Parameters["label"] = "-window";

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("rn", typeof(long)),
            ("WindowLabel", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 1L, "-window"],
            ["Bob", 1L, "-window"],
            ["Cara", 2L, "-window"],
            ["Dora", 1L, "-window"]);
    }

    [TestMethod]
    public void ScriptParameterParallelHelperCaptureSample_WhenExecutedWithIndexedRows_ShouldUseRuntimeParameters()
    {
        var sample = GeneratedCodeSamplesCatalog.GetByFileName("Q127_ScriptParameterParallelHelperCapture.cs");
        var provider = new DirectRuntimeV2RegressionSchemaProvider(CreateRuntimeV2Rows());
        var inspection = InstanceCreator.CompileForInspection(
            sample.Query,
            Guid.NewGuid().ToString(),
            provider,
            LoggerResolver,
            sample.CompilationOptions);

        Assert.Contains("EvaluationHelper.ProjectRowsParallel", inspection.GeneratedCSharpCode);
        Assert.Contains("QueryRows.FromRowShards", inspection.GeneratedCSharpCode);

        var vm = CompileSample(sample.FileName, provider);
        vm.Parameters["threshold"] = 4990;
        vm.Parameters["label"] = "hot";

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Label", typeof(string)),
            ("Heavy", typeof(int)));
        var expectedRows = new List<object[]>();
        for (var value = 4991; value <= 4999; value++)
            expectedRows.Add([$"Row{value}", "hot", value * 5]);

        TableMaterializationTestHelper.AssertRowsUnordered(table, expectedRows.ToArray());
    }

    private CompiledQuery CompileSample(string fileName, ISchemaProvider provider)
    {
        var sample = GeneratedCodeSamplesCatalog.GetByFileName(fileName);
        var options = sample.CompilationOptions ?? TestCompilationOptions;

        return InstanceCreator.CompileForExecution(
            sample.Query,
            Guid.NewGuid().ToString(),
            provider,
            LoggerResolver,
            options);
    }

    private static BasicSchemaProvider<BasicEntity> CreateBasicProvider(
        IEnumerable<BasicEntity> aRows,
        IEnumerable<BasicEntity> bRows)
    {
        return new BasicSchemaProvider<BasicEntity>(
            new Dictionary<string, IEnumerable<BasicEntity>>
            {
                ["#A"] = aRows,
                ["#B"] = bRows,
                ["#C"] = []
            });
    }

    private static IReadOnlyList<BasicEntity> CreateBasicRows()
    {
        return
        [
            new BasicEntity { Name = "Alice", Country = "PL", City = "Warsaw", Population = 120m },
            new BasicEntity { Name = "Bob", Country = "DE", City = "Berlin", Population = 80m },
            new BasicEntity { Name = "Cara", Country = "PL", City = "Krakow", Population = 200m },
            new BasicEntity { Name = "Dora", Country = "FR", City = "Paris", Population = 50m }
        ];
    }

    private static IReadOnlyList<BasicEntity> CreateTargetRows()
    {
        return
        [
            new BasicEntity { Name = "TargetWarsaw", Country = "PL", City = "Warsaw" },
            new BasicEntity { Name = "TargetBerlin", Country = "DE", City = "Berlin" }
        ];
    }

    private static BasicEntity[] CreateLargeCountryRows()
    {
        return Enumerable.Range(0, 5000)
            .Select(index => new BasicEntity
            {
                Name = $"Name{index}",
                Country = index % 2 == 0 ? "PL" : "DE",
                City = index % 2 == 0 ? "Warsaw" : "Berlin",
                Population = index
            })
            .ToArray();
    }

    private static RuntimeV2RegressionEntity[] CreateRuntimeV2Rows()
    {
        return Enumerable.Range(0, 5000)
            .Select(index => new RuntimeV2RegressionEntity
            {
                Id = index,
                Name = $"Row{index}",
                Value = index,
                Category = index % 2 == 0 ? "A" : "B",
                Department = index % 3 == 0 ? "Engineering" : "Support",
                Salary = index
            })
            .ToArray();
    }

    private sealed class DirectRuntimeV2RegressionSchemaProvider(IReadOnlyList<RuntimeV2RegressionEntity> rows)
        : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return new DirectRuntimeV2RegressionSchema(rows);
        }
    }

    private sealed class DirectRuntimeV2RegressionSchema(IReadOnlyList<RuntimeV2RegressionEntity> rows)
        : SchemaBase("test", CreateLibrary())
    {
        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            return new RuntimeV2RegressionTable();
        }

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters)
        {
            return EnsureSourceType<T, RuntimeV2RegressionEntity>(
                name,
                new DirectRuntimeV2RegressionRowSource(rows));
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodsManager = new MethodsManager();
            methodsManager.RegisterLibraries(new LibraryBase());
            methodsManager.RegisterLibraries(new RuntimeV2RegressionLibrary());
            return new MethodsAggregator(methodsManager);
        }
    }

    private sealed class DirectRuntimeV2RegressionRowSource(IReadOnlyList<RuntimeV2RegressionEntity> rows)
        : RowSource<RuntimeV2RegressionEntity>
    {
        public override IEnumerable<IReadOnlyList<RuntimeV2RegressionEntity>> Chunks => [rows];
    }
}

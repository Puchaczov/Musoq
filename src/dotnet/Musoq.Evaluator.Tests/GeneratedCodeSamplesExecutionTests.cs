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

    [TestMethod]
    public void ScriptParameterGroupByHelperCaptureSample_WhenExecutedWithLargeSource_ShouldUseRuntimeParameters()
    {
        var vm = CompileSample(
            "Q123_ScriptParameterGroupByHelperCapture.cs",
            CreateBasicProvider(CreateLargeCountryRows(), []));
        vm.Parameters["suffix"] = "-large";
        vm.Parameters["minCount"] = 2500;

        var table = vm.Run(TestContext.CancellationToken);

        var counts = table.Rows.ToDictionary(row => (string)row[0], row => Convert.ToInt32(row[1]));
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(2500, counts["PL-large"]);
        Assert.AreEqual(2500, counts["DE-large"]);
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

        var rows = table.Rows.ToDictionary(row => (string)row[0], row => (string)row[1]);
        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("TargetWarsaw-match", rows["Alice"]);
        Assert.AreEqual("TargetBerlin-match", rows["Bob"]);
        Assert.IsTrue(rows.ContainsKey("Cara"));
        Assert.IsTrue(rows.ContainsKey("Dora"));
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

        var rows = table.Rows
            .Select(static row => $"{(string)row[0]}|{(string)row[1]}")
            .OrderBy(static row => row, StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEqual(
            new[] { "Bob|Bob", "Cara|Cara" },
            rows);
        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void ScriptParameterCteHelperCaptureSample_WhenExecuted_ShouldUseRuntimeParameters()
    {
        var vm = CompileSample(
            "Q125_ScriptParameterCteHelperCapture.cs",
            CreateBasicProvider(CreateBasicRows(), CreateTargetRows()));
        vm.Parameters["country"] = "PL";

        var table = vm.Run(TestContext.CancellationToken);

        var rows = table.Rows.ToDictionary(row => (string)row[0], row => (string)row[1]);
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("PL", rows["Alice"]);
        Assert.AreEqual("PL", rows["Cara"]);
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

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual(1, RowNumberFor(table, "Alice"));
        Assert.AreEqual(2, RowNumberFor(table, "Cara"));
        Assert.AreEqual(1, RowNumberFor(table, "Bob"));
        Assert.AreEqual(1, RowNumberFor(table, "Dora"));
        Assert.IsTrue(table.Rows.All(row => (string)row[2] == "-window"));
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

        var rows = table.Rows.ToDictionary(row => (string)row[0]);
        Assert.AreEqual(9, table.Count);
        for (var value = 4991; value <= 4999; value++)
        {
            var row = rows[$"Row{value}"];
            Assert.AreEqual("hot", row[1]);
            Assert.AreEqual(value * 5, row[2]);
        }
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

    private static int RowNumberFor(Table table, string name)
    {
        return Convert.ToInt32(table.Rows.Single(row => (string)row[0] == name)[1]);
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

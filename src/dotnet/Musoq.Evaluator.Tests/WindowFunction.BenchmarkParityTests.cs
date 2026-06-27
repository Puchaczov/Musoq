using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Components;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using SchemaColumn = Musoq.Schema.DataSources.SchemaColumn;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class WindowFunctionBenchmarkParityTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();

    public static IEnumerable<object[]> BenchmarkScenarioQueries()
    {
        yield return
        [
            "RowNumber_NoPartition",
            "select Name, RowNumber() over (order by Salary desc) as rn from #test.entities()",
            5
        ];

        yield return
        [
            "RowNumber_Partitioned",
            "select Name, Department, RowNumber() over (partition by Department order by Salary desc) as rn from #test.entities()",
            5
        ];

        yield return
        [
            "Rank_Partitioned",
            "select Name, Department, Rank() over (partition by Department order by Salary desc) as rn from #test.entities()",
            5
        ];

        yield return
        [
            "DenseRank_Partitioned",
            "select Name, Department, DenseRank() over (partition by Department order by Salary desc) as rn from #test.entities()",
            5
        ];

        yield return
        [
            "Lag_Partitioned",
            "select Name, Department, Lag(Salary, 1) over (partition by Department order by Salary desc) as prev from #test.entities()",
            5
        ];

        yield return
        [
            "Lead_Partitioned",
            "select Name, Department, Lead(Salary, 1) over (partition by Department order by Salary desc) as nxt from #test.entities()",
            5
        ];

        yield return
        [
            "SumAggregate_WholePartition",
            "select Name, Department, Sum(ToDecimal(Salary)) over (partition by Department) as total from #test.entities()",
            5
        ];

        yield return
        [
            "SumAggregate_Running",
            "select Name, Department, Sum(ToDecimal(Salary)) over (partition by Department order by Salary) as running from #test.entities()",
            5
        ];

        yield return
        [
            "MultipleWindows",
            @"select Name, Department,
                    RowNumber() over (partition by Department order by Salary desc) as rn,
                    Sum(ToDecimal(Salary)) over (partition by Department) as total,
                    Lag(Salary, 1) over (partition by Department order by Salary desc) as prev
                from #test.entities()",
            5
        ];

        yield return
        [
            "CountAggregate_WholePartition",
            "select Name, Department, Count(Name) over (partition by Department) as cnt from #test.entities()",
            5
        ];

        yield return
        [
            "AvgAggregate_Running",
            "select Name, Department, Avg(ToDecimal(Salary)) over (partition by Department order by Salary) as running_avg from #test.entities()",
            5
        ];
    }

    public static IEnumerable<object[]> BenchmarkFrameAndQualifyScenarioQueries()
    {
        yield return
        [
            "Frame_RunningSum",
            "select Name, Sum(ToDecimal(Salary)) over (order by Name rows between unbounded preceding and current row) as RunSum " +
            "from #test.entities()",
            5
        ];

        yield return
        [
            "Frame_SlidingWindow3",
            "select Name, Sum(ToDecimal(Salary)) over (order by Name rows between 1 preceding and 1 following) as SlideSum " +
            "from #test.entities()",
            5
        ];

        yield return
        [
            "Frame_FullPartition",
            "select Name, Department, Sum(ToDecimal(Salary)) over (partition by Department rows between unbounded preceding and unbounded following) as Total " +
            "from #test.entities()",
            5
        ];

        yield return
        [
            "Frame_Partitioned_RunningSum",
            "select Name, Department, Sum(ToDecimal(Salary)) over (partition by Department order by Name rows between unbounded preceding and current row) as RunSum " +
            "from #test.entities()",
            5
        ];

        yield return
        [
            "Qualify_RowNumber_TopN",
            "select Name, Department, RowNumber() over (partition by Department order by Salary desc) as rn " +
            "from #test.entities() " +
            "qualify RowNumber() over (partition by Department order by Salary desc) <= 3",
            5
        ];

        yield return
        [
            "Qualify_FramedSum_Filter",
            "select Name, Department, Sum(ToDecimal(Salary)) over (partition by Department order by Name rows between unbounded preceding and current row) as RunSum " +
            "from #test.entities() " +
            "qualify Sum(ToDecimal(Salary)) over (partition by Department order by Name rows between unbounded preceding and current row) > 100000",
            4
        ];

        yield return
        [
            "Qualify_Rank_TopN",
            "select Name, Department, Rank() over (partition by Department order by Salary desc) as rnk " +
            "from #test.entities() " +
            "qualify Rank() over (partition by Department order by Salary desc) <= 5",
            5
        ];

        yield return
        [
            "Frame_Count_Sliding",
            "select Name, Count(Name) over (order by Name rows between 2 preceding and current row) as Cnt " +
            "from #test.entities()",
            5
        ];

        yield return
        [
            "Frame_Avg_Running",
            "select Name, Avg(ToDecimal(Salary)) over (partition by Department order by Name rows between unbounded preceding and current row) as RunAvg " +
            "from #test.entities()",
            5
        ];

        yield return
        [
            "Frame_MultipleFrames",
            "select Name, Department, " +
            "Sum(ToDecimal(Salary)) over (partition by Department order by Name rows between unbounded preceding and current row) as RunSum, " +
            "Sum(ToDecimal(Salary)) over (partition by Department order by Name rows between 1 preceding and 1 following) as SlideSum " +
            "from #test.entities()",
            5
        ];

        yield return
        [
            "Qualify_WithWhere",
            "select Name, Department, RowNumber() over (partition by Department order by Salary desc) as rn " +
            "from #test.entities() where Salary > 50000 " +
            "qualify RowNumber() over (partition by Department order by Salary desc) <= 3",
            5
        ];
    }

    [TestMethod]
    [DynamicData(nameof(BenchmarkScenarioQueries))]
    public void BenchmarkWindowScenario_WhenEntityTypeIsPrivate_ShouldCompileAndRunWithExecutionIr(
        string scenario,
        string query,
        int expectedRows)
    {
        AssertBenchmarkWindowScenarioCompilesAndRunsWithExecutionIr(scenario, query, expectedRows);
    }

    [TestMethod]
    [DynamicData(nameof(BenchmarkFrameAndQualifyScenarioQueries))]
    public void BenchmarkFrameAndQualifyScenario_WhenEntityTypeIsPrivate_ShouldCompileAndRunWithExecutionIr(
        string scenario,
        string query,
        int expectedRows)
    {
        AssertBenchmarkWindowScenarioCompilesAndRunsWithExecutionIr(scenario, query, expectedRows);
    }

    [TestMethod]
    public void QualifyFramedSumBenchmarkScenario_WhenEntityTypeIsPrivate_ShouldFilterOnRenderedWindowResult()
    {
        var table = Run(
            "select Name, Department, Sum(ToDecimal(Salary)) over (partition by Department order by Name rows between unbounded preceding and current row) as RunSum " +
            "from #test.entities() " +
            "qualify Sum(ToDecimal(Salary)) over (partition by Department order by Name rows between unbounded preceding and current row) > 100000");

        var names = table.Select(row => (string)row.Values[0]).OrderBy(name => name).ToArray();

        CollectionAssert.AreEqual(new[] { "Bea", "Cal", "Dana", "Eli" }, names);
    }

    private static void AssertBenchmarkWindowScenarioCompilesAndRunsWithExecutionIr(
        string scenario,
        string query,
        int expectedRows)
    {
        var entities = CreateEntities();
        var inspection = CompileForInspection(query, entities);

        AssertExecutionIrWindowSupported(scenario, inspection.ExecutionPlanText);

        var table = Compile(query, entities).Run();

        Assert.AreEqual(expectedRows, table.Count, scenario);
    }

    [TestMethod]
    public void RowNumberWindow_WhenEntityTypeIsPrivate_ShouldMatchBenchmarkOrdering()
    {
        var table = Run(
            "select Name, RowNumber() over (order by Salary desc) as rn from #test.entities()");

        var rows = table.OrderBy(row => Convert.ToInt64(row.Values[1])).ToArray();

        Assert.AreEqual("Bea", rows[0].Values[0]);
        Assert.AreEqual(1L, Convert.ToInt64(rows[0].Values[1]));
        Assert.AreEqual("Cal", rows[1].Values[0]);
        Assert.AreEqual(2L, Convert.ToInt64(rows[1].Values[1]));
        Assert.AreEqual("Dana", rows[2].Values[0]);
        Assert.AreEqual(3L, Convert.ToInt64(rows[2].Values[1]));
        Assert.AreEqual("Ada", rows[3].Values[0]);
        Assert.AreEqual(4L, Convert.ToInt64(rows[3].Values[1]));
        Assert.AreEqual("Eli", rows[4].Values[0]);
        Assert.AreEqual(5L, Convert.ToInt64(rows[4].Values[1]));
    }

    [TestMethod]
    public void LagWindow_WhenEntityTypeIsPrivate_ShouldReadResolverBackedRows()
    {
        var table = Run(
            "select Name, Department, Lag(Salary, 1) over (partition by Department order by Salary desc) as prev from #test.entities()");

        AssertLagValue(table, "Bea", null);
        AssertLagValue(table, "Cal", 200000);
        AssertLagValue(table, "Ada", 150000);
        AssertLagValue(table, "Dana", null);
        AssertLagValue(table, "Eli", 120000);
    }

    [TestMethod]
    public void RunningSumWindow_WhenEntityTypeIsPrivate_ShouldEvaluatePluginWindowAggregate()
    {
        var table = Run(
            "select Name, Department, Sum(ToDecimal(Salary)) over (partition by Department order by Salary) as running from #test.entities()");

        AssertWindowDecimal(table, "Ada", 100000m);
        AssertWindowDecimal(table, "Cal", 250000m);
        AssertWindowDecimal(table, "Bea", 450000m);
        AssertWindowDecimal(table, "Eli", 80000m);
        AssertWindowDecimal(table, "Dana", 200000m);
    }

    private static Table Run(string query)
    {
        return Compile(query, CreateEntities()).Run();
    }

    private static CompiledQuery Compile(string query, IReadOnlyList<WinEntity> entities)
    {
        return InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            new WinSchemaProvider(entities),
            LoggerResolver);
    }

    private static QueryInspectionResult CompileForInspection(string query, IReadOnlyList<WinEntity> entities)
    {
        return InstanceCreator.CompileForInspection(
            query,
            Guid.NewGuid().ToString(),
            new WinSchemaProvider(entities),
            LoggerResolver);
    }

    private static void AssertExecutionIrWindowSupported(string scenario, string executionPlanText)
    {
        Assert.IsFalse(
            executionPlanText.Contains("ExecutionPlanUnsupported", StringComparison.Ordinal),
            $"{scenario} should be renderable by Execution IR, but inspection returned:{Environment.NewLine}{executionPlanText}");
        Assert.IsTrue(
            executionPlanText.Contains("Compute", StringComparison.Ordinal) &&
            executionPlanText.Contains("Window", StringComparison.Ordinal),
            $"{scenario} should produce rendered window Execution IR:{Environment.NewLine}{executionPlanText}");
    }

    private static void AssertLagValue(Table table, string name, int? expected)
    {
        var row = table.Single(item => (string)item.Values[0] == name);
        var actual = row.Values[2];

        if (expected == null)
        {
            Assert.IsNull(actual);
            return;
        }

        Assert.AreEqual(expected.Value, Convert.ToInt32(actual));
    }

    private static void AssertWindowDecimal(Table table, string name, decimal expected)
    {
        var row = table.Single(item => (string)item.Values[0] == name);

        Assert.AreEqual(expected, Convert.ToDecimal(row.Values[2]));
    }

    private static IReadOnlyList<WinEntity> CreateEntities()
    {
        return
        [
            new() { Name = "Ada", Department = "Engineering", Salary = 100000 },
            new() { Name = "Bea", Department = "Engineering", Salary = 200000 },
            new() { Name = "Cal", Department = "Engineering", Salary = 150000 },
            new() { Name = "Dana", Department = "Sales", Salary = 120000 },
            new() { Name = "Eli", Department = "Sales", Salary = 80000 }
        ];
    }

    private sealed class WinEntity
    {
        public string Name { get; init; } = string.Empty;
        public string Department { get; init; } = string.Empty;
        public int Salary { get; init; }
    }

    private sealed class WinSchemaProvider(IReadOnlyList<WinEntity> entities) : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return new WinSchema(entities);
        }
    }

    private sealed class WinSchema(IReadOnlyList<WinEntity> entities) : SchemaBase("test", CreateLibrary())
    {
        public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
        {
            return new WinTable();
        }

        public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
        {
            return EnsureSourceType<T, WinEntity>(name, new EntitySource<WinEntity>([entities], WinTable.NameToIndexMap, WinTable.IndexToObjectAccessMap));
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodManager = new MethodsManager();
            methodManager.RegisterLibraries(new Library());
            return new MethodsAggregator(methodManager);
        }
    }

    private sealed class WinTable : ISchemaTable
    {
        public static readonly IReadOnlyDictionary<string, int> NameToIndexMap = new Dictionary<string, int>
        {
            { nameof(WinEntity.Name), 0 },
            { nameof(WinEntity.Department), 1 },
            { nameof(WinEntity.Salary), 2 }
        };

        public static readonly IReadOnlyDictionary<int, Func<WinEntity, object?>> IndexToObjectAccessMap =
            new Dictionary<int, Func<WinEntity, object?>>
            {
                { 0, entity => entity.Name },
                { 1, entity => entity.Department },
                { 2, entity => entity.Salary }
            };

        public ISchemaColumn[] Columns =>
        [
            new SchemaColumn(nameof(WinEntity.Name), 0, typeof(string)),
            new SchemaColumn(nameof(WinEntity.Department), 1, typeof(string)),
            new SchemaColumn(nameof(WinEntity.Salary), 2, typeof(int))
        ];

        public SchemaTableMetadata Metadata { get; } = new(typeof(WinEntity));

        public ISchemaColumn? GetColumnByName(string name)
        {
            return Columns.First(column => column.ColumnName == name);
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns.Where(column => column.ColumnName == name).ToArray();
        }
    }

    private sealed class Library : LibraryBase;
}

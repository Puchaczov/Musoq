using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Benchmarks.Scenarios;
using Musoq.Converter;
using Musoq.Schema;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class QueryResultTests
{
    public static IEnumerable<object[]> Cases => ScenarioCatalog.All.Select(scenario => new object[] { scenario.Id });

    [TestMethod]
    [DynamicData(nameof(Cases))]
    public void SharedQuery_ReturnsExpectedColumnsAndRows(string id)
    {
        var scenario = ScenarioCatalog.Get(id);
        var expected = Expectations.Get(id);
        using var query = InstanceCreator.CompileForExecution(scenario.Sql, "correctness", new FixtureProvider(FixtureData.TestSize),
            BenchmarkLogger.Instance, ExecutionSettings.Create(scenario));
        ResultAssertions.Execution(expected, query, scenario);
        ResultAssertions.Execution(expected, query, scenario);
    }


    [TestMethod]
    public void Catalog_HasUniqueIdsAndCompleteExpectationsAndRegistration()
    {
        var ids = ScenarioCatalog.All.Select(scenario => scenario.Id).ToArray();
        Assert.AreEqual(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
        CollectionAssert.AreEquivalent(ids, new QueryExecutionBenchmark().Scenarios.ToArray());
        CollectionAssert.AreEquivalent(ids, Expectations.Ids.ToArray());
    }
}

public sealed record ExpectedResult((string Name, Type Type)[] Columns, object?[][] Rows);

public static partial class Expectations
{
    private static readonly Dictionary<string, ExpectedResult> Results = Create();
    public static IEnumerable<string> Ids => Results.Keys;
    public static ExpectedResult Get(string id) => Results[id];
    private static Dictionary<string, ExpectedResult> Create()
    {
        Dictionary<string, ExpectedResult> results = new()
        {
            ["B01_Scan"] = new([("Id", typeof(int)), ("Name", typeof(string))],
                [[0, "item-0"], [1, "item-1"], [2, "item-2"], [3, "item-3"]])
        };
        AddRelational(results);
        AddAnalytics(results);
        results.Add("B14_Skewed", new([("Id", typeof(int)), ("Count", typeof(long)), ("Total", typeof(decimal?))], [[0, 4L, 10m]]));
        AddInterpretation(results);
        AddComposed(results);
        results.Add("B01_Wide", new(new[] { ("Id", typeof(int)) }.Concat(Enumerable.Range(0, 64)
            .Select(index => ($"C{index}", typeof(decimal)))).ToArray(), Enumerable.Range(0, 4)
            .Select(row => new object?[] { row }.Concat(Enumerable.Range(0, 64).Select(column => (object?)(row + 1m + column))).ToArray()).ToArray()));
        return results;
    }
}

public static class ResultAssertions
{
    public static void Execution(ExpectedResult expected, Musoq.Evaluator.CompiledQuery query, Scenario scenario)
    {
        using var table = query.Run();
        Table(expected, table, scenario);
    }

    public static void Table(ExpectedResult expected, Musoq.Evaluator.Tables.Table table, Scenario scenario)
    {
        var columns = table.Columns.OrderBy(column => column.ColumnIndex).ToArray();
        Assert.HasCount(expected.Columns.Length, columns, scenario.Id);
        for (var index = 0; index < columns.Length; index++)
        {
            Assert.AreEqual(expected.Columns[index].Name, columns[index].ColumnName, scenario.Id);
            Assert.AreEqual(expected.Columns[index].Type, columns[index].ColumnType, scenario.Id);
        }
        Rows(expected.Rows, table.Select(Values).ToArray(), scenario.Ordered, scenario.Id);
    }

    public static object?[] Values(Musoq.Evaluator.Tables.Row row)
    {
        var values = new object?[row.Count];
        var visitor = new AssertionVisitor(values);
        row.VisitColumns(ref visitor);
        return values;
    }

    private readonly struct AssertionVisitor(object?[] values) : IColumnValueVisitor
    {
        public void Visit<T>(int columnIndex, in T value) => values[columnIndex] = value;
    }

    public static void Rows(object?[][] expected, object?[][] actual, bool ordered, string id)
    {
        Assert.HasCount(expected.Length, actual, id);
        var remaining = actual.ToList();
        for (var index = 0; index < expected.Length; index++)
        {
            var match = ordered ? 0 : remaining.FindIndex(row => row.SequenceEqual(expected[index]));
            Assert.IsTrue(match >= 0, $"{id}: missing row {index}.");
            CollectionAssert.AreEqual(expected[index], remaining[match], $"{id}: row {index}.");
            remaining.RemoveAt(match);
        }
    }
}

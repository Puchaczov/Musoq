using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class RecursiveCteBenchmarkTests
{
    public static IEnumerable<object[]> Scenarios =>
        from scenario in Enum.GetValues<RecursiveCteBenchmarkScenario>()
        from mode in new[] { ParallelizationMode.None, ParallelizationMode.Full }
        select new object[] { scenario, mode };

    [TestMethod]
    [DynamicData(nameof(Scenarios))]
    public void MusoqGenerated_ShouldMatchHandwrittenSemiNaive(
        RecursiveCteBenchmarkScenario scenario,
        ParallelizationMode executionMode)
    {
        var benchmark = new RecursiveCteBenchmark
        {
            Scenario = scenario,
            Scale = 64,
            ExecutionMode = executionMode
        };

        benchmark.Setup();
        try
        {
            var expected = benchmark.HandwrittenSemiNaive();
            var actual = benchmark.MusoqGenerated();

            CollectionAssert.AreEqual(ReadColumns(expected), ReadColumns(actual));
            CollectionAssert.AreEqual(ReadRows(expected), ReadRows(actual));
        }
        finally
        {
            benchmark.Cleanup();
        }
    }

    private static string[] ReadColumns(Table table) =>
        table.Columns
            .OrderBy(static column => column.ColumnIndex)
            .Select(static column => $"{column.ColumnName}:{column.ColumnType.FullName}")
            .ToArray();

    private static string[] ReadRows(Table table) =>
        table.Rows
            .Select(static row => string.Join("|", row.Values))
            .Order(StringComparer.Ordinal)
            .ToArray();
}

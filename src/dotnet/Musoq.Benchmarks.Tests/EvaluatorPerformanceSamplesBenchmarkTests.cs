namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class EvaluatorPerformanceSamplesBenchmarkTests
{
    [TestMethod]
    public void Q227_JoinAggregate_ShouldReturnOneGroupPerCity()
    {
        var query = EvaluatorPerformanceBenchmarkSupport.Compile(
            EvaluatorPerformanceScenario.Q227_PerformanceJoinAggregate,
            128);

        var table = query.Run();

        Assert.AreEqual(64, table.Count);
        Assert.AreEqual("City_0", table.Rows.First()[0]);
        Assert.AreEqual(4L, table.Rows.First()[1]);
    }

    [TestMethod]
    public void Q228_WideCorrelatedSubquery_ShouldPreserveMatchAndLookupSemantics()
    {
        var query = EvaluatorPerformanceBenchmarkSupport.Compile(
            EvaluatorPerformanceScenario.Q228_PerformanceWideCorrelatedSubquery,
            16);

        var rows = query.Run().Rows.ToArray();

        Assert.AreEqual(16, rows.Length);
        CollectionAssert.AreEqual(
            new object?[] { "Name_0", "N", "Y", null },
            rows[0].Values);
        CollectionAssert.AreEqual(
            new object?[] { "Name_1", "N", "Y", null },
            rows[1].Values);
    }

    [TestMethod]
    public void Q229_WindowCteSetOperation_ShouldDeduplicateEquivalentArms()
    {
        var query = EvaluatorPerformanceBenchmarkSupport.Compile(
            EvaluatorPerformanceScenario.Q229_PerformanceWindowCteSetOperation,
            8);

        var rows = query.Run().Rows.ToArray();

        Assert.AreEqual(8, rows.Length);
        CollectionAssert.AreEqual(
            new object?[] { "Name_0", "Country_0", 1L },
            rows[0].Values);
        CollectionAssert.AreEqual(
            new object?[] { "Name_4", "Country_0", 3L },
            rows[4].Values);
    }

    [TestMethod]
    public void Q230_TableProjection_ShouldFilterNonPositivePopulation()
    {
        var query = EvaluatorPerformanceBenchmarkSupport.Compile(
            EvaluatorPerformanceScenario.Q230_PerformanceTableProjection,
            16);

        var table = query.Run();

        Assert.AreEqual(15, table.Count);
        CollectionAssert.AreEqual(
            new object?[] { "Name_1", "City_1", 10m },
            table.Rows.First().Values);
    }
}

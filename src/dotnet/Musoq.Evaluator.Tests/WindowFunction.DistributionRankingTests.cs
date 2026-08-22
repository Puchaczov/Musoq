using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class WindowFunctionDistributionRankingTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    [FeatureEvidence("percent-rank-cume-dist", FeatureEvidenceKind.RuntimePositive)]
    public void DistributionRankings_WithPartitionsCompositePeersAndNulls_ShouldUsePeerOrdinals()
    {
        const string query = """
            select Name, City,
                   PercentRank() over (
                       partition by City
                       order by NullableValue desc nulls last, Country asc) as PercentRankValue,
                   CumeDist() over (
                       partition by City
                       order by NullableValue desc nulls last, Country asc) as CumeDistValue
            from #A.Entities()
            """;
        var sources = CreateSingleSource(
            new BasicEntity("Alpha") { City = "A", Country = "US", NullableValue = 10 },
            new BasicEntity("Beta") { City = "A", Country = "US", NullableValue = 10 },
            new BasicEntity("Gamma") { City = "A", Country = "CA", NullableValue = 10 },
            new BasicEntity("Delta") { City = "A", Country = "US", NullableValue = null },
            new BasicEntity("Solo") { City = "B", Country = null, NullableValue = null });

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("City", typeof(string)),
            ("PercentRankValue", typeof(double)),
            ("CumeDistValue", typeof(double)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alpha", "A", 1d / 3d, 0.75d],
            ["Beta", "A", 1d / 3d, 0.75d],
            ["Gamma", "A", 0d, 0.25d],
            ["Delta", "A", 1d, 1d],
            ["Solo", "B", 0d, 1d]);
    }

    [TestMethod]
    public void DistributionRankings_ShouldAnalyzeWithoutDiagnostics()
    {
        const string query = "select PercentRank() over (order by Population), CumeDist() over (order by Population) from #A.Entities()";
        var provider = new BasicSchemaProvider<BasicEntity>(CreateSingleSource());
        var analysis = new QueryAnalyzer(provider).Analyze(query);

        Assert.IsFalse(analysis.HasErrors, string.Join(" | ", analysis.Diagnostics));
    }

    [TestMethod]
    public void DistributionRankings_WithAscendingAndDescendingOrder_ShouldReversePeerPositions()
    {
        const string query = """
            select Name,
                   PERCENT_RANK() over (order by Population) as AscPercent,
                   CUME_DIST() over (order by Population) as AscCume,
                   PERCENT_RANK() over (order by Population desc) as DescPercent,
                   CUME_DIST() over (order by Population desc) as DescCume
            from #A.Entities()
            """;
        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 },
            new BasicEntity("Charlie") { Population = 200 });

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 0d, 1d / 3d, 1d, 1d],
            ["Bob", 0.5d, 1d, 0d, 2d / 3d],
            ["Charlie", 0.5d, 1d, 0d, 2d / 3d]);
    }

    [TestMethod]
    public void DistributionRankings_WithEmptyInput_ShouldReturnNoRows()
    {
        const string query = """
            select PercentRank() over (order by Population) as PercentRankValue,
                   CumeDist() over (order by Population) as CumeDistValue
            from #A.Entities()
            """;

        var table = CreateAndRunVirtualMachine(query, CreateSingleSource())
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("PercentRankValue", typeof(double)),
            ("CumeDistValue", typeof(double)));
        Assert.AreEqual(0, table.Count);
    }

}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Benchmarks.Scenarios;
using Musoq.Converter;
using Musoq.Schema;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class CostWorkloadTests
{
    public static IEnumerable<object[]> ScaleCases =>
        from id in new[]
        {
            "B01_Scan_Unordered", "B01_Wide_Unordered", "B02_Filter_Selectivity0", "B02_Filter_Selectivity1",
            "B02_Filter_Selectivity50", "B02_Filter_Selectivity100", "B07_StringKeyJoin", "B07_NullableCompositeJoin",
            "B10_PartitionedRangeExists", "B03_StableScalarReuse", "B03_VolatileScalarControl", "B13_ApplyInvariant",
            "B14_CompositeGroups", "B16_PartitionedRowsFrame", "B27_Binary4KiB", "B30_Text1KiB",
            "B26_ComputedProjection_Accepted", "B26_ComputedProjection_Residual"
        }
        from count in new[] { 1_000, 10_000, 100_000 }
        select new object[] { id, count };

    [TestMethod]
    [DynamicData(nameof(ScaleCases))]
    public void StandardSizes_PreserveCardinalityAndIndependentChecksums(string id, int count)
    {
        var scenario = ScenarioCatalog.Get(id);
        using var query = Compile(scenario, count);
        using var table = query.Run();
        var visitor = new ScaleVisitor(id);
        long rows = 0;
        foreach (var row in table)
        {
            row.VisitColumns(ref visitor);
            rows++;
        }
        var expected = ExpectedScale(id, count);
        Assert.AreEqual(expected.Rows, rows, id);
        Assert.AreEqual(expected.IdSum, visitor.IdSum, id);
        if (id.StartsWith("B26_ComputedProjection_", StringComparison.Ordinal))
            Assert.AreEqual((long)count * (count - 1), visitor.ValueSum, id);
        if (id == "B14_CompositeGroups")
            Assert.AreEqual(count * (count + 1m) / 2, visitor.Total, id);
        if (id == "B16_PartitionedRowsFrame")
            Assert.AreEqual(WindowTotal(count), visitor.Total, id);
        if (id == "B27_Binary4KiB")
            Assert.AreEqual(4092L * count, visitor.LengthSum, id);
        if (id == "B30_Text1KiB")
            Assert.AreEqual(Enumerable.Range(0, count).Sum(index => 1018L - index.ToString(System.Globalization.CultureInfo.InvariantCulture).Length), visitor.LengthSum, id);
    }

    public static IEnumerable<object[]> EmptyCases => ScaleCases.Select(item => (string)item[0]).Distinct()
        .Select(id => new object[] { id });

    [TestMethod]
    [DynamicData(nameof(EmptyCases))]
    public void NewWorkloads_EmptyInputsPreserveTheirDeclaredColumns(string id)
    {
        var scenario = ScenarioCatalog.Get(id);
        using var query = Compile(scenario, 0);
        ResultAssertions.Execution(new ExpectedResult(Expectations.Get(id).Columns, []), query, scenario);
    }

    [TestMethod]
    public void CompositeGrouping_CrossesTheHundredRowBoundary()
    {
        var scenario = ScenarioCatalog.Get("B14_CompositeGroups");
        var expected = new ExpectedResult(Expectations.Get(scenario.Id).Columns,
        [
            ["group-00000000", 0, 1225m], ["group-00000000", 1, 1250m],
            ["group-00000000", 2, 1275m], ["group-00000000", 3, 1300m],
            ["group-00000001", 0, 101m], ["group-00000001", 1, 102m],
            ["group-00000001", 2, 103m], ["group-00000001", 3, 104m]
        ]);
        using var query = Compile(scenario, 104);
        ResultAssertions.Execution(expected, query, scenario);
    }

    [TestMethod]
    public void PartitionedRange_FirstRepeatedPartitionHasExactlyOneMatch()
    {
        var scenario = ScenarioCatalog.Get("B10_PartitionedRangeExists");
        using var query = Compile(scenario, 65);
        ResultAssertions.Execution(new(Expectations.Get(scenario.Id).Columns, [[0]]), query, scenario);
    }

    [TestMethod]
    [DataRow(1984, 31776)]
    [DataRow(2048, 33824)]
    [DataRow(2049, 33856)]
    public void PartitionedWindow_UsesExactlyThirtyTwoPartitionRows(int id, int total)
    {
        var scenario = ScenarioCatalog.Get("B16_PartitionedRowsFrame");
        using var query = Compile(scenario, 2050);
        using var table = query.Run();
        var row = table.Single(row => row.GetValue<int>(0) == id);
        Assert.AreEqual((decimal?)total, row.GetValue<decimal?>(1));
    }

    [TestMethod]
    public void AddedWorkloads_HaveExactlyFiftyFourNewCasesAndNoSortInProjectionControls()
    {
        Assert.HasCount(84, ScenarioCatalog.All);
        Assert.HasCount(279, CampaignCases.CreateComplete(new CampaignOptions(), Path.GetTempPath()));
        Assert.DoesNotContain("order by", ScenarioCatalog.Get("B01_Scan_Unordered").Sql);
        Assert.DoesNotContain("order by", ScenarioCatalog.Get("B01_Wide_Unordered").Sql);
    }

    private static Musoq.Evaluator.CompiledQuery Compile(Scenario scenario, int count) =>
        InstanceCreator.CompileForExecution(scenario.Sql, "cost-contract",
            FixtureCatalog.Prepare(scenario, count).CreateSchemaProvider(), BenchmarkLogger.Instance, ExecutionSettings.Create(scenario, count));

    private static (long Rows, long IdSum) ExpectedScale(string id, long count)
    {
        var sum = count * (count - 1) / 2;
        if (id.StartsWith("B02_Filter_Selectivity", StringComparison.Ordinal))
        {
            var percent = int.Parse(id.AsSpan("B02_Filter_Selectivity".Length), System.Globalization.CultureInfo.InvariantCulture);
            var blocks = count / 100;
            return (blocks * percent, blocks * percent * (percent - 1) / 2 + 100 * percent * blocks * (blocks - 1) / 2);
        }
        return id switch
        {
            "B07_NullableCompositeJoin" => (count * 4 / 5, sum - 5 * (count / 5) * (count / 5 - 1) / 2),
            "B10_PartitionedRangeExists" => (count - 64, (count - 64) * (count - 65) / 2),
            "B13_ApplyInvariant" => (count * 8, sum * 8),
            "B14_CompositeGroups" => (count / 25, 0),
            _ => (count, sum)
        };
    }

    private static decimal WindowTotal(int count) => Enumerable.Range(0, count).Sum(index =>
    {
        var previous = Math.Min(31, index / 64);
        return (previous + 1) * (index + 1m) - 32m * previous * (previous + 1);
    });

    private struct ScaleVisitor(string id) : IColumnValueVisitor
    {
        public long IdSum { get; private set; }
        public long LengthSum { get; private set; }
        public long ValueSum { get; private set; }
        public decimal Total { get; private set; }

        public void Visit<T>(int columnIndex, in T value)
        {
            if (id.StartsWith("B26_ComputedProjection_", StringComparison.Ordinal) && columnIndex == 1 && value is long computed)
                ValueSum += computed;
            if (columnIndex == 0 && value is int rowId)
                IdSum += rowId;
            if ((id == "B27_Binary4KiB" && columnIndex == 2 || id == "B30_Text1KiB" && columnIndex == 3) && value is int length)
                LengthSum += length;
            if ((id == "B14_CompositeGroups" && columnIndex == 2 || id == "B16_PartitionedRowsFrame" && columnIndex == 1) && value is decimal total)
                Total += total;
        }
    }
}

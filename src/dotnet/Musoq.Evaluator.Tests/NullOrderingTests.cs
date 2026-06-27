using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class NullOrderingTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void OrderBy_AscendingNullsLast_ShouldPlaceNullAfterValues()
    {
        var result = Run(
            "select Name, City from #A.Entities() order by City nulls last, Name",
            new BasicEntity("Null") { City = null },
            new BasicEntity("Berlin") { City = "Berlin" },
            new BasicEntity("Athens") { City = "Athens" });

        CollectionAssert.AreEqual(
            new[] { "Athens", "Berlin", "Null" },
            result.Select(row => (string)row[0]).ToArray());
    }

    [TestMethod]
    public void OrderBy_DescendingNullsFirstThroughAlias_ShouldPlaceNullBeforeValues()
    {
        var result = Run(
            "select Name, City as SortCity from #A.Entities() order by SortCity desc nulls first, Name",
            new BasicEntity("Null") { City = null },
            new BasicEntity("Berlin") { City = "Berlin" },
            new BasicEntity("Athens") { City = "Athens" });

        CollectionAssert.AreEqual(
            new[] { "Null", "Berlin", "Athens" },
            result.Select(row => (string)row[0]).ToArray());
    }

    [TestMethod]
    public void OrderBy_MultipleKeysWithNullableValue_ShouldApplyEachNullPolicy()
    {
        var result = Run(
            "select Name, Country, NullableValue from #A.Entities() order by Country nulls last, NullableValue desc nulls first, Name",
            new BasicEntity("A") { Country = "PL", NullableValue = 2 },
            new BasicEntity("B") { Country = null, NullableValue = 9 },
            new BasicEntity("C") { Country = "PL", NullableValue = null },
            new BasicEntity("D") { Country = "US", NullableValue = 1 });

        CollectionAssert.AreEqual(
            new[] { "C", "A", "D", "B" },
            result.Select(row => (string)row[0]).ToArray());
    }

    [TestMethod]
    public void WindowRowNumber_DescendingNullsFirst_ShouldUseExplicitNullOrdering()
    {
        var result = Run(
            "select Name, RowNumber() over (order by City desc nulls first) as RN from #A.Entities()",
            new BasicEntity("Null") { City = null },
            new BasicEntity("Berlin") { City = "Berlin" },
            new BasicEntity("Athens") { City = "Athens" });

        Assert.AreEqual(1L, result.Single(row => (string)row[0] == "Null")[1]);
        Assert.AreEqual(2L, result.Single(row => (string)row[0] == "Berlin")[1]);
        Assert.AreEqual(3L, result.Single(row => (string)row[0] == "Athens")[1]);
    }

    [TestMethod]
    public void WindowRunningAggregate_NullsLast_ShouldUseExplicitNullOrdering()
    {
        var result = Run(
            "select Name, Sum(Population) over (order by NullableValue nulls last rows between unbounded preceding and current row) as RunSum from #A.Entities()",
            new BasicEntity("Null") { NullableValue = null, Population = 10 },
            new BasicEntity("Two") { NullableValue = 2, Population = 20 },
            new BasicEntity("One") { NullableValue = 1, Population = 30 });

        Assert.AreEqual(30m, Convert.ToDecimal(result.Single(row => (string)row[0] == "One")[1]));
        Assert.AreEqual(50m, Convert.ToDecimal(result.Single(row => (string)row[0] == "Two")[1]));
        Assert.AreEqual(60m, Convert.ToDecimal(result.Single(row => (string)row[0] == "Null")[1]));
    }

    private Musoq.Evaluator.Tables.Table Run(string query, params BasicEntity[] rows)
    {
        var vm = CreateAndRunVirtualMachine(query, CreateSingleSource(rows));
        return TableMaterializationTestHelper.Materialize(vm.Run(TestContext.CancellationToken));
    }
}

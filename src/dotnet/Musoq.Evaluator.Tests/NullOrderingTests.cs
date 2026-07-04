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

        TableMaterializationTestHelper.AssertColumns(
            result,
            ("Name", typeof(string)),
            ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            result,
            ["Athens", "Athens"],
            ["Berlin", "Berlin"],
            new object?[] { "Null", null });
    }

    [TestMethod]
    public void OrderBy_DescendingNullsFirstThroughAlias_ShouldPlaceNullBeforeValues()
    {
        var result = Run(
            "select Name, City as SortCity from #A.Entities() order by SortCity desc nulls first, Name",
            new BasicEntity("Null") { City = null },
            new BasicEntity("Berlin") { City = "Berlin" },
            new BasicEntity("Athens") { City = "Athens" });

        TableMaterializationTestHelper.AssertColumns(
            result,
            ("Name", typeof(string)),
            ("SortCity", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            result,
            new object?[] { "Null", null },
            ["Berlin", "Berlin"],
            ["Athens", "Athens"]);
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

        TableMaterializationTestHelper.AssertColumns(
            result,
            ("Name", typeof(string)),
            ("Country", typeof(string)),
            ("NullableValue", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            result,
            new object?[] { "C", "PL", null },
            ["A", "PL", 2],
            ["D", "US", 1],
            new object?[] { "B", null, 9 });
    }

    [TestMethod]
    public void WindowRowNumber_DescendingNullsFirst_ShouldUseExplicitNullOrdering()
    {
        var result = Run(
            "select Name, RowNumber() over (order by City desc nulls first) as RN from #A.Entities()",
            new BasicEntity("Null") { City = null },
            new BasicEntity("Berlin") { City = "Berlin" },
            new BasicEntity("Athens") { City = "Athens" });

        TableMaterializationTestHelper.AssertColumns(
            result,
            ("Name", typeof(string)),
            ("RN", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            result,
            ["Null", 1L],
            ["Berlin", 2L],
            ["Athens", 3L]);
    }

    [TestMethod]
    public void WindowRunningAggregate_NullsLast_ShouldUseExplicitNullOrdering()
    {
        var result = Run(
            "select Name, Sum(Population) over (order by NullableValue nulls last rows between unbounded preceding and current row) as RunSum from #A.Entities()",
            new BasicEntity("Null") { NullableValue = null, Population = 10 },
            new BasicEntity("Two") { NullableValue = 2, Population = 20 },
            new BasicEntity("One") { NullableValue = 1, Population = 30 });

        TableMaterializationTestHelper.AssertColumns(
            result,
            ("Name", typeof(string)),
            ("RunSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            result,
            ["One", 30m],
            ["Two", 50m],
            ["Null", 60m]);
    }

    private Musoq.Evaluator.Tables.Table Run(string query, params BasicEntity[] rows)
    {
        var vm = CreateAndRunVirtualMachine(query, CreateSingleSource(rows));
        return TableMaterializationTestHelper.Materialize(vm.Run(TestContext.CancellationToken));
    }
}

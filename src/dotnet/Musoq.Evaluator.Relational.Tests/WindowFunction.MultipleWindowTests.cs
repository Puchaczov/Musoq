using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public class WindowFunctionMultipleWindowTests : BasicEntityTestBase
{
    [TestMethod]
    public void WhenTwoWindowsInSelect_ShouldComputeBothIndependently()
    {
        var query = (
            "\n"
            + "            select Name,\n"
            + "                   RowNumber() over (order by Name) as RowNum,\n"
            + "                   Sum(Population) over (order by Name) as RunSum\n"
            + "            from #A.entities()"
        );

        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { Population = 300 },
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 }
        );

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("RowNum", typeof(long)),
            ("RunSum", typeof(decimal?))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 1L, 100m],
            ["Bob", 2L, 300m],
            ["Charlie", 3L, 600m]
        );
    }

    [TestMethod]
    public void WhenThreeRankingWindows_ShouldComputeAllCorrectly()
    {
        var query = (
            "\n"
            + "            select Name,\n"
            + "                   RowNumber() over (order by Population) as RowNum,\n"
            + "                   Rank() over (order by Population) as RankVal,\n"
            + "                   DenseRank() over (order by Population) as DenseVal\n"
            + "            from #A.entities()"
        );

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 },
            new BasicEntity("Charlie") { Population = 200 },
            new BasicEntity("Diana") { Population = 300 }
        );

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("RowNum", typeof(long)),
            ("RankVal", typeof(long)),
            ("DenseVal", typeof(long))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 1L, 1L, 1L],
            ["Bob", 2L, 2L, 2L],
            ["Charlie", 3L, 2L, 2L],
            ["Diana", 4L, 4L, 3L]
        );
    }

    [TestMethod]
    public void WhenSameFunctionDifferentPartitions_ShouldComputeSeparately()
    {
        var query = (
            "\n"
            + "            select Name, City,\n"
            + "                   Sum(Population) over (partition by City) as CityTotal,\n"
            + "                   Sum(Population) over () as GrandTotal\n"
            + "            from #A.entities()"
        );

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "LA", Population = 100 },
            new BasicEntity("Bob") { City = "NYC", Population = 200 },
            new BasicEntity("Charlie") { City = "LA", Population = 300 }
        );

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("City", typeof(string)),
            ("CityTotal", typeof(decimal?)),
            ("GrandTotal", typeof(decimal?))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "LA", 400m, 600m],
            ["Bob", "NYC", 200m, 600m],
            ["Charlie", "LA", 400m, 600m]
        );
    }

    [TestMethod]
    public void WhenLagAndLeadTogether_ShouldComputeBothCorrectly()
    {
        var query = (
            "\n"
            + "            select Name,\n"
            + "                   Lag(Population) over (order by Name) as PrevPop,\n"
            + "                   Lead(Population) over (order by Name) as NextPop\n"
            + "            from #A.entities()"
        );

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 },
            new BasicEntity("Charlie") { Population = 300 }
        );

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("PrevPop", typeof(decimal?)),
            ("NextPop", typeof(decimal?))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", null, 200m],
            ["Bob", 100m, 300m],
            ["Charlie", 200m, null]
        );
    }

    [TestMethod]
    public void WhenWindowFunctionOnExpressionArgument_ShouldComputeCorrectly()
    {
        var query = (
            "\n"
            + "            select Name, Sum(Population * 2) over (order by Name) as DoubledRunSum\n"
            + "            from #A.entities()"
        );

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 },
            new BasicEntity("Charlie") { Population = 300 }
        );

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("DoubledRunSum", typeof(decimal?))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Alice", 200m], ["Bob", 600m], ["Charlie", 1200m]);
    }

    [TestMethod]
    public void WhenWindowAliasInOuterOrderBy_ShouldSortByRunningSum()
    {
        var query = (
            "\n"
            + "            select Name, Sum(Population) over (order by Name) as RunSum\n"
            + "            from #A.entities()\n"
            + "            order by RunSum desc"
        );

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 },
            new BasicEntity("Charlie") { Population = 300 }
        );

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("RunSum", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Charlie", 600m], ["Bob", 300m], ["Alice", 100m]);
    }

    [TestMethod]
    public void WhenMultipleOrderByColumnsInWindow_ShouldSortByAll()
    {
        var query = (
            "\n"
            + "            select Name, City, RowNumber() over (order by City, Name) as RowNum\n"
            + "            from #A.entities()"
        );

        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { City = "NYC" },
            new BasicEntity("Alice") { City = "LA" },
            new BasicEntity("Bob") { City = "NYC" },
            new BasicEntity("Diana") { City = "LA" }
        );

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("City", typeof(string)),
            ("RowNum", typeof(long))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "LA", 1L],
            ["Diana", "LA", 2L],
            ["Bob", "NYC", 3L],
            ["Charlie", "NYC", 4L]
        );
    }
}

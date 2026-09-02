using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class WindowFunctionEdgeCaseTests : BasicEntityTestBase
{

    [TestMethod]
    public void WhenEmptySource_ShouldReturnEmptyTable()
    {
        var query = "select Name, RowNumber() over (order by Name) as RowNum from #A.entities()";

        var sources = CreateSingleSource();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("RowNum", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table);
    }

    [TestMethod]
    public void WhenSingleRow_ShouldHandleWindowFunctions()
    {
        var query = @"
            select Name,
                   RowNumber() over (order by Name) as RowNum,
                   Lag(Population) over (order by Name) as PrevPop,
                   Lead(Population) over (order by Name) as NextPop
            from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 100 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)), ("RowNum", typeof(long)),
            ("PrevPop", typeof(decimal?)), ("NextPop", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table, ["Alice", 1L, null, null]);
    }

    [TestMethod]
    public void WhenAllRowsSamePartition_ShouldTreatAsOneGroup()
    {
        var query = @"
            select Name, City, RowNumber() over (partition by City order by Name) as RowNum
            from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { City = "NYC" },
            new BasicEntity("Alice") { City = "NYC" },
            new BasicEntity("Bob") { City = "NYC" });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)), ("City", typeof(string)), ("RowNum", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "NYC", 1L], ["Bob", "NYC", 2L], ["Charlie", "NYC", 3L]);
    }

    [TestMethod]
    public void WhenLagOnStringColumn_ShouldReturnPreviousString()
    {
        var query = "select Name, Lag(Name) over (order by Name) as PrevName from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie"),
            new BasicEntity("Alice"),
            new BasicEntity("Bob"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table, ("Name", typeof(string)), ("PrevName", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", null], ["Bob", "Alice"], ["Charlie", "Bob"]);
    }

    [TestMethod]
    public void WhenLeadOnStringColumn_ShouldReturnNextString()
    {
        var query = "select Name, Lead(Name) over (order by Name) as NextName from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie"),
            new BasicEntity("Alice"),
            new BasicEntity("Bob"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table, ("Name", typeof(string)), ("NextName", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "Bob"], ["Bob", "Charlie"], ["Charlie", null]);
    }

    [TestMethod]
    public void WhenNullInPartitionColumn_ShouldGroupNullsSeparately()
    {
        var query = @"
            select Name, City, RowNumber() over (partition by City order by Name) as RowNum
            from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "LA" },
            new BasicEntity("Bob") { City = null },
            new BasicEntity("Charlie") { City = null },
            new BasicEntity("Diana") { City = "LA" });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)), ("City", typeof(string)), ("RowNum", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "LA", 1L], ["Diana", "LA", 2L],
            ["Bob", null, 1L], ["Charlie", null, 2L]);
    }

    [TestMethod]
    public void WhenLargeDataset_ShouldProcessCorrectly()
    {
        var entities = Enumerable.Range(1, 100)
            .Select(i => new BasicEntity($"Name{i:D3}") { Population = i })
            .ToArray();

        var query = @"
            select Name, RowNumber() over (order by Name) as RowNum,
                   Sum(Population) over (order by Name) as RunSum
            from #A.entities()";

        var sources = CreateSingleSource(entities);

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)), ("RowNum", typeof(long)), ("RunSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            Enumerable.Range(1, 100)
                .Select(i => new object?[] { $"Name{i:D3}", (long)i, i * (i + 1) / 2m })
                .ToArray());
    }

    [TestMethod]
    public void WhenWhereEliminatesAllRows_ShouldReturnEmptyTable()
    {
        var query = @"
            select Name, RowNumber() over (order by Name) as RowNum
            from #A.entities()
            where 1 = 0";

        var sources = CreateSingleSource(
            new BasicEntity("Alice"),
            new BasicEntity("Bob"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table, ("Name", typeof(string)), ("RowNum", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table);
    }

    [TestMethod]
    public void WhenRunningAggregateWithDescOrder_ShouldAccumulateInDescOrder()
    {
        var query = @"
            select Name, Sum(Population) over (order by Population desc) as RunSum
            from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 },
            new BasicEntity("Charlie") { Population = 300 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table, ("Name", typeof(string)), ("RunSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Charlie", 300m], ["Bob", 500m], ["Alice", 600m]);
    }

    [TestMethod]
    public void WhenCustomRunningProductWindowFunction_ShouldComputeCorrectly()
    {
        var query = @"
            select Name, RunningProduct(Population) over (order by Name) as Product
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { Population = 4 },
            new BasicEntity("Alice") { Population = 2 },
            new BasicEntity("Bob") { Population = 3 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table, ("Name", typeof(string)), ("Product", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 2m], ["Bob", 6m], ["Charlie", 24m]);
    }

    [TestMethod]
    public void WhenCustomWindowUsesImplicitOrExplicitRange_ShouldPublishCompletePeerGroup()
    {
        const string query = @"
            select Name,
                   RunningProduct(Population) over (order by NullableValue) as ImplicitProduct,
                   RunningProduct(Population) over (
                       order by NullableValue
                       range between unbounded preceding and current row) as ExplicitProduct
            from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("Alice") { NullableValue = 1, Population = 2 },
            new BasicEntity("Bob") { NullableValue = 1, Population = 3 },
            new BasicEntity("Charlie") { NullableValue = 2, Population = 4 });

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 6m, 6m],
            ["Bob", 6m, 6m],
            ["Charlie", 24m, 24m]);
    }

    [TestMethod]
    public void WhenCustomRunningProductWithPartition_ShouldResetPerPartition()
    {
        var query = @"
            select Name, City, RunningProduct(Population) over (partition by City order by Name) as Product
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { City = "NYC", Population = 5 },
            new BasicEntity("Alice") { City = "LA", Population = 2 },
            new BasicEntity("Bob") { City = "NYC", Population = 3 },
            new BasicEntity("Diana") { City = "LA", Population = 4 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)), ("City", typeof(string)), ("Product", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "LA", 2m], ["Diana", "LA", 8m],
            ["Bob", "NYC", 3m], ["Charlie", "NYC", 15m]);
    }

    [TestMethod]
    public void WhenCustomRunningProductWithBuiltInWindow_ShouldComputeBothCorrectly()
    {
        var query = @"
            select Name,
                   RunningProduct(Population) over (order by Name) as Product,
                   Sum(Population) over (order by Name) as RunningSum
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { Population = 4 },
            new BasicEntity("Alice") { Population = 2 },
            new BasicEntity("Bob") { Population = 3 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)), ("Product", typeof(decimal)), ("RunningSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 2m, 2m], ["Bob", 6m, 5m], ["Charlie", 24m, 9m]);
    }

    [TestMethod]
    public void WhenWindowFunctionInWhereClause_ShouldThrow()
    {
        var query = "select Name from #A.entities() where RowNumber() over (order by Name) = 1";

        var sources = CreateSingleSource(
            new BasicEntity("Alice"),
            new BasicEntity("Bob"));

        Assert.Throws<Exception>(() =>
        {
            var vm = CreateAndRunVirtualMachine(query, sources);
            vm.Run(TestContext.CancellationToken);
        });
    }

    [TestMethod]
    public void WhenNestedWindowInArgument_ShouldThrow()
    {
        var query = @"
            select Name, Sum(RowNumber() over (order by Name)) over (order by Name)
            from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice"),
            new BasicEntity("Bob"));

        Assert.Throws<Exception>(() =>
        {
            var vm = CreateAndRunVirtualMachine(query, sources);
            vm.Run(TestContext.CancellationToken);
        });
    }

    [TestMethod]
    public void WhenUnsupportedFunctionWithOver_ShouldThrow()
    {
        var query = "select Name, ToUpper(Name) over (order by Name) from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice"),
            new BasicEntity("Bob"));

        Assert.Throws<Exception>(() =>
        {
            var vm = CreateAndRunVirtualMachine(query, sources);
            vm.Run(TestContext.CancellationToken);
        });
    }
}

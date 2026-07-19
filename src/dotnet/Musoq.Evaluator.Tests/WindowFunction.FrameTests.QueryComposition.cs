using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class WindowFunctionFrameTests
{
    [TestMethod]
    public void WhenMultipleWindowsUseDifferentPartitions_ShouldPreserveRowAssociations()
    {
        const string query = @"
            select Name, City,
                   RowNumber() over (order by Name) as GlobalNo,
                   RowNumber() over (partition by City order by Name) as CityNo,
                   Lag(Population) over (partition by City order by Name) as PreviousPopulation
            from #A.entities()
            order by Name";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "LA", Population = 100m },
            new BasicEntity("Bob") { City = "LA", Population = 200m },
            new BasicEntity("Charlie") { City = "NYC", Population = 300m },
            new BasicEntity("Diana") { City = "NYC", Population = 400m });

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("City", typeof(string)),
            ("GlobalNo", typeof(long)),
            ("CityNo", typeof(long)),
            ("PreviousPopulation", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Alice", "LA", 1L, 1L, null],
            ["Bob", "LA", 2L, 2L, 100m],
            ["Charlie", "NYC", 3L, 1L, null],
            ["Diana", "NYC", 4L, 2L, 300m]);
    }

    [TestMethod]
    public void WhenWindowCountsNullableValuesAcrossAFrame_ShouldPreserveNullRows()
    {
        const string query = @"
            select Name, NullableValue,
                   Count(NullableValue) over (
                       partition by City
                       order by Name
                       rows between unbounded preceding and current row) as NonNullCount
            from #A.entities()
            order by Name";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "LA", NullableValue = 1 },
            new BasicEntity("Bob") { City = "LA", NullableValue = null },
            new BasicEntity("Charlie") { City = "LA", NullableValue = 2 });

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("NullableValue", typeof(int?)),
            ("NonNullCount", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Alice", 1, 1],
            ["Bob", null, 1],
            ["Charlie", 2, 2]);
    }

    [TestMethod]
    public void WhenFrameWithDescOrdering_ShouldComputeCorrectSlidingWindow()
    {
        var query = @"
            select Name, Sum(Population) over (order by Name desc rows between 1 preceding and 1 following) as SlideSum
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 },
            new BasicEntity("Charlie") { Population = 300 },
            new BasicEntity("Diana") { Population = 400 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("SlideSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Diana", 700m],
            ["Charlie", 900m],
            ["Bob", 600m],
            ["Alice", 300m]);
    }

    [TestMethod]
    public void WhenFrameOverInnerJoin_ShouldComputeAcrossJoinedRows()
    {
        var query = @"
            select a.Name, b.City,
                   Sum(a.Population) over (order by a.Name rows between 1 preceding and current row) as RunSum
            from #A.entities() a
            inner join #B.entities() b on a.Name = b.Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [
                new BasicEntity("Alice") { Population = 100 },
                new BasicEntity("Bob") { Population = 200 },
                new BasicEntity("Charlie") { Population = 300 }
            ]},
            { "#B", [
                new BasicEntity("Alice") { City = "NYC" },
                new BasicEntity("Bob") { City = "LA" },
                new BasicEntity("Charlie") { City = "SF" }
            ]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("b.City", typeof(string)),
            ("RunSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "NYC", 100m],
            ["Bob", "LA", 300m],
            ["Charlie", "SF", 500m]);
    }

    [TestMethod]
    public void WhenFrameWithWhereClause_ShouldComputeOverFilteredRows()
    {
        var query = @"
            select Name,
                   Sum(Population) over (order by Name rows between 1 preceding and current row) as RunSum
            from #A.Entities()
            where Population > 100";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 },
            new BasicEntity("Charlie") { Population = 300 },
            new BasicEntity("Diana") { Population = 400 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("RunSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Bob", 200m],
            ["Charlie", 500m],
            ["Diana", 700m]);
    }

    [TestMethod]
    public void WhenFrameInsideCte_ShouldComputeAndPassThrough()
    {
        var query = @"
            with windowed as (
                select Name, Population,
                       Sum(Population) over (order by Name rows between unbounded preceding and current row) as RunSum
                from #A.entities()
            )
            select Name, RunSum from windowed where RunSum > 300";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice") { Population = 100 },
                    new BasicEntity("Bob") { Population = 200 },
                    new BasicEntity("Charlie") { Population = 300 },
                    new BasicEntity("Diana") { Population = 400 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("RunSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Charlie", 600m],
            ["Diana", 1000m]);
    }

    [TestMethod]
    public void WhenFrameWithPartitionAndDescOrder_ShouldComputePerPartition()
    {
        var query = @"
            select City, Name,
                   Sum(Population) over (partition by City order by Name desc rows between 1 preceding and current row) as FrameSum
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "NYC", Population = 100 },
            new BasicEntity("Bob") { City = "NYC", Population = 200 },
            new BasicEntity("Charlie") { City = "LA", Population = 300 },
            new BasicEntity("Diana") { City = "LA", Population = 400 },
            new BasicEntity("Eve") { City = "LA", Population = 500 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("Name", typeof(string)),
            ("FrameSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["NYC", "Bob", 200m],
            ["NYC", "Alice", 300m],
            ["LA", "Eve", 500m],
            ["LA", "Diana", 900m],
            ["LA", "Charlie", 700m]);
    }

}

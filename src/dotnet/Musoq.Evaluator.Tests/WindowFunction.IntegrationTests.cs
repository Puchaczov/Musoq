using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class WindowFunctionIntegrationTests : BasicEntityTestBase
{

    [TestMethod]
    public void WhenWindowInsideCte_ShouldAllowFilteringByRowNumber()
    {
        var query = @"
            with ranked as (
                select Name, RowNumber() over (order by Name) as RowNum from #A.entities()
            )
            select Name, RowNum from ranked where RowNum <= 2";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Charlie"),
                    new BasicEntity("Alice"),
                    new BasicEntity("Bob"),
                    new BasicEntity("Diana")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("RowNum", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 1L],
            ["Bob", 2L]);
    }

    [TestMethod]
    public void WhenWindowOverCteSource_ShouldComputeWindowValues()
    {
        var query = @"
            with p as (
                select City, Country from #A.entities()
            )
                select City, Country, RowNumber() over (order by City) as RowNum from p";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("Country", typeof(string)),
            ("RowNum", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["BERLIN", "GERMANY", 1L],
            ["MUNICH", "GERMANY", 2L],
            ["WARSAW", "POLAND", 3L]);
    }

    [TestMethod]
    public void WhenWindowOverAggregatedCte_ShouldComputeRunningTotal()
    {
        var query = @"
            with agg as (
                select City, Sum(Population) as CityPop from #A.entities() group by City
            )
            select City, CityPop, Sum(CityPop) over (order by City) as RunningPop from agg";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice") { City = "Berlin", Population = 100 },
                    new BasicEntity("Bob") { City = "Berlin", Population = 200 },
                    new BasicEntity("Charlie") { City = "Munich", Population = 300 },
                    new BasicEntity("Diana") { City = "Warsaw", Population = 150 },
                    new BasicEntity("Eve") { City = "Warsaw", Population = 250 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("CityPop", typeof(decimal?)),
            ("RunningPop", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Berlin", 300m, 300m],
            ["Munich", 300m, 600m],
            ["Warsaw", 400m, 1000m]);
    }

    [TestMethod]
    public void WhenWindowOverInnerJoin_ShouldNumberJoinedRows()
    {
        var query = @"
            select a.Name, b.City, RowNumber() over (order by a.Name) as RowNum
            from #A.entities() a
            inner join #B.entities() b on a.Id = b.Id";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [
                new BasicEntity("Alice") { Id = 1 },
                new BasicEntity("Bob") { Id = 2 },
                new BasicEntity("Charlie") { Id = 3 }
            ]},
            { "#B", [
                new BasicEntity("x") { Id = 1, City = "NYC" },
                new BasicEntity("y") { Id = 2, City = "LA" },
                new BasicEntity("z") { Id = 4, City = "SF" }
            ]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("b.City", typeof(string)),
            ("RowNum", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "NYC", 1L],
            ["Bob", "LA", 2L]);
    }

    [TestMethod]
    public void WhenWindowPartitionByJoinedColumn_ShouldPartitionCorrectly()
    {
        var query = @"
            select a.Name, b.City, RowNumber() over (partition by b.City order by a.Name) as RowNum
            from #A.entities() a
            inner join #B.entities() b on a.Id = b.Id";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [
                new BasicEntity("Alice") { Id = 1 },
                new BasicEntity("Bob") { Id = 2 },
                new BasicEntity("Charlie") { Id = 3 },
                new BasicEntity("Diana") { Id = 4 }
            ]},
            { "#B", [
                new BasicEntity("x") { Id = 1, City = "NYC" },
                new BasicEntity("y") { Id = 2, City = "LA" },
                new BasicEntity("z") { Id = 3, City = "NYC" },
                new BasicEntity("w") { Id = 4, City = "LA" }
            ]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("b.City", typeof(string)),
            ("RowNum", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "NYC", 1L],
            ["Charlie", "NYC", 2L],
            ["Bob", "LA", 1L],
            ["Diana", "LA", 2L]);
    }

    [TestMethod]
    public void WhenOuterOrderByWindowAlias_ShouldSortByWindowResult()
    {
        var query = @"
            select Name, RowNumber() over (order by Name) as RowNum
            from #A.entities()
            order by RowNum desc";

        var sources = CreateSingleSource(
            new BasicEntity("Alice"),
            new BasicEntity("Bob"),
            new BasicEntity("Charlie"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("RowNum", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Charlie", 3L],
            ["Bob", 2L],
            ["Alice", 1L]);
    }

    [TestMethod]
    public void WhenWindowWithSkip_ShouldSkipRows()
    {
        var query = @"
            select Name, RowNumber() over (order by Name) as RowNum
            from #A.entities()
            skip 1";

        var sources = CreateSingleSource(
            new BasicEntity("Alice"),
            new BasicEntity("Bob"),
            new BasicEntity("Charlie"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("RowNum", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Bob", 2L],
            ["Charlie", 3L]);
    }

    [TestMethod]
    public void WhenWindowWithTake_ShouldLimitRows()
    {
        var query = @"
            select Name, RowNumber() over (order by Name) as RowNum
            from #A.entities()
            take 2";

        var sources = CreateSingleSource(
            new BasicEntity("Alice"),
            new BasicEntity("Bob"),
            new BasicEntity("Charlie"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("RowNum", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Alice", 1L],
            ["Bob", 2L]);
    }

    [TestMethod]
    public void WhenWindowWithSkipAndTake_ShouldPaginate()
    {
        var query = @"
            select Name, RowNumber() over (order by Name) as RowNum
            from #A.entities()
            order by RowNum asc
            skip 1
            take 1";

        var sources = CreateSingleSource(
            new BasicEntity("Alice"),
            new BasicEntity("Bob"),
            new BasicEntity("Charlie"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("RowNum", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Bob", 2L]);
    }

    [TestMethod]
    public void WhenDistinctWithWindow_ShouldDeduplicateWindowResults()
    {
        var query = @"
            select distinct City, Count(Name) over (partition by City) as CityCount
            from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "LA" },
            new BasicEntity("Bob") { City = "NYC" },
            new BasicEntity("Charlie") { City = "LA" },
            new BasicEntity("Diana") { City = "NYC" },
            new BasicEntity("Eve") { City = "NYC" });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("CityCount", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["LA", 2],
            ["NYC", 3]);
    }

    [TestMethod]
    public void WhenCaseWhenOnWindowResult_ShouldEvaluateCorrectly()
    {
        var query = @"
            select Name,
                   RowNumber() over (order by Name) as RowNum,
                   case when RowNumber() over (order by Name) <= 2 then 'Top' else 'Bottom' end as Category
            from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice"),
            new BasicEntity("Bob"),
            new BasicEntity("Charlie"),
            new BasicEntity("Diana"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("RowNum", typeof(long)),
            ("Category", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 1L, "Top"],
            ["Bob", 2L, "Top"],
            ["Charlie", 3L, "Bottom"],
            ["Diana", 4L, "Bottom"]);
    }

    [TestMethod]
    public void WhenReorderedSyntaxWithWindow_ShouldWork()
    {
        var query = "from #A.entities() select Name, RowNumber() over (order by Name) as RowNum";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie"),
            new BasicEntity("Alice"),
            new BasicEntity("Bob"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("RowNum", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 1L],
            ["Bob", 2L],
            ["Charlie", 3L]);
    }

    [TestMethod]
    public void WhenWindowOverUnionCte_ShouldNumberCombinedRows()
    {
        var query = @"
            with combined as (
                select City, Country from #A.entities()
                union (Country, City)
                select City, Country from #B.entities()
            )
            select City, Country, RowNumber() over (order by City) as rn from combined";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            },
            {
                "#B", [
                    new BasicEntity("MUNICH", "GERMANY", 350),
                    new BasicEntity("PARIS", "FRANCE", 200)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("Country", typeof(string)),
            ("rn", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["BERLIN", "GERMANY", 1L],
            ["MUNICH", "GERMANY", 2L],
            ["PARIS", "FRANCE", 3L],
            ["WARSAW", "POLAND", 4L]);
    }

    [TestMethod]
    public void WhenFrameOverCteWithOrderBy_ShouldComputeRunningSum()
    {
        var query = @"
            with data as (
                select Name, Population from #A.entities()
            )
            select Name,
                   Sum(Population) over (order by Name rows between unbounded preceding and current row) as RunSum
            from data
            order by Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Charlie") { Population = 300 },
                    new BasicEntity("Alice") { Population = 100 },
                    new BasicEntity("Bob") { Population = 200 },
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
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Alice", 100m],
            ["Bob", 300m],
            ["Charlie", 600m],
            ["Diana", 1000m]);
    }

    [TestMethod]
    public void WhenQualifyWithCteAndWindow_ShouldFilterInsideCte()
    {
        var query = @"
            with top2 as (
                select Name, City, RowNumber() over (partition by City order by Name) as rn
                from #A.entities()
                qualify RowNumber() over (partition by City order by Name) <= 2
            )
            select Name, City, rn from top2 order by City, rn";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice") { City = "LA" },
                    new BasicEntity("Bob") { City = "LA" },
                    new BasicEntity("Charlie") { City = "LA" },
                    new BasicEntity("Diana") { City = "NYC" },
                    new BasicEntity("Eve") { City = "NYC" },
                    new BasicEntity("Frank") { City = "NYC" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("City", typeof(string)),
            ("rn", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Alice", "LA", 1L],
            ["Bob", "LA", 2L],
            ["Diana", "NYC", 1L],
            ["Eve", "NYC", 2L]);
    }

    [TestMethod]
    public void WhenWindowWithJoinAndOrderByAlias_ShouldSortCorrectly()
    {
        var query = @"
            select a.Name, b.City,
                   RowNumber() over (order by a.Name) as rn
            from #A.entities() a
            inner join #B.entities() b on a.Id = b.Id
            order by rn desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [
                new BasicEntity("Alice") { Id = 1 },
                new BasicEntity("Bob") { Id = 2 },
                new BasicEntity("Charlie") { Id = 3 }
            ]},
            { "#B", [
                new BasicEntity("x") { Id = 1, City = "NYC" },
                new BasicEntity("y") { Id = 2, City = "LA" },
                new BasicEntity("z") { Id = 3, City = "SF" }
            ]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("b.City", typeof(string)),
            ("rn", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Charlie", "SF", 3L],
            ["Bob", "LA", 2L],
            ["Alice", "NYC", 1L]);
    }
}

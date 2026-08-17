using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class QualifyTests
{
    [TestMethod]
    public void WhenQualifyInsideCte_ShouldFilterBeforeOuterQuery()
    {
        var query = @"
            with ranked as (
                select Name, City, RowNumber() over (partition by City order by Name) as rn
                from #A.entities()
                qualify RowNumber() over (partition by City order by Name) <= 1
            )
            select Name, City from ranked";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice") { City = "NYC" },
                    new BasicEntity("Bob") { City = "NYC" },
                    new BasicEntity("Charlie") { City = "LA" },
                    new BasicEntity("Diana") { City = "LA" },
                    new BasicEntity("Eve") { City = "SF" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "NYC"],
            ["Charlie", "LA"],
            ["Eve", "SF"]);
    }

    [TestMethod]
    public void WhenQualifyWithInnerJoin_ShouldFilterJoinedResult()
    {
        var query = @"
            select a.Name, b.City, RowNumber() over (partition by b.City order by a.Name) as rn
            from #A.entities() a
            inner join #B.entities() b on a.Id = b.Id
            qualify RowNumber() over (partition by b.City order by a.Name) <= 1";

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
            ("rn", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "NYC", 1L],
            ["Bob", "LA", 1L]);
    }

    [TestMethod]
    public void WhenQualifyWithOrderBy_ShouldSortAfterFiltering()
    {
        var query = @"
            select Name, RowNumber() over (order by Name) as rn
            from #A.Entities()
            qualify RowNumber() over (order by Name) <= 3
            order by rn desc";

        var sources = CreateSingleSource(
            new BasicEntity("Eve"),
            new BasicEntity("Charlie"),
            new BasicEntity("Alice"),
            new BasicEntity("Bob"),
            new BasicEntity("Diana"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("rn", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Charlie", 3L],
            ["Bob", 2L],
            ["Alice", 1L]);
    }

    [TestMethod]
    public void WhenQualifyWithNotEquals_ShouldExcludeMatching()
    {
        var query = @"
            select Name, RowNumber() over (order by Name) as rn
            from #A.Entities()
            qualify RowNumber() over (order by Name) != 2";

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
            ("rn", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Alice", 1L],
            ["Charlie", 3L],
            ["Diana", 4L]);
    }

    [TestMethod]
    public void WhenMultipleWindowFunctionsWithQualifyOnOne_ShouldFilterCorrectly()
    {
        var query = @"
            select Name, City,
                   RowNumber() over (partition by City order by Name) as rn,
                   Count(Name) over (partition by City) as cnt
            from #A.Entities()
            qualify RowNumber() over (partition by City order by Name) <= 1";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "NYC" },
            new BasicEntity("Bob") { City = "NYC" },
            new BasicEntity("Charlie") { City = "LA" },
            new BasicEntity("Diana") { City = "LA" },
            new BasicEntity("Eve") { City = "LA" });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("City", typeof(string)),
            ("rn", typeof(long)),
            ("cnt", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "NYC", 1L, 2],
            ["Charlie", "LA", 1L, 3]);
    }

}

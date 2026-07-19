using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class ThirdRoundWindowResultTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void RowsAndRangeWithPeerValues_ShouldPreserveCompleteRowAssociations()
    {
        const string query = @"
            select Name, Population,
                   Sum(Population) over (
                       order by Population, Name
                       rows between unbounded preceding and current row) as RowsSum,
                   Sum(Population) over (
                       order by Population, Name
                       range between unbounded preceding and current row) as RangeSum
            from #A.entities()
            order by Population, Name";

        var table = CreateAndRunVirtualMachine(
            query,
            CreateSingleSource(
                new BasicEntity("Charlie") { Population = 200 },
                new BasicEntity("Alice") { Population = 100 },
                new BasicEntity("Bob") { Population = 100 }))
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Population", typeof(decimal)),
            ("RowsSum", typeof(decimal)),
            ("RangeSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Alice", 100m, 100m, 100m],
            ["Bob", 100m, 200m, 200m],
            ["Charlie", 200m, 400m, 400m]);
    }

    [TestMethod]
    public void MultipleWindowsAndQualify_ShouldApplyWindowBeforeFilterAndRetainShape()
    {
        const string query = @"
            select Name,
                   RowNumber() over (order by Name) as RowNumber,
                   Sum(Population) over (order by Name) as RunningPopulation
            from #A.entities()
            qualify RowNumber() over (order by Name) <= 2
            order by Name";

        var table = CreateAndRunVirtualMachine(
            query,
            CreateSingleSource(
                new BasicEntity("Charlie") { Population = 300 },
                new BasicEntity("Alice") { Population = 100 },
                new BasicEntity("Bob") { Population = 200 }))
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("RowNumber", typeof(long)),
            ("RunningPopulation", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Alice", 1L, 100m],
            ["Bob", 2L, 300m]);
    }

    [TestMethod]
    public void ValueAccessWindowsWithFrame_ShouldPreserveNullAndPeerResults()
    {
        const string query = @"
            select Name,
                   FirstValue(Name) over (
                       order by Name rows between 1 preceding and 1 following) as FirstInFrame,
                   LastValue(Name) over (
                       order by Name rows between 1 preceding and 1 following) as LastInFrame,
                   NthValue(Name, 2) over (
                       order by Name rows between 1 preceding and 1 following) as SecondInFrame
            from #A.entities()
            order by Name";

        var table = CreateAndRunVirtualMachine(
            query,
            CreateSingleSource(
                new BasicEntity("Diana"),
                new BasicEntity("Charlie"),
                new BasicEntity("Alice"),
                new BasicEntity("Bob")))
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("FirstInFrame", typeof(string)),
            ("LastInFrame", typeof(string)),
            ("SecondInFrame", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Alice", "Alice", "Bob", "Bob"],
            ["Bob", "Alice", "Charlie", "Bob"],
            ["Charlie", "Bob", "Diana", "Charlie"],
            ["Diana", "Charlie", "Diana", "Diana"]);
    }

    [TestMethod]
    public void WindowOverCteAndSetOperation_ShouldKeepBranchRanksAndOuterOrder()
    {
        const string query = @"
            with ranked as (
                select Name, Country,
                       RowNumber() over (partition by Country order by Name) as BranchRank
                from #A.entities()
                union all (Name, Country, BranchRank)
                select Name, Country,
                       RowNumber() over (partition by Country order by Name) as BranchRank
                from #B.entities()
            )
            select Country, Name, BranchRank,
                   RowNumber() over (partition by Country order by BranchRank, Name) as CombinedRank
            from ranked
            order by Country, CombinedRank, Name";

        var sources = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { Name = "Alpha", Country = "DE", Population = 100 },
                new BasicEntity { Name = "Beta", Country = "DE", Population = 200 }
            ],
            ["#B"] =
            [
                new BasicEntity { Name = "Gamma", Country = "DE", Population = 300 },
                new BasicEntity { Name = "Delta", Country = "FR", Population = 400 }
            ]
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("Name", typeof(string)),
            ("BranchRank", typeof(long)),
            ("CombinedRank", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["DE", "Alpha", 1L, 1L],
            ["DE", "Gamma", 1L, 2L],
            ["DE", "Beta", 2L, 3L],
            ["FR", "Delta", 1L, 1L]);
    }
}

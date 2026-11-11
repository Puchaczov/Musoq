using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class ThirdRoundWindowResultTests : BasicEntityTestBase
{
    [TestMethod]
    public void RowsAndRangeWithPeerValues_ShouldPreserveCompleteRowAssociations()
    {
        const string query = (
            "\n"
            + "            select Name, Population,\n"
            + "                   Sum(Population) over (\n"
            + "                       order by Population, Name\n"
            + "                       rows between unbounded preceding and current row) as RowsSum,\n"
            + "                   Sum(Population) over (\n"
            + "                       order by Population, Name\n"
            + "                       range between unbounded preceding and current row) as RangeSum\n"
            + "            from #A.entities()\n"
            + "            order by Population, Name"
        );

        var table = CreateAndRunVirtualMachine(
                query,
                CreateSingleSource(
                    new BasicEntity("Charlie") { Population = 200 },
                    new BasicEntity("Alice") { Population = 100 },
                    new BasicEntity("Bob") { Population = 100 }
                )
            )
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Population", typeof(decimal)),
            ("RowsSum", typeof(decimal?)),
            ("RangeSum", typeof(decimal?))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Alice", 100m, 100m, 100m],
            ["Bob", 100m, 200m, 200m],
            ["Charlie", 200m, 400m, 400m]
        );
    }

    [TestMethod]
    public void MultipleWindowsAndQualify_ShouldApplyWindowBeforeFilterAndRetainShape()
    {
        const string query = (
            "\n"
            + "            select Name,\n"
            + "                   RowNumber() over (order by Name) as RowNumber,\n"
            + "                   Sum(Population) over (order by Name) as RunningPopulation\n"
            + "            from #A.entities()\n"
            + "            qualify RowNumber() over (order by Name) <= 2\n"
            + "            order by Name"
        );

        var table = CreateAndRunVirtualMachine(
                query,
                CreateSingleSource(
                    new BasicEntity("Charlie") { Population = 300 },
                    new BasicEntity("Alice") { Population = 100 },
                    new BasicEntity("Bob") { Population = 200 }
                )
            )
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("RowNumber", typeof(long)),
            ("RunningPopulation", typeof(decimal?))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Alice", 1L, 100m], ["Bob", 2L, 300m]);
    }

    [TestMethod]
    public void ValueAccessWindowsWithFrame_ShouldPreserveNullAndPeerResults()
    {
        const string query = (
            "\n"
            + "            select Name,\n"
            + "                   FirstValue(Name) over (\n"
            + "                       order by Name rows between 1 preceding and 1 following) as FirstInFrame,\n"
            + "                   LastValue(Name) over (\n"
            + "                       order by Name rows between 1 preceding and 1 following) as LastInFrame,\n"
            + "                   NthValue(Name, 2) over (\n"
            + "                       order by Name rows between 1 preceding and 1 following) as SecondInFrame\n"
            + "            from #A.entities()\n"
            + "            order by Name"
        );

        var table = CreateAndRunVirtualMachine(
                query,
                CreateSingleSource(
                    new BasicEntity("Diana"),
                    new BasicEntity("Charlie"),
                    new BasicEntity("Alice"),
                    new BasicEntity("Bob")
                )
            )
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("FirstInFrame", typeof(string)),
            ("LastInFrame", typeof(string)),
            ("SecondInFrame", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Alice", "Alice", "Bob", "Bob"],
            ["Bob", "Alice", "Charlie", "Bob"],
            ["Charlie", "Bob", "Diana", "Charlie"],
            ["Diana", "Charlie", "Diana", "Diana"]
        );
    }

    [TestMethod]
    public void WindowOverCteAndSetOperation_ShouldKeepBranchRanksAndOuterOrder()
    {
        const string query = (
            "\n"
            + "            with ranked as (\n"
            + "                select Name, Country,\n"
            + "                       RowNumber() over (partition by Country order by Name) as BranchRank\n"
            + "                from #A.entities()\n"
            + "                union all (Name, Country, BranchRank)\n"
            + "                select Name, Country,\n"
            + "                       RowNumber() over (partition by Country order by Name) as BranchRank\n"
            + "                from #B.entities()\n"
            + "            )\n"
            + "            select Country, Name, BranchRank,\n"
            + "                   RowNumber() over (partition by Country order by BranchRank, Name) as CombinedRank\n"
            + "            from ranked\n"
            + "            order by Country, CombinedRank, Name"
        );

        var sources = new System.Collections.Generic.Dictionary<
            string,
            System.Collections.Generic.IEnumerable<BasicEntity>
        >
        {
            ["#A"] =
            [
                new BasicEntity
                {
                    Name = "Alpha",
                    Country = "DE",
                    Population = 100,
                },
                new BasicEntity
                {
                    Name = "Beta",
                    Country = "DE",
                    Population = 200,
                },
            ],
            ["#B"] =
            [
                new BasicEntity
                {
                    Name = "Gamma",
                    Country = "DE",
                    Population = 300,
                },
                new BasicEntity
                {
                    Name = "Delta",
                    Country = "FR",
                    Population = 400,
                },
            ],
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("Name", typeof(string)),
            ("BranchRank", typeof(long)),
            ("CombinedRank", typeof(long))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["DE", "Alpha", 1L, 1L],
            ["DE", "Gamma", 1L, 2L],
            ["DE", "Beta", 2L, 3L],
            ["FR", "Delta", 1L, 1L]
        );
    }
}

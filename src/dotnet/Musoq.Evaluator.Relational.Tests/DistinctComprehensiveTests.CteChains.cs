using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Comprehensive tests for DISTINCT keyword in various query scenarios.
///     These tests explore DISTINCT usage in CTEs, nested queries, joins, set operations,
///     and ensure correct deduplication behavior.
/// </summary>
public partial class DistinctComprehensiveTests
{
    [TestMethod]
    public void Distinct_InFirstCte_OfMultipleCtes_ShouldWork()
    {
        var query = (
            "\n"
            + "            with\n"
            + "            cte1 as (\n"
            + "                select distinct Country from #A.Entities()\n"
            + "            ),\n"
            + "            cte2 as (\n"
            + "                select Country from cte1\n"
            + "            )\n"
            + "            select Country from cte2"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Krakow", "Poland", 400),
                    new BasicEntity("Berlin", "Germany", 350),
                ]
            },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Germany"], ["Poland"]);
    }

    [TestMethod]
    public void Distinct_InSecondCte_FromFirstCteWithDuplicates_ShouldDeduplicate()
    {
        var query = (
            "\n"
            + "            with\n"
            + "            cte1 as (\n"
            + "                select Country from #A.Entities()\n"
            + "            ),\n"
            + "            cte2 as (\n"
            + "                select distinct Country from cte1\n"
            + "            )\n"
            + "            select Country from cte2"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Krakow", "Poland", 400),
                    new BasicEntity("Berlin", "Germany", 350),
                ]
            },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Germany"], ["Poland"]);
    }

    [TestMethod]
    public void Distinct_InBothCtes_ShouldDeduplicateAtEachLevel()
    {
        var query = (
            "\n"
            + "            with\n"
            + "            cte1 as (\n"
            + "                select distinct Country from #A.Entities()\n"
            + "            ),\n"
            + "            cte2 as (\n"
            + "                select distinct Country from cte1\n"
            + "            )\n"
            + "            select Country from cte2"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Krakow", "Poland", 400),
                    new BasicEntity("Gdansk", "Poland", 200),
                    new BasicEntity("Berlin", "Germany", 350),
                    new BasicEntity("Munich", "Germany", 400),
                ]
            },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Germany"], ["Poland"]);
    }

    [TestMethod]
    public void Distinct_InTwoIndependentCtes_JoinedInOuterQuery_ShouldWork()
    {
        var query = (
            "\n"
            + "            with\n"
            + "            cte1 as (\n"
            + "                select distinct Country from #A.Entities()\n"
            + "            ),\n"
            + "            cte2 as (\n"
            + "                select distinct Country from #B.Entities()\n"
            + "            )\n"
            + "            select c1.Country as Country1, c2.Country as Country2\n"
            + "            from cte1 c1\n"
            + "            inner join cte2 c2 on c1.Country = c2.Country"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Krakow", "Poland", 400),
                    new BasicEntity("Berlin", "Germany", 350),
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("Lisbon", "Portugal", 600),
                    new BasicEntity("Porto", "Portugal", 400),
                    new BasicEntity("Madrid", "Spain", 700),
                    new BasicEntity("Munich", "Germany", 300),
                ]
            },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Country1", typeof(string)), ("Country2", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Germany", "Germany"]);
    }

    [TestMethod]
    public void Distinct_InDeeplyNestedCtes_ShouldWork()
    {
        var query = (
            "\n"
            + "            with\n"
            + "            cte1 as (\n"
            + "                select Country from #A.Entities()\n"
            + "            ),\n"
            + "            cte2 as (\n"
            + "                select distinct Country from cte1\n"
            + "            ),\n"
            + "            cte3 as (\n"
            + "                select Country from cte2\n"
            + "            )\n"
            + "            select Country from cte3"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Krakow", "Poland", 400),
                    new BasicEntity("Berlin", "Germany", 350),
                    new BasicEntity("Munich", "Germany", 300),
                    new BasicEntity("Paris", "France", 600),
                ]
            },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["France"], ["Germany"], ["Poland"]);
    }

    [TestMethod]
    public void Distinct_AtMultipleLevels_ShouldNotCreateDuplicates()
    {
        var query = (
            "\n"
            + "            with cte as (\n"
            + "                select distinct Country from #A.Entities()\n"
            + "            )\n"
            + "            select distinct Country from cte"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Krakow", "Poland", 400),
                    new BasicEntity("Gdansk", "Poland", 200),
                    new BasicEntity("Poznan", "Poland", 300),
                ]
            },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Poland"]);
    }
}

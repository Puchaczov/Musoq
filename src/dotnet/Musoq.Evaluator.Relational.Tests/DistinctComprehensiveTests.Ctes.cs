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
    public void Distinct_CteAsFilterSource_UsingJoin_ShouldWork()
    {
        var query = (
            "\n"
            + "            with distinctCountries as (\n"
            + "                select distinct Country as Country from #B.Entities()\n"
            + "            )\n"
            + "            select a.City, a.Country from #A.Entities() a\n"
            + "            inner join distinctCountries dc on a.Country = dc.Country"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Berlin", "Germany", 350),
                    new BasicEntity("Paris", "France", 600),
                ]
            },
            { "#B", [new BasicEntity("Poznan", "Poland", 300), new BasicEntity("Poznan", "Poland", 200)] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Warsaw", "Poland"]);
    }

    [TestMethod]
    public void Distinct_InsideCte_WithJoin_ShouldWork()
    {
        var query = (
            "\n"
            + "            with cte as (\n"
            + "                select distinct a.Country as Country\n"
            + "                from #A.Entities() a\n"
            + "                inner join #B.Entities() b on a.Country = b.Country\n"
            + "            )\n"
            + "            select Country from cte"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Krakow", "Poland", 400),
                    new BasicEntity("Berlin", "Germany", 350),
                    new BasicEntity("Paris", "France", 600),
                ]
            },
            { "#B", [new BasicEntity("Poznan", "Poland", 300), new BasicEntity("Munich", "Germany", 400)] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Germany"], ["Poland"]);
    }

    [TestMethod]
    public void Distinct_WithExpressions_InCte_ShouldWork()
    {
        var query = (
            "\n"
            + "            with cte as (\n"
            + "                select distinct ToUpperInvariant(Country) as UpperCountry from #A.Entities()\n"
            + "            )\n"
            + "            select UpperCountry from cte"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Warsaw", "poland", 500),
                    new BasicEntity("Krakow", "Poland", 400),
                    new BasicEntity("Berlin", "GERMANY", 350),
                ]
            },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("UpperCountry", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["GERMANY"], ["POLAND"]);
    }

    [TestMethod]
    public void Distinct_WithSkipTake_InCte_ShouldApplyAfterDistinct()
    {
        var query = (
            "\n"
            + "            with cte as (\n"
            + "                select distinct Country from #A.Entities() order by Country skip 1 take 2\n"
            + "            )\n"
            + "            select Country from cte"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Krakow", "Poland", 400),
                    new BasicEntity("Berlin", "Germany", 350),
                    new BasicEntity("Paris", "France", 600),
                    new BasicEntity("Madrid", "Spain", 700),
                ]
            },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Germany"], ["Poland"]);
    }

    [TestMethod]
    public void Distinct_InCte_ReorderedSyntax_ShouldWork()
    {
        var query = (
            "\n"
            + "            with cte as (\n"
            + "                from #A.Entities() select distinct Country\n"
            + "            )\n"
            + "            select Country from cte"
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
    public void Distinct_OuterQuery_ReorderedSyntax_FromCte_ShouldWork()
    {
        var query = (
            "\n"
            + "            with cte as (\n"
            + "                select Country from #A.Entities()\n"
            + "            )\n"
            + "            from cte select distinct Country"
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
    public void Distinct_MixedSyntax_ReorderedCte_RegularOuter_ShouldWork()
    {
        var query = (
            "\n"
            + "            with cte as (\n"
            + "                from #A.Entities() select distinct Country\n"
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
    public void Distinct_ComplexScenario_CteWithJoin_GroupByInOuter_ShouldWork()
    {
        var query = (
            "\n"
            + "            with distinctCountries as (\n"
            + "                select distinct a.Country as Country, a.City as City\n"
            + "                from #A.Entities() a\n"
            + "                inner join #B.Entities() b on a.Country = b.Country\n"
            + "            )\n"
            + "            select Country, Count(City) as CityCount\n"
            + "            from distinctCountries\n"
            + "            group by Country"
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
                ]
            },
            { "#B", [new BasicEntity("Poznan", "Poland", 300), new BasicEntity("Munich", "Germany", 400)] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)), ("CityCount", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Germany", 1L], ["Poland", 3L]);
    }

    [TestMethod]
    public void Distinct_OnGroupedSumResult_UsingNestedCte_ShouldWork()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Krakow", "Poland", 400),
                    new BasicEntity("Berlin", "Germany", 350),
                    new BasicEntity("Paris", "France", 600),
                    new BasicEntity("Lyon", "France", 300),
                ]
            },
        };

        var querySimple = (
            "\n"
            + "            with cte1 as (\n"
            + "                select Country as Country from #A.Entities()\n"
            + "            ),\n"
            + "            cte2 as (\n"
            + "                select distinct Country from cte1\n"
            + "            )\n"
            + "            select Country from cte2"
        );

        var vmSimple = CreateAndRunVirtualMachine(querySimple, sources);
        var tableSimple = vmSimple.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(tableSimple, ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(tableSimple, ["France"], ["Germany"], ["Poland"]);

        var queryGrouped = (
            "\n"
            + "            with grouped as (\n"
            + "                select Country as Country, Sum(Population) as PopSum from #A.Entities() group by Country\n"
            + "            ),\n"
            + "            distinctSums as (\n"
            + "                select distinct PopSum from grouped\n"
            + "            )\n"
            + "            select PopSum from distinctSums"
        );

        var vmGrouped = CreateAndRunVirtualMachine(queryGrouped, sources);
        var tableGrouped = vmGrouped.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(tableGrouped, ("PopSum", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(tableGrouped, [350m], [900m]);
    }

    [TestMethod]
    public void Debug_CteWithGroupByAggregation_WithoutDistinct_ShouldWork()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Krakow", "Poland", 400),
                    new BasicEntity("Berlin", "Germany", 350),
                    new BasicEntity("Paris", "France", 600),
                    new BasicEntity("Lyon", "France", 300),
                ]
            },
        };

        var querySum = (
            "\n"
            + "            with grouped as (\n"
            + "                select Country, Sum(Population) from #A.Entities() group by Country\n"
            + "            )\n"
            + "            select * from grouped"
        );

        var vmSum = CreateAndRunVirtualMachine(querySum, sources);
        var tableSum = vmSum.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            tableSum,
            ("Country", typeof(string)),
            ("Sum(Population)", typeof(decimal?))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(
            tableSum,
            ["Germany", 350m],
            ["France", 900m],
            ["Poland", 900m]
        );
    }

    [TestMethod]
    public void CountAggregateInCte_ShouldReturnCorrectCounts()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Krakow", "Poland", 400),
                    new BasicEntity("Berlin", "Germany", 350),
                    new BasicEntity("Paris", "France", 600),
                    new BasicEntity("Lyon", "France", 300),
                ]
            },
        };

        var queryCount = (
            "\n"
            + "            with grouped as (\n"
            + "                select Country, Count(City) from #A.Entities() group by Country\n"
            + "            )\n"
            + "            select * from grouped"
        );

        var vmCount = CreateAndRunVirtualMachine(queryCount, sources);
        var tableCount = vmCount.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            tableCount,
            ("Country", typeof(string)),
            ("Count(City)", typeof(long))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(tableCount, ["Germany", 1L], ["France", 2L], ["Poland", 2L]);
    }
}

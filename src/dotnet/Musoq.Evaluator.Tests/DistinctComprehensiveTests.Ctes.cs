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
        var query = @"
            with distinctCountries as (
                select distinct Country as Country from #B.Entities()
            )
            select a.City, a.Country from #A.Entities() a
            inner join distinctCountries dc on a.Country = dc.Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Berlin", "Germany", 350),
                    new BasicEntity("Paris", "France", 600)
                ]
            },
            {
                "#B", [
                    new BasicEntity("Poznan", "Poland", 300),
                    new BasicEntity("Poznan", "Poland", 200)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)), ("a.Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Warsaw", "Poland"]);
    }













    [TestMethod]
    public void Distinct_InsideCte_WithJoin_ShouldWork()
    {
        var query = @"
            with cte as (
                select distinct a.Country as Country
                from #A.Entities() a
                inner join #B.Entities() b on a.Country = b.Country
            )
            select Country from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Krakow", "Poland", 400),
                    new BasicEntity("Berlin", "Germany", 350),
                    new BasicEntity("Paris", "France", 600)
                ]
            },
            {
                "#B", [
                    new BasicEntity("Poznan", "Poland", 300),
                    new BasicEntity("Munich", "Germany", 400)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Germany"], ["Poland"]);
    }



    [TestMethod]
    public void Distinct_WithExpressions_InCte_ShouldWork()
    {
        var query = @"
            with cte as (
                select distinct ToUpperInvariant(Country) as UpperCountry from #A.Entities()
            )
            select UpperCountry from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Warsaw", "poland", 500),
                    new BasicEntity("Krakow", "Poland", 400),
                    new BasicEntity("Berlin", "GERMANY", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        TableMaterializationTestHelper.AssertColumns(table, ("UpperCountry", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["GERMANY"], ["POLAND"]);
    }

    [TestMethod]
    public void Distinct_WithSkipTake_InCte_ShouldApplyAfterDistinct()
    {
        var query = @"
            with cte as (
                select distinct Country from #A.Entities() order by Country skip 1 take 2
            )
            select Country from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Krakow", "Poland", 400),
                    new BasicEntity("Berlin", "Germany", 350),
                    new BasicEntity("Paris", "France", 600),
                    new BasicEntity("Madrid", "Spain", 700)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Germany"], ["Poland"]);
    }

    [TestMethod]
    public void Distinct_InCte_ReorderedSyntax_ShouldWork()
    {
        var query = @"
            with cte as (
                from #A.Entities() select distinct Country
            )
            select Country from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Krakow", "Poland", 400),
                    new BasicEntity("Berlin", "Germany", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Germany"], ["Poland"]);
    }

    [TestMethod]
    public void Distinct_OuterQuery_ReorderedSyntax_FromCte_ShouldWork()
    {
        var query = @"
            with cte as (
                select Country from #A.Entities()
            )
            from cte select distinct Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Krakow", "Poland", 400),
                    new BasicEntity("Berlin", "Germany", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Germany"], ["Poland"]);
    }

    [TestMethod]
    public void Distinct_MixedSyntax_ReorderedCte_RegularOuter_ShouldWork()
    {
        var query = @"
            with cte as (
                from #A.Entities() select distinct Country
            )
            select distinct Country from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Krakow", "Poland", 400),
                    new BasicEntity("Berlin", "Germany", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Germany"], ["Poland"]);
    }

    [TestMethod]
    public void Distinct_ComplexScenario_CteWithJoin_GroupByInOuter_ShouldWork()
    {
        var query = @"
            with distinctCountries as (
                select distinct a.Country as Country, a.City as City
                from #A.Entities() a
                inner join #B.Entities() b on a.Country = b.Country
            )
            select Country, Count(City) as CityCount
            from distinctCountries
            group by Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Krakow", "Poland", 400),
                    new BasicEntity("Gdansk", "Poland", 200),
                    new BasicEntity("Berlin", "Germany", 350)
                ]
            },
            {
                "#B", [
                    new BasicEntity("Poznan", "Poland", 300),
                    new BasicEntity("Munich", "Germany", 400)
                ]
            }
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
                "#A", [
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Krakow", "Poland", 400),
                    new BasicEntity("Berlin", "Germany", 350),
                    new BasicEntity("Paris", "France", 600),
                    new BasicEntity("Lyon", "France", 300)
                ]
            }
        };


        var querySimple = @"
            with cte1 as (
                select Country as Country from #A.Entities()
            ),
            cte2 as (
                select distinct Country from cte1
            )
            select Country from cte2";

        var vmSimple = CreateAndRunVirtualMachine(querySimple, sources);
        var tableSimple = vmSimple.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(tableSimple, ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(tableSimple, ["France"], ["Germany"], ["Poland"]);


        var queryGrouped = @"
            with grouped as (
                select Country as Country, Sum(Population) as PopSum from #A.Entities() group by Country
            ),
            distinctSums as (
                select distinct PopSum from grouped
            )
            select PopSum from distinctSums";

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
                "#A", [
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Krakow", "Poland", 400),
                    new BasicEntity("Berlin", "Germany", 350),
                    new BasicEntity("Paris", "France", 600),
                    new BasicEntity("Lyon", "France", 300)
                ]
            }
        };


        var querySum = @"
            with grouped as (
                select Country, Sum(Population) from #A.Entities() group by Country
            )
            select * from grouped";

        var vmSum = CreateAndRunVirtualMachine(querySum, sources);
        var tableSum = vmSum.Run(TestContext.CancellationToken);


        TableMaterializationTestHelper.AssertColumns(tableSum, ("Country", typeof(string)), ("Sum(Population)", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            tableSum,
            ["Germany", 350m], ["France", 900m], ["Poland", 900m]);
    }

    [TestMethod]
    public void CountAggregateInCte_ShouldReturnCorrectCounts()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Krakow", "Poland", 400),
                    new BasicEntity("Berlin", "Germany", 350),
                    new BasicEntity("Paris", "France", 600),
                    new BasicEntity("Lyon", "France", 300)
                ]
            }
        };


        var queryCount = @"
            with grouped as (
                select Country, Count(City) from #A.Entities() group by Country
            )
            select * from grouped";

        var vmCount = CreateAndRunVirtualMachine(queryCount, sources);
        var tableCount = vmCount.Run(TestContext.CancellationToken);


        TableMaterializationTestHelper.AssertColumns(tableCount, ("Country", typeof(string)), ("Count(City)", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            tableCount,
            ["Germany", 1L], ["France", 2L], ["Poland", 2L]);
    }

}

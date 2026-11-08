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
        var query = @"
            with
            cte1 as (
                select distinct Country from #A.Entities()
            ),
            cte2 as (
                select Country from cte1
            )
            select Country from cte2";

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
    public void Distinct_InSecondCte_FromFirstCteWithDuplicates_ShouldDeduplicate()
    {
        var query = @"
            with
            cte1 as (
                select Country from #A.Entities()
            ),
            cte2 as (
                select distinct Country from cte1
            )
            select Country from cte2";

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
    public void Distinct_InBothCtes_ShouldDeduplicateAtEachLevel()
    {
        var query = @"
            with
            cte1 as (
                select distinct Country from #A.Entities()
            ),
            cte2 as (
                select distinct Country from cte1
            )
            select Country from cte2";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Krakow", "Poland", 400),
                    new BasicEntity("Gdansk", "Poland", 200),
                    new BasicEntity("Berlin", "Germany", 350),
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
    public void Distinct_InTwoIndependentCtes_JoinedInOuterQuery_ShouldWork()
    {
        var query = @"
            with
            cte1 as (
                select distinct Country from #A.Entities()
            ),
            cte2 as (
                select distinct Country from #B.Entities()
            )
            select c1.Country as Country1, c2.Country as Country2
            from cte1 c1
            inner join cte2 c2 on c1.Country = c2.Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Krakow", "Poland", 400),
                    new BasicEntity("Berlin", "Germany", 350)
                ]
            },
            {
                "#B", [
                    new BasicEntity("Lisbon", "Portugal", 600),
                    new BasicEntity("Porto", "Portugal", 400),
                    new BasicEntity("Madrid", "Spain", 700),
                    new BasicEntity("Munich", "Germany", 300)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Country1", typeof(string)), ("Country2", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Germany", "Germany"]);
    }

    [TestMethod]
    public void Distinct_InDeeplyNestedCtes_ShouldWork()
    {
        var query = @"
            with
            cte1 as (
                select Country from #A.Entities()
            ),
            cte2 as (
                select distinct Country from cte1
            ),
            cte3 as (
                select Country from cte2
            )
            select Country from cte3";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Krakow", "Poland", 400),
                    new BasicEntity("Berlin", "Germany", 350),
                    new BasicEntity("Munich", "Germany", 300),
                    new BasicEntity("Paris", "France", 600)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["France"], ["Germany"], ["Poland"]);
    }

    [TestMethod]
    public void Distinct_AtMultipleLevels_ShouldNotCreateDuplicates()
    {
        var query = @"
            with cte as (
                select distinct Country from #A.Entities()
            )
            select distinct Country from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Krakow", "Poland", 400),
                    new BasicEntity("Gdansk", "Poland", 200),
                    new BasicEntity("Poznan", "Poland", 300)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Poland"]);
    }

}

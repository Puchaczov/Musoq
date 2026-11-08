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
    public void Distinct_WithWhere_InsideCte_ShouldFilterThenDeduplicate()
    {
        var query = @"
            with cte as (
                select distinct Country from #A.Entities() where Population > 300
            )
            select Country from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Krakow", "Poland", 200),
                    new BasicEntity("Berlin", "Germany", 350),
                    new BasicEntity("Paris", "France", 600),
                    new BasicEntity("Lyon", "France", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["France"],
            ["Germany"],
            ["Poland"]);
    }

    [TestMethod]
    public void Distinct_WithOrderBy_InsideCte_ShouldDeduplicateThenOrder()
    {
        var query = @"
            with cte as (
                select distinct Country from #A.Entities() order by Country desc
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
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Poland"],
            ["Germany"]);
    }

    [TestMethod]
    public void Distinct_OnAggregatedResultsInCte_ShouldWork()
    {
        var query = @"
            with cte as (
                select distinct Sum(Population) as PopSum from #A.Entities() group by Country
            )
            select PopSum from cte";

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

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        TableMaterializationTestHelper.AssertColumns(table, ("PopSum", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            [350m],
            [900m]);
    }

    [TestMethod]
    public void Distinct_InsideCte_ShouldDeduplicateBeforeOuterQuery()
    {
        var query = @"
            with cte as (
                select distinct Country from #A.Entities()
            )
            select Country from cte";

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
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["France"],
            ["Germany"],
            ["Poland"]);
    }

    [TestMethod]
    public void Distinct_InsideCte_MultipleColumns_ShouldDeduplicateCombinations()
    {
        var query = @"
            with cte as (
                select distinct City, Country from #A.Entities()
            )
            select City, Country from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Warsaw", "Poland", 400),
                    new BasicEntity("Berlin", "Germany", 350),
                    new BasicEntity("Berlin", "Germany", 300)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Berlin", "Germany"],
            ["Warsaw", "Poland"]);
    }

    [TestMethod]
    public void Distinct_InsideCte_AllDuplicates_ShouldReturnSingleRow()
    {
        var query = @"
            with cte as (
                select distinct Country from #A.Entities()
            )
            select Country from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Krakow", "Poland", 400),
                    new BasicEntity("Gdansk", "Poland", 300)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Poland"]);
    }

    [TestMethod]
    public void Distinct_OuterQuery_FromCteWithDuplicates_ShouldDeduplicate()
    {
        var query = @"
            with cte as (
                select Country from #A.Entities()
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
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Germany"],
            ["Poland"]);
    }

    [TestMethod]
    public void Distinct_OuterQuery_FromCteWithDistinct_ShouldMaintainDistinct()
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
                    new BasicEntity("Berlin", "Germany", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Germany"],
            ["Poland"]);
    }

}

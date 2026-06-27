using System.Collections.Generic;
using System.Linq;
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


        Assert.AreEqual(3, table.Count, "DISTINCT with WHERE should filter then deduplicate");

        var countries = table.Select(row => row.Values[0]?.ToString()).OrderBy(c => c).ToArray();
        Assert.AreEqual("France", countries[0], "First country should be France");
        Assert.AreEqual("Germany", countries[1], "Second country should be Germany");
        Assert.AreEqual("Poland", countries[2], "Third country should be Poland");
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

        Assert.AreEqual(2, table.Count, "Should have 2 distinct countries");

        var countries = table.Select(row => (string)row.Values[0]).ToList();
        Assert.Contains("Poland", countries, "Should contain Poland");
        Assert.Contains("Germany", countries, "Should contain Germany");
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


        Assert.AreEqual(2, table.Count, "DISTINCT on aggregated sums should produce 2 unique values");

        var actualValues = table.Select(r => (decimal)r.Values[0]).OrderBy(x => x).ToArray();
        Assert.AreEqual(350m, actualValues[0], "First distinct value should be 350");
        Assert.AreEqual(900m, actualValues[1], "Second distinct value should be 900");
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

        Assert.AreEqual(3, table.Count, "CTE with DISTINCT should produce 3 unique countries");

        var countries = table.Select(row => row.Values[0]?.ToString()).ToList();
        Assert.Contains("France", countries, "Should contain France");
        Assert.Contains("Germany", countries, "Should contain Germany");
        Assert.Contains("Poland", countries, "Should contain Poland");
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

        Assert.AreEqual(2, table.Count, "CTE with DISTINCT should produce 2 unique city-country combinations");

        var combinations = table.Select(row => (row.Values[0]?.ToString(), row.Values[1]?.ToString())).ToList();
        Assert.IsTrue(combinations.Any(c => c is { Item1: "Berlin", Item2: "Germany" }),
            "Should contain Berlin, Germany");
        Assert.IsTrue(combinations.Any(c => c is { Item1: "Warsaw", Item2: "Poland" }),
            "Should contain Warsaw, Poland");
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

        Assert.AreEqual(1, table.Count, "CTE with DISTINCT should produce 1 unique country");
        var countries = table.Select(row => row.Values[0]?.ToString()).ToList();
        Assert.Contains("Poland", countries, "Should contain Poland");
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

        Assert.AreEqual(2, table.Count, "DISTINCT in outer query should deduplicate CTE results");

        var countries = table.Select(row => row.Values[0]?.ToString()).ToList();
        Assert.Contains("Germany", countries, "Should contain Germany");
        Assert.Contains("Poland", countries, "Should contain Poland");
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

        Assert.AreEqual(2, table.Count, "Double DISTINCT should still produce 2 unique countries");

        var countries = table.Select(row => row.Values[0]?.ToString()).ToList();
        Assert.Contains("Germany", countries, "Should contain Germany");
        Assert.Contains("Poland", countries, "Should contain Poland");
    }

}

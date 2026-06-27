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

        Assert.AreEqual(2, table.Count, "First CTE with DISTINCT should affect subsequent CTEs");

        var countries = table.Select(row => row.Values[0]?.ToString()).ToList();
        Assert.Contains("Germany", countries, "Should contain Germany");
        Assert.Contains("Poland", countries, "Should contain Poland");
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

        Assert.AreEqual(2, table.Count, "Second CTE with DISTINCT should deduplicate first CTE");

        var countries = table.Select(row => row.Values[0]?.ToString()).ToList();
        Assert.Contains("Germany", countries, "Should contain Germany");
        Assert.Contains("Poland", countries, "Should contain Poland");
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

        Assert.AreEqual(2, table.Count, "DISTINCT in both CTEs should produce 2 unique countries");

        var countries = table.Select(row => row.Values[0]?.ToString()).ToList();
        Assert.Contains("Germany", countries, "Should contain Germany");
        Assert.Contains("Poland", countries, "Should contain Poland");
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

        Assert.AreEqual(1, table.Count, "Only Germany is common between both CTEs");
        var country1 = table[0].Values[0]?.ToString();
        var country2 = table[0].Values[1]?.ToString();
        Assert.AreEqual("Germany", country1, "Country1 should be Germany");
        Assert.AreEqual("Germany", country2, "Country2 should be Germany");
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

        Assert.AreEqual(3, table.Count, "Nested CTEs should preserve distinct behavior");

        var countries = table.Select(row => row.Values[0]?.ToString()).ToList();
        Assert.Contains("France", countries, "Should contain France");
        Assert.Contains("Germany", countries, "Should contain Germany");
        Assert.Contains("Poland", countries, "Should contain Poland");
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


        Assert.AreEqual(1, table.Count, "Multiple DISTINCT at different levels should not create duplicates");
        var countries = table.Select(row => row.Values[0]?.ToString()).ToList();
        Assert.Contains("Poland", countries, "Should contain Poland");
    }

}

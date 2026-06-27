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


        Assert.AreEqual(1, table.Count);
        var cities = table.Select(row => row.Values[0]?.ToString()).ToList();
        Assert.Contains("Warsaw", cities, "Should contain Warsaw");
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


        Assert.AreEqual(2, table.Count);

        var countries = table.Select(row => row.Values[0]?.ToString()).ToList();
        Assert.Contains("Germany", countries, "Should contain Germany");
        Assert.Contains("Poland", countries, "Should contain Poland");
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


        Assert.AreEqual(2, table.Count);

        var countries = table.Select(row => row.Values[0]?.ToString()).ToList();
        Assert.Contains("GERMANY", countries, "Should contain GERMANY");
        Assert.Contains("POLAND", countries, "Should contain POLAND");
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


        Assert.AreEqual(2, table.Count, "Should have 2 countries after SKIP 1 TAKE 2");

        var countries = table.Select(row => row.Values[0]?.ToString()).ToList();
        CollectionAssert.AreEquivalent(new[] { "Germany", "Poland" }, countries,
            "Should contain Germany and Poland (ordered distinct, skip France, take 2)");
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

        Assert.AreEqual(2, table.Count);

        var countries = table.Select(row => row.Values[0]?.ToString()).ToList();
        Assert.Contains("Germany", countries, "Should contain Germany");
        Assert.Contains("Poland", countries, "Should contain Poland");
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

        Assert.AreEqual(2, table.Count);

        var countries = table.Select(row => row.Values[0]?.ToString()).ToList();
        Assert.Contains("Germany", countries, "Should contain Germany");
        Assert.Contains("Poland", countries, "Should contain Poland");
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

        Assert.AreEqual(2, table.Count);

        var countries = table.Select(row => row.Values[0]?.ToString()).ToList();
        Assert.Contains("Germany", countries, "Should contain Germany");
        Assert.Contains("Poland", countries, "Should contain Poland");
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


        Assert.AreEqual(2, table.Count);

        var results = table.Select(row => (row.Values[0]?.ToString(), (long)row.Values[1])).OrderBy(r => r.Item1)
            .ToArray();
        Assert.AreEqual("Germany", results[0].Item1, "First country should be Germany");
        Assert.AreEqual(1L, results[0].Item2, "Germany should have 1 city");
        Assert.AreEqual("Poland", results[1].Item1, "Second country should be Poland");
        Assert.AreEqual(3L, results[1].Item2, "Poland should have 3 cities");
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

        var simpleValues = string.Join(", ", tableSimple.Select(r => r.Values[0]?.ToString() ?? "null"));
        Assert.AreEqual(3, tableSimple.Count,
            $"Simple DISTINCT in nested CTE should produce 3 countries. Actual: [{simpleValues}]");


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


        Assert.AreEqual(2, tableGrouped.Count, "DISTINCT on grouped sums should produce 2 unique values");

        var groupedValues = tableGrouped.Select(r => (decimal)r.Values[0]).OrderBy(x => x).ToArray();
        Assert.AreEqual(350m, groupedValues[0], "First distinct sum should be 350");
        Assert.AreEqual(900m, groupedValues[1], "Second distinct sum should be 900");
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


        var sumValues = string.Join(", ", tableSum.Select(r => r.Values[1]?.ToString() ?? "null"));
        Assert.AreEqual(3, tableSum.Count, $"Should have 3 rows (one per country). Actual sum values: [{sumValues}]");


        var valuesSumDecimal = tableSum.Select(r => (decimal?)r.Values[1]).OrderBy(x => x).ToList();
        Assert.IsTrue(valuesSumDecimal.All(v => v != 0),
            $"Sum values should be non-zero. Actual: [{string.Join(", ", valuesSumDecimal)}]");
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


        var countValues = string.Join(", ", tableCount.Select(r => r.Values[1]?.ToString() ?? "null"));
        Assert.AreEqual(3, tableCount.Count, $"Count: Should have 3 rows. Actual values: [{countValues}]");


        var countValuesLong = tableCount.Select(r => (long)r.Values[1]).OrderBy(x => x).ToList();
        Assert.IsTrue(countValuesLong.All(v => v != 0),
            $"Count values should be non-zero. Actual: [{string.Join(", ", countValuesLong)}]");


        Assert.AreEqual(1L, countValuesLong[0], "Germany should have count of 1");
        Assert.AreEqual(2L, countValuesLong[1], "France should have count of 2");
        Assert.AreEqual(2L, countValuesLong[2], "Poland should have count of 2");
    }

}

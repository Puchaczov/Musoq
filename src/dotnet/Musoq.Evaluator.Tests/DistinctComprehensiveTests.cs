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
[TestClass]
public class DistinctComprehensiveTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    #region DISTINCT with WHERE clause in CTE

    /// <summary>
    ///     Tests DISTINCT with WHERE clause inside CTE.
    /// </summary>
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

    #endregion

    #region DISTINCT with ORDER BY in CTE

    /// <summary>
    ///     Tests DISTINCT with ORDER BY inside CTE.
    ///     Note: Due to parallelization, the outer query may not preserve the CTE's ORDER BY.
    ///     This test only verifies deduplication works, not order preservation.
    /// </summary>
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

    #endregion

    #region DISTINCT with Aggregation scenarios

    /// <summary>
    ///     BUG: Tests DISTINCT on aggregated results in CTE.
    ///     The DISTINCT is not being applied when the query also has GROUP BY.
    ///     Using Sum() instead of Count() to avoid the separate Count-in-CTE bug.
    ///     Root Cause: DistinctToGroupByVisitor skips transformation when GROUP BY exists
    ///     because you can't add aggregates to GROUP BY. This is a complex fix requiring
    ///     either code generation changes or a multi-pass transformation.
    ///     Expected: 2 unique sum values (350 and 900)
    ///     Actual: 3 rows (DISTINCT is ignored)
    /// </summary>
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

    #endregion

    #region DISTINCT with subqueries (using CTE pattern)

    /// <summary>
    ///     Tests using distinct CTE as filter source in outer query.
    ///     Note: Musoq doesn't support inline subqueries in WHERE IN, use CTE + join pattern.
    /// </summary>
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
        Assert.IsTrue(cities.Contains("Warsaw"), "Should contain Warsaw");
    }

    #endregion

    #region DISTINCT in CTE Inner Query

    /// <summary>
    ///     Tests DISTINCT inside CTE and selecting all rows from CTE.
    ///     The CTE should contain only distinct values.
    /// </summary>
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
        Assert.IsTrue(countries.Contains("France"), "Should contain France");
        Assert.IsTrue(countries.Contains("Germany"), "Should contain Germany");
        Assert.IsTrue(countries.Contains("Poland"), "Should contain Poland");
    }

    /// <summary>
    ///     Tests DISTINCT inside CTE with multiple columns.
    /// </summary>
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
        Assert.IsTrue(combinations.Any(c => c.Item1 == "Berlin" && c.Item2 == "Germany"), "Should contain Berlin, Germany");
        Assert.IsTrue(combinations.Any(c => c.Item1 == "Warsaw" && c.Item2 == "Poland"), "Should contain Warsaw, Poland");
    }

    /// <summary>
    ///     Tests DISTINCT inside CTE when all rows are duplicates.
    /// </summary>
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
        Assert.IsTrue(countries.Contains("Poland"), "Should contain Poland");
    }

    #endregion

    #region DISTINCT in Outer Query (selecting from CTE)

    /// <summary>
    ///     Tests DISTINCT in outer query when selecting from CTE that has duplicates.
    /// </summary>
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
        Assert.IsTrue(countries.Contains("Germany"), "Should contain Germany");
        Assert.IsTrue(countries.Contains("Poland"), "Should contain Poland");
    }

    /// <summary>
    ///     Tests that DISTINCT in outer query works when CTE already has distinct values.
    /// </summary>
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
        Assert.IsTrue(countries.Contains("Germany"), "Should contain Germany");
        Assert.IsTrue(countries.Contains("Poland"), "Should contain Poland");
    }

    #endregion

    #region DISTINCT in Multiple CTEs

    /// <summary>
    ///     Tests DISTINCT in first CTE of multiple CTEs.
    /// </summary>
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
        Assert.IsTrue(countries.Contains("Germany"), "Should contain Germany");
        Assert.IsTrue(countries.Contains("Poland"), "Should contain Poland");
    }

    /// <summary>
    ///     Tests DISTINCT in second CTE (referencing first CTE without distinct).
    /// </summary>
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
        Assert.IsTrue(countries.Contains("Germany"), "Should contain Germany");
        Assert.IsTrue(countries.Contains("Poland"), "Should contain Poland");
    }

    /// <summary>
    ///     Tests DISTINCT in both CTEs - values should be deduplicated at each level.
    /// </summary>
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
        Assert.IsTrue(countries.Contains("Germany"), "Should contain Germany");
        Assert.IsTrue(countries.Contains("Poland"), "Should contain Poland");
    }

    /// <summary>
    ///     Tests independent CTEs both with DISTINCT, then joined.
    /// </summary>
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

    #endregion

    #region DISTINCT with Set Operations (requires key columns - documented limitation)

    // NOTE: Musoq requires key columns for set operations (UNION, UNION ALL, EXCEPT, INTERSECT).
    // This is a documented limitation and not a DISTINCT bug. These tests are skipped.

    /// <summary>
    ///     Tests DISTINCT with UNION ALL using aliased columns (key workaround).
    ///     Skipped: Set operations require key columns.
    /// </summary>
    [TestMethod]
    public void Distinct_BeforeUnionAll_WithAlias_ShouldDeduplicateEachSide()
    {
        var query = @"
            select distinct Country as c from #A.Entities()
            union all (Country)
            select distinct Country as c from #B.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Krakow", "Poland", 400)
                ]
            },
            {
                "#B", [
                    new BasicEntity("Berlin", "Germany", 350),
                    new BasicEntity("Munich", "Germany", 300)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(2, table.Count, "UNION ALL should return 2 rows (one from each side)");
        var countries = table.Select(row => row.Values[0]?.ToString()).OrderBy(c => c).ToArray();
        Assert.AreEqual("Germany", countries[0], "First country should be Germany");
        Assert.AreEqual("Poland", countries[1], "Second country should be Poland");
    }

    /// <summary>
    ///     Tests DISTINCT in CTE combined with UNION using aliased columns.
    ///     Skipped: Set operations require key columns.
    /// </summary>
    [TestMethod]
    public void Distinct_InCte_WithUnion_WithAlias_ShouldWork()
    {
        var query = @"
            with cte as (
                select distinct Country as c from #A.Entities()
                union (Country)
                select distinct Country as c from #B.Entities()
            )
            select c from cte";

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
                    new BasicEntity("Berlin", "Germany", 400)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(3, table.Count, "UNION should deduplicate across both sides");
        var countries = table.Select(row => row.Values[0]?.ToString()).OrderBy(c => c).ToArray();
        Assert.AreEqual("Germany", countries[0], "First country should be Germany");
        Assert.AreEqual("Poland", countries[1], "Second country should be Poland");
        Assert.AreEqual("Portugal", countries[2], "Third country should be Portugal");
    }

    /// <summary>
    ///     Tests DISTINCT in CTE with EXCEPT operation using aliased columns.
    ///     Skipped: Set operations require key columns.
    /// </summary>
    [TestMethod]
    public void Distinct_InCte_WithExcept_WithAlias_ShouldWork()
    {
        var query = @"
            with cte as (
                select distinct Country as c from #A.Entities()
                except (Country)
                select distinct Country as c from #B.Entities()
            )
            select c from cte";

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
                    new BasicEntity("Munich", "Germany", 300)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(1, table.Count, "EXCEPT should remove Germany from result");
        var countries = table.Select(row => row.Values[0]?.ToString()).ToList();
        Assert.IsTrue(countries.Contains("Poland"), "Should contain Poland");
    }

    /// <summary>
    ///     Tests DISTINCT in CTE with INTERSECT operation using aliased columns.
    ///     Skipped: Set operations require key columns.
    /// </summary>
    [TestMethod]
    public void Distinct_InCte_WithIntersect_WithAlias_ShouldWork()
    {
        var query = @"
            with cte as (
                select distinct Country as c from #A.Entities()
                intersect (Country)
                select distinct Country as c from #B.Entities()
            )
            select c from cte";

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
                    new BasicEntity("Munich", "Germany", 300),
                    new BasicEntity("Madrid", "Spain", 700)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(1, table.Count, "INTERSECT should return only common countries");
        var countries = table.Select(row => row.Values[0]?.ToString()).ToList();
        Assert.IsTrue(countries.Contains("Germany"), "Should contain Germany");
    }

    #endregion

    #region DISTINCT with JOINs

    /// <summary>
    ///     Tests DISTINCT on result of JOIN.
    /// </summary>
    [TestMethod]
    public void Distinct_OnJoinResult_ShouldDeduplicateJoinedRows()
    {
        var query = @"
            select distinct a.Country
            from #A.Entities() a
            inner join #B.Entities() b on a.Country = b.Country";

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
                    new BasicEntity("Poznan", "Poland", 300),
                    new BasicEntity("Munich", "Germany", 400)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(2, table.Count, "DISTINCT should deduplicate join results");

        var countries = table.Select(row => row.Values[0]?.ToString()).ToList();
        Assert.IsTrue(countries.Contains("Germany"), "Should contain Germany");
        Assert.IsTrue(countries.Contains("Poland"), "Should contain Poland");
    }

    /// <summary>
    ///     Tests DISTINCT inside CTE that performs a JOIN.
    /// </summary>
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
        Assert.IsTrue(countries.Contains("Germany"), "Should contain Germany");
        Assert.IsTrue(countries.Contains("Poland"), "Should contain Poland");
    }

    #endregion

    #region Edge cases

    /// <summary>
    ///     Tests DISTINCT with NULL values.
    /// </summary>
    [TestMethod]
    public void Distinct_WithNullValues_ShouldTreatNullAsDistinctValue()
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
                    new BasicEntity("Unknown", null, 0),
                    new BasicEntity("Berlin", "Germany", 350),
                    new BasicEntity("NoCountry", null, 100)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(3, table.Count, "NULL should be treated as a distinct value");

        var countries = table.Select(row => row.Values[0]?.ToString()).ToList();
        Assert.IsTrue(countries.Contains(null), "Should contain null");
        Assert.IsTrue(countries.Contains("Germany"), "Should contain Germany");
        Assert.IsTrue(countries.Contains("Poland"), "Should contain Poland");
    }

    /// <summary>
    ///     Tests DISTINCT in deeply nested CTEs (3 levels).
    /// </summary>
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
        Assert.IsTrue(countries.Contains("France"), "Should contain France");
        Assert.IsTrue(countries.Contains("Germany"), "Should contain Germany");
        Assert.IsTrue(countries.Contains("Poland"), "Should contain Poland");
    }

    /// <summary>
    ///     Tests DISTINCT applied at multiple levels (CTE + outer query).
    /// </summary>
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
        Assert.IsTrue(countries.Contains("Poland"), "Should contain Poland");
    }

    /// <summary>
    ///     Tests DISTINCT in CTE with expressions (not just column references).
    /// </summary>
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
        Assert.IsTrue(countries.Contains("GERMANY"), "Should contain GERMANY");
        Assert.IsTrue(countries.Contains("POLAND"), "Should contain POLAND");
    }

    /// <summary>
    ///     Tests DISTINCT with SKIP and TAKE in CTE.
    /// </summary>
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

    #endregion

    #region Reordered Syntax (FROM-first)

    /// <summary>
    ///     Tests DISTINCT in CTE using reordered syntax (FROM first).
    /// </summary>
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
        Assert.IsTrue(countries.Contains("Germany"), "Should contain Germany");
        Assert.IsTrue(countries.Contains("Poland"), "Should contain Poland");
    }

    /// <summary>
    ///     Tests DISTINCT in outer query with reordered syntax.
    /// </summary>
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
        Assert.IsTrue(countries.Contains("Germany"), "Should contain Germany");
        Assert.IsTrue(countries.Contains("Poland"), "Should contain Poland");
    }

    /// <summary>
    ///     Tests mixed syntax - reordered in CTE, regular in outer query.
    /// </summary>
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
        Assert.IsTrue(countries.Contains("Germany"), "Should contain Germany");
        Assert.IsTrue(countries.Contains("Poland"), "Should contain Poland");
    }

    #endregion

    #region Complex combined scenarios

    /// <summary>
    ///     Complex test combining DISTINCT in CTE with JOIN and GROUP BY in outer query.
    /// </summary>
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

        var results = table.Select(row => (row.Values[0]?.ToString(), (int)row.Values[1])).OrderBy(r => r.Item1)
            .ToArray();
        Assert.AreEqual("Germany", results[0].Item1, "First country should be Germany");
        Assert.AreEqual(1, results[0].Item2, "Germany should have 1 city");
        Assert.AreEqual("Poland", results[1].Item1, "Second country should be Poland");
        Assert.AreEqual(3, results[1].Item2, "Poland should have 3 cities");
    }

    /// <summary>
    ///     Tests DISTINCT values returned from a grouped result in CTE.
    ///     Note: COUNT(DISTINCT column) syntax is not supported, use nested CTEs.
    /// </summary>
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

    /// <summary>
    ///     Debug test: Verify that the first CTE produces correct values without DISTINCT in the second.
    /// </summary>
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

    /// <summary>
    ///     Test: Count() aggregate in CTE should work correctly.
    ///     Note: The original test was incorrectly counting Name which was null (not set by the constructor).
    ///     The BasicEntity(city, country, population) constructor doesn't set Name.
    ///     Fixed to use Count(City) which has actual values.
    ///     Expected: [1, 2, 2] (counts for each country)
    /// </summary>
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


        var valuesCountInt = tableCount.Select(r => (int?)r.Values[1]).OrderBy(x => x).ToList();
        Assert.IsTrue(valuesCountInt.All(v => v != 0),
            $"Count values should be non-zero. Actual: [{string.Join(", ", valuesCountInt)}]");


        Assert.AreEqual(1, valuesCountInt[0], "Germany should have count of 1");
        Assert.AreEqual(2, valuesCountInt[1], "France should have count of 2");
        Assert.AreEqual(2, valuesCountInt[2], "Poland should have count of 2");
    }

    #endregion
}

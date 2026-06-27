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
public partial class DistinctComprehensiveTests : BasicEntityTestBase
{
    public required TestContext TestContext { get; set; }

    #region DISTINCT with WHERE clause in CTE

    /// <summary>
    ///     Tests DISTINCT with WHERE clause inside CTE.
    /// </summary>

    #endregion

    #region DISTINCT with ORDER BY in CTE

    /// <summary>
    ///     Tests DISTINCT with ORDER BY inside CTE.
    ///     Note: Due to parallelization, the outer query may not preserve the CTE's ORDER BY.
    ///     This test only verifies deduplication works, not order preservation.
    /// </summary>

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

    #endregion

    #region DISTINCT with subqueries (using CTE pattern)

    /// <summary>
    ///     Tests using distinct CTE as filter source in outer query.
    ///     Note: Musoq doesn't support inline subqueries in WHERE IN, use CTE + join pattern.
    /// </summary>

    #endregion

    #region DISTINCT in CTE Inner Query

    /// <summary>
    ///     Tests DISTINCT inside CTE and selecting all rows from CTE.
    ///     The CTE should contain only distinct values.
    /// </summary>

    /// <summary>
    ///     Tests DISTINCT inside CTE with multiple columns.
    /// </summary>

    /// <summary>
    ///     Tests DISTINCT inside CTE when all rows are duplicates.
    /// </summary>

    #endregion

    #region DISTINCT in Outer Query (selecting from CTE)

    /// <summary>
    ///     Tests DISTINCT in outer query when selecting from CTE that has duplicates.
    /// </summary>

    /// <summary>
    ///     Tests that DISTINCT in outer query works when CTE already has distinct values.
    /// </summary>

    #endregion

    #region DISTINCT in Multiple CTEs

    /// <summary>
    ///     Tests DISTINCT in first CTE of multiple CTEs.
    /// </summary>

    /// <summary>
    ///     Tests DISTINCT in second CTE (referencing first CTE without distinct).
    /// </summary>

    /// <summary>
    ///     Tests DISTINCT in both CTEs - values should be deduplicated at each level.
    /// </summary>

    /// <summary>
    ///     Tests independent CTEs both with DISTINCT, then joined.
    /// </summary>

    #endregion

    #region DISTINCT with Set Operations

    // NOTE: Set operators can use omitted, empty, or explicit keys. The skipped scenarios
    // below remain placeholders for future DISTINCT plus set-operation integration coverage.

    /// <summary>
    ///     Tests DISTINCT with UNION ALL using aliased columns.
    ///     Skipped: placeholder for future DISTINCT plus set-operation coverage.
    /// </summary>

    /// <summary>
    ///     Tests DISTINCT in CTE combined with UNION using aliased columns.
    ///     Skipped: placeholder for future DISTINCT plus set-operation coverage.
    /// </summary>

    /// <summary>
    ///     Tests DISTINCT in CTE with EXCEPT operation using aliased columns.
    ///     Skipped: placeholder for future DISTINCT plus set-operation coverage.
    /// </summary>

    /// <summary>
    ///     Tests DISTINCT in CTE with INTERSECT operation using aliased columns.
    ///     Skipped: placeholder for future DISTINCT plus set-operation coverage.
    /// </summary>

    #endregion

    #region DISTINCT with JOINs

    /// <summary>
    ///     Tests DISTINCT on result of JOIN.
    /// </summary>

    /// <summary>
    ///     Tests DISTINCT inside CTE that performs a JOIN.
    /// </summary>

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
        Assert.Contains((string?)null, countries, "Should contain null");
        Assert.Contains("Germany", countries, "Should contain Germany");
        Assert.Contains("Poland", countries, "Should contain Poland");
    }

    /// <summary>
    ///     Tests DISTINCT in deeply nested CTEs (3 levels).
    /// </summary>

    /// <summary>
    ///     Tests DISTINCT applied at multiple levels (CTE + outer query).
    /// </summary>

    /// <summary>
    ///     Tests DISTINCT in CTE with expressions (not just column references).
    /// </summary>

    /// <summary>
    ///     Tests DISTINCT with SKIP and TAKE in CTE.
    /// </summary>

    #endregion

    #region Reordered Syntax (FROM-first)

    /// <summary>
    ///     Tests DISTINCT in CTE using reordered syntax (FROM first).
    /// </summary>

    /// <summary>
    ///     Tests DISTINCT in outer query with reordered syntax.
    /// </summary>

    /// <summary>
    ///     Tests mixed syntax - reordered in CTE, regular in outer query.
    /// </summary>

    #endregion

    #region Complex combined scenarios

    /// <summary>
    ///     Complex test combining DISTINCT in CTE with JOIN and GROUP BY in outer query.
    /// </summary>

    /// <summary>
    ///     Tests DISTINCT values returned from a grouped result in CTE.
    ///     Note: COUNT(DISTINCT column) syntax is not supported, use nested CTEs.
    /// </summary>

    /// <summary>
    ///     Debug test: Verify that the first CTE produces correct values without DISTINCT in the second.
    /// </summary>

    /// <summary>
    ///     Test: Count() aggregate in CTE should work correctly.
    ///     Note: The original test was incorrectly counting Name which was null (not set by the constructor).
    ///     The BasicEntity(city, country, population) constructor doesn't set Name.
    ///     Fixed to use Count(City) which has actual values.
    ///     Expected: [1, 2, 2] (counts for each country)
    /// </summary>

    #endregion
}

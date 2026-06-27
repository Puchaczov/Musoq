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
        Assert.Contains("Poland", countries, "Should contain Poland");
    }

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
        Assert.Contains("Germany", countries, "Should contain Germany");
    }

}

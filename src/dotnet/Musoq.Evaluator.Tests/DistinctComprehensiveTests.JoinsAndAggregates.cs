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
        Assert.Contains("Germany", countries, "Should contain Germany");
        Assert.Contains("Poland", countries, "Should contain Poland");
    }

}

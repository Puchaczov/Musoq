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


        TableMaterializationTestHelper.AssertColumns(table, ("c", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Germany"], ["Poland"]);
    }

}

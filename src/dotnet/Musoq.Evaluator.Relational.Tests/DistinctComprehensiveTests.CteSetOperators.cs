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
    public void Distinct_InCte_WithUnion_WithAlias_ShouldWork()
    {
        var query = (
            "\n"
            + "            with cte as (\n"
            + "                select distinct Country as c from #A.Entities()\n"
            + "                union (Country)\n"
            + "                select distinct Country as c from #B.Entities()\n"
            + "            )\n"
            + "            select c from cte"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Krakow", "Poland", 400),
                    new BasicEntity("Berlin", "Germany", 350),
                ]
            },
            { "#B", [new BasicEntity("Lisbon", "Portugal", 600), new BasicEntity("Berlin", "Germany", 400)] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("c", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Germany"], ["Poland"], ["Portugal"]);
    }

    [TestMethod]
    public void Distinct_InCte_WithExcept_WithAlias_ShouldWork()
    {
        var query = (
            "\n"
            + "            with cte as (\n"
            + "                select distinct Country as c from #A.Entities()\n"
            + "                except (Country)\n"
            + "                select distinct Country as c from #B.Entities()\n"
            + "            )\n"
            + "            select c from cte"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Krakow", "Poland", 400),
                    new BasicEntity("Berlin", "Germany", 350),
                ]
            },
            { "#B", [new BasicEntity("Munich", "Germany", 300)] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("c", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Poland"]);
    }

    [TestMethod]
    public void Distinct_InCte_WithIntersect_WithAlias_ShouldWork()
    {
        var query = (
            "\n"
            + "            with cte as (\n"
            + "                select distinct Country as c from #A.Entities()\n"
            + "                intersect (Country)\n"
            + "                select distinct Country as c from #B.Entities()\n"
            + "            )\n"
            + "            select c from cte"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Krakow", "Poland", 400),
                    new BasicEntity("Berlin", "Germany", 350),
                ]
            },
            { "#B", [new BasicEntity("Munich", "Germany", 300), new BasicEntity("Madrid", "Spain", 700)] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("c", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Germany"]);
    }
}

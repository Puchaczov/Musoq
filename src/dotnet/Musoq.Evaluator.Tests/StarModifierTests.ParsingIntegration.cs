using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class StarModifierTests
{
    [TestMethod]
    public void WhenStarLike_WithCte_ShouldFilterCteColumns()
    {
        const string query = @"
            with src as (
                select Name, City, Country, Population from #A.entities()
            )
            select * like 'C%' from src s";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m) { City = "London", Country = "UK", Population = 100m, Name = "Alice" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        var columnNames = table.Columns.Select(c => c.ColumnName).ToList();
        Assert.HasCount(2, columnNames, $"Expected City and Country columns; got: {string.Join(", ", columnNames)}");
        Assert.IsTrue(columnNames.All(c => c.Contains('C', StringComparison.OrdinalIgnoreCase)),
            $"All columns should contain 'C'; got: {string.Join(", ", columnNames)}");
    }

    [TestMethod]
    public void WhenStarExclude_WithWindowFunction_ShouldWork()
    {
        const string query = @"
            select * exclude (City), RowNumber() over (order by a.Name) as rn
            from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m) { City = "London", Name = "Bob" },
                    new BasicEntity("february", 70m) { City = "Paris", Name = "Alice" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        var columnNames = table.Columns.Select(c => c.ColumnName).ToList();
        Assert.IsFalse(columnNames.Any(c => c.Contains("City")));
        Assert.IsTrue(columnNames.Any(c => c.Contains("rn")));
    }

    [TestMethod]
    public void WhenStarReplace_WithWindowFunction_ShouldWork()
    {
        const string query = @"
            select * replace (Population * 2 as Population), RowNumber() over (order by a.Name) as rn
            from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m) { Name = "Alice", Population = 100m },
                    new BasicEntity("february", 70m) { Name = "Bob", Population = 200m }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        var columnNames = table.Columns.Select(c => c.ColumnName).ToList();
        var popIdx = columnNames.FindIndex(c => c.Contains("Population"));
        var rnIdx = columnNames.FindIndex(c => c.Contains("rn"));

        var alice = table.Single(r => Convert.ToInt32(r.Values[rnIdx]) == 1);
        Assert.AreEqual(200m, alice.Values[popIdx]);
    }

    [TestMethod]
    public void WhenStarExclude_WithInSubqueryInWhere_ShouldWork()
    {
        const string query = @"
            select * exclude (City) from #A.entities() a
            where a.Country in (select b.Country from #B.entities() b)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m) { City = "London", Country = "UK", Population = 100m },
                    new BasicEntity("february", 70m) { City = "Paris", Country = "FR", Population = 200m },
                    new BasicEntity("march", 90m) { City = "Berlin", Country = "DE", Population = 300m }
                ]
            },
            {
                "#B", [
                    new BasicEntity("april", 10m) { Country = "UK" },
                    new BasicEntity("may", 20m) { Country = "DE" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        var columnNames = table.Columns.Select(c => c.ColumnName).ToList();
        Assert.IsFalse(columnNames.Any(c => c.Contains("City")));
    }

    [TestMethod]
    public void WhenStarReplace_WithFilterOnAggregate_ShouldWork()
    {
        const string query = @"
            select a.Country,
                   Count(a.Country) filter (where a.Population > 100) as BigCityCount,
                   Sum(a.Population) as TotalPop
            from #A.entities() a
            group by a.Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m) { Country = "UK", Population = 50m },
                    new BasicEntity("february", 70m) { Country = "UK", Population = 200m },
                    new BasicEntity("march", 90m) { Country = "FR", Population = 300m }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        var uk = table.Single(r => (string)r.Values[0] == "UK");
        Assert.AreEqual(1, Convert.ToInt32(uk.Values[1]));
        Assert.AreEqual(250m, Convert.ToDecimal(uk.Values[2]));
    }

}

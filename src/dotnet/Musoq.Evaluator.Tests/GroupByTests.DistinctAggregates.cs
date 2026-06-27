using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class GroupByTests
{
    [TestMethod]
    public void CountDistinct_SimpleGroupBy_ShouldWork()
    {
        var query = @"select City, Count(distinct Name) from #A.Entities() group by City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { City = "NYC", Name = "John" },
                    new BasicEntity { City = "NYC", Name = "John" },
                    new BasicEntity { City = "NYC", Name = "Jane" },
                    new BasicEntity { City = "LA", Name = "Bob" },
                    new BasicEntity { City = "LA", Name = "Bob" },
                    new BasicEntity { City = "LA", Name = "Alice" },
                    new BasicEntity { City = "LA", Name = "Carol" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Table should have 2 rows (one per city)");

        var nycRow = table.FirstOrDefault(row => (string)row.Values[0] == "NYC");
        var laRow = table.FirstOrDefault(row => (string)row.Values[0] == "LA");

        Assert.IsNotNull(nycRow);
        Assert.IsNotNull(laRow);
        Assert.AreEqual(2L, nycRow.Values[1], "NYC should have 2 distinct names (John, Jane)");
        Assert.AreEqual(3L, laRow.Values[1], "LA should have 3 distinct names (Bob, Alice, Carol)");
    }

    [TestMethod]
    public void CountDistinct_NoGroupBy_ShouldCountAllDistinct()
    {
        var query = @"select Count(distinct Name) from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "John" },
                    new BasicEntity { Name = "John" },
                    new BasicEntity { Name = "Jane" },
                    new BasicEntity { Name = "Bob" },
                    new BasicEntity { Name = "Bob" },
                    new BasicEntity { Name = "Bob" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count, "Table should have 1 row");
        Assert.AreEqual(3L, table[0].Values[0], "Should have 3 distinct names (John, Jane, Bob)");
    }

    [TestMethod]
    public void CountDistinct_WithNullValues_ShouldExcludeNulls()
    {
        var query = @"select Count(distinct Name) from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "John" },
                    new BasicEntity { Name = null },
                    new BasicEntity { Name = "Jane" },
                    new BasicEntity { Name = null },
                    new BasicEntity { Name = "John" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count, "Table should have 1 row");
        Assert.AreEqual(2L, table[0].Values[0], "Should have 2 distinct names (John, Jane), nulls excluded");
    }

    [TestMethod]
    public void CountDistinct_NumericValues_ShouldWork()
    {
        var query = @"select Count(distinct Population) from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Population = 100 },
                    new BasicEntity { Population = 100 },
                    new BasicEntity { Population = 200 },
                    new BasicEntity { Population = 300 },
                    new BasicEntity { Population = 200 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count, "Table should have 1 row");
        Assert.AreEqual(3L, table[0].Values[0], "Should have 3 distinct population values (100, 200, 300)");
    }

    [TestMethod]
    public void CountDistinct_AllDuplicates_ShouldReturnOne()
    {
        var query = @"select Count(distinct Name) from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "Same" },
                    new BasicEntity { Name = "Same" },
                    new BasicEntity { Name = "Same" },
                    new BasicEntity { Name = "Same" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count, "Table should have 1 row");
        Assert.AreEqual(1L, table[0].Values[0], "Should have 1 distinct name");
    }

    [TestMethod]
    public void CountDistinct_AllUnique_ShouldReturnCount()
    {
        var query = @"select Count(distinct Name) from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A" },
                    new BasicEntity { Name = "B" },
                    new BasicEntity { Name = "C" },
                    new BasicEntity { Name = "D" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count, "Table should have 1 row");
        Assert.AreEqual(4L, table[0].Values[0], "Should have 4 distinct names");
    }

    [TestMethod]
    public void SumDistinct_IntValues_ShouldSumUnique()
    {
        var query = @"select Sum(distinct Population) from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Population = 100m },
                    new BasicEntity { Population = 100m },
                    new BasicEntity { Population = 200m },
                    new BasicEntity { Population = 300m },
                    new BasicEntity { Population = 200m }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count, "Table should have 1 row");
        Assert.AreEqual(600m, table[0].Values[0], "Sum of distinct values should be 100 + 200 + 300 = 600");
    }

    [TestMethod]
    public void SumDistinct_WithGroupBy_ShouldSumUniquePerGroup()
    {
        var query = @"select Name, Sum(distinct Population) from #A.Entities() group by Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A", Population = 100m },
                    new BasicEntity { Name = "A", Population = 100m },
                    new BasicEntity { Name = "A", Population = 200m },
                    new BasicEntity { Name = "B", Population = 50m },
                    new BasicEntity { Name = "B", Population = 50m }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Table should have 2 rows");

        var rowA = table.FirstOrDefault(r => r.Values[0] as string == "A");
        var rowB = table.FirstOrDefault(r => r.Values[0] as string == "B");

        Assert.IsNotNull(rowA);
        Assert.IsNotNull(rowB);
        Assert.AreEqual(300m, rowA.Values[1], "Sum of distinct for A should be 100 + 200 = 300");
        Assert.AreEqual(50m, rowB.Values[1], "Sum of distinct for B should be 50");
    }

    [TestMethod]
    public void AvgDistinct_IntValues_ShouldAverageUnique()
    {
        var query = @"select Avg(distinct Population) from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Population = 100m },
                    new BasicEntity { Population = 100m },
                    new BasicEntity { Population = 200m },
                    new BasicEntity { Population = 300m }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count, "Table should have 1 row");
        Assert.AreEqual(200m, table[0].Values[0], "Avg of distinct values should be (100 + 200 + 300) / 3 = 200");
    }

    [TestMethod]
    public void MinDistinct_IntValues_ShouldFindMinUnique()
    {
        var query = @"select Min(distinct Population) from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Population = 500m },
                    new BasicEntity { Population = 500m },
                    new BasicEntity { Population = 100m },
                    new BasicEntity { Population = 300m }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count, "Table should have 1 row");
        Assert.AreEqual(100m, table[0].Values[0], "Min of distinct values should be 100");
    }

    [TestMethod]
    public void MaxDistinct_IntValues_ShouldFindMaxUnique()
    {
        var query = @"select Max(distinct Population) from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Population = 100m },
                    new BasicEntity { Population = 100m },
                    new BasicEntity { Population = 500m },
                    new BasicEntity { Population = 300m }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count, "Table should have 1 row");
        Assert.AreEqual(500m, table[0].Values[0], "Max of distinct values should be 500");
    }
}

using System.Collections.Generic;
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("Count(Name)", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["NYC", 2L],
            ["LA", 3L]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("Count(Name)", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [3L]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("Count(Name)", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [2L]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("Count(Population)", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [3L]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("Count(Name)", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1L]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("Count(Name)", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [4L]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("Sum(Population)", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [600m]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Sum(Population)", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["A", 300m],
            ["B", 50m]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("Avg(Population)", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [200m]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("Min(Population)", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [100m]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("Max(Population)", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [500m]);
    }
}

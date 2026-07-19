using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class OrderByTests
{
    [TestMethod]
    public void WhenOrderByDescWithNullValues_ShouldHandleNulls()
    {
        var query = @"select Name, NullableValue from #A.Entities() order by NullableValue desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a") { NullableValue = 3 },
                    new BasicEntity("b") { NullableValue = null },
                    new BasicEntity("c") { NullableValue = 1 },
                    new BasicEntity("d") { NullableValue = null },
                    new BasicEntity("e") { NullableValue = 2 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table,
            ("Name", typeof(string)), ("NullableValue", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "a", 3 }, new object?[] { "e", 2 },
            new object?[] { "c", 1 }, new object?[] { "b", null }, new object?[] { "d", null });
    }

    [TestMethod]
    public void WhenOrderByDescWithNegativeNumbers_ShouldSortCorrectly()
    {
        var query = @"select City, Money from #A.Entities() order by Money desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a", "jan", 100m),
                    new BasicEntity("b", "feb", -50m),
                    new BasicEntity("c", "mar", 0m),
                    new BasicEntity("d", "apr", -100m),
                    new BasicEntity("e", "may", 50m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table,
            ("City", typeof(string)), ("Money", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "a", 100m }, new object?[] { "e", 50m },
            new object?[] { "c", 0m }, new object?[] { "b", -50m },
            new object?[] { "d", -100m });
    }

    [TestMethod]
    public void WhenOrderByDescWithStrings_ShouldSortDescending()
    {
        var query = @"select Name from #A.Entities() order by Name desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alpha"),
                    new BasicEntity("Zulu"),
                    new BasicEntity("Charlie"),
                    new BasicEntity("Bravo"),
                    new BasicEntity("Delta")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "Zulu" }, new object?[] { "Delta" },
            new object?[] { "Charlie" }, new object?[] { "Bravo" }, new object?[] { "Alpha" });
    }

    [TestMethod]
    public void WhenOrderByDescWithDateTime_ShouldSortDescending()
    {
        var query = @"select City, Time from #A.Entities() order by Time desc";

        var date1 = new DateTime(2024, 1, 1);
        var date2 = new DateTime(2024, 6, 15);
        var date3 = new DateTime(2023, 12, 31);
        var date4 = new DateTime(2024, 12, 31);

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity(date1) { City = "a" },
                    new BasicEntity(date2) { City = "b" },
                    new BasicEntity(date3) { City = "c" },
                    new BasicEntity(date4) { City = "d" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table,
            ("City", typeof(string)), ("Time", typeof(DateTime)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "d", date4 }, new object?[] { "b", date2 },
            new object?[] { "a", date1 }, new object?[] { "c", date3 });
    }

    [TestMethod]
    public void WhenOrderByDescWithSubquery_ShouldWork()
    {
        var query = @"
            select City, Money from
            (select City, Money from #A.Entities() order by Money desc) q
            order by City desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("katowice", "jan", 300m),
                    new BasicEntity("czestochowa", "feb", 400m),
                    new BasicEntity("cracow", "mar", 200m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table,
            ("City", typeof(string)), ("Money", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "katowice", 300m },
            new object?[] { "czestochowa", 400m },
            new object?[] { "cracow", 200m });
    }

    [TestMethod]
    public void WhenOrderByDescAfterUnion_ShouldUseDocumentedRightmostQueryOrdering()
    {
        var query = @"
            select City from #A.Entities() where Money > 200
            union (City)
            select City from #A.Entities() where Money <= 200
            order by City desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("alpha", "jan", 300m),
                    new BasicEntity("beta", "feb", 100m),
                    new BasicEntity("gamma", "mar", 400m),
                    new BasicEntity("delta", "apr", 150m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "alpha" }, new object?[] { "gamma" },
            new object?[] { "delta" }, new object?[] { "beta" });
    }

    [TestMethod]
    public void WhenGlobalOrderByDescIsNeededAfterUnion_ShouldUseDerivedTable()
    {
        var query = @"
            select u.City from (
                select City from #A.Entities() where Money > 200
                union (City)
                select City from #A.Entities() where Money <= 200
            ) u
            order by u.City desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("alpha", "jan", 300m),
                    new BasicEntity("beta", "feb", 100m),
                    new BasicEntity("gamma", "mar", 400m),
                    new BasicEntity("delta", "apr", 150m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("u.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "gamma" }, new object?[] { "delta" },
            new object?[] { "beta" }, new object?[] { "alpha" });
    }

    [TestMethod]
    public void WhenOrderByDescWithDistinct_ShouldWork()
    {
        var query = @"select distinct Country from #A.Entities() order by Country desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("city1", "Poland", 100),
                    new BasicEntity("city2", "Germany", 200),
                    new BasicEntity("city3", "Poland", 150),
                    new BasicEntity("city4", "France", 300),
                    new BasicEntity("city5", "Germany", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "Poland" }, new object?[] { "Germany" }, new object?[] { "France" });
    }

    [TestMethod]
    public void WhenOrderByDescWithComplexWhereClause_ShouldWork()
    {
        var query = @"
            select City, Money from #A.Entities()
            where Money > 100 and Money < 500
            order by Money desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a", "jan", 50m),
                    new BasicEntity("b", "feb", 300m),
                    new BasicEntity("c", "mar", 200m),
                    new BasicEntity("d", "apr", 600m),
                    new BasicEntity("e", "may", 400m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table,
            ("City", typeof(string)), ("Money", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "e", 400m }, new object?[] { "b", 300m },
            new object?[] { "c", 200m });
    }

    [TestMethod]
    public void WhenOrderByDescWithAliasedColumn_ShouldWork()
    {
        var query = @"select City as CityName, Money as Amount from #A.Entities() order by Amount desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a", "jan", 100m),
                    new BasicEntity("b", "feb", 300m),
                    new BasicEntity("c", "mar", 200m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table,
            ("CityName", typeof(string)), ("Amount", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "b", 300m }, new object?[] { "c", 200m },
            new object?[] { "a", 100m });
    }

    [TestMethod]
    public void WhenOrderByDescWithComputedColumn_ShouldWork()
    {
        var query = @"select City, Money * 2 as DoubledMoney from #A.Entities() order by DoubledMoney desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a", "jan", 100m),
                    new BasicEntity("b", "feb", 300m),
                    new BasicEntity("c", "mar", 200m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table,
            ("City", typeof(string)), ("DoubledMoney", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "b", 600m }, new object?[] { "c", 400m },
            new object?[] { "a", 200m });
    }

    [TestMethod]
    public void WhenOrderByDescWithStringFunction_ShouldWork()
    {
        var query = @"select Name from #A.Entities() order by ToUpper(Name) desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("apple"),
                    new BasicEntity("Zebra"),
                    new BasicEntity("banana"),
                    new BasicEntity("Cherry")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "Zebra" }, new object?[] { "Cherry" },
            new object?[] { "banana" }, new object?[] { "apple" });
    }

    [TestMethod]
    public void WhenOrderByDescWithEmptyResult_ShouldNotFail()
    {
        var query = @"select City, Money from #A.Entities() where Money > 1000 order by Money desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a", "jan", 100m),
                    new BasicEntity("b", "feb", 200m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table,
            ("City", typeof(string)), ("Money", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(table);
    }

    [TestMethod]
    public void WhenOrderByDescWithSingleRow_ShouldWork()
    {
        var query = @"select City, Money from #A.Entities() order by Money desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a", "jan", 100m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table,
            ("City", typeof(string)), ("Money", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { "a", 100m });
    }

    [TestMethod]
    public void WhenOrderByDescWithIdenticalValues_ShouldReturnAll()
    {
        var query = @"select City, Money from #A.Entities() order by Money desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a", "jan", 200m),
                    new BasicEntity("b", "feb", 200m),
                    new BasicEntity("c", "mar", 200m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table,
            ("City", typeof(string)), ("Money", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "a", 200m }, new object?[] { "b", 200m },
            new object?[] { "c", 200m });
    }
}

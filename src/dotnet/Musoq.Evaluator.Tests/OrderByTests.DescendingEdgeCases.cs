using System;
using System.Collections.Generic;
using System.Linq;
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

        Assert.AreEqual(5, table.Count);

        Assert.AreEqual(3, table[0].Values[1]);
        Assert.AreEqual(2, table[1].Values[1]);
        Assert.AreEqual(1, table[2].Values[1]);
    }

    [TestMethod]
    public void WhenOrderByDescWithNegativeNumbers_ShouldSortCorrectly()
    {
        var query = @"select City, Money from #A.Entities() order by Money desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a", "jan", Convert.ToDecimal(100)),
                    new BasicEntity("b", "feb", Convert.ToDecimal(-50)),
                    new BasicEntity("c", "mar", Convert.ToDecimal(0)),
                    new BasicEntity("d", "apr", Convert.ToDecimal(-100)),
                    new BasicEntity("e", "may", Convert.ToDecimal(50))
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(5, table.Count);
        Assert.AreEqual(100m, table[0].Values[1]);
        Assert.AreEqual(50m, table[1].Values[1]);
        Assert.AreEqual(0m, table[2].Values[1]);
        Assert.AreEqual(-50m, table[3].Values[1]);
        Assert.AreEqual(-100m, table[4].Values[1]);
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

        Assert.AreEqual(5, table.Count);
        Assert.AreEqual("Zulu", table[0].Values[0]);
        Assert.AreEqual("Delta", table[1].Values[0]);
        Assert.AreEqual("Charlie", table[2].Values[0]);
        Assert.AreEqual("Bravo", table[3].Values[0]);
        Assert.AreEqual("Alpha", table[4].Values[0]);
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

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual(date4, table[0].Values[1]);
        Assert.AreEqual(date2, table[1].Values[1]);
        Assert.AreEqual(date1, table[2].Values[1]);
        Assert.AreEqual(date3, table[3].Values[1]);
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
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("czestochowa", "feb", Convert.ToDecimal(400)),
                    new BasicEntity("cracow", "mar", Convert.ToDecimal(200))
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("katowice", table[0].Values[0]);
        Assert.AreEqual("czestochowa", table[1].Values[0]);
        Assert.AreEqual("cracow", table[2].Values[0]);
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
                    new BasicEntity("alpha", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("beta", "feb", Convert.ToDecimal(100)),
                    new BasicEntity("gamma", "mar", Convert.ToDecimal(400)),
                    new BasicEntity("delta", "apr", Convert.ToDecimal(150))
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);

        var cities = table.Select(r => (string)r.Values[0]).ToList();
        Assert.AreEqual("alpha", cities[0]);
        Assert.AreEqual("gamma", cities[1]);
        Assert.AreEqual("delta", cities[2]);
        Assert.AreEqual("beta", cities[3]);
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
                    new BasicEntity("alpha", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("beta", "feb", Convert.ToDecimal(100)),
                    new BasicEntity("gamma", "mar", Convert.ToDecimal(400)),
                    new BasicEntity("delta", "apr", Convert.ToDecimal(150))
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);

        var cities = table.Select(r => (string)r.Values[0]).ToList();
        Assert.AreEqual("gamma", cities[0]);
        Assert.AreEqual("delta", cities[1]);
        Assert.AreEqual("beta", cities[2]);
        Assert.AreEqual("alpha", cities[3]);
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

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("Poland", table[0].Values[0]);
        Assert.AreEqual("Germany", table[1].Values[0]);
        Assert.AreEqual("France", table[2].Values[0]);
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
                    new BasicEntity("a", "jan", Convert.ToDecimal(50)),
                    new BasicEntity("b", "feb", Convert.ToDecimal(300)),
                    new BasicEntity("c", "mar", Convert.ToDecimal(200)),
                    new BasicEntity("d", "apr", Convert.ToDecimal(600)),
                    new BasicEntity("e", "may", Convert.ToDecimal(400))
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual(400m, table[0].Values[1]);
        Assert.AreEqual(300m, table[1].Values[1]);
        Assert.AreEqual(200m, table[2].Values[1]);
    }

    [TestMethod]
    public void WhenOrderByDescWithAliasedColumn_ShouldWork()
    {
        var query = @"select City as CityName, Money as Amount from #A.Entities() order by Amount desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a", "jan", Convert.ToDecimal(100)),
                    new BasicEntity("b", "feb", Convert.ToDecimal(300)),
                    new BasicEntity("c", "mar", Convert.ToDecimal(200))
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual(300m, table[0].Values[1]);
        Assert.AreEqual(200m, table[1].Values[1]);
        Assert.AreEqual(100m, table[2].Values[1]);
    }

    [TestMethod]
    public void WhenOrderByDescWithComputedColumn_ShouldWork()
    {
        var query = @"select City, Money * 2 as DoubledMoney from #A.Entities() order by DoubledMoney desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a", "jan", Convert.ToDecimal(100)),
                    new BasicEntity("b", "feb", Convert.ToDecimal(300)),
                    new BasicEntity("c", "mar", Convert.ToDecimal(200))
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual(600m, table[0].Values[1]);
        Assert.AreEqual(400m, table[1].Values[1]);
        Assert.AreEqual(200m, table[2].Values[1]);
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

        Assert.AreEqual(4, table.Count);

        Assert.AreEqual("Zebra", table[0].Values[0]);
        Assert.AreEqual("Cherry", table[1].Values[0]);
        Assert.AreEqual("banana", table[2].Values[0]);
        Assert.AreEqual("apple", table[3].Values[0]);
    }

    [TestMethod]
    public void WhenOrderByDescWithEmptyResult_ShouldNotFail()
    {
        var query = @"select City, Money from #A.Entities() where Money > 1000 order by Money desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a", "jan", Convert.ToDecimal(100)),
                    new BasicEntity("b", "feb", Convert.ToDecimal(200))
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void WhenOrderByDescWithSingleRow_ShouldWork()
    {
        var query = @"select City, Money from #A.Entities() order by Money desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a", "jan", Convert.ToDecimal(100))
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(100m, table[0].Values[1]);
    }

    [TestMethod]
    public void WhenOrderByDescWithIdenticalValues_ShouldReturnAll()
    {
        var query = @"select City, Money from #A.Entities() order by Money desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a", "jan", Convert.ToDecimal(200)),
                    new BasicEntity("b", "feb", Convert.ToDecimal(200)),
                    new BasicEntity("c", "mar", Convert.ToDecimal(200))
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual(200m, table[0].Values[1]);
        Assert.AreEqual(200m, table[1].Values[1]);
        Assert.AreEqual(200m, table[2].Values[1]);
    }
}

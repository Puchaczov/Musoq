using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class OrderByTests
{
    [TestMethod]
    public void WhenOrderByAscWithAliasedColumn_ShouldWork()
    {
        var query = @"select City as CityName, Money as Amount from #A.Entities() order by Amount asc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("c", "mar", Convert.ToDecimal(300)),
                    new BasicEntity("a", "jan", Convert.ToDecimal(100)),
                    new BasicEntity("b", "feb", Convert.ToDecimal(200))
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual(100m, table[0].Values[1]);
        Assert.AreEqual(200m, table[1].Values[1]);
        Assert.AreEqual(300m, table[2].Values[1]);
    }

    [TestMethod]
    public void WhenOrderByWithAliasedComputedExpression_ShouldWork()
    {
        var query = @"select City, Money + Population as Total from #A.Entities() order by Total desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("cracow", "poland", 100) { Money = 10m },
                    new BasicEntity("warsaw", "poland", 500) { Money = 20m },
                    new BasicEntity("gdansk", "poland", 200) { Money = 5m }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual(520m, table[0].Values[1]);
        Assert.AreEqual(205m, table[1].Values[1]);
        Assert.AreEqual(110m, table[2].Values[1]);
    }

    [TestMethod]
    public void WhenOrderByWithMultipleAliases_ShouldWork()
    {
        var query = @"select City as CityName, Money as Amount from #A.Entities() order by CityName asc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("amsterdam", "feb", Convert.ToDecimal(100)),
                    new BasicEntity("berlin", "mar", Convert.ToDecimal(200))
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("amsterdam", table[0].Values[0]);
        Assert.AreEqual("berlin", table[1].Values[0]);
        Assert.AreEqual("cracow", table[2].Values[0]);
    }

    [TestMethod]
    public void WhenOrderByWithAliasAndGroupBy_ShouldWork()
    {
        var query = @"select City, Count(City) as CityCount from #A.Entities() group by City order by CityCount desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a", "jan", Convert.ToDecimal(100)),
                    new BasicEntity("a", "feb", Convert.ToDecimal(200)),
                    new BasicEntity("a", "mar", Convert.ToDecimal(300)),
                    new BasicEntity("b", "apr", Convert.ToDecimal(400)),
                    new BasicEntity("b", "may", Convert.ToDecimal(500)),
                    new BasicEntity("c", "jun", Convert.ToDecimal(600))
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("a", table[0].Values[0]);
        Assert.AreEqual(3L, table[0].Values[1]);
        Assert.AreEqual("b", table[1].Values[0]);
        Assert.AreEqual(2L, table[1].Values[1]);
        Assert.AreEqual("c", table[2].Values[0]);
        Assert.AreEqual(1L, table[2].Values[1]);
    }

    [TestMethod]
    public void WhenOrderByWithAliasedAggregateFunction_ShouldWork()
    {
        var query = @"select City, Sum(Money) as TotalMoney from #A.Entities() group by City order by TotalMoney desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a", "jan", Convert.ToDecimal(100)),
                    new BasicEntity("a", "feb", Convert.ToDecimal(200)),
                    new BasicEntity("b", "mar", Convert.ToDecimal(500)),
                    new BasicEntity("c", "apr", Convert.ToDecimal(50))
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("b", table[0].Values[0]);
        Assert.AreEqual(500m, table[0].Values[1]);
        Assert.AreEqual("a", table[1].Values[0]);
        Assert.AreEqual(300m, table[1].Values[1]);
        Assert.AreEqual("c", table[2].Values[0]);
        Assert.AreEqual(50m, table[2].Values[1]);
    }

    [TestMethod]
    public void WhenOrderByAliasCaseInsensitive_ShouldWork()
    {
        var query = @"select City as cityname, Money as AMOUNT from #A.Entities() order by amount desc";

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
    public void WhenOrderByAliasWithTake_ShouldWork()
    {
        var query = @"select City, Money as Amount from #A.Entities() order by Amount desc take 2";

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

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(300m, table[0].Values[1]);
        Assert.AreEqual(200m, table[1].Values[1]);
    }

    [TestMethod]
    public void WhenOrderByTopOffsetUsesHiddenExpression_ShouldReturnOnlyProjectedRows()
    {
        var query = @"select Name from #A.Entities() order by Population + Money desc skip 1 take 3";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "alpha", Population = 100m, Money = 10m },
                    new BasicEntity { Name = "bravo", Population = 60m, Money = 20m },
                    new BasicEntity { Name = "charlie", Population = 70m, Money = 5m },
                    new BasicEntity { Name = "delta", Population = 30m, Money = 35m },
                    new BasicEntity { Name = "echo", Population = 20m, Money = 1m }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("bravo", table[0].Values[0]);
        Assert.AreEqual("charlie", table[1].Values[0]);
        Assert.AreEqual("delta", table[2].Values[0]);
    }
}

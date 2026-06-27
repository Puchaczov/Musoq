using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class OrderByTests
{
    [TestMethod]
    public void WhenOrderByAliasShadowsColumnName_ShouldSortByAliasExpression()
    {
        // Bug repro: alias "Name" shadows the original column "Name".
        // ORDER BY Name should use the aliased ToInt32(Name), not the raw string column.
        var query = @"select Name as RawName, ToInt32(Name) as Name from #A.Entities() order by Name asc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("20"),
                    new BasicEntity("3"),
                    new BasicEntity("1"),
                    new BasicEntity("10"),
                    new BasicEntity("2")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(5, table.Count);

        // Integer sort: 1, 2, 3, 10, 20  (NOT string sort: "1", "10", "2", "20", "3")
        Assert.AreEqual("1", table[0].Values[0]);
        Assert.AreEqual(1, table[0].Values[1]);

        Assert.AreEqual("2", table[1].Values[0]);
        Assert.AreEqual(2, table[1].Values[1]);

        Assert.AreEqual("3", table[2].Values[0]);
        Assert.AreEqual(3, table[2].Values[1]);

        Assert.AreEqual("10", table[3].Values[0]);
        Assert.AreEqual(10, table[3].Values[1]);

        Assert.AreEqual("20", table[4].Values[0]);
        Assert.AreEqual(20, table[4].Values[1]);
    }

    [TestMethod]
    public void WhenOrderByAliasShadowsColumnNameDesc_ShouldSortByAliasExpression()
    {
        var query = @"select Name as RawName, ToInt32(Name) as Name from #A.Entities() order by Name desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("20"),
                    new BasicEntity("3"),
                    new BasicEntity("1"),
                    new BasicEntity("10"),
                    new BasicEntity("2")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(5, table.Count);

        Assert.AreEqual("20", table[0].Values[0]);
        Assert.AreEqual(20, table[0].Values[1]);

        Assert.AreEqual("10", table[1].Values[0]);
        Assert.AreEqual(10, table[1].Values[1]);

        Assert.AreEqual("3", table[2].Values[0]);
        Assert.AreEqual(3, table[2].Values[1]);

        Assert.AreEqual("2", table[3].Values[0]);
        Assert.AreEqual(2, table[3].Values[1]);

        Assert.AreEqual("1", table[4].Values[0]);
        Assert.AreEqual(1, table[4].Values[1]);
    }

    [TestMethod]
    public void WhenOrderByAliasShadowsColumnNameWithGroupBy_ShouldSortByAliasExpression()
    {
        var query = @"select Name as RawName, ToInt32(Name) as Name from #A.Entities() group by Name order by Name asc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("20"),
                    new BasicEntity("3"),
                    new BasicEntity("1"),
                    new BasicEntity("10"),
                    new BasicEntity("2")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(5, table.Count);

        Assert.AreEqual("1", table[0].Values[0]);
        Assert.AreEqual(1, table[0].Values[1]);

        Assert.AreEqual("2", table[1].Values[0]);
        Assert.AreEqual(2, table[1].Values[1]);

        Assert.AreEqual("3", table[2].Values[0]);
        Assert.AreEqual(3, table[2].Values[1]);

        Assert.AreEqual("10", table[3].Values[0]);
        Assert.AreEqual(10, table[3].Values[1]);

        Assert.AreEqual("20", table[4].Values[0]);
        Assert.AreEqual(20, table[4].Values[1]);
    }
}

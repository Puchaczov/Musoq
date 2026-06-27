using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class OrderByTests
{
    [TestMethod]
    public void WhenOrderByAliasOfFunctionCall_ShouldSortByTransformedValue()
    {
        var query = @"select Name, ToInt32(Name) as NumValue from #A.Entities() order by NumValue asc";

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

    [TestMethod]
    public void WhenOrderByAliasOfFunctionCallDesc_ShouldSortByTransformedValue()
    {
        var query = @"select Name, ToInt32(Name) as NumValue from #A.Entities() order by NumValue desc";

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
    public void WhenOrderByAliasOfFunctionCallWithGroupBy_ShouldSortByTransformedValue()
    {
        var query = @"select Name, ToInt32(Name) as NumValue from #A.Entities() group by Name order by NumValue asc";

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

    [TestMethod]
    public void WhenOrderByAliasOfFunctionCallOnlyAliasInSelect_ShouldSortByTransformedValue()
    {
        var query = @"select ToInt32(Name) as NumValue from #A.Entities() order by NumValue asc";

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

        Assert.AreEqual(1, table[0].Values[0]);
        Assert.AreEqual(2, table[1].Values[0]);
        Assert.AreEqual(3, table[2].Values[0]);
        Assert.AreEqual(10, table[3].Values[0]);
        Assert.AreEqual(20, table[4].Values[0]);
    }

    [TestMethod]
    public void WhenOrderByAliasOfFunctionCallWithWhere_ShouldSortByTransformedValue()
    {
        var query = @"select Name, ToInt32(Name) as NumValue from #A.Entities() where ToInt32(Name) > 2 order by NumValue asc";

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

        Assert.AreEqual(3, table.Count);

        Assert.AreEqual("3", table[0].Values[0]);
        Assert.AreEqual(3, table[0].Values[1]);

        Assert.AreEqual("10", table[1].Values[0]);
        Assert.AreEqual(10, table[1].Values[1]);

        Assert.AreEqual("20", table[2].Values[0]);
        Assert.AreEqual(20, table[2].Values[1]);
    }

    [TestMethod]
    public void WhenOrderByAliasOfToStringFromDecimal_ShouldSortByString()
    {
        var query = @"select Money, ToString(Money) as MoneyStr from #A.Entities() order by MoneyStr asc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a", "jan", Convert.ToDecimal(100)),
                    new BasicEntity("b", "jan", Convert.ToDecimal(20)),
                    new BasicEntity("c", "jan", Convert.ToDecimal(3)),
                    new BasicEntity("d", "jan", Convert.ToDecimal(1000)),
                    new BasicEntity("e", "jan", Convert.ToDecimal(5))
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(5, table.Count);

        // String sort: "100", "1000", "20", "3", "5"
        Assert.AreEqual("100", table[0].Values[1]);
        Assert.AreEqual("1000", table[1].Values[1]);
        Assert.AreEqual("20", table[2].Values[1]);
        Assert.AreEqual("3", table[3].Values[1]);
        Assert.AreEqual("5", table[4].Values[1]);
    }

    [TestMethod]
    public void WhenOrderByAliasWithSameColumnAliasedAndTransformed_ShouldSortByTransformedValue()
    {
        // This is the exact user-reported scenario:
        // SELECT SomeStringColumn as OldColumn, ToInt32(SomeStringColumn) as SomeAlias
        // ORDER BY SomeAlias
        // Bug: sorts by string OldColumn value instead of integer SomeAlias value
        var query = @"select Name as OldColumn, ToInt32(Name) as NumValue from #A.Entities() order by NumValue asc";

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

        // Integer sort: 1, 2, 3, 10, 20 (NOT string sort: "1", "10", "2", "20", "3")
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
    public void WhenOrderByAliasWithSameColumnAliasedAndTransformedDesc_ShouldSortByTransformedValue()
    {
        var query = @"select Name as OldColumn, ToInt32(Name) as NumValue from #A.Entities() order by NumValue desc";

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
    public void WhenOrderByAliasWithSameColumnAliasedAndTransformedWithGroupBy_ShouldSortByTransformedValue()
    {
        var query = @"select Name as OldColumn, ToInt32(Name) as NumValue from #A.Entities() group by Name order by NumValue asc";

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

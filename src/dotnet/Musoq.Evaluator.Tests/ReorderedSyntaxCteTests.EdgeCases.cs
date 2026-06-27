using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class ReorderedSyntaxCteTests
{
    [TestMethod]
    public void ReorderedQuery_WithCaseWhen_InCte_ShouldWork()
    {
        var query = @"
            with cte as (
                from #A.Entities()
                select City,
                       case when Population > 400 then 'Large' else 'Small' end as Size
            )
            select City, Size from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("KATOWICE", "POLAND", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row =>
            (string)row.Values[0] == "WARSAW" &&
            (string)row.Values[1] == "Large"));
        Assert.IsTrue(table.Any(row =>
            (string)row.Values[0] == "KATOWICE" &&
            (string)row.Values[1] == "Small"));
    }

    [TestMethod]
    public void ReorderedQuery_WithArithmeticExpressions_InCte_ShouldWork()
    {
        var query = @"
            with cte as (
                from #A.Entities()
                select City, Population * 2 as DoubledPop, Population + 100 as IncreasedPop
            )
            select City, DoubledPop, IncreasedPop from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("WARSAW", table[0].Values[0]);
        Assert.AreEqual(1000m, table[0].Values[1]);
        Assert.AreEqual(600m, table[0].Values[2]);
    }

    [TestMethod]
    public void ReorderedQuery_WithStar_InCte_ShouldWork()
    {
        var query = @"
            with cte as (
                from #A.Entities() where Country = 'POLAND' select *
            )
            select City, Country from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("WARSAW", table[0].Values[0]);
    }

    [TestMethod]
    public void ReorderedQuery_WithAliasedStar_InCte_ShouldWork()
    {
        var query = @"
            with cte as (
                from #A.Entities() a where a.Country = 'POLAND' select a.*
            )
            select City, Country from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("WARSAW", table[0].Values[0]);
    }

    [TestMethod]
    public void ReorderedQuery_ComplexWhereConditions_InCte_ShouldWork()
    {
        var query = @"
            with cte as (
                from #A.Entities()
                where Country = 'POLAND' and Population > 300 or Country = 'GERMANY'
                select City, Country, Population
            )
            select City, Country from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("KATOWICE", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "WARSAW"));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "BERLIN"));
    }

    [TestMethod]
    public void ReorderedQuery_WithFunctionCalls_InCte_ShouldWork()
    {
        var query = @"
            with cte as (
                from #A.Entities()
                select ToUpper(City) as UpperCity, ToLower(Country) as LowerCountry
            )
            select UpperCity, LowerCountry from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Warsaw", "Poland", 500)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("WARSAW", table[0].Values[0]);
        Assert.AreEqual("poland", table[0].Values[1]);
    }

}

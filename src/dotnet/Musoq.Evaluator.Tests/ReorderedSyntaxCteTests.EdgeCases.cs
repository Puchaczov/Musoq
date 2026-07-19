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

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Size", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["WARSAW", "Large"], ["KATOWICE", "Small"]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)), ("DoubledPop", typeof(decimal)), ("IncreasedPop", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["WARSAW", 1000m, 600m]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["WARSAW", "POLAND"]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["WARSAW", "POLAND"]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["WARSAW", "POLAND"], ["BERLIN", "GERMANY"]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("UpperCity", typeof(string)), ("LowerCountry", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["WARSAW", "poland"]);
    }

}

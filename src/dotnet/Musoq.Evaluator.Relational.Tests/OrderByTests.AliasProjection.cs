using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class OrderByTests
{
    [TestMethod]
    public void WhenOrderByUsesProjectionAlias_ShouldKeepCompatibilityBehaviorWithoutMq5009()
    {
        const string query = "select City as SortKey, Money from #A.Entities() order by SortKey";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity("zeta", "city", 1m),
                new BasicEntity("alpha", "city", 2m)
            ]
        };

        var analysis = new QueryAnalyzer(
            new BasicSchemaProvider<BasicEntity>(sources)).Analyze(query);

        Assert.IsFalse(analysis.HasErrors, string.Join(" | ", analysis.Diagnostics));
        Assert.IsFalse(analysis.Warnings.Any(static warning => (int)warning.Code == 5009));
        Assert.IsNull(ErrorMetadataCatalog.Get((DiagnosticCode)5009));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["alpha", 2m], ["zeta", 1m]);
    }

    [TestMethod]
    public void WhenOrderByAscWithAliasedColumn_ShouldWork()
    {
        var query = @"select City as CityName, Money as Amount from #A.Entities() order by Amount asc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("c", "mar", 300m),
                    new BasicEntity("a", "jan", 100m),
                    new BasicEntity("b", "feb", 200m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("CityName", typeof(string)), ("Amount", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["a", 100m], ["b", 200m], ["c", 300m]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Total", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["warsaw", 520m], ["gdansk", 205m], ["cracow", 110m]);
    }

    [TestMethod]
    public void WhenOrderByWithMultipleAliases_ShouldWork()
    {
        var query = @"select City as CityName, Money as Amount from #A.Entities() order by CityName asc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("cracow", "jan", 300m),
                    new BasicEntity("amsterdam", "feb", 100m),
                    new BasicEntity("berlin", "mar", 200m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("CityName", typeof(string)), ("Amount", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["amsterdam", 100m], ["berlin", 200m], ["cracow", 300m]);
    }

    [TestMethod]
    public void WhenOrderByWithAliasAndGroupBy_ShouldWork()
    {
        var query = @"select City, Count(City) as CityCount from #A.Entities() group by City order by CityCount desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a", "jan", 100m),
                    new BasicEntity("a", "feb", 200m),
                    new BasicEntity("a", "mar", 300m),
                    new BasicEntity("b", "apr", 400m),
                    new BasicEntity("b", "may", 500m),
                    new BasicEntity("c", "jun", 600m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("CityCount", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["a", 3L], ["b", 2L], ["c", 1L]);
    }

    [TestMethod]
    public void WhenOrderByWithAliasedAggregateFunction_ShouldWork()
    {
        var query = @"select City, Sum(Money) as TotalMoney from #A.Entities() group by City order by TotalMoney desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a", "jan", 100m),
                    new BasicEntity("a", "feb", 200m),
                    new BasicEntity("b", "mar", 500m),
                    new BasicEntity("c", "apr", 50m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("TotalMoney", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["b", 500m], ["a", 300m], ["c", 50m]);
    }

    [TestMethod]
    public void WhenOrderByAliasCaseInsensitive_ShouldWork()
    {
        var query = @"select City as cityname, Money as AMOUNT from #A.Entities() order by amount desc";

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

        TableMaterializationTestHelper.AssertColumns(table, ("cityname", typeof(string)), ("AMOUNT", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["b", 300m], ["c", 200m], ["a", 100m]);
    }

    [TestMethod]
    public void WhenOrderByAliasWithTake_ShouldWork()
    {
        var query = @"select City, Money as Amount from #A.Entities() order by Amount desc take 2";

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

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Amount", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["b", 300m], ["c", 200m]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["bravo"], ["charlie"], ["delta"]);
    }
}

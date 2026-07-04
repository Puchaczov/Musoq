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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("RawName", typeof(string)),
            ("Name", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["1", 1],
            ["2", 2],
            ["3", 3],
            ["10", 10],
            ["20", 20]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("RawName", typeof(string)),
            ("Name", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["20", 20],
            ["10", 10],
            ["3", 3],
            ["2", 2],
            ["1", 1]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("RawName", typeof(string)),
            ("Name", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["1", 1],
            ["2", 2],
            ["3", 3],
            ["10", 10],
            ["20", 20]);
    }
}

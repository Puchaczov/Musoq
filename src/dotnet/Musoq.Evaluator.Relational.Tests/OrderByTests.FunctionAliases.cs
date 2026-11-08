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

        TableMaterializationTestHelper.AssertColumns(table,
            ("Name", typeof(string)), ("NumValue", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "1", 1 }, new object?[] { "2", 2 },
            new object?[] { "3", 3 }, new object?[] { "10", 10 }, new object?[] { "20", 20 });
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

        TableMaterializationTestHelper.AssertColumns(table,
            ("Name", typeof(string)), ("NumValue", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "20", 20 }, new object?[] { "10", 10 },
            new object?[] { "3", 3 }, new object?[] { "2", 2 }, new object?[] { "1", 1 });
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

        TableMaterializationTestHelper.AssertColumns(table,
            ("Name", typeof(string)), ("NumValue", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "1", 1 }, new object?[] { "2", 2 },
            new object?[] { "3", 3 }, new object?[] { "10", 10 }, new object?[] { "20", 20 });
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

        TableMaterializationTestHelper.AssertColumns(table, ("NumValue", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { 1 }, new object?[] { 2 }, new object?[] { 3 },
            new object?[] { 10 }, new object?[] { 20 });
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

        TableMaterializationTestHelper.AssertColumns(table,
            ("Name", typeof(string)), ("NumValue", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "3", 3 }, new object?[] { "10", 10 }, new object?[] { "20", 20 });
    }

    [TestMethod]
    public void WhenOrderByAliasOfToStringFromDecimal_ShouldSortByString()
    {
        var query = @"select Money, ToString(Money) as MoneyStr from #A.Entities() order by MoneyStr asc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a", "jan", 100m),
                    new BasicEntity("b", "jan", 20m),
                    new BasicEntity("c", "jan", 3m),
                    new BasicEntity("d", "jan", 1000m),
                    new BasicEntity("e", "jan", 5m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table,
            ("Money", typeof(decimal)), ("MoneyStr", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { 100m, "100" }, new object?[] { 1000m, "1000" },
            new object?[] { 20m, "20" }, new object?[] { 3m, "3" },
            new object?[] { 5m, "5" });
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

        TableMaterializationTestHelper.AssertColumns(table,
            ("OldColumn", typeof(string)), ("NumValue", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "1", 1 }, new object?[] { "2", 2 },
            new object?[] { "3", 3 }, new object?[] { "10", 10 }, new object?[] { "20", 20 });
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

        TableMaterializationTestHelper.AssertColumns(table,
            ("OldColumn", typeof(string)), ("NumValue", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "20", 20 }, new object?[] { "10", 10 },
            new object?[] { "3", 3 }, new object?[] { "2", 2 }, new object?[] { "1", 1 });
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

        TableMaterializationTestHelper.AssertColumns(table,
            ("OldColumn", typeof(string)), ("NumValue", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "1", 1 }, new object?[] { "2", 2 },
            new object?[] { "3", 3 }, new object?[] { "10", 10 }, new object?[] { "20", 20 });
    }
}

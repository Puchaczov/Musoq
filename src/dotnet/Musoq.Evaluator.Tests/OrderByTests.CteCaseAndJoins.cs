using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class OrderByTests
{
    [TestMethod]
    public void WhenOrderByWithinCteExpression_ShouldSucceed()
    {
        const string query =
            @"with cte as ( select City, Money from #A.Entities() order by Money ) select City from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("katowice", "jan", 300m),
                    new BasicEntity("katowice", "feb", 100m),
                    new BasicEntity("czestochowa", "jan", 400m),
                    new BasicEntity("cracow", "jan", 10m),
                    new BasicEntity("glasgow", "feb", -10m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["glasgow"],
            ["cracow"],
            ["katowice"],
            ["katowice"],
            ["czestochowa"]);
    }

    [TestMethod]
    public void WhenOrderByDescWithinCteExpression_ShouldSucceed()
    {
        const string query =
            @"with cte as ( select City, Money from #A.Entities() order by Money desc ) select City from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("katowice", "jan", 300m),
                    new BasicEntity("katowice", "feb", 100m),
                    new BasicEntity("czestochowa", "jan", 400m),
                    new BasicEntity("cracow", "jan", 10m),
                    new BasicEntity("glasgow", "feb", -10m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["czestochowa"],
            ["katowice"],
            ["katowice"],
            ["cracow"],
            ["glasgow"]);
    }

    [TestMethod]
    public void WhenOrderByWithMultipleColumnsFirstDescWithinCteExpression_ShouldSucceed()
    {
        const string query =
            @"with cte as ( select City, Money from #A.Entities() order by Money desc, City ) select City from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("katowice", "jan", 300m),
                    new BasicEntity("katowice", "feb", 100m),
                    new BasicEntity("czestochowa", "jan", 400m),
                    new BasicEntity("cracow", "jan", 10m),
                    new BasicEntity("glasgow", "feb", -10m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["czestochowa"],
            ["katowice"],
            ["katowice"],
            ["cracow"],
            ["glasgow"]);
    }

    [TestMethod]
    public void WhenOrderByWithMultipleColumnsBothDescWithinCteExpression_ShouldSucceed()
    {
        const string query =
            @"with cte as ( select City, Money from #A.Entities() order by Money desc, City desc ) select City from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("katowice", "jan", 300m),
                    new BasicEntity("katowice", "feb", 100m),
                    new BasicEntity("czestochowa", "jan", 400m),
                    new BasicEntity("cracow", "jan", 10m),
                    new BasicEntity("glasgow", "feb", -10m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["czestochowa"],
            ["katowice"],
            ["katowice"],
            ["cracow"],
            ["glasgow"]);
    }

    [TestMethod]
    public void WhenOrderByWithMultipleColumnsBothDescWithinCteExpression_BothRetrieved_ShouldSucceed()
    {
        const string query =
            @"with cte as ( select City, Money from #A.Entities() order by Money desc, City desc ) select City, Money from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("katowice", "jan", 300m),
                    new BasicEntity("katowice", "feb", 100m),
                    new BasicEntity("czestochowa", "jan", 400m),
                    new BasicEntity("cracow", "jan", 10m),
                    new BasicEntity("glasgow", "feb", -10m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("Money", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["czestochowa", 400m],
            ["katowice", 300m],
            ["katowice", 100m],
            ["cracow", 10m],
            ["glasgow", -10m]);
    }

    [TestMethod]
    public void WhenOrderByCaseWhenExpression_ShouldSucceed()
    {
        var query = @"select City from #A.Entities() order by case when Money > 0 then Money else 0d end";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("katowice", "jan", 300m),
                    new BasicEntity("katowice", "feb", 100m),
                    new BasicEntity("czestochowa", "jan", 400m),
                    new BasicEntity("cracow", "jan", 10m),
                    new BasicEntity("glasgow", "feb", -10m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["glasgow"],
            ["cracow"],
            ["katowice"],
            ["katowice"],
            ["czestochowa"]);
    }

    [TestMethod]
    public void WhenOrderByCaseWhenDescExpression_ShouldSucceed()
    {
        var query = @"select City from #A.Entities() order by case when Money > 0 then Money else 0d end desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("katowice", "jan", 300m),
                    new BasicEntity("katowice", "feb", 100m),
                    new BasicEntity("czestochowa", "jan", 400m),
                    new BasicEntity("cracow", "jan", 10m),
                    new BasicEntity("glasgow", "feb", -10m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["czestochowa"],
            ["katowice"],
            ["katowice"],
            ["cracow"],
            ["glasgow"]);
    }

    [TestMethod]
    public void WhenOrderByMultipleColumnsFirstOneIsCaseWhenExpression_ShouldSucceed()
    {
        var query = @"select City from #A.Entities() order by case when Money > 0 then Money else 0d end, City";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("katowice", "jan", 300m),
                    new BasicEntity("katowice", "feb", 100m),
                    new BasicEntity("czestochowa", "jan", 400m),
                    new BasicEntity("cracow", "jan", 10m),
                    new BasicEntity("glasgow", "feb", -10m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["glasgow"],
            ["cracow"],
            ["katowice"],
            ["katowice"],
            ["czestochowa"]);
    }

    [TestMethod]
    public void WhenOrderByMultipleColumnsFirstOneIsCaseWhenDescExpression_ShouldSucceed()
    {
        var query =
            @"select City from #A.Entities() order by case when Money > 0 then Money else 0d end desc, City desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("katowice", "jan", 300m),
                    new BasicEntity("katowice", "feb", 100m),
                    new BasicEntity("czestochowa", "jan", 400m),
                    new BasicEntity("cracow", "jan", 10m),
                    new BasicEntity("glasgow", "feb", -10m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["czestochowa"],
            ["katowice"],
            ["katowice"],
            ["cracow"],
            ["glasgow"]);
    }

    [TestMethod]
    public void WhenOrderByWithInnerJoin_ShouldSucceed()
    {
        var query =
            @"select a.City from #A.Entities() a inner join #A.Entities() b on a.City = b.City order by a.Money";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("katowice", "jan", 300m),
                    new BasicEntity("czestochowa", "jan", 400m),
                    new BasicEntity("cracow", "jan", 10m),
                    new BasicEntity("glasgow", "feb", -10m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["glasgow"],
            ["cracow"],
            ["katowice"],
            ["czestochowa"]);
    }

    [TestMethod]
    public void WhenOrderByDescendingWithInnerJoin_ShouldSucceed()
    {
        var query =
            @"select a.City from #A.Entities() a inner join #A.Entities() b on a.City = b.City order by a.Money desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("katowice", "jan", 300m),
                    new BasicEntity("czestochowa", "jan", 400m),
                    new BasicEntity("cracow", "jan", 10m),
                    new BasicEntity("glasgow", "feb", -10m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["czestochowa"],
            ["katowice"],
            ["cracow"],
            ["glasgow"]);
    }

    [TestMethod]
    public void WhenOrderByWithInnerJoinAndGroupBy_ShouldSucceed()
    {
        var query =
            @"select a.City from #A.Entities() a inner join #A.Entities() b on a.City = b.City group by a.City order by a.City";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("katowice", "jan", 300m),
                    new BasicEntity("czestochowa", "jan", 400m),
                    new BasicEntity("cracow", "jan", 10m),
                    new BasicEntity("glasgow", "feb", -10m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["cracow"],
            ["czestochowa"],
            ["glasgow"],
            ["katowice"]);
    }

    [TestMethod]
    public void WhenOrderByDescendingWithInnerJoinAndGroupBy_ShouldSucceed()
    {
        var query =
            @"select a.City from #A.Entities() a inner join #A.Entities() b on a.City = b.City group by a.City order by a.City desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("katowice", "jan", 300m),
                    new BasicEntity("czestochowa", "jan", 400m),
                    new BasicEntity("cracow", "jan", 10m),
                    new BasicEntity("glasgow", "feb", -10m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["katowice"],
            ["glasgow"],
            ["czestochowa"],
            ["cracow"]);
    }

    [TestMethod]
    public void WhenOrderByWithGroupBy_ShouldSucceed()
    {
        const string query = """
                             select
                                a.GetTypeName(a.Name),
                                a.Count(a.Name)
                             from #A.Entities() a
                             group by a.GetTypeName(a.Name)
                             order by a.GetTypeName(a.Name)
                             """;
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [new BasicEntity("a"), new BasicEntity("b"), new BasicEntity("c")]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.GetTypeName(a.Name)", typeof(string)),
            ("a.Count(a.Name)", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["System.String", 3L]);
    }
}

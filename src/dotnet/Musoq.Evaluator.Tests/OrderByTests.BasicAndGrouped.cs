using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class OrderByTests
{
    [TestMethod]
    public void WhenOrderByColumn_ShouldSucceed()
    {
        var query = @"select City from #A.Entities() order by Money";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("czestochowa", "jan", 400m),
                    new BasicEntity("katowice", "jan", 300m),
                    new BasicEntity("cracow", "jan", -200m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["cracow"], ["katowice"], ["czestochowa"]);
    }

    [TestMethod]
    public void WhenOrderByDescColumn_ShouldSucceed()
    {
        var query = @"select City from #A.Entities() order by Money desc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("katowice", "jan", 300m),
                    new BasicEntity("czestochowa", "jan", 400m),
                    new BasicEntity("cracow", "jan", -200m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["czestochowa"], ["katowice"], ["cracow"]);
    }

    [TestMethod]
    public void WhenOrderByMultipleColumnFirstDesc_ShouldSucceed()
    {
        var query = @"select City from #A.Entities() order by Money desc, Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("katowice", "jan", 300m),
                    new BasicEntity("czestochowa", "jan", 400m),
                    new BasicEntity("cracow", "jan", -200m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["czestochowa"], ["katowice"], ["cracow"]);
    }

    [TestMethod]
    public void WhenOrderByMultipleColumns_ShoulSucceed()
    {
        var query = @"select City from #A.Entities() order by Money, Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("katowice", "jan", 300m),
                    new BasicEntity("czestochowa", "jan", 400m),
                    new BasicEntity("cracow", "jan", -200m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["cracow"], ["katowice"], ["czestochowa"]);
    }

    [TestMethod]
    public void WhenOrderByMultipleColumnsBothDesc_ShouldSucceed()
    {
        var query = @"select City from #A.Entities() order by Money desc, Name desc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("katowice", "jan", 300m),
                    new BasicEntity("czestochowa", "jan", 400m),
                    new BasicEntity("cracow", "jan", -200m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["czestochowa"], ["katowice"], ["cracow"]);
    }

    [TestMethod]
    public void WhenOrderByMultipleColumnsSecondColumnDesc_ShouldSucceed()
    {
        var query = @"select City + '-' + ToString(Money) from #A.Entities() order by City, Money desc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("katowice", "jan", 300m),
                    new BasicEntity("katowice", "feb", 100m),
                    new BasicEntity("czestochowa", "jan", 400m),
                    new BasicEntity("cracow", "jan", 10m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City + - + ToString(Money)", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["cracow-10"], ["czestochowa-400"], ["katowice-300"], ["katowice-100"]);
    }

    [TestMethod]
    public void WhenOrderByAfterGroupBy_ShouldSuccess()
    {
        var query = @"select City from #A.Entities() group by City order by City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("katowice", "jan", 300m),
                    new BasicEntity("katowice", "feb", 100m),
                    new BasicEntity("czestochowa", "jan", 400m),
                    new BasicEntity("cracow", "jan", 10m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["cracow"], ["czestochowa"], ["katowice"]);
    }

    [TestMethod]
    public void WhenOrderByWithDescAfterGroupBy_ShouldSucceed()
    {
        var query = @"select City from #A.Entities() group by City order by City desc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("katowice", "jan", 300m),
                    new BasicEntity("katowice", "feb", 100m),
                    new BasicEntity("czestochowa", "jan", 400m),
                    new BasicEntity("cracow", "jan", 10m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["katowice"], ["czestochowa"], ["cracow"]);
    }

    [TestMethod]
    public void WhenOrderByWithGroupByMultipleColumnAndFirstDesc_ShouldSucceed()
    {
        var query = @"select City, Money from #A.Entities() group by City, Money order by City desc, Money";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("katowice", "jan", 300m),
                    new BasicEntity("katowice", "feb", 100m),
                    new BasicEntity("czestochowa", "jan", 400m),
                    new BasicEntity("cracow", "jan", 10m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Money", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["katowice", 100m], ["katowice", 300m], ["czestochowa", 400m], ["cracow", 10m]);
    }

    [TestMethod]
    public void WhenOrderByAfterGroupByMultipleColumnBothDesc_ShouldSucceed()
    {
        var query = @"select City, Money from #A.Entities() group by City, Money order by City desc, Money desc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("katowice", "jan", 300m),
                    new BasicEntity("katowice", "feb", 100m),
                    new BasicEntity("czestochowa", "jan", 400m),
                    new BasicEntity("cracow", "jan", 10m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Money", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["katowice", 300m], ["katowice", 100m], ["czestochowa", 400m], ["cracow", 10m]);
    }

    [TestMethod]
    public void WhenOrderByAfterGroupByHaving_ShouldSucceed()
    {
        var query = @"select City, Sum(Money) from #A.Entities() group by City having Sum(Money) >= 400 order by City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("katowice", "jan", 300m),
                    new BasicEntity("katowice", "feb", 100m),
                    new BasicEntity("czestochowa", "jan", 400m),
                    new BasicEntity("cracow", "jan", 10m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Sum(Money)", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["czestochowa", 400m], ["katowice", 400m]);
    }

    [TestMethod]
    public void WhenOrderByDescAfterGroupByHaving_ShouldSucceed()
    {
        var query =
            @"select City, Sum(Money) from #A.Entities() group by City having Sum(Money) >= 400 order by City desc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("katowice", "jan", 300m),
                    new BasicEntity("katowice", "feb", 100m),
                    new BasicEntity("czestochowa", "jan", 400m),
                    new BasicEntity("cracow", "jan", 10m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Sum(Money)", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["katowice", 400m], ["czestochowa", 400m]);
    }

    [TestMethod]
    public void WhenOrderByClauseWithOperation_ShouldSucceed()
    {
        const string query = @"select Money from #A.Entities() order by Money * -1";

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

        TableMaterializationTestHelper.AssertColumns(table, ("Money", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [400m], [300m], [100m], [10m], [-10m]);
    }

    [TestMethod]
    public void WhenOrderByClauseWithOperationDesc_ShouldSucceed()
    {
        var query = @"select Money from #A.Entities() order by Money * -1 desc";

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

        TableMaterializationTestHelper.AssertColumns(table, ("Money", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [-10m], [10m], [100m], [300m], [400m]);
    }
}

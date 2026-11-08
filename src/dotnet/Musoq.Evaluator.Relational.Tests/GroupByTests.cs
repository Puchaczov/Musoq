using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public partial class GroupByTests : BasicEntityTestBase
{

    [TestMethod]
    public void GroupByWithParentSumTest()
    {
        var query = @"select SumIncome(Money, 1), SumOutcome(Money, 1) from #A.Entities() group by Month, City";
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("SumIncome(Money, 1)", typeof(decimal?)), ("SumOutcome(Money, 1)", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            [700m, -200m], [700m, -200m], [700m, -200m]);
    }

    [TestMethod]
    public void GroupBySubtractGroupsTest()
    {
        var query =
            @"select SumIncome(Money), SumOutcome(Money), SumIncome(Money) - Abs(SumOutcome(Money)) from #A.Entities() group by Month";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("jan", 400m), new BasicEntity("jan", 300m),
                    new BasicEntity("jan", -200m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("SumIncome(Money)", typeof(decimal?)), ("SumOutcome(Money)", typeof(decimal?)),
            ("SumIncome(Money) - Abs(SumOutcome(Money))", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [700m, -200m, 500m]);
    }

    [TestMethod]
    public void SimpleGroupByTest()
    {
        var query = @"select Name, Count(Name) from #A.Entities() group by Name having Count(Name) >= 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("ABBA"),
                    new BasicEntity("ABBA"),
                    new BasicEntity("BABBA"),
                    new BasicEntity("ABBA"),
                    new BasicEntity("BABBA"),
                    new BasicEntity("CECCA"),
                    new BasicEntity("ABBA")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Count(Name)", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["ABBA", 4L], ["BABBA", 2L]);
    }



    [TestMethod]
    public void SimpleGroupByWithSkipTest()
    {
        var query = @"select Name, Count(Name) from #A.Entities() group by Name skip 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("ABBA"),
                    new BasicEntity("ABBA"),
                    new BasicEntity("BABBA"),
                    new BasicEntity("ABBA"),
                    new BasicEntity("BABBA"),
                    new BasicEntity("CECCA"),
                    new BasicEntity("ABBA")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Count(Name)", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["CECCA", 1L]);
    }

    [TestMethod]
    public void SimpleGroupByWithTakeTest()
    {
        var query = @"select Name, Count(Name) from #A.Entities() group by Name take 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("ABBA"),
                    new BasicEntity("ABBA"),
                    new BasicEntity("BABBA"),
                    new BasicEntity("ABBA"),
                    new BasicEntity("BABBA"),
                    new BasicEntity("CECCA"),
                    new BasicEntity("ABBA")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Count(Name)", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["ABBA", 4L], ["BABBA", 2L]);
    }

    [TestMethod]
    public void SimpleGroupByWithSkipTakeTest()
    {
        var query = @"select Name, Count(Name) from #A.Entities() group by Name skip 2 take 1";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("ABBA"),
                    new BasicEntity("ABBA"),
                    new BasicEntity("BABBA"),
                    new BasicEntity("ABBA"),
                    new BasicEntity("BABBA"),
                    new BasicEntity("CECCA"),
                    new BasicEntity("ABBA")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Count(Name)", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["CECCA", 1L]);
    }

    [TestMethod]
    public void GroupByWithValueTest()
    {
        var query = @"select Country, Sum(Population) from #A.Entities() group by Country";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("ABBA", 200),
                    new BasicEntity("ABBA", 500),
                    new BasicEntity("BABBA", 100),
                    new BasicEntity("ABBA", 10),
                    new BasicEntity("BABBA", 100),
                    new BasicEntity("CECCA", 1000)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)), ("Sum(Population)", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["ABBA", 710m], ["BABBA", 200m], ["CECCA", 1000m]);
    }

    [TestMethod]
    public void GroupByMultipleColumnsTest()
    {
        var query = @"select Country, City, Count(Country), Count(City) from #A.Entities() group by Country, City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("POLAND", "WARSAW"),
                    new BasicEntity("POLAND", "CZESTOCHOWA"),
                    new BasicEntity("UK", "LONDON"),
                    new BasicEntity("POLAND", "CZESTOCHOWA"),
                    new BasicEntity("UK", "MANCHESTER"),
                    new BasicEntity("ANGOLA", "LLL")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)), ("City", typeof(string)),
            ("Count(Country)", typeof(long)), ("Count(City)", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["POLAND", "WARSAW", 1L, 1L], ["POLAND", "CZESTOCHOWA", 2L, 2L],
            ["UK", "LONDON", 1L, 1L], ["UK", "MANCHESTER", 1L, 1L], ["ANGOLA", "LLL", 1L, 1L]);
    }





    [TestMethod]
    public void GroupByForFakeWindowTest()
    {
        var query =
            "select Window(Population) from #A.Entities() group by 'fake'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("KATOWICE", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Window(Population)", typeof(IEnumerable<decimal>)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [new decimal[] { 500, 400, 250, 250, 350 }]);
    }

    [TestMethod]
    public void GroupByForCountriesWideWindowTest()
    {
        var query =
            "select Window(Population) from #A.Entities() group by Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("KATOWICE", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Window(Population)", typeof(IEnumerable<decimal>)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            [new decimal[] { 500, 400, 250 }], [new decimal[] { 250, 350 }]);
    }

    [TestMethod]
    public void GroupByWithWhereTest()
    {
        var query =
            "select Country, City as 'City', Count(City, 1), Count(City) as 'CountOfCities' from #A.Entities() where Country = 'POLAND' group by Country, City";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("KATOWICE", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)), ("City", typeof(string)),
            ("Count(City, 1)", typeof(long)), ("CountOfCities", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["POLAND", "WARSAW", 3L, 1L], ["POLAND", "CZESTOCHOWA", 3L, 1L],
            ["POLAND", "KATOWICE", 3L, 1L]);
    }

    [TestMethod]
    public void ReorderedGroupByWithWhereAndSkipTakeTest()
    {
        var query =
            "from #A.Entities() where Country = 'POLAND' group by Country, City select Country, City as 'City', Count(City, 1), Count(City) as 'CountOfCities' skip 1 take 1";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("KATOWICE", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)), ("City", typeof(string)),
            ("Count(City, 1)", typeof(long)), ("CountOfCities", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["POLAND", "CZESTOCHOWA", 3L, 1L]);
    }

    [TestMethod]
    public void GroupWasNotListedTest()
    {
        var query = "select Count(Country) from #A.entities() group by Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("KATOWICE", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Count(Country)", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [3L], [2L]);
    }
}

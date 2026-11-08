using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class GroupByTests
{

    [TestMethod]
    public void SimpleRowNumberForGroupByTest()
    {
        var query = @"select Name, Count(Name), RowNumber() from #A.Entities() group by Name";
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)), ("Count(Name)", typeof(long)), ("RowNumber()", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["ABBA", 4L, 1], ["BABBA", 2L, 2], ["CECCA", 1L, 3]);
    }

    [TestMethod]
    public void GroupByWithParentCountTest()
    {
        var query =
            "select Country, City as 'City', Count(City, 1), Count(City) as 'CountOfCities' from #A.Entities() group by Country, City";

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
            ["POLAND", "KATOWICE", 3L, 1L], ["GERMANY", "BERLIN", 2L, 1L],
            ["GERMANY", "MUNICH", 2L, 1L]);
    }

    [TestMethod]
    public void CountWithFakeGroupByTest()
    {
        var query = "select Count(Country) from #A.entities() group by 'fake'";

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
        TableMaterializationTestHelper.AssertRowsInOrder(table, [5L]);
    }

    [TestMethod]
    public void CountWithoutGroupByTest()
    {
        var query = "select Count(Country), Sum(Population) from #A.entities()";

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
            ("Count(Country)", typeof(long)), ("Sum(Population)", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [5L, 1750m]);
    }

    [TestMethod]
    public void CountWithRowNumberAndWithoutGroupByTest()
    {
        var query = "select Count(Country), RowNumber() from #A.entities()";

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
            ("Count(Country)", typeof(long)), ("RowNumber()", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [5L, 1]);
    }

    [TestMethod]
    public void SumWithoutGroupByAndWithNoGroupingField()
    {
        var query = "select Sum(Population) from #A.entities()";

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

        TableMaterializationTestHelper.AssertColumns(table, ("Sum(Population)", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1750m]);
    }

}

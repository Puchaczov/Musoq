using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public partial class GroupByTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void GroupByWithParentSumTest()
    {
        var query = @"select SumIncome(Money, 1), SumOutcome(Money, 1) from #A.Entities() group by Month, City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(-200))
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual(Convert.ToDecimal(700), table[0].Values[0]);
        Assert.AreEqual(Convert.ToDecimal(-200), table[0].Values[1]);
        Assert.AreEqual(Convert.ToDecimal(700), table[1].Values[0]);
        Assert.AreEqual(Convert.ToDecimal(-200), table[1].Values[1]);
        Assert.AreEqual(Convert.ToDecimal(700), table[2].Values[0]);
        Assert.AreEqual(Convert.ToDecimal(-200), table[2].Values[1]);
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
                    new BasicEntity("jan", Convert.ToDecimal(400)), new BasicEntity("jan", Convert.ToDecimal(300)),
                    new BasicEntity("jan", Convert.ToDecimal(-200))
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(Convert.ToDecimal(700), table[0].Values[0]);
        Assert.AreEqual(Convert.ToDecimal(-200), table[0].Values[1]);
        Assert.AreEqual(Convert.ToDecimal(500), table[0].Values[2]);
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

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("Count(Name)", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(long), table.Columns.ElementAt(1).ColumnType);

        Assert.AreEqual(2, table.Count, "Table should contain 2 rows");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "ABBA" &&
                (long)row.Values[1] == 4L),
            "Missing ABBA/4");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "BABBA" &&
                (long)row.Values[1] == 2L),
            "Missing BABBA/2");
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

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("Count(Name)", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(long), table.Columns.ElementAt(1).ColumnType);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("CECCA", table[0].Values[0]);
        Assert.AreEqual(1L, table[0].Values[1]);
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

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("Count(Name)", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(long), table.Columns.ElementAt(1).ColumnType);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("ABBA", table[0].Values[0]);
        Assert.AreEqual(4L, table[0].Values[1]);
        Assert.AreEqual("BABBA", table[1].Values[0]);
        Assert.AreEqual(2L, table[1].Values[1]);
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

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("Count(Name)", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(long), table.Columns.ElementAt(1).ColumnType);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("CECCA", table[0].Values[0]);
        Assert.AreEqual(1L, table[0].Values[1]);
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

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("Country", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("Sum(Population)", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(decimal?), table.Columns.ElementAt(1).ColumnType);

        Assert.AreEqual(3, table.Count, "Table should have 3 entries");

        Assert.IsTrue(table.Any(entry =>
            (string)entry.Values[0] == "ABBA" &&
            (decimal)entry.Values[1] == Convert.ToDecimal(710)
        ), "First entry should be 'ABBA' with value 710");

        Assert.IsTrue(table.Any(entry =>
            (string)entry.Values[0] == "BABBA" &&
            (decimal)entry.Values[1] == Convert.ToDecimal(200)
        ), "Second entry should be 'BABBA' with value 200");

        Assert.IsTrue(table.Any(entry =>
            (string)entry.Values[0] == "CECCA" &&
            (decimal)entry.Values[1] == Convert.ToDecimal(1000)
        ), "Third entry should be 'CECCA' with value 1000");
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

        Assert.AreEqual(4, table.Columns.Count());
        Assert.AreEqual("Country", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("City", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual("Count(Country)", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(long), table.Columns.ElementAt(2).ColumnType);
        Assert.AreEqual("Count(City)", table.Columns.ElementAt(3).ColumnName);
        Assert.AreEqual(typeof(long), table.Columns.ElementAt(3).ColumnType);

        Assert.AreEqual(5, table.Count, "Table should contain 5 rows");

        Assert.AreEqual(2,
            table.Count(row =>
                (string)row.Values[0] == "POLAND" &&
                new[] { "WARSAW", "CZESTOCHOWA" }.Contains((string)row.Values[1]) &&
                (((long)row.Values[2] == 1L && (long)row.Values[3] == 1L) ||
                 ((long)row.Values[2] == 2L && (long)row.Values[3] == 2L))), "Expected data for Polish cities not found");

        Assert.AreEqual(2,
            table.Count(row =>
                (string)row.Values[0] == "UK" &&
                new[] { "LONDON", "MANCHESTER" }.Contains((string)row.Values[1]) &&
                (long)row.Values[2] == 1L && (long)row.Values[3] == 1L), "Expected data for UK cities not found");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "ANGOLA" &&
                (string)row.Values[1] == "LLL" &&
                (long)row.Values[2] == 1L && (long)row.Values[3] == 1L),
            "Expected data for Angola not found");
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

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Window(Population)", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(IEnumerable<decimal>), table.Columns.ElementAt(0).ColumnType);

        var window = ((IEnumerable<decimal>)table[0][0]).ToArray();

        Assert.AreEqual(5, window.Length);
        Assert.AreEqual(500, window.ElementAt(0));
        Assert.AreEqual(400, window.ElementAt(1));
        Assert.AreEqual(250, window.ElementAt(2));
        Assert.AreEqual(250, window.ElementAt(3));
        Assert.AreEqual(350, window.ElementAt(4));
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

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Window(Population)", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(IEnumerable<decimal>), table.Columns.ElementAt(0).ColumnType);

        for (var i = 0; i < 2; i++)
        {
            var window = ((IEnumerable<decimal>)table[i][0]).ToArray();

            if (window.Length == 3)
            {
                Assert.AreEqual(500, window.ElementAt(0));
                Assert.AreEqual(400, window.ElementAt(1));
                Assert.AreEqual(250, window.ElementAt(2));
            }
            else
            {
                Assert.HasCount(2, window);
                Assert.AreEqual(250, window.ElementAt(0));
                Assert.AreEqual(350, window.ElementAt(1));
            }
        }
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

        Assert.AreEqual(4, table.Columns.Count());
        Assert.AreEqual("Country", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("City", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual("Count(City, 1)", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(long), table.Columns.ElementAt(2).ColumnType);
        Assert.AreEqual("CountOfCities", table.Columns.ElementAt(3).ColumnName);
        Assert.AreEqual(typeof(long), table.Columns.ElementAt(3).ColumnType);

        Assert.AreEqual(3, table.Count, "Table should have 3 entries");

        Assert.IsTrue(table.Any(entry =>
                (string)entry.Values[0] == "POLAND" &&
                (string)entry.Values[1] == "WARSAW" &&
                Convert.ToInt32(entry.Values[2]) == 3 &&
                Convert.ToInt32(entry.Values[3]) == 1),
            "Entry for POLAND - WARSAW should match expected values");

        Assert.IsTrue(table.Any(entry =>
                (string)entry.Values[0] == "POLAND" &&
                (string)entry.Values[1] == "CZESTOCHOWA" &&
                Convert.ToInt32(entry.Values[2]) == 3 &&
                Convert.ToInt32(entry.Values[3]) == 1),
            "Entry for POLAND - CZESTOCHOWA should match expected values");

        Assert.IsTrue(table.Any(entry =>
                (string)entry.Values[0] == "POLAND" &&
                (string)entry.Values[1] == "KATOWICE" &&
                Convert.ToInt32(entry.Values[2]) == 3 &&
                Convert.ToInt32(entry.Values[3]) == 1),
            "Entry for POLAND - KATOWICE should match expected values");
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

        Assert.AreEqual(4, table.Columns.Count());
        Assert.AreEqual("Country", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("City", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual("Count(City, 1)", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(long), table.Columns.ElementAt(2).ColumnType);
        Assert.AreEqual("CountOfCities", table.Columns.ElementAt(3).ColumnName);
        Assert.AreEqual(typeof(long), table.Columns.ElementAt(3).ColumnType);

        Assert.AreEqual("POLAND", table[0].Values[0]);
        Assert.AreEqual("CZESTOCHOWA", table[0].Values[1]);
        Assert.AreEqual(3L, table[0].Values[2]);
        Assert.AreEqual(1L, table[0].Values[3]);
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

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Count(Country)", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(long), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(2, table.Count, "Table should contain 2 rows");

        Assert.IsTrue(table.Any(row => (long)row.Values[0] == 3L), "Missing value 3");
        Assert.IsTrue(table.Any(row => (long)row.Values[0] == 2L), "Missing value 2");
    }
}

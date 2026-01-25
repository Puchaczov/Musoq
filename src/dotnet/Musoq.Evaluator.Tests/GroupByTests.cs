using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class GroupByTests : BasicEntityTestBase
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
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(1).ColumnType);

        Assert.AreEqual(2, table.Count, "Table should contain 2 rows");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "ABBA" &&
                (int)row.Values[1] == 4),
            "Missing ABBA/4");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "BABBA" &&
                (int)row.Values[1] == 2),
            "Missing BABBA/2");
    }


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

        Assert.AreEqual(3, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("Count(Name)", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual("RowNumber()", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(2).ColumnType);

        Assert.AreEqual(3, table.Count, "Result should contain exactly 3 rows");

        int[] rowNumbers = [1, 2, 3];

        Assert.IsTrue(table.Any(row =>
            (string)row.Values[0] == "ABBA" &&
            (int)row.Values[1] == 4 &&
            rowNumbers.Contains((int)row.Values[2])
        ), "Expected combination (ABBA, 4, 1) not found");

        Assert.IsTrue(table.Any(row =>
            (string)row.Values[0] == "BABBA" &&
            (int)row.Values[1] == 2 &&
            rowNumbers.Contains((int)row.Values[2])
        ), "Expected combination (BABBA, 2, 2) not found");

        Assert.IsTrue(table.Any(row =>
            (string)row.Values[0] == "CECCA" &&
            (int)row.Values[1] == 1 &&
            rowNumbers.Contains((int)row.Values[2])
        ), "Expected combination (CECCA, 1, 3) not found");

        var rowNumbersSet = new HashSet<int>(table.Select(row => (int)row.Values[2]));

        Assert.HasCount(3, rowNumbersSet, "Row numbers should be unique");
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
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(1).ColumnType);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("CECCA", table[0].Values[0]);
        Assert.AreEqual(Convert.ToInt32(1), table[0].Values[1]);
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
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(1).ColumnType);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("ABBA", table[0].Values[0]);
        Assert.AreEqual(Convert.ToInt32(4), table[0].Values[1]);
        Assert.AreEqual("BABBA", table[1].Values[0]);
        Assert.AreEqual(Convert.ToInt32(2), table[1].Values[1]);
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
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(1).ColumnType);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("CECCA", table[0].Values[0]);
        Assert.AreEqual(Convert.ToInt32(1), table[0].Values[1]);
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
        Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(1).ColumnType);

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
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(2).ColumnType);
        Assert.AreEqual("Count(City)", table.Columns.ElementAt(3).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(3).ColumnType);

        Assert.AreEqual(5, table.Count, "Table should contain 5 rows");

        Assert.AreEqual(2,
            table.Count(row =>
                (string)row.Values[0] == "POLAND" &&
                new[] { "WARSAW", "CZESTOCHOWA" }.Contains((string)row.Values[1]) &&
                (((int)row.Values[2] == 1 && (int)row.Values[3] == 1) ||
                 ((int)row.Values[2] == 2 && (int)row.Values[3] == 2))), "Expected data for Polish cities not found");

        Assert.AreEqual(2,
            table.Count(row =>
                (string)row.Values[0] == "UK" &&
                new[] { "LONDON", "MANCHESTER" }.Contains((string)row.Values[1]) &&
                (int)row.Values[2] == 1 && (int)row.Values[3] == 1), "Expected data for UK cities not found");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "ANGOLA" &&
                (string)row.Values[1] == "LLL" &&
                (int)row.Values[2] == 1 && (int)row.Values[3] == 1),
            "Expected data for Angola not found");
    }

    [TestMethod]
    public void GroupBySubstrTest()
    {
        var query =
            @"select Substring(Name, 0, 2), Count(Substring(Name, 0, 2)) from #A.Entities() group by Substring(Name, 0, 2)";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("AA:1"),
                    new BasicEntity("AA:2"),
                    new BasicEntity("AA:3"),
                    new BasicEntity("BB:1"),
                    new BasicEntity("BB:2"),
                    new BasicEntity("CC:1")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("Substring(Name, 0, 2)", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("Count(Substring(Name, 0, 2))", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(1).ColumnType);

        Assert.AreEqual(3, table.Count, "Table should have 3 entries");

        Assert.IsTrue(table.Any(entry =>
            (string)entry.Values[0] == "AA" &&
            (int)entry.Values[1] == Convert.ToInt32(3)
        ), "First entry should be 'AA' with value 3");

        Assert.IsTrue(table.Any(entry =>
            (string)entry.Values[0] == "BB" &&
            (int)entry.Values[1] == Convert.ToInt32(2)
        ), "Second entry should be 'BB' with value 2");

        Assert.IsTrue(table.Any(entry =>
            (string)entry.Values[0] == "CC" &&
            (int)entry.Values[1] == Convert.ToInt32(1)
        ), "Third entry should be 'CC' with value 1");
    }

    [TestMethod]
    public void GroupByWithSelectedConstantModifiedByFunctionTest()
    {
        var query =
            @"select Name, Count(Name), Inc(10d), 1 from #A.Entities() group by Name having Count(Name) >= 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("ABBA"),
                    new BasicEntity("ABBA"),
                    new BasicEntity("BABBA"),
                    new BasicEntity("ABBA"),
                    new BasicEntity("BABBA"),
                    new BasicEntity("CECCA")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("Count(Name)", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual("Inc(10)", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(2).ColumnType);
        Assert.AreEqual("1", table.Columns.ElementAt(3).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(3).ColumnType);

        Assert.AreEqual(2, table.Count, "Table should contain 2 rows");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "ABBA" &&
                (int)row.Values[1] == 3 &&
                (decimal)row.Values[2] == 11m &&
                (int)row.Values[3] == 1),
            "Expected row for ABBA with values 3, 11, 1");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "BABBA" &&
                (int)row.Values[1] == 2 &&
                (decimal)row.Values[2] == 11m &&
                (int)row.Values[3] == 1),
            "Expected row for BABBA with values 2, 11, 1");
    }

    [TestMethod]
    public void GroupByColumnSubstringTest()
    {
        var query =
            """
            select 
                Country, 
                Substring(City, IndexOf(City, ':')) as 'City', 
                Count(City) as 'Count', 
                Sum(Population) as 'Sum' 
            from #A.Entities() 
            group by Substring(City, IndexOf(City, ':')), Country
            """;

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW:TARGOWEK", "POLAND", 500),
                    new BasicEntity("WARSAW:URSYNOW", "POLAND", 500),
                    new BasicEntity("KATOWICE:ZAWODZIE", "POLAND", 250)
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
        Assert.AreEqual("Count", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(2).ColumnType);
        Assert.AreEqual("Sum", table.Columns.ElementAt(3).ColumnName);
        Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(3).ColumnType);

        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry =>
            (string)entry.Values[0] == "POLAND" &&
            (string)entry.Values[1] == "WARSAW" &&
            (int)entry.Values[2] == Convert.ToInt32(2) &&
            (decimal)entry.Values[3] == Convert.ToDecimal(1000)
        ), "First entry should match POLAND, WARSAW, 2, 1000");

        Assert.IsTrue(table.Any(entry =>
            (string)entry.Values[0] == "POLAND" &&
            (string)entry.Values[1] == "KATOWICE" &&
            (int)entry.Values[2] == Convert.ToInt32(1) &&
            (decimal)entry.Values[3] == Convert.ToDecimal(250)
        ), "Second entry should match POLAND, KATOWICE, 1, 250");
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

        Assert.AreEqual(4, table.Columns.Count());
        Assert.AreEqual("Country", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("City", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual("Count(City, 1)", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(2).ColumnType);
        Assert.AreEqual("CountOfCities", table.Columns.ElementAt(3).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(3).ColumnType);

        Assert.AreEqual(5, table.Count, "Table should have 5 entries");

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

        Assert.IsTrue(table.Any(entry =>
                (string)entry.Values[0] == "GERMANY" &&
                (string)entry.Values[1] == "BERLIN" &&
                Convert.ToInt32(entry.Values[2]) == 2 &&
                Convert.ToInt32(entry.Values[3]) == 1),
            "Entry for GERMANY - BERLIN should match expected values");

        Assert.IsTrue(table.Any(entry =>
                (string)entry.Values[0] == "GERMANY" &&
                (string)entry.Values[1] == "MUNICH" &&
                Convert.ToInt32(entry.Values[2]) == 2 &&
                Convert.ToInt32(entry.Values[3]) == 1),
            "Entry for GERMANY - MUNICH should match expected values");
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

        var window = (IEnumerable<decimal>)table[0][0];

        Assert.AreEqual(5, window.Count());
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
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(2).ColumnType);
        Assert.AreEqual("CountOfCities", table.Columns.ElementAt(3).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(3).ColumnType);

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
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(2).ColumnType);
        Assert.AreEqual("CountOfCities", table.Columns.ElementAt(3).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(3).ColumnType);

        Assert.AreEqual("POLAND", table[0].Values[0]);
        Assert.AreEqual("CZESTOCHOWA", table[0].Values[1]);
        Assert.AreEqual(Convert.ToInt32(3), table[0].Values[2]);
        Assert.AreEqual(Convert.ToInt32(1), table[0].Values[3]);
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
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(2, table.Count, "Table should contain 2 rows");

        Assert.IsTrue(table.Any(row => (int)row.Values[0] == 3), "Missing value 3");
        Assert.IsTrue(table.Any(row => (int)row.Values[0] == 2), "Missing value 2");
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

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Count(Country)", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(5, table[0].Values[0]);
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

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("Count(Country)", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("Sum(Population)", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(1).ColumnType);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(5, table[0].Values[0]);
        Assert.AreEqual(Convert.ToDecimal(1750), table[0].Values[1]);
    }

    /// <summary>
    ///     This is actually coherent with Postgres behavior.
    ///     -- create
    ///     CREATE TABLE Cities (
    ///     City TEXT NOT NULL,
    ///     Country TEXT NOT NULL,
    ///     Population INT NOT NULL
    ///     );
    ///     -- insert
    ///     INSERT INTO Cities VALUES ('WARSAW', 'POLAND', 500);
    ///     INSERT INTO Cities VALUES ('CZESTOCHOWA', 'POLAND', 400);
    ///     INSERT INTO Cities VALUES ('KATOWICE', 'POLAND', 250);
    ///     INSERT INTO Cities VALUES ('BERLIN', 'GERMANY', 250);
    ///     INSERT INTO Cities VALUES ('MUNICH', 'GERMANY', 350);
    ///     -- fetch
    ///     SELECT Count(Country), Row_Number() over () FROM Cities;
    /// </summary>
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

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("Count(Country)", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("RowNumber()", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(1).ColumnType);

        Assert.AreEqual(1, table.Count);

        Assert.AreEqual(5, table[0].Values[0]);
        Assert.AreEqual(1, table[0].Values[1]);
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

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Sum(Population)", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(1, table.Count);

        Assert.AreEqual(1750m, table[0].Values[0]);
    }

    [TestMethod]
    public void GroupBySimpleAccessTest()
    {
        var query = @"select Month from #A.Entities() group by Month";
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

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("jan", table[0].Values[0]);
    }

    [TestMethod]
    public void GroupByComplexObjectAccessTest()
    {
        var query = @"select Self.Month from #A.Entities() group by Self.Month";
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

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("jan", table[0].Values[0]);
    }

    [TestMethod]
    public void GroupByComplexObjectAccessWithSumTest()
    {
        var query = @"select Self.Month, Sum(Self.Money) from #A.Entities() group by Self.Month";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(-200)),
                    new BasicEntity("cracow", "feb", Convert.ToDecimal(100))
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry =>
                (string)entry.Values[0] == "jan" &&
                (decimal)entry.Values[1] == 500m),
            "First entry should have values 'jan' and 500m");

        Assert.IsTrue(table.Any(entry =>
                (string)entry.Values[0] == "feb" &&
                (decimal)entry.Values[1] == 100m),
            "Second entry should have values 'feb' and 100m");
    }

    [TestMethod]
    public void GroupByWithCaseWhenInSelectTest()
    {
        var query =
            @"select (case when Self.Month = 'jan' then 'JANUARY' when Self.Month = 'feb' then 'FEBRUARY' else 'NONE' end) from #A.Entities() group by Self.Month";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(-200)),
                    new BasicEntity("cracow", "feb", Convert.ToDecimal(100)),
                    new BasicEntity("cracow", "march", Convert.ToDecimal(100))
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count, "Table should contain 3 rows");

        Assert.IsTrue(table.All(row =>
                new[] { "JANUARY", "FEBRUARY", "NONE" }.Contains((string)row[0])),
            "Expected rows with values: JANUARY, FEBRUARY, NONE in order");
    }

    [TestMethod]
    public void GroupByWithCaseWhenAsGroupingResultFunctionTest()
    {
        var query =
            @"select (case when e.Month = e.Month then e.Month else '' end), Count(case when e.Month = e.Month then e.Month else '' end) from #A.Entities() e group by (case when e.Month = e.Month then e.Month else '' end)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(-200)),
                    new BasicEntity("cracow", "feb", Convert.ToDecimal(100)),
                    new BasicEntity("cracow", "march", Convert.ToDecimal(100))
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsTrue(table.Any(entry =>
                (string)entry[0] == "jan"),
            "First entry should be 'jan'");

        Assert.IsTrue(table.Any(entry =>
                (string)entry[0] == "feb"),
            "Second entry should be 'feb'");

        Assert.IsTrue(table.Any(entry =>
                (string)entry[0] == "march"),
            "Third entry should be 'march'");
    }

    [TestMethod]
    public void GroupByWithFieldLinkSyntaxTest()
    {
        var query =
            @"select ::1, Count(::1), ::2 from #A.Entities() e group by (case when e.Month = e.Month then e.Month else '' end), 'fake'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(-200)),
                    new BasicEntity("cracow", "feb", Convert.ToDecimal(100)),
                    new BasicEntity("cracow", "march", Convert.ToDecimal(100))
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        var column = table.Columns.ElementAt(0);
        Assert.AreEqual("::1", column.ColumnName);
        Assert.AreEqual(typeof(string), column.ColumnType);

        column = table.Columns.ElementAt(1);
        Assert.AreEqual("Count(::1)", column.ColumnName);
        Assert.AreEqual(typeof(int), column.ColumnType);

        column = table.Columns.ElementAt(2);
        Assert.AreEqual("::2", column.ColumnName);
        Assert.AreEqual(typeof(string), column.ColumnType);

        Assert.AreEqual(3, table.Count, "Table should contain 3 rows");

        Assert.IsTrue(table.Any(row => (string)row[0] == "jan"), "Missing jan row");
        Assert.IsTrue(table.Any(row => (string)row[0] == "feb"), "Missing feb row");
        Assert.IsTrue(table.Any(row => (string)row[0] == "march"), "Missing march row");
    }

    [TestMethod]
    public void GroupByWithFieldLinkSyntaxAndCustomColumnNamingTest()
    {
        var query =
            @"select ::1 as firstColumn, Count(::1) as secondColumn, ::2 as thirdColumn from #A.Entities() e group by (case when e.Month = e.Month then e.Month else '' end), 'fake'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(-200)),
                    new BasicEntity("cracow", "feb", Convert.ToDecimal(100)),
                    new BasicEntity("cracow", "march", Convert.ToDecimal(100))
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        var column = table.Columns.ElementAt(0);
        Assert.AreEqual("firstColumn", column.ColumnName);
        Assert.AreEqual(typeof(string), column.ColumnType);

        column = table.Columns.ElementAt(1);
        Assert.AreEqual("secondColumn", column.ColumnName);
        Assert.AreEqual(typeof(int), column.ColumnType);

        column = table.Columns.ElementAt(2);
        Assert.AreEqual("thirdColumn", column.ColumnName);
        Assert.AreEqual(typeof(string), column.ColumnType);

        Assert.IsTrue(table.Any(entry => (string)entry[0] == "jan"), "First row should be 'jan'");
        Assert.IsTrue(table.Any(entry => (string)entry[0] == "feb"), "Second row should be 'feb'");
        Assert.IsTrue(table.Any(entry => (string)entry[0] == "march"), "Third row should be 'march'");
    }

    [TestMethod]
    public void WhenGroupByUsedWithJoinsByMethodInvocation_ShouldRetrieveValues()
    {
        var query =
            @"
select 
    countries.GetCountry() as Country,
    population.Sum(population.GetPopulation()) as Population
from #A.entities() countries 
inner join #B.entities() cities on countries.GetCountry() = cities.GetCountry() 
inner join #C.entities() population on cities.GetCity() = population.GetCity()
group by countries.GetCountry()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Country = "Poland" },
                    new BasicEntity { Country = "Germany" }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Country = "Poland", City = "Krakow" },
                    new BasicEntity { Country = "Poland", City = "Wroclaw" },
                    new BasicEntity { Country = "Poland", City = "Warszawa" },
                    new BasicEntity { Country = "Poland", City = "Gdansk" },
                    new BasicEntity { Country = "Germany", City = "Berlin" }
                ]
            },
            {
                "#C", [
                    new BasicEntity { City = "Krakow", Population = 400 },
                    new BasicEntity { City = "Wroclaw", Population = 500 },
                    new BasicEntity { City = "Warszawa", Population = 1000 },
                    new BasicEntity { City = "Gdansk", Population = 200 },
                    new BasicEntity { City = "Berlin", Population = 400 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry =>
            (string)entry[0] == "Poland" &&
            (decimal)entry[1] == 2100m
        ), "First entry should be Poland with 2100");

        Assert.IsTrue(table.Any(entry =>
            (string)entry[0] == "Germany" &&
            (decimal)entry[1] == 400m
        ), "Second entry should be Germany with 400");
    }

    [TestMethod]
    public void WhenGroupByWithWhereUsed_WhereUsesFieldThatWillBeUsedInResultingTable_ShouldSuccess()
    {
        var query = @"
select 
    a.Country,
    b.AggregateValues(b.City)
from #A.entities() a     
inner join #B.entities() b on a.Country = b.Country
where a.Country = 'Poland'
group by a.Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Country = "Poland" }
                ]
            },
            {
                "#B", [
                    new BasicEntity { City = "Warsaw", Country = "Poland" },
                    new BasicEntity { City = "Gdansk", Country = "Poland" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual("Poland", table[0][0]);
        Assert.AreEqual("Warsaw,Gdansk", table[0][1]);
    }

    [TestMethod]
    public void WhenGroupByWithWhereUsed_WhereUsesFieldThatWontBeUsedInResultingTable_ShouldSuccess()
    {
        var query = @"
select 
    a.Country,
    b.AggregateValues(b.City)
from #A.entities() a     
inner join #B.entities() b on a.Country = b.Country
where b.Population > 200
group by a.Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Country = "Poland" }
                ]
            },
            {
                "#B", [
                    new BasicEntity { City = "Warsaw", Country = "Poland", Population = 200 },
                    new BasicEntity { City = "Gdansk", Country = "Poland", Population = 300 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual("Poland", table[0][0]);
        Assert.AreEqual("Gdansk", table[0][1]);
    }

    [Ignore("WORK IN PROGRESS")]
    [TestMethod]
    public void WhenAccessingTheFirstLetterWithMethodCallInsideAggregation_ShouldSucceed()
    {
        var query = @"
select 
    a.Country,
    AggregateValues(GetElementAt(a.Country, 0))
from #A.entities() a
group by a.Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Country = "Poland" },
                    new BasicEntity { Country = "Brazil" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry =>
            (string)entry[0] == "Poland" &&
            (string)entry[1] == "P"
        ), "First entry should be Poland with 'P'");

        Assert.IsTrue(table.Any(entry =>
            (string)entry[0] == "Brazil" &&
            (string)entry[1] == "B"
        ), "Second entry should be Brazil with 'B'");
    }

    [Ignore("WORK IN PROGRESS")]
    [TestMethod]
    public void WhenAccessingTheFirstLetterWithIndexerInsideAggregation_ShouldSucceed()
    {
        var query = @"
select 
    a.Country,
    AggregateValues(a.Country[0])
from #A.entities() a
group by a.Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Country = "Poland" },
                    new BasicEntity { Country = "Brazil" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry =>
            (string)entry[0] == "Poland" &&
            (string)entry[1] == "P"
        ), "First entry should be Poland with 'P'");

        Assert.IsTrue(table.Any(entry =>
            (string)entry[0] == "Brazil" &&
            (string)entry[1] == "B"
        ), "Second entry should be Brazil with 'B'");
    }
}

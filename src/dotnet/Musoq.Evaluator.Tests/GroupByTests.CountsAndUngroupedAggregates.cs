using System;
using System.Collections.Generic;
using System.Linq;
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

        Assert.AreEqual(3, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("Count(Name)", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(long), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual("RowNumber()", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(2).ColumnType);

        Assert.AreEqual(3, table.Count, "Result should contain exactly 3 rows");

        int[] rowNumbers = [1, 2, 3];

        Assert.IsTrue(table.Any(row =>
            (string)row.Values[0] == "ABBA" &&
            (long)row.Values[1] == 4L &&
            rowNumbers.Contains((int)row.Values[2])
        ), "Expected combination (ABBA, 4, 1) not found");

        Assert.IsTrue(table.Any(row =>
            (string)row.Values[0] == "BABBA" &&
            (long)row.Values[1] == 2L &&
            rowNumbers.Contains((int)row.Values[2])
        ), "Expected combination (BABBA, 2, 2) not found");

        Assert.IsTrue(table.Any(row =>
            (string)row.Values[0] == "CECCA" &&
            (long)row.Values[1] == 1L &&
            rowNumbers.Contains((int)row.Values[2])
        ), "Expected combination (CECCA, 1, 3) not found");

        var rowNumbersSet = new HashSet<int>(table.Select(row => (int)row.Values[2]));

        Assert.HasCount(3, rowNumbersSet, "Row numbers should be unique");
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
        Assert.AreEqual(typeof(long), table.Columns.ElementAt(2).ColumnType);
        Assert.AreEqual("CountOfCities", table.Columns.ElementAt(3).ColumnName);
        Assert.AreEqual(typeof(long), table.Columns.ElementAt(3).ColumnType);

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
        Assert.AreEqual(typeof(long), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(5L, table[0].Values[0]);
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
        Assert.AreEqual(typeof(long), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("Sum(Population)", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(decimal?), table.Columns.ElementAt(1).ColumnType);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(5L, table[0].Values[0]);
        Assert.AreEqual(Convert.ToDecimal(1750), table[0].Values[1]);
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

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("Count(Country)", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(long), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("RowNumber()", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(1).ColumnType);

        Assert.AreEqual(1, table.Count);

        Assert.AreEqual(5L, table[0].Values[0]);
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
        Assert.AreEqual(typeof(decimal?), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(1, table.Count);

        Assert.AreEqual(1750m, table[0].Values[0]);
    }

}

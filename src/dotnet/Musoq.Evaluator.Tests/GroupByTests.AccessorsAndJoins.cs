using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class GroupByTests
{

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
        Assert.AreEqual(typeof(long), table.Columns.ElementAt(1).ColumnType);

        Assert.AreEqual(3, table.Count, "Table should have 3 entries");

        Assert.IsTrue(table.Any(entry =>
            (string)entry.Values[0] == "AA" &&
            (long)entry.Values[1] == 3L
        ), "First entry should be 'AA' with value 3");

        Assert.IsTrue(table.Any(entry =>
            (string)entry.Values[0] == "BB" &&
            (long)entry.Values[1] == 2L
        ), "Second entry should be 'BB' with value 2");

        Assert.IsTrue(table.Any(entry =>
            (string)entry.Values[0] == "CC" &&
            (long)entry.Values[1] == 1L
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
        Assert.AreEqual(typeof(long), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual("Inc(10)", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(2).ColumnType);
        Assert.AreEqual("1", table.Columns.ElementAt(3).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(3).ColumnType);

        Assert.AreEqual(2, table.Count, "Table should contain 2 rows");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "ABBA" &&
                (long)row.Values[1] == 3L &&
                (decimal)row.Values[2] == 11m &&
                (int)row.Values[3] == 1),
            "Expected row for ABBA with values 3, 11, 1");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "BABBA" &&
                (long)row.Values[1] == 2L &&
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
        Assert.AreEqual(typeof(long), table.Columns.ElementAt(2).ColumnType);
        Assert.AreEqual("Sum", table.Columns.ElementAt(3).ColumnName);
        Assert.AreEqual(typeof(decimal?), table.Columns.ElementAt(3).ColumnType);

        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry =>
            (string)entry.Values[0] == "POLAND" &&
            (string)entry.Values[1] == "WARSAW" &&
            (long)entry.Values[2] == 2L &&
            (decimal)entry.Values[3] == Convert.ToDecimal(1000)
        ), "First entry should match POLAND, WARSAW, 2, 1000");

        Assert.IsTrue(table.Any(entry =>
            (string)entry.Values[0] == "POLAND" &&
            (string)entry.Values[1] == "KATOWICE" &&
            (long)entry.Values[2] == 1L &&
            (decimal)entry.Values[3] == Convert.ToDecimal(250)
        ), "Second entry should match POLAND, KATOWICE, 1, 250");
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
    public void GroupByWithExplicitGroupedExpressionTest()
    {
        var query =
            @"select (case when e.Month = e.Month then e.Month else '' end) as monthKey, Count((case when e.Month = e.Month then e.Month else '' end)) as monthCount, 'fake' as constantKey from #A.Entities() e group by (case when e.Month = e.Month then e.Month else '' end), 'fake'";

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
        Assert.AreEqual("monthKey", column.ColumnName);
        Assert.AreEqual(typeof(string), column.ColumnType);

        column = table.Columns.ElementAt(1);
        Assert.AreEqual("monthCount", column.ColumnName);
        Assert.AreEqual(typeof(long), column.ColumnType);

        column = table.Columns.ElementAt(2);
        Assert.AreEqual("constantKey", column.ColumnName);
        Assert.AreEqual(typeof(string), column.ColumnType);

        Assert.AreEqual(3, table.Count, "Table should contain 3 rows");

        Assert.IsTrue(table.Any(row => (string)row[0] == "jan"), "Missing jan row");
        Assert.IsTrue(table.Any(row => (string)row[0] == "feb"), "Missing feb row");
        Assert.IsTrue(table.Any(row => (string)row[0] == "march"), "Missing march row");
    }

    [TestMethod]
    public void GroupByWithExplicitGroupedExpressionAndCustomColumnNamingTest()
    {
        var query =
            @"select (case when e.Month = e.Month then e.Month else '' end) as firstColumn, Count((case when e.Month = e.Month then e.Month else '' end)) as secondColumn, 'fake' as thirdColumn from #A.Entities() e group by (case when e.Month = e.Month then e.Month else '' end), 'fake'";

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
        Assert.AreEqual(typeof(long), column.ColumnType);

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

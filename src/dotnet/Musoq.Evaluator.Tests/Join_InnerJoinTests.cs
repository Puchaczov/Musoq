using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class Join_InnerJoinTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void SimpleJoinShorthandTest()
    {
        const string query = "select a.Id, b.Id from #A.entities() a join #B.entities() b on a.Id = b.Id";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("x") { Id = 1 }, new BasicEntity("y") { Id = 2 }] },
            { "#B", [new BasicEntity("x") { Id = 2 }, new BasicEntity("z") { Id = 3 }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(2, table.Columns.Count());

        Assert.AreEqual(typeof(int), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(1).ColumnType);

        Assert.AreEqual(2, table[0][0]);
        Assert.AreEqual(2, table[0][1]);
    }

    [TestMethod]
    public void SimpleJoinShorthandUppercaseTest()
    {
        const string query = "SELECT A.Id, B.Id FROM #A.entities() A JOIN #B.entities() B ON A.Id = B.Id";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("x") { Id = 1 }, new BasicEntity("y") { Id = 2 }] },
            { "#B", [new BasicEntity("x") { Id = 2 }, new BasicEntity("z") { Id = 3 }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual(2, table[0][0]);
        Assert.AreEqual(2, table[0][1]);
    }

    [TestMethod]
    public void WhenSomeColumnsAreUsedAndNotEveryUsedTableHasUsedOwnColumns_MustNotThrow()
    {
        const string query = @"
select
    countries.Country
from #A.entities() countries
inner join #B.entities() cities on 1 = 1
inner join #C.entities() population on 1 = 1";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [] },
            { "#B", [] },
            { "#C", [] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void SimpleJoinTest()
    {
        var query =
            @"
select
    countries.Country,
    cities.City,
    population.Population
from #A.entities() countries
inner join #B.entities() cities on countries.Country = cities.Country
inner join #C.entities() population on cities.City = population.City";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Poland", "Krakow"),
                    new BasicEntity("Germany", "Berlin")
                ]
            },
            {
                "#B", [
                    new BasicEntity("Poland", "Krakow"),
                    new BasicEntity("Poland", "Wroclaw"),
                    new BasicEntity("Poland", "Warszawa"),
                    new BasicEntity("Poland", "Gdansk"),
                    new BasicEntity("Germany", "Berlin")
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

        Assert.AreEqual(3, table.Columns.Count());

        Assert.AreEqual("countries.Country", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual(0, table.Columns.ElementAt(0).ColumnIndex);

        Assert.AreEqual("cities.City", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual(1, table.Columns.ElementAt(1).ColumnIndex);

        Assert.AreEqual("population.Population", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(2).ColumnType);
        Assert.AreEqual(2, table.Columns.ElementAt(2).ColumnIndex);

        Assert.AreEqual(5, table.Count, "Table should have 5 entries");

        Assert.IsTrue(table.Any(entry =>
                (string)entry[0] == "Poland" &&
                (string)entry[1] == "Krakow" &&
                (decimal)entry[2] == 400m),
            "Entry for Krakow should match");

        Assert.IsTrue(table.Any(entry =>
                (string)entry[0] == "Poland" &&
                (string)entry[1] == "Wroclaw" &&
                (decimal)entry[2] == 500m),
            "Entry for Wroclaw should match");

        Assert.IsTrue(table.Any(entry =>
                (string)entry[0] == "Poland" &&
                (string)entry[1] == "Warszawa" &&
                (decimal)entry[2] == 1000m),
            "Entry for Warszawa should match");

        Assert.IsTrue(table.Any(entry =>
                (string)entry[0] == "Poland" &&
                (string)entry[1] == "Gdansk" &&
                (decimal)entry[2] == 200m),
            "Entry for Gdansk should match");

        Assert.IsTrue(table.Any(entry =>
                (string)entry[0] == "Germany" &&
                (string)entry[1] == "Berlin" &&
                (decimal)entry[2] == 400m),
            "Entry for Berlin should match");
    }

    [TestMethod]
    public void InnerJoinCteTablesTest()
    {
        var query = @"
with p as (
    select Country, City, Id from #A.entities()
), x as (
    select Country, City, Id from #B.entities()
)
select p.Id, x.Id from p inner join x on p.Country = x.Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Poland", "Krakow") { Id = 0 },
                    new BasicEntity("Germany", "Berlin") { Id = 1 },
                    new BasicEntity("Russia", "Moscow") { Id = 2 }
                ]
            },
            {
                "#B", [
                    new BasicEntity("Poland", "Krakow") { Id = 0 },
                    new BasicEntity("Poland", "Wroclaw") { Id = 1 },
                    new BasicEntity("Poland", "Warszawa") { Id = 2 },
                    new BasicEntity("Poland", "Gdansk") { Id = 3 },
                    new BasicEntity("Germany", "Berlin") { Id = 4 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsTrue(table.Count == 5 && table.Columns.Count() == 2, "Table should contain 5 rows and 2 columns");

        Assert.AreEqual(4, table.Count(row => (int)row[0] == 0), "Expected 4 rows with first column value 0");

        Assert.IsTrue(table.All(row =>
                new[] { (0, 0), (0, 1), (0, 2), (0, 3), (1, 4) }.Contains(((int)row[0], (int)row[1]))),
            "Expected rows with values: (0,0), (0,1), (0,2), (0,3), (1,4)");
    }

    [TestMethod]
    public void InnerJoinCteSelfJoinTest()
    {
        var query = @"
with p as (
    select Country, City, Id from #A.entities()
), x as (
    select Country, City, Id from p
)
select p.Id, x.Id from p p inner join x on p.Country = x.Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Poland", "Krakow") { Id = 0 },
                    new BasicEntity("Germany", "Berlin") { Id = 1 },
                    new BasicEntity("Russia", "Moscow") { Id = 2 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count, "Table should have 3 entries");
        Assert.AreEqual(2, table.Columns.Count(), "Table should have 2 columns");

        Assert.IsTrue(table.Any(entry =>
            (int)entry[0] == 0 &&
            (int)entry[1] == 0), "First entry should be 0, 0");

        Assert.IsTrue(table.Any(entry =>
            (int)entry[0] == 1 &&
            (int)entry[1] == 1), "Second entry should be 1, 1");

        Assert.IsTrue(table.Any(entry =>
            (int)entry[0] == 2 &&
            (int)entry[1] == 2), "Third entry should be 2, 2");
    }

    [TestMethod]
    public void ComplexCteIssue1Test()
    {
        var query = @"
with p as (
	select
        Country
	from #A.entities()
), x as (
	select
		Country
	from p group by Country
)
select p.Country, x.Country from p inner join x on p.Country = x.Country where p.Country = 'Poland'
";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Poland", "Krakow") { Id = 0 },
                    new BasicEntity("Germany", "Berlin") { Id = 1 },
                    new BasicEntity("Russia", "Moscow") { Id = 2 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(2, table.Columns.Count());

        Assert.AreEqual("Poland", table[0][0]);
        Assert.AreEqual("Poland", table[0][1]);
    }

    [TestMethod]
    public void ComplexCteIssue1WithGroupByTest()
    {
        var query = @"
with p as (
	select
        Country
	from #A.entities()
), x as (
	select
		Country
	from p group by Country
)
select p.Country, p.Count(p.Country) from p inner join x on p.Country = x.Country group by p.Country having p.Count(p.Country) > 1
";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Poland", "Krakow") { Id = 0 },
                    new BasicEntity("Poland", "Krakow") { Id = 0 },
                    new BasicEntity("Germany", "Berlin") { Id = 1 },
                    new BasicEntity("Russia", "Moscow") { Id = 2 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(2, table.Columns.Count());

        Assert.AreEqual("Poland", table[0][0]);
        Assert.AreEqual(2, table[0][1]);
    }

    [TestMethod]
    public void InnerJoinJoinPassMethodContextTest()
    {
        var query = @"
select
    a.ToDecimal(a.Id),
    b.ToDecimal(b.Id),
    c.ToDecimal(c.Id)
from #A.entities() a inner join #B.entities() b on 1 = 1 inner join #C.entities() c on 1 = 1";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("xX") { Id = 1 }
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("xX") { Id = 2 }
                ]
            },
            {
                "#C",
                [
                    new BasicEntity("xX") { Id = 3 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1m, table[0][0]);
        Assert.AreEqual(2m, table[0][1]);
        Assert.AreEqual(3m, table[0][2]);
    }

    [TestMethod]
    public void WhenJoinedByMethodInvocations_ShouldRetrieveValues()
    {
        var query =
            @"
select
    countries.GetCountry(),
    cities.GetCity(),
    population.GetPopulation()
from #A.entities() countries
inner join #B.entities() cities on countries.GetCountry() = cities.GetCountry()
inner join #C.entities() population on cities.GetCity() = population.GetCity()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Poland", "Krakow"),
                    new BasicEntity("Germany", "Berlin")
                ]
            },
            {
                "#B", [
                    new BasicEntity("Poland", "Krakow"),
                    new BasicEntity("Poland", "Wroclaw"),
                    new BasicEntity("Poland", "Warszawa"),
                    new BasicEntity("Poland", "Gdansk"),
                    new BasicEntity("Germany", "Berlin")
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

        Assert.IsTrue(table.Count == 5 && table.Columns.Count() == 3, "Table should contain 5 rows and 3 columns");

        Assert.IsTrue(table.Count(row => (string)row[0] == "Poland") == 4 &&
                      table.Any(row =>
                          (string)row[0] == "Poland" &&
                          (string)row[1] == "Krakow" &&
                          (decimal)row[2] == 400m) &&
                      table.Any(row =>
                          (string)row[0] == "Poland" &&
                          (string)row[1] == "Wroclaw" &&
                          (decimal)row[2] == 500m) &&
                      table.Any(row =>
                          (string)row[0] == "Poland" &&
                          (string)row[1] == "Warszawa" &&
                          (decimal)row[2] == 1000m) &&
                      table.Any(row =>
                          (string)row[0] == "Poland" &&
                          (string)row[1] == "Gdansk" &&
                          (decimal)row[2] == 200m),
            "Expected four rows for Poland with cities Krakow(400), Wroclaw(500), Warszawa(1000), Gdansk(200)");

        Assert.IsTrue(table.Any(row =>
                (string)row[0] == "Germany" &&
                (string)row[1] == "Berlin" &&
                (decimal)row[2] == 400m),
            "Expected one row for Germany with city Berlin(400)");
    }

    [TestMethod]
    public void WhenSelfJoined_ShouldRetrieveValues()
    {
        var query =
            @"
select
    countries.GetCountry(),
    cities.GetCity()
from #A.entities() countries
inner join #A.entities() cities on countries.Country = cities.Country
";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Poland", "Krakow"),
                    new BasicEntity("Germany", "Berlin")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count());

        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry =>
            (string)entry[0] == "Poland" &&
            (string)entry[1] == "Krakow"
        ), "First entry should be Poland, Krakow");

        Assert.IsTrue(table.Any(entry =>
            (string)entry[0] == "Germany" &&
            (string)entry[1] == "Berlin"
        ), "Second entry should be Germany, Berlin");
    }

    [TestMethod]
    public void WhenSelfJoined_WithMethodUsedModifyJoinedValues_ShouldPass()
    {
        var query =
            @"
select
    t.Country,
    t2.City
from #A.entities() t
inner join #A.entities() t2 on t.Trim(t.Country) = t2.Trim(t2.Country)
";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity(" Poland ", " Krakow"),
                    new BasicEntity("Germany ", " Berlin")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count());

        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry =>
            (string)entry[0] == " Poland " &&
            (string)entry[1] == " Krakow"
        ), "First entry should be Poland, Krakow");

        Assert.IsTrue(table.Any(entry =>
            (string)entry[0] == "Germany " &&
            (string)entry[1] == " Berlin"
        ), "Second entry should be Germany, Berlin");
    }
}

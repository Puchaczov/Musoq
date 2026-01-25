using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class JoinTests : BasicEntityTestBase
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
            {
                "#A", []
            },
            {
                "#B", []
            },
            {
                "#C", []
            }
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
    public void JoinWithCaseWhen2Test()
    {
        var query = @"
select
    countries.Country,
    (case when population.Population > 400 then cities.ToUpperInvariant(cities.City) else cities.City end) as 'cities.City',
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
        Assert.AreEqual(5, table.Count);

        Assert.IsTrue(table.Any(row =>
            (string)row[0] == "Poland" &&
            (string)row[1] == "Krakow" &&
            (decimal)row[2] == 400m
        ), "Expected row (Poland, Krakow, 400) not found");

        Assert.IsTrue(table.Any(row =>
            (string)row[0] == "Poland" &&
            (string)row[1] == "WROCLAW" &&
            (decimal)row[2] == 500m
        ), "Expected row (Poland, WROCLAW, 500) not found");

        Assert.IsTrue(table.Any(row =>
            (string)row[0] == "Poland" &&
            (string)row[1] == "WARSZAWA" &&
            (decimal)row[2] == 1000m
        ), "Expected row (Poland, WARSZAWA, 1000) not found");

        Assert.IsTrue(table.Any(row =>
            (string)row[0] == "Poland" &&
            (string)row[1] == "Gdansk" &&
            (decimal)row[2] == 200m
        ), "Expected row (Poland, Gdansk, 200) not found");

        Assert.IsTrue(table.Any(row =>
            (string)row[0] == "Germany" &&
            (string)row[1] == "Berlin" &&
            (decimal)row[2] == 400m
        ), "Expected row (Germany, Berlin, 400) not found");
    }

    [TestMethod]
    public void JoinWithCaseWhenTest()
    {
        var query =
            "select countries.Country, (case when population.Population >= 500 then 'big' else 'low' end), population.Population from #A.entities() countries inner join #B.entities() cities on countries.Country = cities.Country inner join #C.entities() population on cities.City = population.City";

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

        Assert.AreEqual("case when population.Population >= 500 then big else low end",
            table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual(1, table.Columns.ElementAt(1).ColumnIndex);

        Assert.AreEqual("population.Population", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(2).ColumnType);
        Assert.AreEqual(2, table.Columns.ElementAt(2).ColumnIndex);

        Assert.AreEqual(5, table.Count, "Table should contain 5 rows");

        var polandLow = table.Count(row =>
            (string)row[0] == "Poland" &&
            (string)row[1] == "low" &&
            ((decimal)row[2] == 400m || (decimal)row[2] == 200m)) == 2;

        var polandBig = table.Count(row =>
            (string)row[0] == "Poland" &&
            (string)row[1] == "big" &&
            ((decimal)row[2] == 500m || (decimal)row[2] == 1000m)) == 2;

        var germanyLow = table.Count(row =>
            (string)row[0] == "Germany" &&
            (string)row[1] == "low" &&
            (decimal)row[2] == 400m) == 1;

        Assert.IsTrue(polandLow && polandBig && germanyLow,
            "Expected data distribution not found");
    }

    [TestMethod]
    public void JoinWithGroupByTest()
    {
        var query = @"
select
    cities.Country,
    countries.Sum(population.Population)
from #A.entities() countries
inner join #B.entities() cities on countries.Country = cities.Country
inner join #C.entities() population on cities.City = population.City
group by cities.Country";

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

        Assert.AreEqual(2, table.Columns.Count());

        Assert.AreEqual("cities.Country", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual(0, table.Columns.ElementAt(0).ColumnIndex);

        Assert.AreEqual("countries.Sum(population.Population)", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual(1, table.Columns.ElementAt(1).ColumnIndex);

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
    public void JoinWithGroupByAndOrderByTest()
    {
        var query = @"
select
    cities.GetTypeName(cities.Country)
from #A.entities() countries
inner join #B.entities() cities on countries.Country = cities.Country
group by cities.GetTypeName(cities.Country)
order by cities.GetTypeName(cities.Country)";

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
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());

        Assert.AreEqual("cities.GetTypeName(cities.Country)", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(1, table.Count);

        Assert.AreEqual("System.String", table[0][0]);
    }

    [TestMethod]
    public void JoinWithOrderByTest()
    {
        var query = @"
select
    cities.Country
from #A.entities() countries
inner join #B.entities() cities on countries.Country = cities.Country
order by cities.GetTypeName(cities.Country)";

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
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);
    }

    [TestMethod]
    public void JoinWithExceptTest()
    {
        const string query = @"
select
    countries.Country, cities.City, population.Population
from #A.entities() countries
inner join #B.entities() cities on countries.Country = cities.Country
inner join #C.entities() population on cities.City = population.City
except (countries.Country, cities.City, population.Population)
select
    countries.Country, cities.City, population.Population
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

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void JoinWithUnionTest()
    {
        var query =
            @"
select
    countries.Country, cities.City, population.Population
from #A.entities() countries
inner join #B.entities() cities on countries.Country = cities.Country
inner join #C.entities() population on cities.City = population.City
union (countries.Country, cities.City, population.Population)
select
    countries.Country, cities.City, population.Population
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

        Assert.AreEqual(5, table.Count, "Table should contain 5 rows");

        Assert.AreEqual(4,
            table.Count(row =>
                (string)row[0] == "Poland" &&
                new[] { "Krakow", "Wroclaw", "Warszawa", "Gdansk" }.Contains((string)row[1]) &&
                new[] { 400m, 500m, 1000m, 200m }.Contains((decimal)row[2])),
            "Expected 4 Polish cities with their values not found");

        Assert.IsTrue(table.Any(row =>
                (string)row[0] == "Germany" &&
                (string)row[1] == "Berlin" &&
                (decimal)row[2] == 400m),
            "Expected data for Berlin not found");
    }

    [TestMethod]
    public void JoinWithUnionAllTest()
    {
        var query =
            @"
select
    countries.Country, cities.City, population.Population
from #A.entities() countries
inner join #B.entities() cities on countries.Country = cities.Country
inner join #C.entities() population on cities.City = population.City
union all (countries.Country, cities.City, population.Population)
select
    countries.Country, cities.City, population.Population
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

        Assert.AreEqual(10, table.Count, "Table should contain 10 rows");

        Assert.AreEqual(2,
            table.Count(row =>
                (string)row[0] == "Poland" &&
                (string)row[1] == "Krakow" &&
                (decimal)row[2] == 400m), "Should have exactly 2 rows of Poland/Krakow/400");

        Assert.AreEqual(2,
            table.Count(row =>
                (string)row[0] == "Poland" &&
                (string)row[1] == "Wroclaw" &&
                (decimal)row[2] == 500m), "Should have exactly 2 rows of Poland/Wroclaw/500");

        Assert.AreEqual(2,
            table.Count(row =>
                (string)row[0] == "Poland" &&
                (string)row[1] == "Warszawa" &&
                (decimal)row[2] == 1000m), "Should have exactly 2 rows of Poland/Warszawa/1000");

        Assert.AreEqual(2,
            table.Count(row =>
                (string)row[0] == "Poland" &&
                (string)row[1] == "Gdansk" &&
                (decimal)row[2] == 200m), "Should have exactly 2 rows of Poland/Gdansk/200");

        Assert.AreEqual(2,
            table.Count(row =>
                (string)row[0] == "Germany" &&
                (string)row[1] == "Berlin" &&
                (decimal)row[2] == 400m), "Should have exactly 2 rows of Germany/Berlin/400");
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
    public void SimpleLeftJoinTest()
    {
        var query = "select a.Id, b.Id from #A.entities() a left outer join #B.entities() b on a.Id = b.Id";

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
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(2, table.Columns.Count());

        var column = table.Columns.ElementAt(0);
        Assert.AreEqual(0, column.ColumnIndex);
        Assert.AreEqual("a.Id", column.ColumnName);
        Assert.AreEqual(typeof(int), column.ColumnType);

        column = table.Columns.ElementAt(1);
        Assert.AreEqual(1, column.ColumnIndex);
        Assert.AreEqual("b.Id", column.ColumnName);
        Assert.AreEqual(typeof(int?), column.ColumnType);

        Assert.AreEqual(1, table[0][0]);
        Assert.IsNull(table[0][1]);
    }

    [TestMethod]
    public void SimpleLeftJoinShorthandTest()
    {
        const string query = "select a.Id, b.Id from #A.entities() a left join #B.entities() b on a.Id = b.Id";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("xX") { Id = 1 }, new BasicEntity("yY") { Id = 2 }] },
            { "#B", [new BasicEntity("xX") { Id = 2 }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(2, table.Columns.Count());

        var rows = new HashSet<(int?, int?)>
        {
            ((int?)table[0][0], (int?)table[0][1]),
            ((int?)table[1][0], (int?)table[1][1])
        };

        Assert.Contains((1, null), rows, "Expected row (1, null) not found");
        Assert.Contains((2, 2), rows, "Expected row (2, 2) not found");
    }

    [TestMethod]
    public void SimpleLeftJoinShorthandUppercaseTest()
    {
        const string query = "SELECT A.Id, B.Id FROM #A.entities() A LEFT JOIN #B.entities() B ON A.Id = B.Id";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("xX") { Id = 1 }, new BasicEntity("yY") { Id = 2 }] },
            { "#B", [new BasicEntity("xX") { Id = 2 }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(2, table.Columns.Count());

        var rows = new HashSet<(int?, int?)>
        {
            ((int?)table[0][0], (int?)table[0][1]),
            ((int?)table[1][0], (int?)table[1][1])
        };

        Assert.Contains((1, null), rows, "Expected row (1, null) not found");
        Assert.Contains((2, 2), rows, "Expected row (2, 2) not found");
    }

    [TestMethod]
    public void MultipleLeftJoinTest()
    {
        const string query =
            "select a.Id, b.Id, c.Id from #A.entities() a left outer join #B.entities() b on a.Id = b.Id left outer join #B.entities() c on b.Id = c.Id";

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
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(3, table.Columns.Count());

        var column = table.Columns.ElementAt(0);
        Assert.AreEqual(0, column.ColumnIndex);
        Assert.AreEqual("a.Id", column.ColumnName);
        Assert.AreEqual(typeof(int), column.ColumnType);

        column = table.Columns.ElementAt(1);
        Assert.AreEqual(1, column.ColumnIndex);
        Assert.AreEqual("b.Id", column.ColumnName);
        Assert.AreEqual(typeof(int?), column.ColumnType);

        column = table.Columns.ElementAt(2);
        Assert.AreEqual(2, column.ColumnIndex);
        Assert.AreEqual("c.Id", column.ColumnName);
        Assert.AreEqual(typeof(int?), column.ColumnType);

        Assert.AreEqual(1, table[0][0]);
        Assert.IsNull(table[0][1]);
        Assert.IsNull(table[0][2]);
    }

    [TestMethod]
    public void MultipleLeftJoinWithCTriesMatchBButFailTest()
    {
        var query =
            "select a.Id, b.Id, c.Id from #A.entities() a left outer join #B.entities() b on a.Id = b.Id left outer join #C.entities() c on b.Id = c.Id";

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
                ]
            },
            {
                "#C",
                [
                    new BasicEntity("xX") { Id = 1 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(3, table.Columns.Count());

        var column = table.Columns.ElementAt(0);
        Assert.AreEqual(0, column.ColumnIndex);
        Assert.AreEqual("a.Id", column.ColumnName);
        Assert.AreEqual(typeof(int), column.ColumnType);

        column = table.Columns.ElementAt(1);
        Assert.AreEqual(1, column.ColumnIndex);
        Assert.AreEqual("b.Id", column.ColumnName);
        Assert.AreEqual(typeof(int?), column.ColumnType);

        column = table.Columns.ElementAt(2);
        Assert.AreEqual(2, column.ColumnIndex);
        Assert.AreEqual("c.Id", column.ColumnName);
        Assert.AreEqual(typeof(int?), column.ColumnType);

        Assert.AreEqual(1, table[0][0]);
        Assert.IsNull(table[0][1]);
        Assert.IsNull(table[0][2]);
    }

    [TestMethod]
    public void MultipleLeftJoinWithCTriesMatchBAndSucceedTest()
    {
        var query =
            "select a.Id, b.Id, c.Id from #A.entities() a left outer join #B.entities() b on a.Id = b.Id left outer join #C.entities() c on b.Id = c.Id";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("xX") { Id = 1 },
                    new BasicEntity("xX") { Id = 2 }
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("xX") { Id = 1 }
                ]
            },
            {
                "#C",
                [
                    new BasicEntity("xX") { Id = 1 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(3, table.Columns.Count());

        var column = table.Columns.ElementAt(0);
        Assert.AreEqual(0, column.ColumnIndex);
        Assert.AreEqual("a.Id", column.ColumnName);
        Assert.AreEqual(typeof(int), column.ColumnType);

        column = table.Columns.ElementAt(1);
        Assert.AreEqual(1, column.ColumnIndex);
        Assert.AreEqual("b.Id", column.ColumnName);
        Assert.AreEqual(typeof(int?), column.ColumnType);

        column = table.Columns.ElementAt(2);
        Assert.AreEqual(2, column.ColumnIndex);
        Assert.AreEqual("c.Id", column.ColumnName);
        Assert.AreEqual(typeof(int?), column.ColumnType);

        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry =>
            (int)entry[0] == 1 &&
            (int)entry[1] == 1 &&
            (int)entry[2] == 1
        ), "First entry should be 1, 1, 1");

        Assert.IsTrue(table.Any(entry =>
            (int)entry[0] == 2 &&
            entry[1] == null &&
            entry[2] == null
        ), "Second entry should be 2, null, null");
    }

    [TestMethod]
    public void SimpleRightJoinTest()
    {
        var query = "select a.Id, b.Id from #A.entities() a right outer join #B.entities() b on a.Id = b.Id";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("xX") { Id = 1 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(2, table.Columns.Count());

        var column = table.Columns.ElementAt(0);
        Assert.AreEqual(0, column.ColumnIndex);
        Assert.AreEqual("a.Id", column.ColumnName);
        Assert.AreEqual(typeof(int?), column.ColumnType);

        column = table.Columns.ElementAt(1);
        Assert.AreEqual(1, column.ColumnIndex);
        Assert.AreEqual("b.Id", column.ColumnName);
        Assert.AreEqual(typeof(int), column.ColumnType);

        Assert.IsNull(table[0][0]);
        Assert.AreEqual(1, table[0][1]);
    }

    [TestMethod]
    public void MultipleRightJoinWithCTriesMatchBAndSucceedForASingleTest()
    {
        var query =
            "select a.Id, b.Id, c.Id from #A.entities() a right outer join #B.entities() b on a.Id = b.Id right outer join #C.entities() c on b.Id = c.Id";

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
                    new BasicEntity("xX") { Id = 1 }
                ]
            },
            {
                "#C",
                [
                    new BasicEntity("xX") { Id = 1 },
                    new BasicEntity("xX") { Id = 2 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(3, table.Columns.Count());

        var column = table.Columns.ElementAt(0);
        Assert.AreEqual(0, column.ColumnIndex);
        Assert.AreEqual("a.Id", column.ColumnName);
        Assert.AreEqual(typeof(int?), column.ColumnType);

        column = table.Columns.ElementAt(1);
        Assert.AreEqual(1, column.ColumnIndex);
        Assert.AreEqual("b.Id", column.ColumnName);
        Assert.AreEqual(typeof(int?), column.ColumnType);

        column = table.Columns.ElementAt(2);
        Assert.AreEqual(2, column.ColumnIndex);
        Assert.AreEqual("c.Id", column.ColumnName);
        Assert.AreEqual(typeof(int), column.ColumnType);

        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry =>
            (int?)entry[0] == 1 &&
            (int?)entry[1] == 1 &&
            (int?)entry[2] == 1
        ), "First entry should be 1, 1, 1");

        Assert.IsTrue(table.Any(entry =>
            entry[0] == null &&
            entry[1] == null &&
            (int?)entry[2] == 2
        ), "Second entry should be null, null, 2");
    }

    [TestMethod]
    public void RightOuterJoinPassMethodContextTest()
    {
        var query =
            "select a.ToDecimal(a.Id), b.ToDecimal(b.Id), c.ToDecimal(c.Id) from #A.entities() a right outer join #B.entities() b on 1 = 1 right outer join #C.entities() c on 1 = 1";

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
    public void LeftOuterJoinPassMethodContextTest()
    {
        var query = @"
select a.ToDecimal(a.Id), b.ToDecimal(b.Id), c.ToDecimal(c.Id)
from #A.entities() a
left outer join #B.entities() b on 1 = 1
left outer join #C.entities() c on 1 = 1";

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
    public void LeftOuterJoinWithFourOtherJoinsTest()
    {
        var query = @"
select a.ToDecimal(a.Id), b.ToDecimal(b.Id), c.ToDecimal(c.Id), d.ToDecimal(d.Id)
from #A.entities() a
left outer join #B.entities() b on 1 = 1
left outer join #C.entities() c on 1 = 1
left outer join #D.entities() d on 1 = 1";

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
            },
            {
                "#D",
                [
                    new BasicEntity("xX") { Id = 4 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1m, table[0][0]);
        Assert.AreEqual(2m, table[0][1]);
        Assert.AreEqual(3m, table[0][2]);
        Assert.AreEqual(4m, table[0][3]);
    }

    [TestMethod]
    public void LeftOuterRightOuterJoinPassMethodContextTest()
    {
        var query =
            "select a.ToDecimal(a.Id), b.ToDecimal(b.Id), c.ToDecimal(c.Id) from #A.entities() a left outer join #B.entities() b on 1 = 1 right outer join #C.entities() c on 1 = 1";

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
    public void RightOuterLeftOuterJoinPassMethodContextTest()
    {
        var query =
            "select a.ToDecimal(a.Id), b.ToDecimal(b.Id), c.ToDecimal(c.Id) from #A.entities() a right outer join #B.entities() b on 1 = 1 left outer join #C.entities() c on 1 = 1";

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

    [TestMethod]
    public void WhenMultipleAliasesAroundCteQuery_LeftOuterJoin_ShouldRetrieveValues()
    {
        var query =
            @"
with first as (
    select a.Country as Country from #A.entities() a
), second as (
    select a.Country as Country from #A.entities() a
), third as (
    select
        a.Country,
        b.Country
    from first a left outer join second b on a.Country = b.Country
)
select * from third
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

        Assert.AreEqual("a.Country", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);

        Assert.AreEqual("b.Country", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);

        Assert.AreEqual(2, table.Count);

        Assert.IsTrue(table.Any(entry =>
                (string)entry[0] == "Poland" &&
                (string)entry[1] == "Poland"),
            "First entry should be Poland");

        Assert.IsTrue(table.Any(entry =>
                (string)entry[0] == "Germany" &&
                (string)entry[1] == "Germany"),
            "Second entry should be Germany");
    }

    [TestMethod]
    public void WhenMultipleAliasesAroundCteQuery_RightOuterJoin_ShouldRetrieveValues()
    {
        var query =
            @"
with first as (
    select a.Country as Country from #A.entities() a
), second as (
    select a.Country as Country from #A.entities() a
), third as (
    select
        a.Country,
        b.Country
    from first a right outer join second b on a.Country = b.Country
)
select * from third
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

        Assert.AreEqual("a.Country", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);

        Assert.AreEqual("b.Country", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);

        Assert.AreEqual(2, table.Count, "Table should contain 2 rows");

        Assert.IsTrue(table.Any(row =>
                (string)row[0] == "Poland" &&
                (string)row[1] == "Poland"),
            "Expected row with Poland in both columns");

        Assert.IsTrue(table.Any(row =>
                (string)row[0] == "Germany" &&
                (string)row[1] == "Germany"),
            "Expected row with Germany in both columns");
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class Join_IntegrationTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

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
    public void JoinWithGroupByAndUnqualifiedSharedAggregateShouldInferAlias()
    {
        var query = @"
select
    cities.Country,
    Sum(population.Population) as TotalPopulation
from #A.entities() countries
inner join #B.entities() cities on countries.Country = cities.Country
inner join #C.entities() population on cities.City = population.City
group by cities.Country
order by TotalPopulation desc";

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

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("Poland", table[0][0]);
        Assert.AreEqual(2100m, table[0][1]);
        Assert.AreEqual("Germany", table[1][0]);
        Assert.AreEqual(400m, table[1][1]);
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
}

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class JoinIntegrationTests : BasicEntityTestBase
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("countries.Country", typeof(string)), ("cities.City", typeof(string)),
            ("population.Population", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Poland", "Krakow", 400m], ["Poland", "WROCLAW", 500m],
            ["Poland", "WARSZAWA", 1000m], ["Poland", "Gdansk", 200m], ["Germany", "Berlin", 400m]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("countries.Country", typeof(string)),
            ("case when population.Population >= 500 then big else low end", typeof(string)),
            ("population.Population", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Poland", "low", 400m], ["Poland", "big", 500m], ["Poland", "big", 1000m],
            ["Poland", "low", 200m], ["Germany", "low", 400m]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("cities.Country", typeof(string)), ("countries.Sum(population.Population)", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Poland", 2100m], ["Germany", 400m]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("cities.Country", typeof(string)), ("TotalPopulation", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Poland", 2100m], ["Germany", 400m]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("cities.GetTypeName(cities.Country)", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["System.String"]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("cities.Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Poland"], ["Poland"], ["Poland"], ["Poland"], ["Germany"]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("countries.Country", typeof(string)), ("cities.City", typeof(string)),
            ("population.Population", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(table);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("countries.Country", typeof(string)), ("cities.City", typeof(string)),
            ("population.Population", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Poland", "Krakow", 400m], ["Poland", "Wroclaw", 500m],
            ["Poland", "Warszawa", 1000m], ["Poland", "Gdansk", 200m], ["Germany", "Berlin", 400m]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("countries.Country", typeof(string)), ("cities.City", typeof(string)),
            ("population.Population", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Poland", "Krakow", 400m], ["Poland", "Krakow", 400m],
            ["Poland", "Wroclaw", 500m], ["Poland", "Wroclaw", 500m],
            ["Poland", "Warszawa", 1000m], ["Poland", "Warszawa", 1000m],
            ["Poland", "Gdansk", 200m], ["Poland", "Gdansk", 200m],
            ["Germany", "Berlin", 400m], ["Germany", "Berlin", 400m]);
    }
}

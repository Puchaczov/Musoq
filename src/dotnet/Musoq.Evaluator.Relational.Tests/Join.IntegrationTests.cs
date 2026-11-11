using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public class JoinIntegrationTests : BasicEntityTestBase
{
    [TestMethod]
    public void JoinWithCaseWhen2Test()
    {
        var query = (
            "\n"
            + "select\n"
            + "    countries.Country,\n"
            + "    (case when population.Population > 400 then cities.ToUpperInvariant(cities.City) else cities.City end) as 'cities.City',\n"
            + "    population.Population\n"
            + "from #A.entities() countries\n"
            + "inner join #B.entities() cities on countries.Country = cities.Country\n"
            + "inner join #C.entities() population on cities.City = population.City"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Poland", "Krakow"), new BasicEntity("Germany", "Berlin")] },
            {
                "#B",
                [
                    new BasicEntity("Poland", "Krakow"),
                    new BasicEntity("Poland", "Wroclaw"),
                    new BasicEntity("Poland", "Warszawa"),
                    new BasicEntity("Poland", "Gdansk"),
                    new BasicEntity("Germany", "Berlin"),
                ]
            },
            {
                "#C",
                [
                    new BasicEntity { City = "Krakow", Population = 400 },
                    new BasicEntity { City = "Wroclaw", Population = 500 },
                    new BasicEntity { City = "Warszawa", Population = 1000 },
                    new BasicEntity { City = "Gdansk", Population = 200 },
                    new BasicEntity { City = "Berlin", Population = 400 },
                ]
            },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("cities.City", typeof(string)),
            ("Population", typeof(decimal))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Poland", "Krakow", 400m],
            ["Poland", "WROCLAW", 500m],
            ["Poland", "WARSZAWA", 1000m],
            ["Poland", "Gdansk", 200m],
            ["Germany", "Berlin", 400m]
        );
    }

    [TestMethod]
    public void JoinWithCaseWhenTest()
    {
        var query =
            "select countries.Country, (case when population.Population >= 500 then 'big' else 'low' end), population.Population from #A.entities() countries inner join #B.entities() cities on countries.Country = cities.Country inner join #C.entities() population on cities.City = population.City";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Poland", "Krakow"), new BasicEntity("Germany", "Berlin")] },
            {
                "#B",
                [
                    new BasicEntity("Poland", "Krakow"),
                    new BasicEntity("Poland", "Wroclaw"),
                    new BasicEntity("Poland", "Warszawa"),
                    new BasicEntity("Poland", "Gdansk"),
                    new BasicEntity("Germany", "Berlin"),
                ]
            },
            {
                "#C",
                [
                    new BasicEntity { City = "Krakow", Population = 400 },
                    new BasicEntity { City = "Wroclaw", Population = 500 },
                    new BasicEntity { City = "Warszawa", Population = 1000 },
                    new BasicEntity { City = "Gdansk", Population = 200 },
                    new BasicEntity { City = "Berlin", Population = 400 },
                ]
            },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("case when population.Population >= 500 then big else low end", typeof(string)),
            ("Population", typeof(decimal))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Poland", "low", 400m],
            ["Poland", "big", 500m],
            ["Poland", "big", 1000m],
            ["Poland", "low", 200m],
            ["Germany", "low", 400m]
        );
    }

    [TestMethod]
    public void JoinWithGroupByTest()
    {
        var query = (
            "\n"
            + "select\n"
            + "    cities.Country,\n"
            + "    countries.Sum(population.Population)\n"
            + "from #A.entities() countries\n"
            + "inner join #B.entities() cities on countries.Country = cities.Country\n"
            + "inner join #C.entities() population on cities.City = population.City\n"
            + "group by cities.Country"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Poland", "Krakow"), new BasicEntity("Germany", "Berlin")] },
            {
                "#B",
                [
                    new BasicEntity("Poland", "Krakow"),
                    new BasicEntity("Poland", "Wroclaw"),
                    new BasicEntity("Poland", "Warszawa"),
                    new BasicEntity("Poland", "Gdansk"),
                    new BasicEntity("Germany", "Berlin"),
                ]
            },
            {
                "#C",
                [
                    new BasicEntity { City = "Krakow", Population = 400 },
                    new BasicEntity { City = "Wroclaw", Population = 500 },
                    new BasicEntity { City = "Warszawa", Population = 1000 },
                    new BasicEntity { City = "Gdansk", Population = 200 },
                    new BasicEntity { City = "Berlin", Population = 400 },
                ]
            },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("countries.Sum(population.Population)", typeof(decimal?))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Poland", 2100m], ["Germany", 400m]);
    }

    [TestMethod]
    public void JoinWithGroupByAndUnqualifiedSharedAggregateShouldInferAlias()
    {
        var query = (
            "\n"
            + "select\n"
            + "    cities.Country,\n"
            + "    Sum(population.Population) as TotalPopulation\n"
            + "from #A.entities() countries\n"
            + "inner join #B.entities() cities on countries.Country = cities.Country\n"
            + "inner join #C.entities() population on cities.City = population.City\n"
            + "group by cities.Country\n"
            + "order by TotalPopulation desc"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Poland", "Krakow"), new BasicEntity("Germany", "Berlin")] },
            {
                "#B",
                [
                    new BasicEntity("Poland", "Krakow"),
                    new BasicEntity("Poland", "Wroclaw"),
                    new BasicEntity("Poland", "Warszawa"),
                    new BasicEntity("Poland", "Gdansk"),
                    new BasicEntity("Germany", "Berlin"),
                ]
            },
            {
                "#C",
                [
                    new BasicEntity { City = "Krakow", Population = 400 },
                    new BasicEntity { City = "Wroclaw", Population = 500 },
                    new BasicEntity { City = "Warszawa", Population = 1000 },
                    new BasicEntity { City = "Gdansk", Population = 200 },
                    new BasicEntity { City = "Berlin", Population = 400 },
                ]
            },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("TotalPopulation", typeof(decimal?))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Poland", 2100m], ["Germany", 400m]);
    }

    [TestMethod]
    public void JoinWithGroupByAndOrderByTest()
    {
        var query = (
            "\n"
            + "select\n"
            + "    cities.GetTypeName(cities.Country)\n"
            + "from #A.entities() countries\n"
            + "inner join #B.entities() cities on countries.Country = cities.Country\n"
            + "group by cities.GetTypeName(cities.Country)\n"
            + "order by cities.GetTypeName(cities.Country)"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Poland", "Krakow"), new BasicEntity("Germany", "Berlin")] },
            {
                "#B",
                [
                    new BasicEntity("Poland", "Krakow"),
                    new BasicEntity("Poland", "Wroclaw"),
                    new BasicEntity("Poland", "Warszawa"),
                    new BasicEntity("Poland", "Gdansk"),
                    new BasicEntity("Germany", "Berlin"),
                ]
            },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("cities.GetTypeName(cities.Country)", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["System.String"]);
    }

    [TestMethod]
    public void JoinWithOrderByTest()
    {
        var query = (
            "\n"
            + "select\n"
            + "    cities.Country\n"
            + "from #A.entities() countries\n"
            + "inner join #B.entities() cities on countries.Country = cities.Country\n"
            + "order by cities.GetTypeName(cities.Country)"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Poland", "Krakow"), new BasicEntity("Germany", "Berlin")] },
            {
                "#B",
                [
                    new BasicEntity("Poland", "Krakow"),
                    new BasicEntity("Poland", "Wroclaw"),
                    new BasicEntity("Poland", "Warszawa"),
                    new BasicEntity("Poland", "Gdansk"),
                    new BasicEntity("Germany", "Berlin"),
                ]
            },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Poland"],
            ["Poland"],
            ["Poland"],
            ["Poland"],
            ["Germany"]
        );
    }

    [TestMethod]
    public void JoinWithExceptTest()
    {
        const string query = (
            "\n"
            + "select\n"
            + "    countries.Country, cities.City, population.Population\n"
            + "from #A.entities() countries\n"
            + "inner join #B.entities() cities on countries.Country = cities.Country\n"
            + "inner join #C.entities() population on cities.City = population.City\n"
            + "except (countries.Country, cities.City, population.Population)\n"
            + "select\n"
            + "    countries.Country, cities.City, population.Population\n"
            + "from #A.entities() countries\n"
            + "inner join #B.entities() cities on countries.Country = cities.Country\n"
            + "inner join #C.entities() population on cities.City = population.City"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Poland", "Krakow"), new BasicEntity("Germany", "Berlin")] },
            {
                "#B",
                [
                    new BasicEntity("Poland", "Krakow"),
                    new BasicEntity("Poland", "Wroclaw"),
                    new BasicEntity("Poland", "Warszawa"),
                    new BasicEntity("Poland", "Gdansk"),
                    new BasicEntity("Germany", "Berlin"),
                ]
            },
            {
                "#C",
                [
                    new BasicEntity { City = "Krakow", Population = 400 },
                    new BasicEntity { City = "Wroclaw", Population = 500 },
                    new BasicEntity { City = "Warszawa", Population = 1000 },
                    new BasicEntity { City = "Gdansk", Population = 200 },
                    new BasicEntity { City = "Berlin", Population = 400 },
                ]
            },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("City", typeof(string)),
            ("Population", typeof(decimal))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table);
    }

    [TestMethod]
    public void JoinWithUnionTest()
    {
        var query = (
            "\n"
            + "select\n"
            + "    countries.Country, cities.City, population.Population\n"
            + "from #A.entities() countries\n"
            + "inner join #B.entities() cities on countries.Country = cities.Country\n"
            + "inner join #C.entities() population on cities.City = population.City\n"
            + "union (countries.Country, cities.City, population.Population)\n"
            + "select\n"
            + "    countries.Country, cities.City, population.Population\n"
            + "from #A.entities() countries\n"
            + "inner join #B.entities() cities on countries.Country = cities.Country\n"
            + "inner join #C.entities() population on cities.City = population.City"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Poland", "Krakow"), new BasicEntity("Germany", "Berlin")] },
            {
                "#B",
                [
                    new BasicEntity("Poland", "Krakow"),
                    new BasicEntity("Poland", "Wroclaw"),
                    new BasicEntity("Poland", "Warszawa"),
                    new BasicEntity("Poland", "Gdansk"),
                    new BasicEntity("Germany", "Berlin"),
                ]
            },
            {
                "#C",
                [
                    new BasicEntity { City = "Krakow", Population = 400 },
                    new BasicEntity { City = "Wroclaw", Population = 500 },
                    new BasicEntity { City = "Warszawa", Population = 1000 },
                    new BasicEntity { City = "Gdansk", Population = 200 },
                    new BasicEntity { City = "Berlin", Population = 400 },
                ]
            },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("City", typeof(string)),
            ("Population", typeof(decimal))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Poland", "Krakow", 400m],
            ["Poland", "Wroclaw", 500m],
            ["Poland", "Warszawa", 1000m],
            ["Poland", "Gdansk", 200m],
            ["Germany", "Berlin", 400m]
        );
    }

    [TestMethod]
    public void JoinWithUnionAllTest()
    {
        var query = (
            "\n"
            + "select\n"
            + "    countries.Country, cities.City, population.Population\n"
            + "from #A.entities() countries\n"
            + "inner join #B.entities() cities on countries.Country = cities.Country\n"
            + "inner join #C.entities() population on cities.City = population.City\n"
            + "union all (countries.Country, cities.City, population.Population)\n"
            + "select\n"
            + "    countries.Country, cities.City, population.Population\n"
            + "from #A.entities() countries\n"
            + "inner join #B.entities() cities on countries.Country = cities.Country\n"
            + "inner join #C.entities() population on cities.City = population.City"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Poland", "Krakow"), new BasicEntity("Germany", "Berlin")] },
            {
                "#B",
                [
                    new BasicEntity("Poland", "Krakow"),
                    new BasicEntity("Poland", "Wroclaw"),
                    new BasicEntity("Poland", "Warszawa"),
                    new BasicEntity("Poland", "Gdansk"),
                    new BasicEntity("Germany", "Berlin"),
                ]
            },
            {
                "#C",
                [
                    new BasicEntity { City = "Krakow", Population = 400 },
                    new BasicEntity { City = "Wroclaw", Population = 500 },
                    new BasicEntity { City = "Warszawa", Population = 1000 },
                    new BasicEntity { City = "Gdansk", Population = 200 },
                    new BasicEntity { City = "Berlin", Population = 400 },
                ]
            },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("City", typeof(string)),
            ("Population", typeof(decimal))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Poland", "Krakow", 400m],
            ["Poland", "Krakow", 400m],
            ["Poland", "Wroclaw", 500m],
            ["Poland", "Wroclaw", 500m],
            ["Poland", "Warszawa", 1000m],
            ["Poland", "Warszawa", 1000m],
            ["Poland", "Gdansk", 200m],
            ["Poland", "Gdansk", 200m],
            ["Germany", "Berlin", 400m],
            ["Germany", "Berlin", 400m]
        );
    }
}

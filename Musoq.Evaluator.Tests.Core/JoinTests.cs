using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Core.Schema;

namespace Musoq.Evaluator.Tests.Core
{
    [TestClass]
    public class JoinTests : TestBase
    {

        [TestMethod]
        public void SimpleJoinTest()
        {
            var query =
                "select countries.Country, cities.City, population.Population from #A.entities() countries inner join #B.entities() cities on countries.Country = cities.Country inner join #C.entities() population on cities.City = population.City";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("Poland", "Krakow"),
                        new BasicEntity("Germany", "Berlin")
                    }
                },
                {
                    "#B", new[]
                    {
                        new BasicEntity("Poland", "Krakow"),
                        new BasicEntity("Poland", "Wroclaw"),
                        new BasicEntity("Poland", "Warszawa"),
                        new BasicEntity("Poland", "Gdansk"),
                        new BasicEntity("Germany", "Berlin")
                    }
                },
                {
                    "#C", new[]
                    {
                        new BasicEntity {City = "Krakow", Population = 400},
                        new BasicEntity {City = "Wroclaw", Population = 500},
                        new BasicEntity {City = "Warszawa", Population = 1000},
                        new BasicEntity {City = "Gdansk", Population = 200},
                        new BasicEntity {City = "Berlin", Population = 400}
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

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

            Assert.AreEqual("Poland", table[0][0]);
            Assert.AreEqual("Krakow", table[0][1]);
            Assert.AreEqual(400m, table[0][2]);

            Assert.AreEqual("Poland", table[1][0]);
            Assert.AreEqual("Wroclaw", table[1][1]);
            Assert.AreEqual(500m, table[1][2]);

            Assert.AreEqual("Poland", table[2][0]);
            Assert.AreEqual("Warszawa", table[2][1]);
            Assert.AreEqual(1000m, table[2][2]);

            Assert.AreEqual("Poland", table[3][0]);
            Assert.AreEqual("Gdansk", table[3][1]);
            Assert.AreEqual(200m, table[3][2]);

            Assert.AreEqual("Germany", table[4][0]);
            Assert.AreEqual("Berlin", table[4][1]);
            Assert.AreEqual(400m, table[4][2]);
        }

        [TestMethod]
        public void JoinWithCaseWhen2Test()
        {
            var query =
                "select countries.Country, (case when population.Population > 400 then cities.ToUpperInvariant(cities.City) else cities.City end) as 'cities.City', population.Population from #A.entities() countries inner join #B.entities() cities on countries.Country = cities.Country inner join #C.entities() population on cities.City = population.City";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("Poland", "Krakow"),
                        new BasicEntity("Germany", "Berlin")
                    }
                },
                {
                    "#B", new[]
                    {
                        new BasicEntity("Poland", "Krakow"),
                        new BasicEntity("Poland", "Wroclaw"),
                        new BasicEntity("Poland", "Warszawa"),
                        new BasicEntity("Poland", "Gdansk"),
                        new BasicEntity("Germany", "Berlin")
                    }
                },
                {
                    "#C", new[]
                    {
                        new BasicEntity {City = "Krakow", Population = 400},
                        new BasicEntity {City = "Wroclaw", Population = 500},
                        new BasicEntity {City = "Warszawa", Population = 1000},
                        new BasicEntity {City = "Gdansk", Population = 200},
                        new BasicEntity {City = "Berlin", Population = 400}
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

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

            Assert.AreEqual("Poland", table[0][0]);
            Assert.AreEqual("Krakow", table[0][1]);
            Assert.AreEqual(400m, table[0][2]);

            Assert.AreEqual("Poland", table[1][0]);
            Assert.AreEqual("WROCLAW", table[1][1]);
            Assert.AreEqual(500m, table[1][2]);

            Assert.AreEqual("Poland", table[2][0]);
            Assert.AreEqual("WARSZAWA", table[2][1]);
            Assert.AreEqual(1000m, table[2][2]);

            Assert.AreEqual("Poland", table[3][0]);
            Assert.AreEqual("Gdansk", table[3][1]);
            Assert.AreEqual(200m, table[3][2]);

            Assert.AreEqual("Germany", table[4][0]);
            Assert.AreEqual("Berlin", table[4][1]);
            Assert.AreEqual(400m, table[4][2]);
        }

        [TestMethod]
        public void JoinWithCaseWhenTest()
        {
            var query =
                "select countries.Country, (case when population.Population >= 500 then 'big' else 'low' end), population.Population from #A.entities() countries inner join #B.entities() cities on countries.Country = cities.Country inner join #C.entities() population on cities.City = population.City";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("Poland", "Krakow"),
                        new BasicEntity("Germany", "Berlin")
                    }
                },
                {
                    "#B", new[]
                    {
                        new BasicEntity("Poland", "Krakow"),
                        new BasicEntity("Poland", "Wroclaw"),
                        new BasicEntity("Poland", "Warszawa"),
                        new BasicEntity("Poland", "Gdansk"),
                        new BasicEntity("Germany", "Berlin")
                    }
                },
                {
                    "#C", new[]
                    {
                        new BasicEntity {City = "Krakow", Population = 400},
                        new BasicEntity {City = "Wroclaw", Population = 500},
                        new BasicEntity {City = "Warszawa", Population = 1000},
                        new BasicEntity {City = "Gdansk", Population = 200},
                        new BasicEntity {City = "Berlin", Population = 400}
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(3, table.Columns.Count());

            Assert.AreEqual("countries.Country", table.Columns.ElementAt(0).ColumnName);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
            Assert.AreEqual(0, table.Columns.ElementAt(0).ColumnIndex);

            Assert.AreEqual("case when population.Population >= 500 then 'big' else 'low' end", table.Columns.ElementAt(1).ColumnName);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
            Assert.AreEqual(1, table.Columns.ElementAt(1).ColumnIndex);

            Assert.AreEqual("population.Population", table.Columns.ElementAt(2).ColumnName);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(2).ColumnType);
            Assert.AreEqual(2, table.Columns.ElementAt(2).ColumnIndex);

            Assert.AreEqual("Poland", table[0][0]);
            Assert.AreEqual("low", table[0][1]);
            Assert.AreEqual(400m, table[0][2]);

            Assert.AreEqual("Poland", table[1][0]);
            Assert.AreEqual("big", table[1][1]);
            Assert.AreEqual(500m, table[1][2]);

            Assert.AreEqual("Poland", table[2][0]);
            Assert.AreEqual("big", table[2][1]);
            Assert.AreEqual(1000m, table[2][2]);

            Assert.AreEqual("Poland", table[3][0]);
            Assert.AreEqual("low", table[3][1]);
            Assert.AreEqual(200m, table[3][2]);

            Assert.AreEqual("Germany", table[4][0]);
            Assert.AreEqual("low", table[4][1]);
            Assert.AreEqual(400m, table[4][2]);
        }

        [TestMethod]
        public void JoinWithGroupByTest()
        {
            var query =
                "select cities.Country, countries.Sum(population.Population) from #A.entities() countries inner join #B.entities() cities on countries.Country = cities.Country inner join #C.entities() population on cities.City = population.City group by cities.Country";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("Poland", "Krakow"),
                        new BasicEntity("Germany", "Berlin")
                    }
                },
                {
                    "#B", new[]
                    {
                        new BasicEntity("Poland", "Krakow"),
                        new BasicEntity("Poland", "Wroclaw"),
                        new BasicEntity("Poland", "Warszawa"),
                        new BasicEntity("Poland", "Gdansk"),
                        new BasicEntity("Germany", "Berlin")
                    }
                },
                {
                    "#C", new[]
                    {
                        new BasicEntity {City = "Krakow", Population = 400},
                        new BasicEntity {City = "Wroclaw", Population = 500},
                        new BasicEntity {City = "Warszawa", Population = 1000},
                        new BasicEntity {City = "Gdansk", Population = 200},
                        new BasicEntity {City = "Berlin", Population = 400}
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(2, table.Columns.Count());

            Assert.AreEqual("cities.Country", table.Columns.ElementAt(0).ColumnName);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
            Assert.AreEqual(0, table.Columns.ElementAt(0).ColumnIndex);

            Assert.AreEqual("Sum(population.Population)", table.Columns.ElementAt(1).ColumnName);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(1).ColumnType);
            Assert.AreEqual(1, table.Columns.ElementAt(1).ColumnIndex);

            Assert.AreEqual("Poland", table[0][0]);
            Assert.AreEqual(2100m, table[0][1]);

            Assert.AreEqual("Germany", table[1][0]);
            Assert.AreEqual(400m, table[1][1]);
        }

        [TestMethod]
        public void JoinWithExceptTest()
        {
            var query =
       @"
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
                    "#A", new[]
                    {
                        new BasicEntity("Poland", "Krakow"),
                        new BasicEntity("Germany", "Berlin")
                    }
                },
                {
                    "#B", new[]
                    {
                        new BasicEntity("Poland", "Krakow"),
                        new BasicEntity("Poland", "Wroclaw"),
                        new BasicEntity("Poland", "Warszawa"),
                        new BasicEntity("Poland", "Gdansk"),
                        new BasicEntity("Germany", "Berlin")
                    }
                },
                {
                    "#C", new[]
                    {
                        new BasicEntity {City = "Krakow", Population = 400},
                        new BasicEntity {City = "Wroclaw", Population = 500},
                        new BasicEntity {City = "Warszawa", Population = 1000},
                        new BasicEntity {City = "Gdansk", Population = 200},
                        new BasicEntity {City = "Berlin", Population = 400}
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

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
                    "#A", new[]
                    {
                        new BasicEntity("Poland", "Krakow"),
                        new BasicEntity("Germany", "Berlin")
                    }
                },
                {
                    "#B", new[]
                    {
                        new BasicEntity("Poland", "Krakow"),
                        new BasicEntity("Poland", "Wroclaw"),
                        new BasicEntity("Poland", "Warszawa"),
                        new BasicEntity("Poland", "Gdansk"),
                        new BasicEntity("Germany", "Berlin")
                    }
                },
                {
                    "#C", new[]
                    {
                        new BasicEntity {City = "Krakow", Population = 400},
                        new BasicEntity {City = "Wroclaw", Population = 500},
                        new BasicEntity {City = "Warszawa", Population = 1000},
                        new BasicEntity {City = "Gdansk", Population = 200},
                        new BasicEntity {City = "Berlin", Population = 400}
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

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

            Assert.AreEqual("Poland", table[0][0]);
            Assert.AreEqual("Krakow", table[0][1]);
            Assert.AreEqual(400m, table[0][2]);

            Assert.AreEqual("Poland", table[1][0]);
            Assert.AreEqual("Wroclaw", table[1][1]);
            Assert.AreEqual(500m, table[1][2]);

            Assert.AreEqual("Poland", table[2][0]);
            Assert.AreEqual("Warszawa", table[2][1]);
            Assert.AreEqual(1000m, table[2][2]);

            Assert.AreEqual("Poland", table[3][0]);
            Assert.AreEqual("Gdansk", table[3][1]);
            Assert.AreEqual(200m, table[3][2]);

            Assert.AreEqual("Germany", table[4][0]);
            Assert.AreEqual("Berlin", table[4][1]);
            Assert.AreEqual(400m, table[4][2]);
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
                    "#A", new[]
                    {
                        new BasicEntity("Poland", "Krakow"),
                        new BasicEntity("Germany", "Berlin")
                    }
                },
                {
                    "#B", new[]
                    {
                        new BasicEntity("Poland", "Krakow"),
                        new BasicEntity("Poland", "Wroclaw"),
                        new BasicEntity("Poland", "Warszawa"),
                        new BasicEntity("Poland", "Gdansk"),
                        new BasicEntity("Germany", "Berlin")
                    }
                },
                {
                    "#C", new[]
                    {
                        new BasicEntity {City = "Krakow", Population = 400},
                        new BasicEntity {City = "Wroclaw", Population = 500},
                        new BasicEntity {City = "Warszawa", Population = 1000},
                        new BasicEntity {City = "Gdansk", Population = 200},
                        new BasicEntity {City = "Berlin", Population = 400}
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

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

            Assert.AreEqual(10, table.Count);

            Assert.AreEqual("Poland", table[0][0]);
            Assert.AreEqual("Krakow", table[0][1]);
            Assert.AreEqual(400m, table[0][2]);

            Assert.AreEqual("Poland", table[1][0]);
            Assert.AreEqual("Wroclaw", table[1][1]);
            Assert.AreEqual(500m, table[1][2]);

            Assert.AreEqual("Poland", table[2][0]);
            Assert.AreEqual("Warszawa", table[2][1]);
            Assert.AreEqual(1000m, table[2][2]);

            Assert.AreEqual("Poland", table[3][0]);
            Assert.AreEqual("Gdansk", table[3][1]);
            Assert.AreEqual(200m, table[3][2]);

            Assert.AreEqual("Germany", table[4][0]);
            Assert.AreEqual("Berlin", table[4][1]);
            Assert.AreEqual(400m, table[4][2]);

            Assert.AreEqual("Poland", table[5][0]);
            Assert.AreEqual("Krakow", table[5][1]);
            Assert.AreEqual(400m, table[5][2]);

            Assert.AreEqual("Poland", table[6][0]);
            Assert.AreEqual("Wroclaw", table[6][1]);
            Assert.AreEqual(500m, table[6][2]);

            Assert.AreEqual("Poland", table[7][0]);
            Assert.AreEqual("Warszawa", table[7][1]);
            Assert.AreEqual(1000m, table[7][2]);

            Assert.AreEqual("Poland", table[8][0]);
            Assert.AreEqual("Gdansk", table[8][1]);
            Assert.AreEqual(200m, table[8][2]);

            Assert.AreEqual("Germany", table[9][0]);
            Assert.AreEqual("Berlin", table[9][1]);
            Assert.AreEqual(400m, table[9][2]);
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
                    "#A", new[]
                    {
                        new BasicEntity("Poland", "Krakow") {Id = 0},
                        new BasicEntity("Germany", "Berlin") {Id = 1},
                        new BasicEntity("Russia", "Moscow") {Id = 2}
                    }
                },
                {
                    "#B", new[]
                    {
                        new BasicEntity("Poland", "Krakow") {Id = 0},
                        new BasicEntity("Poland", "Wroclaw") {Id = 1},
                        new BasicEntity("Poland", "Warszawa") {Id = 2},
                        new BasicEntity("Poland", "Gdansk") {Id = 3},
                        new BasicEntity("Germany", "Berlin") {Id = 4}
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(5, table.Count);
            Assert.AreEqual(2, table.Columns.Count());

            Assert.AreEqual(0, table[0][0]);
            Assert.AreEqual(0, table[0][1]);

            Assert.AreEqual(0, table[1][0]);
            Assert.AreEqual(1, table[1][1]);

            Assert.AreEqual(0, table[2][0]);
            Assert.AreEqual(2, table[2][1]);

            Assert.AreEqual(0, table[3][0]);
            Assert.AreEqual(3, table[3][1]);

            Assert.AreEqual(1, table[4][0]);
            Assert.AreEqual(4, table[4][1]);
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
select p.Id, x.Id from p inner join x on p.Country = x.Country";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("Poland", "Krakow") {Id = 0},
                        new BasicEntity("Germany", "Berlin") {Id = 1},
                        new BasicEntity("Russia", "Moscow") {Id = 2}
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(3, table.Count);
            Assert.AreEqual(2, table.Columns.Count());

            Assert.AreEqual(0, table[0][0]);
            Assert.AreEqual(0, table[0][1]);

            Assert.AreEqual(1, table[1][0]);
            Assert.AreEqual(1, table[1][1]);

            Assert.AreEqual(2, table[2][0]);
            Assert.AreEqual(2, table[2][1]);
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
                    "#A", new[]
                    {
                        new BasicEntity("Poland", "Krakow") {Id = 0},
                        new BasicEntity("Germany", "Berlin") {Id = 1},
                        new BasicEntity("Russia", "Moscow") {Id = 2}
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

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
select p.Country, Count(p.Country) from p inner join x on p.Country = x.Country group by p.Country having Count(p.Country) > 1
";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("Poland", "Krakow") {Id = 0},
                        new BasicEntity("Poland", "Krakow") {Id = 0},
                        new BasicEntity("Germany", "Berlin") {Id = 1},
                        new BasicEntity("Russia", "Moscow") {Id = 2}
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

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
                    "#A", new[]
                    {
                        new BasicEntity("xX") { Id = 1 }
                    }
                },
                {
                    "#B",
                    new BasicEntity[]
                    {

                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

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
            Assert.AreEqual((int?)null, table[0][1]);
        }

        [TestMethod]
        public void MultipleLeftJoinTest()
        {
            var query = "select a.Id, b.Id, c.Id from #A.entities() a left outer join #B.entities() b on a.Id = b.Id left outer join #B.entities() c on b.Id = c.Id";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("xX") { Id = 1 }
                    }
                },
                {
                    "#B",
                    new BasicEntity[]
                    {

                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

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
            Assert.AreEqual((int?)null, table[0][1]);
            Assert.AreEqual((int?)null, table[0][2]);
        }

        [TestMethod]
        public void MultipleLeftJoinWithCTriesMatchBButFailTest()
        {
            var query = "select a.Id, b.Id, c.Id from #A.entities() a left outer join #B.entities() b on a.Id = b.Id left outer join #C.entities() c on b.Id = c.Id";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("xX") { Id = 1 }
                    }
                },
                {
                    "#B",
                    new BasicEntity[]
                    {

                    }
                },
                {
                    "#C",
                    new BasicEntity[]
                    {
                        new("xX") { Id = 1 }
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

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
            Assert.AreEqual((int?)null, table[0][1]);
            Assert.AreEqual((int?)null, table[0][2]);
        }

        [TestMethod]
        public void MultipleLeftJoinWithCTriesMatchBAndSucceedTest()
        {
            var query = "select a.Id, b.Id, c.Id from #A.entities() a left outer join #B.entities() b on a.Id = b.Id left outer join #C.entities() c on b.Id = c.Id";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("xX") { Id = 1 },
                        new BasicEntity("xX") { Id = 2 }
                    }
                },
                {
                    "#B",
                    new BasicEntity[]
                    {
                        new("xX") { Id = 1 }
                    }
                },
                {
                    "#C",
                    new BasicEntity[]
                    {
                        new("xX") { Id = 1 }
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

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

            Assert.AreEqual(1, table[0][0]);
            Assert.AreEqual(1, table[0][1]);
            Assert.AreEqual(1, table[0][2]);

            Assert.AreEqual(2, table[1][0]);
            Assert.AreEqual((int?)null, table[1][1]);
            Assert.AreEqual((int?)null, table[1][2]);
        }

        [TestMethod]
        public void SimpleRightJoinTest()
        {
            var query = "select a.Id, b.Id from #A.entities() a right outer join #B.entities() b on a.Id = b.Id";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new BasicEntity[]
                    {
                    }
                },
                {
                    "#B",
                    new BasicEntity[] {
                        new("xX") { Id = 1 }
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

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

            Assert.AreEqual((int?)null, table[0][0]);
            Assert.AreEqual(1, table[0][1]);
        }

        [TestMethod]
        public void MultipleRightJoinWithCTriesMatchBAndSucceedForASingleTest()
        {
            var query = "select a.Id, b.Id, c.Id from #A.entities() a right outer join #B.entities() b on a.Id = b.Id right outer join #C.entities() c on b.Id = c.Id";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("xX") { Id = 1 }
                    }
                },
                {
                    "#B",
                    new BasicEntity[]
                    {
                        new("xX") { Id = 1 }
                    }
                },
                {
                    "#C",
                    new BasicEntity[]
                    {
                        new("xX") { Id = 1 },
                        new("xX") { Id = 2 }
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

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

            Assert.AreEqual(1, table[0][0]);
            Assert.AreEqual(1, table[0][1]);
            Assert.AreEqual(1, table[0][2]);

            Assert.AreEqual((int?)null, table[1][0]);
            Assert.AreEqual((int?)null, table[1][1]);
            Assert.AreEqual(2, table[1][2]);
        }

        [TestMethod]
        public void RightOuterJoinPassMethodContextTest()
        {

            var query = "select a.ToDecimal(a.Id), b.ToDecimal(b.Id), c.ToDecimal(c.Id) from #A.entities() a right outer join #B.entities() b on 1 = 1 right outer join #C.entities() c on 1 = 1";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("xX") { Id = 1 }
                    }
                },
                {
                    "#B",
                    new BasicEntity[]
                    {
                        new("xX") { Id = 2 }
                    }
                },
                {
                    "#C",
                    new BasicEntity[]
                    {
                        new("xX") { Id = 3 }
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual(1m, table[0][0]);
            Assert.AreEqual(2m, table[0][1]);
            Assert.AreEqual(3m, table[0][2]);
        }

        [TestMethod]
        public void LeftOuterJoinPassMethodContextTest()
        {

            var query = "select a.ToDecimal(a.Id), b.ToDecimal(b.Id), c.ToDecimal(c.Id) from #A.entities() a left outer join #B.entities() b on 1 = 1 left outer join #C.entities() c on 1 = 1";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("xX") { Id = 1 }
                    }
                },
                {
                    "#B",
                    new BasicEntity[]
                    {
                        new("xX") { Id = 2 }
                    }
                },
                {
                    "#C",
                    new BasicEntity[]
                    {
                        new("xX") { Id = 3 }
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual(1m, table[0][0]);
            Assert.AreEqual(2m, table[0][1]);
            Assert.AreEqual(3m, table[0][2]);
        }

        [TestMethod]
        public void LeftOuterRightOuterJoinPassMethodContextTest()
        {

            var query = "select a.ToDecimal(a.Id), b.ToDecimal(b.Id), c.ToDecimal(c.Id) from #A.entities() a left outer join #B.entities() b on 1 = 1 right outer join #C.entities() c on 1 = 1";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("xX") { Id = 1 }
                    }
                },
                {
                    "#B",
                    new BasicEntity[]
                    {
                        new("xX") { Id = 2 }
                    }
                },
                {
                    "#C",
                    new BasicEntity[]
                    {
                        new("xX") { Id = 3 }
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual(1m, table[0][0]);
            Assert.AreEqual(2m, table[0][1]);
            Assert.AreEqual(3m, table[0][2]);
        }

        [TestMethod]
        public void RightOuterLeftOuterJoinPassMethodContextTest()
        {

            var query = "select a.ToDecimal(a.Id), b.ToDecimal(b.Id), c.ToDecimal(c.Id) from #A.entities() a right outer join #B.entities() b on 1 = 1 left outer join #C.entities() c on 1 = 1";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("xX") { Id = 1 }
                    }
                },
                {
                    "#B",
                    new BasicEntity[]
                    {
                        new("xX") { Id = 2 }
                    }
                },
                {
                    "#C",
                    new BasicEntity[]
                    {
                        new("xX") { Id = 3 }
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual(1m, table[0][0]);
            Assert.AreEqual(2m, table[0][1]);
            Assert.AreEqual(3m, table[0][2]);
        }

        [TestMethod]
        public void InnerJoinJoinPassMethodContextTest()
        {

            var query = "select a.ToDecimal(a.Id), b.ToDecimal(b.Id), c.ToDecimal(c.Id) from #A.entities() a inner join #B.entities() b on 1 = 1 inner join #C.entities() c on 1 = 1";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("xX") { Id = 1 }
                    }
                },
                {
                    "#B",
                    new BasicEntity[]
                    {
                        new("xX") { Id = 2 }
                    }
                },
                {
                    "#C",
                    new BasicEntity[]
                    {
                        new("xX") { Id = 3 }
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual(1m, table[0][0]);
            Assert.AreEqual(2m, table[0][1]);
            Assert.AreEqual(3m, table[0][2]);
        }
    }
}

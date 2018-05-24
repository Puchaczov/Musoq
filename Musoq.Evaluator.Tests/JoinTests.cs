using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema;

namespace Musoq.Evaluator.Tests
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

            Assert.AreEqual("countries.Country", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
            Assert.AreEqual(0, table.Columns.ElementAt(0).ColumnOrder);

            Assert.AreEqual("cities.City", table.Columns.ElementAt(1).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
            Assert.AreEqual(1, table.Columns.ElementAt(1).ColumnOrder);

            Assert.AreEqual("population.Population", table.Columns.ElementAt(2).Name);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(2).ColumnType);
            Assert.AreEqual(2, table.Columns.ElementAt(2).ColumnOrder);

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

            Assert.AreEqual("cities.Country", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
            Assert.AreEqual(0, table.Columns.ElementAt(0).ColumnOrder);

            Assert.AreEqual("Sum(population.Population)", table.Columns.ElementAt(1).Name);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(1).ColumnType);
            Assert.AreEqual(1, table.Columns.ElementAt(1).ColumnOrder);

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

            Assert.AreEqual("countries.Country", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
            Assert.AreEqual(0, table.Columns.ElementAt(0).ColumnOrder);

            Assert.AreEqual("cities.City", table.Columns.ElementAt(1).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
            Assert.AreEqual(1, table.Columns.ElementAt(1).ColumnOrder);

            Assert.AreEqual("population.Population", table.Columns.ElementAt(2).Name);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(2).ColumnType);
            Assert.AreEqual(2, table.Columns.ElementAt(2).ColumnOrder);

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

            Assert.AreEqual("countries.Country", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
            Assert.AreEqual(0, table.Columns.ElementAt(0).ColumnOrder);

            Assert.AreEqual("cities.City", table.Columns.ElementAt(1).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
            Assert.AreEqual(1, table.Columns.ElementAt(1).ColumnOrder);

            Assert.AreEqual("population.Population", table.Columns.ElementAt(2).Name);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(2).ColumnType);
            Assert.AreEqual(2, table.Columns.ElementAt(2).ColumnOrder);

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

            Assert.AreEqual("countries.Country", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
            Assert.AreEqual(0, table.Columns.ElementAt(0).ColumnOrder);

            Assert.AreEqual("cities.City", table.Columns.ElementAt(1).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
            Assert.AreEqual(1, table.Columns.ElementAt(1).ColumnOrder);

            Assert.AreEqual("population.Population", table.Columns.ElementAt(2).Name);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(2).ColumnType);
            Assert.AreEqual(2, table.Columns.ElementAt(2).ColumnOrder);

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

        [Ignore]
        [TestMethod]
        public void SimpleLeftJoinTest()
        {
            var query = "select a.Id from #A.x1() a left outer join #B.x2() b on a.Id = b.Id";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("xX")
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();
        }
    }
}

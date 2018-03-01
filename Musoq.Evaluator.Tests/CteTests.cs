using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema;

namespace Musoq.Evaluator.Tests
{
    [TestClass]
    public class CteTests : TestBase
    {
        [TestMethod]
        public void SimpleCteTest()
        {
            var query = "with p as (select City, Country from #A.entities()) select Country, City from p";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("WARSAW", "POLAND", 500),
                        new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                        new BasicEntity("KATOWICE", "POLAND", 250),
                        new BasicEntity("BERLIN", "GERMANY", 250),
                        new BasicEntity("MUNICH", "GERMANY", 350)
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(2, table.Columns.Count());
            Assert.AreEqual("Country", table.Columns.ElementAt(0).Name);
            Assert.AreEqual("City", table.Columns.ElementAt(1).Name);

            Assert.AreEqual(5, table.Count());

            Assert.AreEqual("POLAND", table[0].Values[0]);
            Assert.AreEqual("WARSAW", table[0].Values[1]);

            Assert.AreEqual("POLAND", table[1].Values[0]);
            Assert.AreEqual("CZESTOCHOWA", table[1].Values[1]);

            Assert.AreEqual("POLAND", table[2].Values[0]);
            Assert.AreEqual("KATOWICE", table[2].Values[1]);

            Assert.AreEqual("GERMANY", table[3].Values[0]);
            Assert.AreEqual("BERLIN", table[3].Values[1]);

            Assert.AreEqual("GERMANY", table[4].Values[0]);
            Assert.AreEqual("MUNICH", table[4].Values[1]);
        }
        
        [TestMethod]
        public void SimpleCteWithStarTest()
        {
            var query = "with p as (select City, Country from #A.entities()) select * from p";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("WARSAW", "POLAND", 500),
                        new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                        new BasicEntity("KATOWICE", "POLAND", 250),
                        new BasicEntity("BERLIN", "GERMANY", 250),
                        new BasicEntity("MUNICH", "GERMANY", 350)
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(2, table.Columns.Count());
            Assert.AreEqual("City", table.Columns.ElementAt(0).Name);
            Assert.AreEqual("Country", table.Columns.ElementAt(1).Name);

            Assert.AreEqual(5, table.Count());

            Assert.AreEqual("WARSAW", table[0].Values[0]);
            Assert.AreEqual("POLAND", table[0].Values[1]);

            Assert.AreEqual("CZESTOCHOWA", table[1].Values[0]);
            Assert.AreEqual("POLAND", table[1].Values[1]);

            Assert.AreEqual("KATOWICE", table[2].Values[0]);
            Assert.AreEqual("POLAND", table[2].Values[1]);

            Assert.AreEqual("BERLIN", table[3].Values[0]);
            Assert.AreEqual("GERMANY", table[3].Values[1]);

            Assert.AreEqual("MUNICH", table[4].Values[0]);
            Assert.AreEqual("GERMANY", table[4].Values[1]);
        }

        [TestMethod]
        public void SimpleCteWithGroupingTest()
        {
            var query = "with p as (select Country, Sum(Population) from #A.entities() group by Country) select * from p";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("WARSAW", "POLAND", 500),
                        new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                        new BasicEntity("KATOWICE", "POLAND", 250),
                        new BasicEntity("BERLIN", "GERMANY", 250),
                        new BasicEntity("MUNICH", "GERMANY", 350)
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(2, table.Columns.Count());
            Assert.AreEqual("Country", table.Columns.ElementAt(0).Name);
            Assert.AreEqual("Sum(Population)", table.Columns.ElementAt(1).Name);

            Assert.AreEqual(2, table.Count());

            Assert.AreEqual("POLAND", table[0].Values[0]);
            Assert.AreEqual(1150m, table[0].Values[1]);

            Assert.AreEqual("GERMANY", table[1].Values[0]);
            Assert.AreEqual(600m, table[1].Values[1]);
        }


        [TestMethod]
        public void SimpleCteWithGrouping2Test()
        {
            var query = "with p as (select Population, Country from #A.entities()) select Country, Sum(Population) from p group by Country";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("WARSAW", "POLAND", 500),
                        new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                        new BasicEntity("KATOWICE", "POLAND", 250),
                        new BasicEntity("BERLIN", "GERMANY", 250),
                        new BasicEntity("MUNICH", "GERMANY", 350)
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(2, table.Columns.Count());
            Assert.AreEqual("Country", table.Columns.ElementAt(0).Name);
            Assert.AreEqual("Sum(Population)", table.Columns.ElementAt(1).Name);

            Assert.AreEqual(2, table.Count());

            Assert.AreEqual("POLAND", table[0].Values[0]);
            Assert.AreEqual(1150m, table[0].Values[1]);

            Assert.AreEqual("GERMANY", table[1].Values[0]);
            Assert.AreEqual(600m, table[1].Values[1]);
        }

        [TestMethod]
        public void SimpleCteWithUnionTest()
        {
            var query = "with p as (select City, Country from #A.entities() union (Country, City) select City, Country from #B.entities()) select City, Country from p";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    new[]
                    {
                        new BasicEntity("WARSAW", "POLAND", 500),
                        new BasicEntity("CZESTOCHOWA", "POLAND", 400)
                    }
                },
                {
                    "#B",
                    new[]
                    {
                        new BasicEntity("KATOWICE", "POLAND", 250),
                        new BasicEntity("BERLIN", "GERMANY", 250),
                        new BasicEntity("MUNICH", "GERMANY", 350)
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(2, table.Columns.Count());
            Assert.AreEqual("City", table.Columns.ElementAt(0).Name);
            Assert.AreEqual("Country", table.Columns.ElementAt(1).Name);

            Assert.AreEqual(5, table.Count());

            Assert.AreEqual("WARSAW", table[0].Values[0]);
            Assert.AreEqual("POLAND", table[0].Values[1]);

            Assert.AreEqual("CZESTOCHOWA", table[1].Values[0]);
            Assert.AreEqual("POLAND", table[1].Values[1]);

            Assert.AreEqual("KATOWICE", table[2].Values[0]);
            Assert.AreEqual("POLAND", table[2].Values[1]);

            Assert.AreEqual("BERLIN", table[3].Values[0]);
            Assert.AreEqual("GERMANY", table[3].Values[1]);

            Assert.AreEqual("MUNICH", table[4].Values[0]);
            Assert.AreEqual("GERMANY", table[4].Values[1]);
        }

        [TestMethod]
        public void SimpleCteWithUnionAllTest()
        {
            var query = "with p as (select City, Country from #A.entities() union all (Country) select City, Country from #B.entities()) select City, Country from p";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    new[]
                    {
                        new BasicEntity("WARSAW", "POLAND", 500),
                        new BasicEntity("CZESTOCHOWA", "POLAND", 400)
                    }
                },
                {
                    "#B",
                    new[]
                    {
                        new BasicEntity("KATOWICE", "POLAND", 250),
                        new BasicEntity("BERLIN", "GERMANY", 250),
                        new BasicEntity("MUNICH", "GERMANY", 350)
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(2, table.Columns.Count());
            Assert.AreEqual("City", table.Columns.ElementAt(0).Name);
            Assert.AreEqual("Country", table.Columns.ElementAt(1).Name);

            Assert.AreEqual(5, table.Count());

            Assert.AreEqual("WARSAW", table[0].Values[0]);
            Assert.AreEqual("POLAND", table[0].Values[1]);

            Assert.AreEqual("CZESTOCHOWA", table[1].Values[0]);
            Assert.AreEqual("POLAND", table[1].Values[1]);

            Assert.AreEqual("KATOWICE", table[2].Values[0]);
            Assert.AreEqual("POLAND", table[2].Values[1]);

            Assert.AreEqual("BERLIN", table[3].Values[0]);
            Assert.AreEqual("GERMANY", table[3].Values[1]);

            Assert.AreEqual("MUNICH", table[4].Values[0]);
            Assert.AreEqual("GERMANY", table[4].Values[1]);
        }

        [TestMethod]
        public void SimpleCteWithExceptTest()
        {
            var query = "with p as (select City, Country from #A.entities() except (Country) select City, Country from #B.entities()) select City, Country from p";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    new[]
                    {
                        new BasicEntity("HELSINKI", "FINLAND", 500),
                        new BasicEntity("WARSAW", "POLAND", 500),
                        new BasicEntity("CZESTOCHOWA", "POLAND", 400)
                    }
                },
                {
                    "#B",
                    new[]
                    {
                        new BasicEntity("KATOWICE", "POLAND", 250),
                        new BasicEntity("BERLIN", "GERMANY", 250),
                        new BasicEntity("MUNICH", "GERMANY", 350)
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(2, table.Columns.Count());
            Assert.AreEqual("City", table.Columns.ElementAt(0).Name);
            Assert.AreEqual("Country", table.Columns.ElementAt(1).Name);

            Assert.AreEqual(1, table.Count());

            Assert.AreEqual("HELSINKI", table[0].Values[0]);
            Assert.AreEqual("FINLAND", table[0].Values[1]);
        }

        [TestMethod]
        public void SimpleCteWithIntersectTest()
        {
            var query = "with p as (select City, Country from #A.entities() intersect (Country, City) select City, Country from #B.entities()) select City, Country from p";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    new[]
                    {
                        new BasicEntity("HELSINKI", "FINLAND", 500),
                        new BasicEntity("WARSAW", "POLAND", 500),
                        new BasicEntity("CZESTOCHOWA", "POLAND", 400)
                    }
                },
                {
                    "#B",
                    new[]
                    {
                        new BasicEntity("WARSAW", "POLAND", 250),
                        new BasicEntity("BERLIN", "GERMANY", 250),
                        new BasicEntity("MUNICH", "GERMANY", 350),
                        new BasicEntity("HELSINKI", "FINLAND", 500)
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(2, table.Columns.Count());
            Assert.AreEqual("City", table.Columns.ElementAt(0).Name);
            Assert.AreEqual("Country", table.Columns.ElementAt(1).Name);

            Assert.AreEqual(2, table.Count());

            Assert.AreEqual("HELSINKI", table[0].Values[0]);
            Assert.AreEqual("FINLAND", table[0].Values[1]);

            Assert.AreEqual("WARSAW", table[1].Values[0]);
            Assert.AreEqual("POLAND", table[1].Values[1]);
        }
    }
}

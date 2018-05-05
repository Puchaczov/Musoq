using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema;
using Musoq.Plugins;

namespace Musoq.Evaluator.Tests
{
    [TestClass]
    public class GroupByTests : TestBase
    {
        [TestMethod]
        public void GroupByWithParentSumTest()
        {
            var query = @"select SumIncome(1, Money), SumOutcome(1, Money) from #A.Entities() group by Month, City";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[]
                {
                    new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(-200))
                }}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(3, table.Count);
            Assert.AreEqual(Convert.ToDecimal(700), table[0].Values[0]);
            Assert.AreEqual(Convert.ToDecimal(-200), table[0].Values[1]);
            Assert.AreEqual(Convert.ToDecimal(700), table[1].Values[0]);
            Assert.AreEqual(Convert.ToDecimal(-200), table[1].Values[1]);
            Assert.AreEqual(Convert.ToDecimal(700), table[2].Values[0]);
            Assert.AreEqual(Convert.ToDecimal(-200), table[2].Values[1]);
        }

        [TestMethod]
        public void GroupBySubtractGroupsTest()
        {
            var query = @"select SumIncome(Money), SumOutcome(Money), SumIncome(Money) - Abs(SumOutcome(Money)) from #A.Entities() group by Month";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("jan", Convert.ToDecimal(400)), new BasicEntity("jan", Convert.ToDecimal(300)), new BasicEntity("jan", Convert.ToDecimal(-200)) }}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual(Convert.ToDecimal(700), table[0].Values[0]);
            Assert.AreEqual(Convert.ToDecimal(-200), table[0].Values[1]);
            Assert.AreEqual(Convert.ToDecimal(500), table[0].Values[2]);
        }

        [TestMethod]
        public void SimpleGroupByTest()
        {
            var query = @"select Name, Count(Name) from #A.Entities() group by Name having Count(Name) >= 2";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("ABBA"),
                        new BasicEntity("ABBA"),
                        new BasicEntity("BABBA"),
                        new BasicEntity("ABBA"),
                        new BasicEntity("BABBA"),
                        new BasicEntity("CECCA"),
                        new BasicEntity("ABBA")
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(2, table.Columns.Count());
            Assert.AreEqual("Name", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
            Assert.AreEqual("Count(Name)", table.Columns.ElementAt(1).Name);
            Assert.AreEqual(typeof(int), table.Columns.ElementAt(1).ColumnType);

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual("ABBA", table[0].Values[0]);
            Assert.AreEqual(Convert.ToInt32(4), table[0].Values[1]);
            Assert.AreEqual("BABBA", table[1].Values[0]);
            Assert.AreEqual(Convert.ToInt32(2), table[1].Values[1]);
        }


        [TestMethod]
        public void SimpleRowNumberForGroupByTest()
        {
            var query = @"select Name, Count(Name), RowNumber() from #A.Entities() group by Name";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("ABBA"),
                        new BasicEntity("ABBA"),
                        new BasicEntity("BABBA"),
                        new BasicEntity("ABBA"),
                        new BasicEntity("BABBA"),
                        new BasicEntity("CECCA"),
                        new BasicEntity("ABBA")
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(3, table.Columns.Count());
            Assert.AreEqual("Name", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
            Assert.AreEqual("Count(Name)", table.Columns.ElementAt(1).Name);
            Assert.AreEqual(typeof(int), table.Columns.ElementAt(1).ColumnType);
            Assert.AreEqual("RowNumber()", table.Columns.ElementAt(2).Name);
            Assert.AreEqual(typeof(int), table.Columns.ElementAt(2).ColumnType);

            Assert.AreEqual(3, table.Count);
            Assert.AreEqual("ABBA", table[0].Values[0]);
            Assert.AreEqual(4, table[0].Values[1]);
            Assert.AreEqual(1, table[0].Values[2]);

            Assert.AreEqual("BABBA", table[1].Values[0]);
            Assert.AreEqual(2, table[1].Values[1]);
            Assert.AreEqual(2, table[1].Values[2]);

            Assert.AreEqual("CECCA", table[2].Values[0]);
            Assert.AreEqual(1, table[2].Values[1]);
            Assert.AreEqual(3, table[2].Values[2]);
        }

        [TestMethod]
        public void SimpleGroupByWithSkipTest()
        {
            var query = @"select Name, Count(Name) from #A.Entities() group by Name skip 2";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("ABBA"),
                        new BasicEntity("ABBA"),
                        new BasicEntity("BABBA"),
                        new BasicEntity("ABBA"),
                        new BasicEntity("BABBA"),
                        new BasicEntity("CECCA"),
                        new BasicEntity("ABBA")
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(2, table.Columns.Count());
            Assert.AreEqual("Name", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
            Assert.AreEqual("Count(Name)", table.Columns.ElementAt(1).Name);
            Assert.AreEqual(typeof(int), table.Columns.ElementAt(1).ColumnType);

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual("CECCA", table[0].Values[0]);
            Assert.AreEqual(Convert.ToInt32(1), table[0].Values[1]);
        }

        [TestMethod]
        public void SimpleGroupByWithTakeTest()
        {
            var query = @"select Name, Count(Name) from #A.Entities() group by Name take 2";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("ABBA"),
                        new BasicEntity("ABBA"),
                        new BasicEntity("BABBA"),
                        new BasicEntity("ABBA"),
                        new BasicEntity("BABBA"),
                        new BasicEntity("CECCA"),
                        new BasicEntity("ABBA")
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(2, table.Columns.Count());
            Assert.AreEqual("Name", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
            Assert.AreEqual("Count(Name)", table.Columns.ElementAt(1).Name);
            Assert.AreEqual(typeof(int), table.Columns.ElementAt(1).ColumnType);

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual("ABBA", table[0].Values[0]);
            Assert.AreEqual(Convert.ToInt32(4), table[0].Values[1]);
            Assert.AreEqual("BABBA", table[1].Values[0]);
            Assert.AreEqual(Convert.ToInt32(2), table[1].Values[1]);
        }

        [TestMethod]
        public void SimpleGroupByWithSkipTakeTest()
        {
            var query = @"select Name, Count(Name) from #A.Entities() group by Name skip 2 take 1";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("ABBA"),
                        new BasicEntity("ABBA"),
                        new BasicEntity("BABBA"),
                        new BasicEntity("ABBA"),
                        new BasicEntity("BABBA"),
                        new BasicEntity("CECCA"),
                        new BasicEntity("ABBA")
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(2, table.Columns.Count());
            Assert.AreEqual("Name", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
            Assert.AreEqual("Count(Name)", table.Columns.ElementAt(1).Name);
            Assert.AreEqual(typeof(int), table.Columns.ElementAt(1).ColumnType);

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual("CECCA", table[0].Values[0]);
            Assert.AreEqual(Convert.ToInt32(1), table[0].Values[1]);
        }

        [TestMethod]
        public void GroupByWithValueTest()
        {
            var query = @"select Country, Sum(Population) from #A.Entities() group by Country";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("ABBA", 200),
                        new BasicEntity("ABBA", 500),
                        new BasicEntity("BABBA", 100),
                        new BasicEntity("ABBA", 10),
                        new BasicEntity("BABBA", 100),
                        new BasicEntity("CECCA", 1000)
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(2, table.Columns.Count());
            Assert.AreEqual("Country", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
            Assert.AreEqual("Sum(Population)", table.Columns.ElementAt(1).Name);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(1).ColumnType);

            Assert.AreEqual(3, table.Count);
            Assert.AreEqual("ABBA", table[0].Values[0]);
            Assert.AreEqual(Convert.ToDecimal(710), table[0].Values[1]);
            Assert.AreEqual("BABBA", table[1].Values[0]);
            Assert.AreEqual(Convert.ToDecimal(200), table[1].Values[1]);
            Assert.AreEqual("CECCA", table[2].Values[0]);
            Assert.AreEqual(Convert.ToDecimal(1000), table[2].Values[1]);
        }

        [TestMethod]
        public void GroupByMultipleColumnsTest()
        {
            var query = @"select Country, City, Count(Country), Count(City) from #A.Entities() group by Country, City";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("POLAND", "WARSAW"),
                        new BasicEntity("POLAND", "CZESTOCHOWA"),
                        new BasicEntity("UK", "LONDON"),
                        new BasicEntity("POLAND", "CZESTOCHOWA"),
                        new BasicEntity("UK", "MANCHESTER"),
                        new BasicEntity("ANGOLA", "LLL")
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(4, table.Columns.Count());
            Assert.AreEqual("Country", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
            Assert.AreEqual("City", table.Columns.ElementAt(1).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
            Assert.AreEqual("Count(Country)", table.Columns.ElementAt(2).Name);
            Assert.AreEqual(typeof(int), table.Columns.ElementAt(2).ColumnType);
            Assert.AreEqual("Count(City)", table.Columns.ElementAt(3).Name);
            Assert.AreEqual(typeof(int), table.Columns.ElementAt(3).ColumnType);

            Assert.AreEqual(5, table.Count);
            Assert.AreEqual("POLAND", table[0].Values[0]);
            Assert.AreEqual("WARSAW", table[0].Values[1]);
            Assert.AreEqual(Convert.ToInt32(1), table[0].Values[2]);
            Assert.AreEqual(Convert.ToInt32(1), table[0].Values[3]);
            Assert.AreEqual("POLAND", table[1].Values[0]);
            Assert.AreEqual("CZESTOCHOWA", table[1].Values[1]);
            Assert.AreEqual(Convert.ToInt32(2), table[1].Values[2]);
            Assert.AreEqual(Convert.ToInt32(2), table[1].Values[3]);
            Assert.AreEqual("UK", table[2].Values[0]);
            Assert.AreEqual("LONDON", table[2].Values[1]);
            Assert.AreEqual(Convert.ToInt32(1), table[2].Values[2]);
            Assert.AreEqual(Convert.ToInt32(1), table[2].Values[3]);
            Assert.AreEqual("UK", table[3].Values[0]);
            Assert.AreEqual("MANCHESTER", table[3].Values[1]);
            Assert.AreEqual(Convert.ToInt32(1), table[3].Values[2]);
            Assert.AreEqual(Convert.ToInt32(1), table[3].Values[3]);
            Assert.AreEqual("ANGOLA", table[4].Values[0]);
            Assert.AreEqual("LLL", table[4].Values[1]);
            Assert.AreEqual(Convert.ToInt32(1), table[4].Values[2]);
            Assert.AreEqual(Convert.ToInt32(1), table[4].Values[3]);
        }

        [TestMethod]
        public void GroupBySubstrTest()
        {
            var query =
                @"select Substr(Name, 0, 2), Count(Substr(Name, 0, 2)) from #A.Entities() group by Substr(Name, 0, 2)";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("AA:1"),
                        new BasicEntity("AA:2"),
                        new BasicEntity("AA:3"),
                        new BasicEntity("BB:1"),
                        new BasicEntity("BB:2"),
                        new BasicEntity("CC:1")
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(2, table.Columns.Count());
            Assert.AreEqual("Substr(Name, 0, 2)", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
            Assert.AreEqual("Count(Substr(Name, 0, 2))", table.Columns.ElementAt(1).Name);
            Assert.AreEqual(typeof(int), table.Columns.ElementAt(1).ColumnType);

            Assert.AreEqual(3, table.Count);

            Assert.AreEqual("AA", table[0].Values[0]);
            Assert.AreEqual(Convert.ToInt32(3), table[0].Values[1]);

            Assert.AreEqual("BB", table[1].Values[0]);
            Assert.AreEqual(Convert.ToInt32(2), table[1].Values[1]);

            Assert.AreEqual("CC", table[2].Values[0]);
            Assert.AreEqual(Convert.ToInt32(1), table[2].Values[1]);
        }

        [TestMethod]
        public void GroupByWithSelectedConstantModifiedByFunctionTest()
        {
            var query =
                @"select Name, Count(Name), Inc(10d), 1 from #A.Entities() group by Name having Count(Name) >= 2";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("ABBA"),
                        new BasicEntity("ABBA"),
                        new BasicEntity("BABBA"),
                        new BasicEntity("ABBA"),
                        new BasicEntity("BABBA"),
                        new BasicEntity("CECCA")
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(4, table.Columns.Count());
            Assert.AreEqual("Name", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
            Assert.AreEqual("Count(Name)", table.Columns.ElementAt(1).Name);
            Assert.AreEqual(typeof(int), table.Columns.ElementAt(1).ColumnType);
            Assert.AreEqual("Inc(10)", table.Columns.ElementAt(2).Name);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(2).ColumnType);
            Assert.AreEqual("1", table.Columns.ElementAt(3).Name);
            Assert.AreEqual(typeof(long), table.Columns.ElementAt(3).ColumnType);

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual("ABBA", table[0].Values[0]);
            Assert.AreEqual(Convert.ToInt32(3), table[0].Values[1]);
            Assert.AreEqual(Convert.ToDecimal(11), table[0].Values[2]);
            Assert.AreEqual(Convert.ToInt64(1), table[0].Values[3]);
            Assert.AreEqual("BABBA", table[1].Values[0]);
            Assert.AreEqual(Convert.ToInt32(2), table[1].Values[1]);
            Assert.AreEqual(Convert.ToDecimal(11), table[1].Values[2]);
            Assert.AreEqual(Convert.ToInt64(1), table[1].Values[3]);
        }

        [TestMethod]
        public void GroupByColumnSubstringTest()
        {
            var query = "select Country, Substr(City, IndexOf(City, ':')) as 'City', Count(City) as 'Count', Sum(Population) as 'Sum' from #A.Entities() group by Substr(City, IndexOf(City, ':')), Country";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("WARSAW:TARGOWEK", "POLAND", 500),
                        new BasicEntity("WARSAW:URSYNOW", "POLAND", 500),
                        new BasicEntity("KATOWICE:ZAWODZIE", "POLAND", 250)
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(4, table.Columns.Count());
            Assert.AreEqual("Country", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
            Assert.AreEqual("City", table.Columns.ElementAt(1).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
            Assert.AreEqual("Count", table.Columns.ElementAt(2).Name);
            Assert.AreEqual(typeof(int), table.Columns.ElementAt(2).ColumnType);
            Assert.AreEqual("Sum", table.Columns.ElementAt(3).Name);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(3).ColumnType);

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual("POLAND", table[0].Values[0]);
            Assert.AreEqual("WARSAW", table[0].Values[1]);
            Assert.AreEqual(Convert.ToInt32(2), table[0].Values[2]);
            Assert.AreEqual(Convert.ToDecimal(1000), table[0].Values[3]);
            Assert.AreEqual("POLAND", table[1].Values[0]);
            Assert.AreEqual("KATOWICE", table[1].Values[1]);
            Assert.AreEqual(Convert.ToInt32(1), table[1].Values[2]);
            Assert.AreEqual(Convert.ToDecimal(250), table[1].Values[3]);
        }

        [TestMethod]
        public void GroupByWithParentCountTest()
        {
            var query = "select Country, City as 'City', ParentCount(1), Count(City) as 'CountOfCities' from #A.Entities() group by Country, City";

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
            var table = vm.Run();

            Assert.AreEqual(4, table.Columns.Count());
            Assert.AreEqual("Country", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
            Assert.AreEqual("City", table.Columns.ElementAt(1).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
            Assert.AreEqual("ParentCount(1)", table.Columns.ElementAt(2).Name);
            Assert.AreEqual(typeof(int), table.Columns.ElementAt(2).ColumnType);
            Assert.AreEqual("CountOfCities", table.Columns.ElementAt(3).Name);
            Assert.AreEqual(typeof(int), table.Columns.ElementAt(3).ColumnType);

            Assert.AreEqual(5, table.Count);
            Assert.AreEqual("POLAND", table[0].Values[0]);
            Assert.AreEqual("WARSAW", table[0].Values[1]);
            Assert.AreEqual(Convert.ToInt32(3), table[0].Values[2]);
            Assert.AreEqual(Convert.ToInt32(1), table[0].Values[3]);

            Assert.AreEqual("POLAND", table[1].Values[0]);
            Assert.AreEqual("CZESTOCHOWA", table[1].Values[1]);
            Assert.AreEqual(Convert.ToInt32(3), table[1].Values[2]);
            Assert.AreEqual(Convert.ToInt32(1), table[1].Values[3]);

            Assert.AreEqual("POLAND", table[2].Values[0]);
            Assert.AreEqual("KATOWICE", table[2].Values[1]);
            Assert.AreEqual(Convert.ToInt32(3), table[2].Values[2]);
            Assert.AreEqual(Convert.ToInt32(1), table[2].Values[3]);

            Assert.AreEqual("GERMANY", table[3].Values[0]);
            Assert.AreEqual("BERLIN", table[3].Values[1]);
            Assert.AreEqual(Convert.ToInt32(2), table[3].Values[2]);
            Assert.AreEqual(Convert.ToInt32(1), table[3].Values[3]);

            Assert.AreEqual("GERMANY", table[4].Values[0]);
            Assert.AreEqual("MUNICH", table[4].Values[1]);
            Assert.AreEqual(Convert.ToInt32(2), table[4].Values[2]);
            Assert.AreEqual(Convert.ToInt32(1), table[4].Values[3]);
        }

        [TestMethod]
        public void GroupByWithWhereTest()
        {
            var query = "select Country, City as 'City', ParentCount(1), Count(City) as 'CountOfCities' from #A.Entities() where Country = 'POLAND' group by Country, City";

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
            var table = vm.Run();

            Assert.AreEqual(4, table.Columns.Count());
            Assert.AreEqual("Country", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
            Assert.AreEqual("City", table.Columns.ElementAt(1).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
            Assert.AreEqual("ParentCount(1)", table.Columns.ElementAt(2).Name);
            Assert.AreEqual(typeof(int), table.Columns.ElementAt(2).ColumnType);
            Assert.AreEqual("CountOfCities", table.Columns.ElementAt(3).Name);
            Assert.AreEqual(typeof(int), table.Columns.ElementAt(3).ColumnType);

            Assert.AreEqual(3, table.Count);
            Assert.AreEqual("POLAND", table[0].Values[0]);
            Assert.AreEqual("WARSAW", table[0].Values[1]);
            Assert.AreEqual(Convert.ToInt32(3), table[0].Values[2]);
            Assert.AreEqual(Convert.ToInt32(1), table[0].Values[3]);

            Assert.AreEqual("POLAND", table[1].Values[0]);
            Assert.AreEqual("CZESTOCHOWA", table[1].Values[1]);
            Assert.AreEqual(Convert.ToInt32(3), table[1].Values[2]);
            Assert.AreEqual(Convert.ToInt32(1), table[1].Values[3]);

            Assert.AreEqual("POLAND", table[2].Values[0]);
            Assert.AreEqual("KATOWICE", table[2].Values[1]);
            Assert.AreEqual(Convert.ToInt32(3), table[2].Values[2]);
            Assert.AreEqual(Convert.ToInt32(1), table[2].Values[3]);
        }

        [TestMethod]
        public void GroupWasNotListedTest()
        {
            var query = "select Count(Country) from #A.entities() group by Country";

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
            var table = vm.Run();

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual("Count(Country)", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(int), table.Columns.ElementAt(0).ColumnType);

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual(3, table[0].Values[0]);
            Assert.AreEqual(2, table[1].Values[0]);
        }

        [TestMethod]
        public void CountWithFakeGroupByTest()
        {
            var query = "select Count(Country) from #A.entities() group by 'fake'";

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
            var table = vm.Run();

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual("Count(Country)", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(int), table.Columns.ElementAt(0).ColumnType);

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual(5, table[0].Values[0]);
        }

        [TestMethod]
        public void CountWithoutGroupByTest()
        {
            var query = "select Count(Country), Sum(Population) from #A.entities()";

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
            var table = vm.Run();

            Assert.AreEqual(2, table.Columns.Count());
            Assert.AreEqual("Count(Country)", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(int), table.Columns.ElementAt(0).ColumnType);
            Assert.AreEqual("Sum(Population)", table.Columns.ElementAt(1).Name);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(1).ColumnType);

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual(5, table[0].Values[0]);
            Assert.AreEqual(Convert.ToDecimal(1750), table[0].Values[1]);
        }

        [Ignore("Not implemented feature - requires join grouping table with source.")]
        [TestMethod]
        public void CountWithRowNumberAndWithoutGroupByTest()
        {
            var query = "select Count(Country), RowNumber() from #A.entities()";

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
            var table = vm.Run();

            Assert.AreEqual(2, table.Columns.Count());
            Assert.AreEqual("Count(Country)", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(int), table.Columns.ElementAt(0).ColumnType);
            Assert.AreEqual("RowNumber()", table.Columns.ElementAt(1).Name);
            Assert.AreEqual(typeof(int), table.Columns.ElementAt(1).ColumnType);

            Assert.AreEqual(5, table.Count);

            Assert.AreEqual(5, table[0].Values[0]);
            Assert.AreEqual(1, table[0].Values[2]);

            Assert.AreEqual(5, table[0].Values[0]);
            Assert.AreEqual(2, table[0].Values[2]);

            Assert.AreEqual(5, table[0].Values[0]);
            Assert.AreEqual(3, table[0].Values[2]);

            Assert.AreEqual(5, table[0].Values[0]);
            Assert.AreEqual(4, table[0].Values[2]);

            Assert.AreEqual(5, table[0].Values[0]);
            Assert.AreEqual(5, table[0].Values[2]);
        }

        [Ignore("Not implemented feature - requires join grouping table with source.")]
        [TestMethod]
        public void SumWithoutGroupByAndWithNotGroupingField()
        {
            var query = "select City, Sum(Population) from #A.entities()";

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
            var table = vm.Run();

            Assert.AreEqual(2, table.Columns.Count());
            Assert.AreEqual("City", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
            Assert.AreEqual("Sum(Population)", table.Columns.ElementAt(1).Name);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(1).ColumnType);

            Assert.AreEqual(5, table.Count);

            Assert.AreEqual("WARSAW", table[0].Values[0]);
            Assert.AreEqual(1750m, table[0].Values[1]);

            Assert.AreEqual("CZESTOCHOWA", table[1].Values[0]);
            Assert.AreEqual(1750m, table[1].Values[1]);

            Assert.AreEqual("KATOWICE", table[2].Values[0]);
            Assert.AreEqual(1750m, table[2].Values[1]);

            Assert.AreEqual("BERLIN", table[3].Values[0]);
            Assert.AreEqual(1750m, table[3].Values[1]);

            Assert.AreEqual("MUNICH", table[4].Values[0]);
            Assert.AreEqual(1750m, table[4].Values[1]);
        }

        [TestMethod]
        public void GroupBySimpleAccessTest()
        {
            var query = @"select Month from #A.Entities() group by Month";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[]
                {
                    new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(-200))
                }}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual("jan", table[0].Values[0]);
        }

        [TestMethod]
        public void GroupByComplexObjectAccessTest()
        {
            var query = @"select Self.Month from #A.Entities() group by Self.Month";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[]
                {
                    new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(-200))
                }}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual("jan", table[0].Values[0]);
        }

        [TestMethod]
        public void GroupByComplexObjectAccessWithSumTest()
        {
            var query = @"select Self.Month, Sum(Self.Money) from #A.Entities() group by Self.Month";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[]
                {
                    new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(-200)),
                    new BasicEntity("cracow", "feb", Convert.ToDecimal(100))
                }}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(2, table.Count);

            Assert.AreEqual("jan", table[0].Values[0]);
            Assert.AreEqual(500m, table[0].Values[1]);

            Assert.AreEqual("feb", table[1].Values[0]);
            Assert.AreEqual(100m, table[1].Values[1]);
        }
    }
}

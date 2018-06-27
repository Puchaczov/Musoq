using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Core.Schema;

namespace Musoq.Evaluator.Tests.Core
{
    [TestClass]
    public class NullabilityTests : TestBase
    {
        [TestMethod]
        public void QueryWithWhereWithNullableValueResultTest()
        {
            var query = "select NullableValue from #A.Entities() where NullableValue <> 5";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    new[]
                    {
                        new BasicEntity{NullableValue = 1},
                        new BasicEntity{NullableValue = null},
                        new BasicEntity{NullableValue = 2}
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual(1, table[0].Values[0]);
            Assert.AreEqual(2, table[1].Values[0]);
        }

        [TestMethod]
        public void QueryWithWhereWithNullableMethodResultTest()
        {
            var query = "select NullableValue from #A.Entities() where NullableMethod(NullableValue) <> 5";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    new[]
                    {
                        new BasicEntity{ NullableValue = 1 },
                        new BasicEntity{ NullableValue = null },
                        new BasicEntity{ NullableValue = 2 }
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual(1, table[0].Values[0]);
            Assert.AreEqual(2, table[1].Values[0]);
        }

        [TestMethod]
        public void GroupBySingleColumnWithNullGroupTest()
        {
            var query = @"select Name from #A.Entities() group by Name";
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
                        new BasicEntity("ABBA"),
                        new BasicEntity(null),
                        new BasicEntity(null)
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual("Name", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

            Assert.AreEqual(4, table.Count);

            Assert.AreEqual("ABBA", table[0].Values[0]);
            Assert.AreEqual("BABBA", table[1].Values[0]);
            Assert.AreEqual("CECCA", table[2].Values[0]);
            Assert.AreEqual(null, table[3].Values[0]);

        }

        [TestMethod]
        public void GroupByMultiColumnWithNullGroupTest()
        {
            var query = @"select Country, City from #A.Entities() group by Country, City";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("POLAND", "WARSAW"),
                        new BasicEntity("POLAND", null),
                        new BasicEntity("UK", "LONDON"),
                        new BasicEntity("POLAND", null),
                        new BasicEntity("UK", "MANCHESTER"),
                        new BasicEntity("ANGOLA", "LLL"),
                        new BasicEntity("POLAND", "WARSAW"),
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(2, table.Columns.Count());
            Assert.AreEqual("Country", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
            Assert.AreEqual("City", table.Columns.ElementAt(1).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);

            Assert.AreEqual(5, table.Count);

            Assert.AreEqual("POLAND", table[0].Values[0]);
            Assert.AreEqual("WARSAW", table[0].Values[1]);

            Assert.AreEqual("POLAND", table[1].Values[0]);
            Assert.AreEqual(null, table[1].Values[1]);

            Assert.AreEqual("UK", table[2].Values[0]);
            Assert.AreEqual("LONDON", table[2].Values[1]);

            Assert.AreEqual("UK", table[3].Values[0]);
            Assert.AreEqual("MANCHESTER", table[3].Values[1]);

            Assert.AreEqual("ANGOLA", table[4].Values[0]);
            Assert.AreEqual("LLL", table[4].Values[1]);
        }

        [TestMethod]
        public void IsNotNullReferenceTypeTest()
        {
            var query = @"select Name from #A.Entities() where Name is not null";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    new[]
                    {
                        new BasicEntity("001"), new BasicEntity(null), new BasicEntity("003"), new BasicEntity(null),
                        new BasicEntity("005"), new BasicEntity("006")
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(4, table.Count);
            Assert.AreEqual("001", table[0].Values[0]);
            Assert.AreEqual("003", table[1].Values[0]);
            Assert.AreEqual("005", table[2].Values[0]);
            Assert.AreEqual("006", table[3].Values[0]);
        }

        [TestMethod]
        public void IsNullReferenceTypeTest()
        {
            var query = @"select City from #A.Entities() where Country is null";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    new[]
                    {
                        new BasicEntity("Poland", "Gdansk"), new BasicEntity(null, "Warsaw"), new BasicEntity("France", "Paris"), new BasicEntity(null, "Bratislava")
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual("Warsaw", table[0].Values[0]);
            Assert.AreEqual("Bratislava", table[1].Values[0]);
        }


        [TestMethod]
        public void IsNotNullValueTypeTest()
        {
            var query = @"select Population from #A.Entities() where Population is not null";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    new[]
                    {
                        new BasicEntity("ABC", 100), new BasicEntity("CBA", 200), new BasicEntity("aaa")
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(3, table.Count);

            Assert.AreEqual(100m, table[0].Values[0]);
            Assert.AreEqual(200m, table[1].Values[0]);
            Assert.AreEqual(0m, table[2].Values[0]);
        }

        [TestMethod]
        public void IsNullValueTypeTest()
        {
            var query = @"select Population from #A.Entities() where Population is null";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    new[]
                    {
                        new BasicEntity("ABC", 100), new BasicEntity("CBA", 200), new BasicEntity("aaa")
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(0, table.Count);
        }
    }
}

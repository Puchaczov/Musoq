using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests
{
    [TestClass]
    public class InTests : BasicEntityTestBase
    {
        [TestMethod]
        public void SimpleInOperator()
        {
            var query = "select Population from #A.Entities() where Population in (100, 400)";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    new[]
                    {
                        new BasicEntity("A", 100),
                        new BasicEntity("AB", 200),
                        new BasicEntity("ABC", 300),
                        new BasicEntity("ABCD", 400)
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual(100m, table[0][0]);
            Assert.AreEqual(400m, table[1][0]);
        }

        [TestMethod]
        public void SimpleNotInOperator()
        {
            var query = "select Population from #A.Entities() where Population not in (100, 400)";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    new[]
                    {
                        new BasicEntity("A", 100),
                        new BasicEntity("AB", 200),
                        new BasicEntity("ABC", 300),
                        new BasicEntity("ABCD", 400)
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual(200m, table[0][0]);
            Assert.AreEqual(300m, table[1][0]);
        }

        [TestMethod]
        public void InWithArgumentFromSourceOperator()
        {
            var query = "select Country from #A.Entities() where City in (Country, 'Warsaw')";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    new[]
                    {
                        new BasicEntity("Poland", "Warsaw"),
                        new BasicEntity("Berlin", "Germany"),
                        new BasicEntity("Singapore", "Singapore"),
                        new BasicEntity("France", "Paris"),
                        new BasicEntity("Monaco", "Monaco") 
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(3, table.Count);
            Assert.AreEqual("Poland", table[0][0]);
            Assert.AreEqual("Singapore", table[1][0]);
            Assert.AreEqual("Monaco", table[2][0]);
        }

        [TestMethod]
        public void NotInWithArgumentFromSourceOperator()
        {
            var query = "select Country from #A.Entities() where City not in (Country, 'Warsaw')";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    new[]
                    {
                        new BasicEntity("Poland", "Warsaw"),
                        new BasicEntity("Berlin", "Germany"),
                        new BasicEntity("Singapore", "Singapore"),
                        new BasicEntity("France", "Paris"),
                        new BasicEntity("Monaco", "Monaco")
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual("Berlin", table[0][0]);
            Assert.AreEqual("France", table[1][0]);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema;

namespace Musoq.Evaluator.Tests
{
    [TestClass]
    public class ComparsionsTests : TestBase
    {

        [TestMethod]
        public void ArithmeticOpsGreaterTest()
        {
            var query = "select City from #A.entities() where Population > 400d";

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

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual("City", table.Columns.ElementAt(0).Name);

            Assert.AreEqual(1, table.Count());
            Assert.AreEqual("WARSAW", table[0].Values[0]);
        }

        [TestMethod]
        public void ArithmeticOpsGreaterEqualTest()
        {
            var query = "select City from #A.entities() where Population >= 400d";

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

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual("City", table.Columns.ElementAt(0).Name);

            Assert.AreEqual(2, table.Count());
            Assert.AreEqual("WARSAW", table[0].Values[0]);
            Assert.AreEqual("CZESTOCHOWA", table[1].Values[0]);
        }

        [TestMethod]
        public void ArithmeticOpsEqualsTest()
        {
            var query = "select City from #A.entities() where Population = 250d";

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

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual("City", table.Columns.ElementAt(0).Name);

            Assert.AreEqual(2, table.Count());
            Assert.AreEqual("KATOWICE", table[0].Values[0]);
            Assert.AreEqual("BERLIN", table[1].Values[0]);
        }

        [TestMethod]
        public void ArithmeticOpsLessTest()
        {
            var query = "select City from #A.entities() where Population < 350d";

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

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual("City", table.Columns.ElementAt(0).Name);

            Assert.AreEqual(2, table.Count());
            Assert.AreEqual("KATOWICE", table[0].Values[0]);
            Assert.AreEqual("BERLIN", table[1].Values[0]);
        }


        [TestMethod]
        public void ArithmeticOpsLessEqualTest()
        {
            var query = "select City from #A.entities() where Population <= 350d";

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

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual("City", table.Columns.ElementAt(0).Name);

            Assert.AreEqual(3, table.Count());
            Assert.AreEqual("KATOWICE", table[0].Values[0]);
            Assert.AreEqual("BERLIN", table[1].Values[0]);
            Assert.AreEqual("MUNICH", table[2].Values[0]);
        }
    }
}

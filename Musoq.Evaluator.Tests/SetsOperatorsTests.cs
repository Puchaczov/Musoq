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
    public class SetsOperatorsTests : TestBase
    {
        [TestMethod]
        public void UnionWithDifferentColumnsAsAKey()
        {
            var query = @"select Name from #A.Entities() union (Name) select MyName as Name from #B.Entities()";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"), new BasicEntity("002")}},
                {"#B", new[] {new BasicEntity("003"), new BasicEntity("004")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(4, table.Count);
            Assert.AreEqual("001", table[0].Values[0]);
            Assert.AreEqual("002", table[1].Values[0]);
            Assert.AreEqual("003", table[2].Values[0]);
            Assert.AreEqual("004", table[3].Values[0]);
        }

        [TestMethod]
        public void UnionWithoutDuplicatedKeysTest()
        {
            var query = @"select Name from #A.Entities() union (Name) select Name from #B.Entities()";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"), new BasicEntity("002")}},
                {"#B", new[] {new BasicEntity("003"), new BasicEntity("004")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(4, table.Count);
            Assert.AreEqual("001", table[0].Values[0]);
            Assert.AreEqual("002", table[1].Values[0]);
            Assert.AreEqual("003", table[2].Values[0]);
            Assert.AreEqual("004", table[3].Values[0]);
        }

        [TestMethod]
        public void UnionWithDuplicatedKeysTest()
        {
            var query = @"select Name from #A.Entities() union (Name) select Name from #B.Entities()";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"), new BasicEntity("002")}},
                {"#B", new[] {new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("005")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(3, table.Count);
            Assert.AreEqual("001", table[0].Values[0]);
            Assert.AreEqual("002", table[1].Values[0]);
            Assert.AreEqual("005", table[2].Values[0]);
        }

        [TestMethod]
        public void MultipleUnionsWithDuplicatedKeysTest()
        {
            var query =
                @"select Name from #A.Entities() union (Name) select Name from #B.Entities() union (Name) select Name from #C.Entities()";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001")}},
                {"#B", new[] {new BasicEntity("001"), new BasicEntity("002")}},
                {"#C", new[] {new BasicEntity("005")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(3, table.Count);
            Assert.AreEqual("001", table[0].Values[0]);
            Assert.AreEqual("002", table[1].Values[0]);
            Assert.AreEqual("005", table[2].Values[0]);
        }

        [TestMethod]
        public void MultipleUnionsWithoutDuplicatedKeysTest()
        {
            var query =
                @"select Name from #A.Entities() union (Name) select Name from #B.Entities() union (Name) select Name from #C.Entities()";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001")}},
                {"#B", new[] {new BasicEntity("002")}},
                {"#C", new[] {new BasicEntity("005")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(3, table.Count);
            Assert.AreEqual("001", table[0].Values[0]);
            Assert.AreEqual("002", table[1].Values[0]);
            Assert.AreEqual("005", table[2].Values[0]);
        }

        [TestMethod]
        public void MultipleUnionsComplexTest()
        {
            var query =
                @"select Name from #A.Entities() union (Name) select Name from #B.Entities() union (Name) select Name from #C.Entities() union (Name) select Name from #D.Entities()";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001")}},
                {"#B", new[] {new BasicEntity("002")}},
                {"#C", new[] {new BasicEntity("005")}},
                {"#D", new[] {new BasicEntity("007"), new BasicEntity("001")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(4, table.Count);
            Assert.AreEqual("001", table[0].Values[0]);
            Assert.AreEqual("002", table[1].Values[0]);
            Assert.AreEqual("005", table[2].Values[0]);
            Assert.AreEqual("007", table[3].Values[0]);
        }

        [TestMethod]
        public void UnionAllWithDuplicatedKeysTest()
        {
            var query = @"select Name from #A.Entities() union all (Name) select Name from #B.Entities()";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"), new BasicEntity("002")}},
                {"#B", new[] {new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("005")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(5, table.Count);
            Assert.AreEqual("001", table[0].Values[0]);
            Assert.AreEqual("002", table[1].Values[0]);
            Assert.AreEqual("001", table[2].Values[0]);
            Assert.AreEqual("002", table[3].Values[0]);
            Assert.AreEqual("005", table[4].Values[0]);
        }

        [TestMethod]
        public void MultipleUnionsAllWithDuplicatedKeysTest()
        {
            var query =
                @"select Name from #A.Entities() union all (Name) select Name from #B.Entities() union all (Name) select Name from #C.Entities()";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001")}},
                {"#B", new[] {new BasicEntity("001"), new BasicEntity("002")}},
                {"#C", new[] {new BasicEntity("005")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(4, table.Count);
            Assert.AreEqual("001", table[0].Values[0]);
            Assert.AreEqual("001", table[1].Values[0]);
            Assert.AreEqual("002", table[2].Values[0]);
            Assert.AreEqual("005", table[3].Values[0]);
        }

        [TestMethod]
        public void MultipleUnionsAllWithoutDuplicatedKeysTest()
        {
            var query =
                @"select Name from #A.Entities() union all (Name) select Name from #B.Entities() union all (Name) select Name from #C.Entities()";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001")}},
                {"#B", new[] {new BasicEntity("002")}},
                {"#C", new[] {new BasicEntity("005")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(3, table.Count);
            Assert.AreEqual("001", table[0].Values[0]);
            Assert.AreEqual("002", table[1].Values[0]);
            Assert.AreEqual("005", table[2].Values[0]);
        }

        [TestMethod]
        public void MultipleUnionsAllComplexTest()
        {
            var query =
                @"select Name from #A.Entities() union all (Name) select Name from #B.Entities() union all (Name) select Name from #C.Entities() union all (Name) select Name from #D.Entities()";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001")}},
                {"#B", new[] {new BasicEntity("002")}},
                {"#C", new[] {new BasicEntity("005")}},
                {"#D", new[] {new BasicEntity("007"), new BasicEntity("001")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(5, table.Count);
            Assert.AreEqual("001", table[0].Values[0]);
            Assert.AreEqual("002", table[1].Values[0]);
            Assert.AreEqual("005", table[2].Values[0]);
            Assert.AreEqual("007", table[3].Values[0]);
            Assert.AreEqual("001", table[4].Values[0]);
        }

        [TestMethod]
        public void UnionAllWithoutDuplicatedKeysTest()
        {
            var query = @"select Name from #A.Entities() union all (Name) select Name from #B.Entities()";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"), new BasicEntity("002")}},
                {"#B", new[] {new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(5, table.Count);
            Assert.AreEqual("001", table[0].Values[0]);
            Assert.AreEqual("002", table[1].Values[0]);
            Assert.AreEqual("003", table[2].Values[0]);
            Assert.AreEqual("004", table[3].Values[0]);
            Assert.AreEqual("001", table[4].Values[0]);
        }

        [TestMethod]
        public void ExceptDoubleSourceTest()
        {
            var query = @"select Name from #A.Entities() except (Name) select Name from #B.Entities()";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"), new BasicEntity("002")}},
                {"#B", new[] {new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual("002", table[0].Values[0]);
        }

        [TestMethod]
        public void ExceptTripleSourcesTest()
        {
            var query =
                @"select Name from #A.Entities() except (Name) select Name from #B.Entities() except (Name) select Name from #C.Entities()";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"), new BasicEntity("002")}},
                {"#B", new[] {new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")}},
                {"#C", new[] {new BasicEntity("002")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(0, table.Count);
        }

        [TestMethod]
        public void IntersectDoubleSourceTest()
        {
            var query = @"select Name from #A.Entities() intersect (Name) select Name from #B.Entities()";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"), new BasicEntity("002")}},
                {"#B", new[] {new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual("001", table[0].Values[0]);
        }

        [TestMethod]
        public void IntersectTripleSourcesTest()
        {
            var query =
                @"select Name from #A.Entities() intersect (Name) select Name from #B.Entities() intersect (Name) select Name from #C.Entities()";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"), new BasicEntity("002")}},
                {"#B", new[] {new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")}},
                {"#C", new[] {new BasicEntity("002"), new BasicEntity("001")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual("001", table[0].Values[0]);
        }

        [TestMethod]
        public void MixedSourcesExceptUnionScenarioTest()
        {
            var query =
                @"select Name from #A.Entities()
except (Name)
select Name from #B.Entities()
union (Name)
select Name from #C.Entities()";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"), new BasicEntity("002")}},
                {"#B", new[] {new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")}},
                {"#C", new[] {new BasicEntity("002"), new BasicEntity("001")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual("002", table[0].Values[0]);
            Assert.AreEqual("001", table[1].Values[0]);
        }

        [TestMethod]
        public void MixedSourcesExceptUnionWithConditionsScenarioTest()
        {
            var query =
                @"select Name from #A.Entities() where Extension = '.txt'
except (Name)
select Name from #B.Entities() where Extension = '.txt'
union (Name)
select Name from #C.Entities() where Extension = '.txt'";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"), new BasicEntity("002")}},
                {"#B", new[] {new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")}},
                {"#C", new[] {new BasicEntity("002"), new BasicEntity("001")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual("002", table[0].Values[0]);
            Assert.AreEqual("001", table[1].Values[0]);
        }

        [TestMethod]
        public void MixedSourcesExceptUnionWithMultipleColumnsConditionsScenarioTest()
        {
            var query =
                @"select Name, RandomNumber() from #A.Entities() where Extension = '.txt'
except (Name)
select Name, RandomNumber() from #B.Entities() where Extension = '.txt'
union (Name)
select Name, RandomNumber() from #C.Entities() where Extension = '.txt'";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"), new BasicEntity("002")}},
                {"#B", new[] {new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")}},
                {"#C", new[] {new BasicEntity("002"), new BasicEntity("001")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual("002", table[0].Values[0]);
            Assert.AreEqual("001", table[1].Values[0]);
        }
    }
}

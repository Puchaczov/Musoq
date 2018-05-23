using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema;

namespace Musoq.Evaluator.Tests
{
    [TestClass]
    public class SetsOperatorsTests : TestBase
    {
        [TestMethod]
        public void UnionWithDifferentColumnsAsAKeyTest()
        {
            var query = @"select Name from #A.Entities() union (Name) select City as Name from #B.Entities()";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"), new BasicEntity("002")}},
                {"#B", new[] {new BasicEntity("003", "", 0), new BasicEntity("004", "", 0)}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(4, table.Count);
            Assert.AreEqual("001", table[0].Values[0]);
            Assert.AreEqual("002", table[1].Values[0]);
            Assert.AreEqual("003", table[2].Values[0]);
            Assert.AreEqual("004", table[3].Values[0]);
        }

        [TestMethod]
        public void UnionWithSkipTest()
        {
            var query = @"select Name from #A.Entities() skip 1 union (Name) select Name from #B.Entities() skip 2";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"), new BasicEntity("002")}},
                {"#B", new[] {new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("005")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual("002", table[0].Values[0]);
            Assert.AreEqual("005", table[1].Values[0]);
        }

        [TestMethod]
        public void UnionAllWithSkipTest()
        {
            var query = @"select Name from #A.Entities() skip 1 union all (Name) select Name from #B.Entities() skip 2";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"), new BasicEntity("005")}},
                {"#B", new[] {new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("005")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual("005", table[0].Values[0]);
            Assert.AreEqual("005", table[1].Values[0]);
        }

        [TestMethod]
        public void MultipleUnionAllWithSkipTest()
        {
            var query = @"
select Name from #A.Entities() skip 1 
union all (Name) 
select Name from #B.Entities() skip 2
union all (Name)
select Name from #C.Entities() skip 3";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"), new BasicEntity("005")}},
                {"#B", new[] {new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("005")}},
                {
                    "#C",
                    new[]
                    {
                        new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("004"), new BasicEntity("005")
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(3, table.Count);
            Assert.AreEqual("005", table[0].Values[0]);
            Assert.AreEqual("005", table[1].Values[0]);
            Assert.AreEqual("005", table[2].Values[0]);
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
            var table = vm.Run();

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
            var table = vm.Run();

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
            var table = vm.Run();

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
            var table = vm.Run();

            Assert.AreEqual(3, table.Count);
            Assert.AreEqual("001", table[0].Values[0]);
            Assert.AreEqual("002", table[1].Values[0]);
            Assert.AreEqual("005", table[2].Values[0]);
        }

        [TestMethod]
        public void MultipleUnionsComplexTest()
        {
            var query =
                @"
select Name from #A.Entities() union (Name) 
select Name from #B.Entities() union (Name) 
select Name from #C.Entities() union (Name) 
select Name from #D.Entities()";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001")}},
                {"#B", new[] {new BasicEntity("002")}},
                {"#C", new[] {new BasicEntity("005")}},
                {"#D", new[] {new BasicEntity("007"), new BasicEntity("001")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

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
            var table = vm.Run();

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
            var table = vm.Run();

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
            var table = vm.Run();

            Assert.AreEqual(3, table.Count);
            Assert.AreEqual("001", table[0].Values[0]);
            Assert.AreEqual("002", table[1].Values[0]);
            Assert.AreEqual("005", table[2].Values[0]);
        }

        [TestMethod]
        public void MultipleUnionsAllComplexTest()
        {
            var query =
                @"
select Name from #A.Entities() union all (Name) 
select Name from #B.Entities() union all (Name) 
select Name from #C.Entities() union all (Name) 
select Name from #D.Entities()";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001")}},
                {"#B", new[] {new BasicEntity("002")}},
                {"#C", new[] {new BasicEntity("005")}},
                {"#D", new[] {new BasicEntity("007"), new BasicEntity("001")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

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
            var table = vm.Run();

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
            var table = vm.Run();

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual("002", table[0].Values[0]);
        }

        [TestMethod]
        public void ExceptWithSkipDoubleSourceTest()
        {
            var query = @"select Name from #A.Entities() skip 1 except (Name) select Name from #B.Entities() skip 2";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("010")}},
                {"#B", new[] {new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("002")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual("010", table[0].Values[0]);
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
            var table = vm.Run();

            Assert.AreEqual(0, table.Count);
        }

        [TestMethod]
        public void ExceptWithSkipTripleSourcesTest()
        {
            var query =
                @"select Name from #A.Entities() skip 1 except (Name) 
select Name from #B.Entities() skip 2 except (Name) 
select Name from #C.Entities() skip 3";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"), new BasicEntity("002")}},
                {"#B", new[] {new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")}},
                {"#C", new[] {new BasicEntity("005")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual("002", table[0].Values[0]);
        }

        [TestMethod]
        public void ExceptMultipleSourcesTest()
        {
            var query =
                @"
select Name from #A.Entities() except (Name) 
select Name from #B.Entities() except (Name) 
select Name from #C.Entities() except (Name) 
select Name from #D.Entities()";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    new[]
                    {
                        new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("007"), new BasicEntity("008")
                    }
                },
                {"#B", new[] {new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")}},
                {"#C", new[] {new BasicEntity("005")}},
                {"#D", new[] {new BasicEntity("007")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual("002", table[0].Values[0]);
            Assert.AreEqual("008", table[1].Values[0]);
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
            var table = vm.Run();

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual("001", table[0].Values[0]);
        }

        [TestMethod]
        public void IntersectWithSkipDoubleSourceTest()
        {
            var query = @"select Name from #A.Entities() skip 1 intersect (Name) select Name from #B.Entities() skip 2";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("005")}},
                {
                    "#B",
                    new[]
                    {
                        new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001"), new BasicEntity("005")
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual("005", table[0].Values[0]);
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
            var table = vm.Run();

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual("001", table[0].Values[0]);
        }

        [TestMethod]
        public void IntersectWithSkipTripleSourcesTest()
        {
            var query =
                @"
select Name from #A.Entities() skip 1 intersect (Name) 
select Name from #B.Entities() skip 2 intersect (Name) 
select Name from #C.Entities()";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("005")}},
                {
                    "#B",
                    new[]
                    {
                        new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001"), new BasicEntity("005")
                    }
                },
                {"#C", new[] {new BasicEntity("002"), new BasicEntity("001"), new BasicEntity("005")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual("005", table[0].Values[0]);
        }

        [TestMethod]
        public void IntersectMultipleSourcesTest()
        {
            var query =
                @"
select Name from #A.Entities() intersect (Name) 
select Name from #B.Entities() intersect (Name) 
select Name from #C.Entities() intersect (Name) 
select Name from #D.Entities()";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    new[]
                    {
                        new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("007"), new BasicEntity("008")
                    }
                },
                {"#B", new[] {new BasicEntity("003"), new BasicEntity("007"), new BasicEntity("001")}},
                {"#C", new[] {new BasicEntity("005"), new BasicEntity("007"), new BasicEntity("001")}},
                {"#D", new[] {new BasicEntity("008"), new BasicEntity("007"), new BasicEntity("001")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual("001", table[0].Values[0]);
            Assert.AreEqual("007", table[1].Values[0]);
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
            var table = vm.Run();

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual("002", table[0].Values[0]);
            Assert.AreEqual("001", table[1].Values[0]);
        }

        [TestMethod]
        public void MixedSourcesExceptUnionScenario1Test()
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
            var table = vm.Run();

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual("002", table[0].Values[0]);
            Assert.AreEqual("001", table[1].Values[0]);
        }

        [TestMethod]
        public void MixedSourcesWithSkipExceptUnionWithConditionsScenarioTest()
        {
            var query =
                @"select Name from #A.Entities() skip 1 where Extension = '.txt'
except (Name)
select Name from #B.Entities() skip 2 where Extension = '.txt'
union (Name)
select Name from #C.Entities() skip 3 where Extension = '.txt'";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"), new BasicEntity("002")}},
                {"#B", new[] {new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")}},
                {"#C", new[] {new BasicEntity("002"), new BasicEntity("001")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual("002", table[0].Values[0]);
        }

        [TestMethod]
        public void MixedSourcesWithSkipIntersectUnionScenarioTest()
        {
            var query =
                @"select Name from #A.Entities() skip 1
intersect (Name)
select Name from #B.Entities() skip 2
union (Name)
select Name from #C.Entities() skip 3";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("002"), new BasicEntity("001")}},
                {"#B", new[] {new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")}},
                {
                    "#C",
                    new[]
                    {
                        new BasicEntity("002"), new BasicEntity("001"), new BasicEntity("003"), new BasicEntity("006")
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual("001", table[0].Values[0]);
            Assert.AreEqual("006", table[1].Values[0]);
        }

        [TestMethod]
        public void MixedSourcesExceptUnionWithMultipleColumnsScenarioTest()
        {
            var query =
                @"select Name, RandomNumber() from #A.Entities()
except (Name)
select Name, RandomNumber() from #B.Entities()
union (Name)
select Name, RandomNumber() from #C.Entities()";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"), new BasicEntity("002")}},
                {"#B", new[] {new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")}},
                {"#C", new[] {new BasicEntity("002"), new BasicEntity("001")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual("002", table[0].Values[0]);
            Assert.AreEqual("001", table[1].Values[0]);
        }

        [TestMethod]
        public void MixedMultipleSourcesTest()
        {
            var query =
                @"
select Name from #A.Entities() union (Name) 
select Name from #B.Entities() except (Name) 
select Name from #C.Entities() intersect (Name) 
select Name from #D.Entities()";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    new[]
                    {
                        new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("007"), new BasicEntity("008")
                    }
                },
                {"#B", new[] {new BasicEntity("003"), new BasicEntity("007"), new BasicEntity("001")}},
                {"#C", new[] {new BasicEntity("005"), new BasicEntity("007")}},
                {"#D", new[] {new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("003")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(3, table.Count);
            Assert.AreEqual("001", table[0].Values[0]);
            Assert.AreEqual("002", table[1].Values[0]);
            Assert.AreEqual("003", table[2].Values[0]);
        }

        [TestMethod]
        public void UnionSourceGroupByTest()
        {
            var query =
                @"select City, Sum(Population) from #A.Entities() group by City
union (City)
select City, Sum(Population) from #B.Entities() group by City
union (City)
select City, Sum(Population) from #C.Entities() group by City";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001", "", 100), new BasicEntity("001", "", 100)}},
                {
                    "#B",
                    new[]
                    {
                        new BasicEntity("003", "", 13), new BasicEntity("003", "", 13), new BasicEntity("003", "", 13)
                    }
                },
                {"#C", new[] {new BasicEntity("002", "", 14), new BasicEntity("002", "", 14)}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(3, table.Count);
            Assert.AreEqual("001", table[0].Values[0]);
            Assert.AreEqual(200m, table[0].Values[1]);
            Assert.AreEqual("003", table[1].Values[0]);
            Assert.AreEqual(39m, table[1].Values[1]);
            Assert.AreEqual("002", table[2].Values[0]);
            Assert.AreEqual(28m, table[2].Values[1]);
        }

        [TestMethod]
        public void UnionAllSourceGroupByTest()
        {
            var query =
                @"select City, Sum(Population) from #A.Entities() group by City
union all (City)
select City, Sum(Population) from #B.Entities() group by City
union all (City)
select City, Sum(Population) from #C.Entities() group by City";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001", "", 100), new BasicEntity("001", "", 100)}},
                {
                    "#B",
                    new[]
                    {
                        new BasicEntity("003", "", 13), new BasicEntity("003", "", 13), new BasicEntity("003", "", 13)
                    }
                },
                {"#C", new[] {new BasicEntity("002", "", 14), new BasicEntity("002", "", 14)}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(3, table.Count);
            Assert.AreEqual("001", table[0].Values[0]);
            Assert.AreEqual(200m, table[0].Values[1]);
            Assert.AreEqual("003", table[1].Values[0]);
            Assert.AreEqual(39m, table[1].Values[1]);
            Assert.AreEqual("002", table[2].Values[0]);
            Assert.AreEqual(28m, table[2].Values[1]);
        }

        [TestMethod]
        public void ExceptSourceGroupByTest()
        {
            var query =
                @"select City, Sum(Population) from #A.Entities() group by City
except (City)
select City, Sum(Population) from #B.Entities() group by City
except (City)
select City, Sum(Population) from #C.Entities() group by City";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    new[]
                    {
                        new BasicEntity("001", "", 100), new BasicEntity("001", "", 100),
                        new BasicEntity("002", "", 500)
                    }
                },
                {
                    "#B",
                    new[]
                    {
                        new BasicEntity("003", "", 13), new BasicEntity("003", "", 13), new BasicEntity("003", "", 13)
                    }
                },
                {"#C", new[] {new BasicEntity("002", "", 14), new BasicEntity("002", "", 14)}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual("001", table[0].Values[0]);
            Assert.AreEqual(200m, table[0].Values[1]);
        }

        [TestMethod]
        public void IntersectSourceGroupByTest()
        {
            var query =
                @"select City, Sum(Population) from #A.Entities() group by City
except (City)
select City, Sum(Population) from #B.Entities() group by City
except (City)
select City, Sum(Population) from #C.Entities() group by City";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    new[]
                    {
                        new BasicEntity("001", "", 100), new BasicEntity("001", "", 100),
                        new BasicEntity("002", "", 500)
                    }
                },
                {
                    "#B",
                    new[]
                    {
                        new BasicEntity("003", "", 13), new BasicEntity("003", "", 13), new BasicEntity("003", "", 13)
                    }
                },
                {"#C", new[] {new BasicEntity("002", "", 14), new BasicEntity("002", "", 14)}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual("001", table[0].Values[0]);
            Assert.AreEqual(200m, table[0].Values[1]);
        }

        [TestMethod]
        public void UnionSameSourceTest()
        {
            var query =
                @"
select Name from #A.Entities() where Name = '001'
union (Name)
select Name from #A.Entities() where Name = '002'";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"), new BasicEntity("002")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual("001", table[0].Values[0]);
            Assert.AreEqual("002", table[1].Values[0]);
        }

        [TestMethod]
        public void UnionMultipleTimesSameSourceTest()
        {
            var query =
                @"
select Name from #A.Entities() where Name = '001'
union (Name)
select Name from #A.Entities() where Name = '002'
union (Name)
select Name from #A.Entities() where Name = '003'
union (Name)
select Name from #A.Entities() where Name = '004'
union (Name)
select Name from #A.Entities() where Name = '005'";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    new[]
                    {
                        new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("003"), new BasicEntity("004"),
                        new BasicEntity("005")
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(5, table.Count);
            Assert.AreEqual("001", table[0].Values[0]);
            Assert.AreEqual("002", table[1].Values[0]);
            Assert.AreEqual("003", table[2].Values[0]);
            Assert.AreEqual("004", table[3].Values[0]);
            Assert.AreEqual("005", table[4].Values[0]);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests
{
    [TestClass]
    public class ComprehensiveJoinTests : BasicEntityTestBase
    {
        private class JoinTestCase
        {
            public string Name { get; set; }
            public int Id { get; set; }
            public int Value { get; set; }
        }

        private CompiledQuery CreateAndRunVirtualMachine(
            string script,
            IDictionary<string, IEnumerable<BasicEntity>> sources,
            CompilationOptions options)
        {
            return InstanceCreator.CompileForExecution(
                script,
                Guid.NewGuid().ToString(),
                new BasicSchemaProvider<BasicEntity>(sources),
                LoggerResolver,
                options);
        }

        [TestMethod]
        public void InnerJoin_Equi_ShouldMatch()
        {
            var query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
inner join #B.entities() b on a.Id = b.Id";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", [
                        new BasicEntity { Name = "A1", Id = 1 },
                        new BasicEntity { Name = "A2", Id = 2 }
                    ]
                },
                {
                    "#B", [
                        new BasicEntity { Name = "B1", Id = 1 },
                        new BasicEntity { Name = "B3", Id = 3 }
                    ]
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run(TestContext.CancellationToken);

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual("A1", table[0][0]);
            Assert.AreEqual("B1", table[0][1]);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void InnerJoin_NonEqui_Greater_ShouldMatch(bool useSortMergeJoin)
        {
            var query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
inner join #B.entities() b on a.Population > b.Population";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", [
                        new BasicEntity { Name = "A1", Population = 100 },
                        new BasicEntity { Name = "A2", Population = 200 }
                    ]
                },
                {
                    "#B", [
                        new BasicEntity { Name = "B1", Population = 50 },
                        new BasicEntity { Name = "B2", Population = 150 }
                    ]
                }
            };

            // A1(100) > B1(50) -> Match
            // A1(100) > B2(150) -> No
            // A2(200) > B1(50) -> Match
            // A2(200) > B2(150) -> Match

            var options = new CompilationOptions(useSortMergeJoin: useSortMergeJoin);
            var vm = CreateAndRunVirtualMachine(query, sources, options);
            var table = vm.Run(TestContext.CancellationToken);

            Assert.AreEqual(3, table.Count, $"Failed with UseSortMergeJoin={useSortMergeJoin}");
            
            var rows = table.Select(r => $"{r[0]}-{r[1]}").OrderBy(x => x).ToList();
            Assert.AreEqual("A1-B1", rows[0]);
            Assert.AreEqual("A2-B1", rows[1]);
            Assert.AreEqual("A2-B2", rows[2]);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void InnerJoin_NonEqui_Less_ShouldMatch(bool useSortMergeJoin)
        {
            var query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
inner join #B.entities() b on a.Population < b.Population";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", [
                        new BasicEntity { Name = "A1", Population = 100 },
                        new BasicEntity { Name = "A2", Population = 200 }
                    ]
                },
                {
                    "#B", [
                        new BasicEntity { Name = "B1", Population = 50 },
                        new BasicEntity { Name = "B2", Population = 150 }
                    ]
                }
            };

            // A1(100) < B1(50) -> No
            // A1(100) < B2(150) -> Match
            // A2(200) < B1(50) -> No
            // A2(200) < B2(150) -> No

            var options = new CompilationOptions(useSortMergeJoin: useSortMergeJoin);
            var vm = CreateAndRunVirtualMachine(query, sources, options);
            var table = vm.Run(TestContext.CancellationToken);

            Assert.AreEqual(1, table.Count, $"Failed with UseSortMergeJoin={useSortMergeJoin}");
            Assert.AreEqual("A1", table[0][0]);
            Assert.AreEqual("B2", table[0][1]);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void InnerJoin_NonEqui_GreaterOrEqual_ShouldMatch(bool useSortMergeJoin)
        {
            var query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
inner join #B.entities() b on a.Population >= b.Population";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", [
                        new BasicEntity { Name = "A1", Population = 100 }
                    ]
                },
                {
                    "#B", [
                        new BasicEntity { Name = "B1", Population = 50 },
                        new BasicEntity { Name = "B2", Population = 100 },
                        new BasicEntity { Name = "B3", Population = 150 }
                    ]
                }
            };

            // A1(100) >= B1(50) -> Match
            // A1(100) >= B2(100) -> Match
            // A1(100) >= B3(150) -> No

            var options = new CompilationOptions(useSortMergeJoin: useSortMergeJoin);
            var vm = CreateAndRunVirtualMachine(query, sources, options);
            var table = vm.Run(TestContext.CancellationToken);

            Assert.AreEqual(2, table.Count, $"Failed with UseSortMergeJoin={useSortMergeJoin}");
            
            var rows = table.Select(r => $"{r[0]}-{r[1]}").OrderBy(x => x).ToList();
            Assert.AreEqual("A1-B1", rows[0]);
            Assert.AreEqual("A1-B2", rows[1]);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void InnerJoin_NonEqui_LessOrEqual_ShouldMatch(bool useSortMergeJoin)
        {
            var query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
inner join #B.entities() b on a.Population <= b.Population";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", [
                        new BasicEntity { Name = "A1", Population = 100 }
                    ]
                },
                {
                    "#B", [
                        new BasicEntity { Name = "B1", Population = 50 },
                        new BasicEntity { Name = "B2", Population = 100 },
                        new BasicEntity { Name = "B3", Population = 150 }
                    ]
                }
            };

            // A1(100) <= B1(50) -> No
            // A1(100) <= B2(100) -> Match
            // A1(100) <= B3(150) -> Match

            var options = new CompilationOptions(useSortMergeJoin: useSortMergeJoin);
            var vm = CreateAndRunVirtualMachine(query, sources, options);
            var table = vm.Run(TestContext.CancellationToken);

            Assert.AreEqual(2, table.Count, $"Failed with UseSortMergeJoin={useSortMergeJoin}");
            
            var rows = table.Select(r => $"{r[0]}-{r[1]}").OrderBy(x => x).ToList();
            Assert.AreEqual("A1-B2", rows[0]);
            Assert.AreEqual("A1-B3", rows[1]);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void InnerJoin_Mixed_EquiAndNonEqui_ShouldMatch(bool useSortMergeJoin)
        {
            // Note: SortMergeJoin currently only supports single key non-equi join optimization if it's the ONLY condition or handled specifically.
            // If there are multiple conditions, it might fall back or handle it.
            // Let's see how it behaves.
            
            var query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
inner join #B.entities() b on a.Id = b.Id AND a.Population > b.Population";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", [
                        new BasicEntity { Name = "A1", Id = 1, Population = 100 },
                        new BasicEntity { Name = "A2", Id = 2, Population = 200 }
                    ]
                },
                {
                    "#B", [
                        new BasicEntity { Name = "B1", Id = 1, Population = 50 },  // Match Id=1, 100 > 50 -> Match
                        new BasicEntity { Name = "B2", Id = 2, Population = 250 } // Match Id=2, 200 > 250 -> No
                    ]
                }
            };

            var options = new CompilationOptions(useSortMergeJoin: useSortMergeJoin);
            var vm = CreateAndRunVirtualMachine(query, sources, options);
            var table = vm.Run(TestContext.CancellationToken);

            Assert.AreEqual(1, table.Count, $"Failed with UseSortMergeJoin={useSortMergeJoin}");
            Assert.AreEqual("A1", table[0][0]);
            Assert.AreEqual("B1", table[0][1]);
        }

        [TestMethod]
        public void LeftJoin_ShouldReturnNullsForMissingRight()
        {
            var query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
left join #B.entities() b on a.Id = b.Id";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", [
                        new BasicEntity { Name = "A1", Id = 1 },
                        new BasicEntity { Name = "A2", Id = 2 }
                    ]
                },
                {
                    "#B", [
                        new BasicEntity { Name = "B1", Id = 1 }
                    ]
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run(TestContext.CancellationToken);

            Assert.AreEqual(2, table.Count);
            
            var row1 = table.Single(r => (string)r[0] == "A1");
            Assert.AreEqual("B1", row1[1]);

            var row2 = table.Single(r => (string)r[0] == "A2");
            Assert.IsNull(row2[1]);
        }

        [TestMethod]
        public void RightJoin_ShouldReturnNullsForMissingLeft()
        {
            var query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
right join #B.entities() b on a.Id = b.Id";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", [
                        new BasicEntity { Name = "A1", Id = 1 }
                    ]
                },
                {
                    "#B", [
                        new BasicEntity { Name = "B1", Id = 1 },
                        new BasicEntity { Name = "B2", Id = 2 }
                    ]
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources, new CompilationOptions());
            var table = vm.Run(TestContext.CancellationToken);

            Assert.AreEqual(2, table.Count);
            
            var row1 = table.Single(r => (string)r[1] == "B1");
            Assert.AreEqual("A1", row1[0]);

            var row2 = table.Single(r => (string)r[1] == "B2");
            Assert.IsNull(row2[0]);
        }
        
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void InnerJoin_NonEqui_NotEqual_ShouldMatch(bool useSortMergeJoin)
        {
             var query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
inner join #B.entities() b on a.Id <> b.Id";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", [
                        new BasicEntity { Name = "A1", Id = 1 },
                        new BasicEntity { Name = "A2", Id = 2 }
                    ]
                },
                {
                    "#B", [
                        new BasicEntity { Name = "B1", Id = 1 },
                        new BasicEntity { Name = "B2", Id = 2 }
                    ]
                }
            };

            // A1(1) <> B1(1) -> No
            // A1(1) <> B2(2) -> Match
            // A2(2) <> B1(1) -> Match
            // A2(2) <> B2(2) -> No

            var options = new CompilationOptions(useSortMergeJoin: useSortMergeJoin);
            var vm = CreateAndRunVirtualMachine(query, sources, options);
            var table = vm.Run(TestContext.CancellationToken);

            Assert.AreEqual(2, table.Count, $"Failed with UseSortMergeJoin={useSortMergeJoin}");
            
            var rows = table.Select(r => $"{r[0]}-{r[1]}").OrderBy(x => x).ToList();
            Assert.AreEqual("A1-B2", rows[0]);
            Assert.AreEqual("A2-B1", rows[1]);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void InnerJoin_OrCondition_ShouldMatch(bool useSortMergeJoin)
        {
            var query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
inner join #B.entities() b on a.Id = b.Id OR a.Population = b.Population";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", [
                        new BasicEntity { Name = "A1", Id = 1, Population = 100 },
                        new BasicEntity { Name = "A2", Id = 2, Population = 200 }
                    ]
                },
                {
                    "#B", [
                        new BasicEntity { Name = "B1", Id = 1, Population = 999 }, // Match Id=1
                        new BasicEntity { Name = "B2", Id = 999, Population = 200 }, // Match Population=200
                        new BasicEntity { Name = "B3", Id = 3, Population = 300 } // No match
                    ]
                }
            };

            // A1(1, 100) matches B1(1, 999) via Id
            // A2(2, 200) matches B2(999, 200) via Population

            var options = new CompilationOptions(useSortMergeJoin: useSortMergeJoin);
            var vm = CreateAndRunVirtualMachine(query, sources, options);
            var table = vm.Run(TestContext.CancellationToken);

            Assert.AreEqual(2, table.Count, $"Failed with UseSortMergeJoin={useSortMergeJoin}");
            
            var rows = table.Select(r => $"{r[0]}-{r[1]}").OrderBy(x => x).ToList();
            Assert.AreEqual("A1-B1", rows[0]);
            Assert.AreEqual("A2-B2", rows[1]);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void InnerJoin_ComplexAndOr_ShouldMatch(bool useSortMergeJoin)
        {
            var query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
inner join #B.entities() b on a.Id = b.Id AND (a.Population > b.Population OR a.Name = b.Name)";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", [
                        new BasicEntity { Name = "A1", Id = 1, Population = 100 },
                        new BasicEntity { Name = "A2", Id = 2, Population = 200 },
                        new BasicEntity { Name = "SameName", Id = 3, Population = 10 }
                    ]
                },
                {
                    "#B", [
                        new BasicEntity { Name = "B1", Id = 1, Population = 50 },   // Match Id=1 AND Pop(100 > 50) -> Match
                        new BasicEntity { Name = "B2", Id = 2, Population = 250 },  // Match Id=2 BUT Pop(200 > 250) False AND Name(A2!=B2) False -> No Match
                        new BasicEntity { Name = "SameName", Id = 3, Population = 50 } // Match Id=3 AND (Pop(10 > 50) False OR Name(SameName=SameName) True) -> Match
                    ]
                }
            };

            var options = new CompilationOptions(useSortMergeJoin: useSortMergeJoin);
            var vm = CreateAndRunVirtualMachine(query, sources, options);
            var table = vm.Run(TestContext.CancellationToken);

            Assert.AreEqual(2, table.Count, $"Failed with UseSortMergeJoin={useSortMergeJoin}");
            
            var rows = table.Select(r => $"{r[0]}-{r[1]}").OrderBy(x => x).ToList();
            Assert.AreEqual("A1-B1", rows[0]);
            Assert.AreEqual("SameName-SameName", rows[1]);
        }

        public TestContext TestContext { get; set; }
    }
}

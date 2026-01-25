using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class ComplexJoinTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

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
    public void HashJoin_ComplexEqui_Numeric_ShouldMatch()
    {
        var query = @"
select 
    a.Id, 
    b.Id 
from #A.entities() a 
inner join #B.entities() b on a.Id = b.Id + 1";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Id = 1 },
                    new BasicEntity { Id = 2 },
                    new BasicEntity { Id = 3 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Id = 0 },
                    new BasicEntity { Id = 1 },
                    new BasicEntity { Id = 2 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources,
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);


        var rows = table.OrderBy(r => (int)r[0]).ToList();

        Assert.AreEqual(1, rows[0][0]);
        Assert.AreEqual(0, rows[0][1]);

        Assert.AreEqual(2, rows[1][0]);
        Assert.AreEqual(1, rows[1][1]);

        Assert.AreEqual(3, rows[2][0]);
        Assert.AreEqual(2, rows[2][1]);
    }

    [TestMethod]
    public void HashJoin_ComplexEqui_String_ShouldMatch()
    {
        var query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
inner join #B.entities() b on a.Name = b.Name + '_Suffix'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "Item1_Suffix" },
                    new BasicEntity { Name = "Item2_Suffix" },
                    new BasicEntity { Name = "NoMatch" }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "Item1" },
                    new BasicEntity { Name = "Item2" },
                    new BasicEntity { Name = "Item3" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources,
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var rows = table.OrderBy(r => (string)r[0]).ToList();

        Assert.AreEqual("Item1_Suffix", rows[0][0]);
        Assert.AreEqual("Item1", rows[0][1]);

        Assert.AreEqual("Item2_Suffix", rows[1][0]);
        Assert.AreEqual("Item2", rows[1][1]);
    }

    [TestMethod]
    public void SortMergeJoin_ComplexInequality_Numeric_ShouldMatch()
    {
        var query = @"
select 
    a.Population, 
    b.Population 
from #A.entities() a 
inner join #B.entities() b on a.Population > b.Population + 100";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Population = 200 },
                    new BasicEntity { Population = 300 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Population = 50 },
                    new BasicEntity { Population = 150 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources,
            new CompilationOptions(useHashJoin: false, useSortMergeJoin: true));
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var rows = table.OrderBy(r => (decimal)r[0]).ThenBy(r => (decimal)r[1]).ToList();


        Assert.AreEqual(200m, rows[0][0]);
        Assert.AreEqual(50m, rows[0][1]);


        Assert.AreEqual(300m, rows[1][0]);
        Assert.AreEqual(50m, rows[1][1]);


        Assert.AreEqual(300m, rows[2][0]);
        Assert.AreEqual(150m, rows[2][1]);
    }

    [TestMethod]
    public void SortMergeJoin_ComplexInequality_DateTime_ShouldMatch()
    {
        var query = @"
select 
    a.Time, 
    b.Time 
from #A.entities() a 
inner join #B.entities() b on a.Time > b.AddDays(b.Time, 1)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Time = new DateTime(2023, 1, 5) }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Time = new DateTime(2023, 1, 1) },
                    new BasicEntity { Time = new DateTime(2023, 1, 3) }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources,
            new CompilationOptions(useHashJoin: false, useSortMergeJoin: true));
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var rows = table.OrderBy(r => (DateTime)r[1]).ToList();

        Assert.AreEqual(new DateTime(2023, 1, 5), rows[0][0]);
        Assert.AreEqual(new DateTime(2023, 1, 1), rows[0][1]);

        Assert.AreEqual(new DateTime(2023, 1, 5), rows[1][0]);
        Assert.AreEqual(new DateTime(2023, 1, 3), rows[1][1]);
    }

    [TestMethod]
    public void HashJoin_MixedComplexAndSimple_ShouldMatch()
    {
        var query = @"
select 
    a.Id, 
    b.Id 
from #A.entities() a 
inner join #B.entities() b on a.Id = b.Id + 1 AND a.Name = b.Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Id = 2, Name = "X" },
                    new BasicEntity { Id = 3, Name = "Y" }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Id = 1, Name = "X" },
                    new BasicEntity { Id = 2, Name = "Z" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources,
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(2, table[0][0]);
        Assert.AreEqual(1, table[0][1]);
    }

    [TestMethod]
    public void HashJoin_LeftModified_ShouldMatch()
    {
        var query = @"
select 
    a.Id, 
    b.Id 
from #A.entities() a 
inner join #B.entities() b on a.Id + 1 = b.Id";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Id = 1 },
                    new BasicEntity { Id = 2 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Id = 2 },
                    new BasicEntity { Id = 3 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources,
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var rows = table.OrderBy(r => (int)r[0]).ToList();

        Assert.AreEqual(1, rows[0][0]);
        Assert.AreEqual(2, rows[0][1]);

        Assert.AreEqual(2, rows[1][0]);
        Assert.AreEqual(3, rows[1][1]);
    }

    [TestMethod]
    public void HashJoin_BothModified_ShouldMatch()
    {
        var query = @"
select 
    a.Id, 
    b.Id 
from #A.entities() a 
inner join #B.entities() b on a.Id + 1 = b.Id - 1";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Id = 1 },
                    new BasicEntity { Id = 2 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Id = 3 },
                    new BasicEntity { Id = 4 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources,
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var rows = table.OrderBy(r => (int)r[0]).ToList();

        Assert.AreEqual(1, rows[0][0]);
        Assert.AreEqual(3, rows[0][1]);

        Assert.AreEqual(2, rows[1][0]);
        Assert.AreEqual(4, rows[1][1]);
    }

    [TestMethod]
    public void SortMergeJoin_LeftModified_ShouldMatch()
    {
        var query = @"
select 
    a.Population, 
    b.Population 
from #A.entities() a 
inner join #B.entities() b on a.Population - 50 > b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Population = 200 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Population = 100 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources,
            new CompilationOptions(useHashJoin: false, useSortMergeJoin: true));
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);

        Assert.AreEqual(200m, table[0][0]);
        Assert.AreEqual(100m, table[0][1]);
    }

    [TestMethod]
    public void SortMergeJoin_BothModified_ShouldMatch()
    {
        var query = @"
select 
    a.Population, 
    b.Population 
from #A.entities() a 
inner join #B.entities() b on a.Population / 2 > b.Population * 2";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Population = 500 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Population = 100 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources,
            new CompilationOptions(useHashJoin: false, useSortMergeJoin: true));
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);

        Assert.AreEqual(500m, table[0][0]);
        Assert.AreEqual(100m, table[0][1]);
    }
}

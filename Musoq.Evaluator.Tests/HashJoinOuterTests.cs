using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class HashJoinOuterTests : BasicEntityTestBase
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
    public void LeftJoin_HashJoin()
    {
        var query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
left join #B.entities() b on a.Population = b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 },
                    new BasicEntity { Name = "A2", Population = 10 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 100 }
                ]
            }
        };


        var vm = CreateAndRunVirtualMachine(query, sources,
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "HashJoin Left Join failed");

        var rows = table.OrderBy(r => r[0]).ToList();
        Assert.AreEqual("A1", rows[0][0]);
        Assert.AreEqual("B1", rows[0][1]);

        Assert.AreEqual("A2", rows[1][0]);
        Assert.IsNull(rows[1][1]);
    }

    [TestMethod]
    public void RightJoin_HashJoin()
    {
        var query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
right join #B.entities() b on a.Population = b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 100 },
                    new BasicEntity { Name = "B2", Population = 200 }
                ]
            }
        };


        var vm = CreateAndRunVirtualMachine(query, sources,
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "HashJoin Right Join failed");

        var rows = table.OrderBy(r => r[1]).ToList();
        Assert.AreEqual("A1", rows[0][0]);
        Assert.AreEqual("B1", rows[0][1]);

        Assert.IsNull(rows[1][0]);
        Assert.AreEqual("B2", rows[1][1]);
    }

    [TestMethod]
    public void LeftJoin_HashJoin_WithProjection()
    {
        var query = @"
select 
    a.Name + '_X', 
    b.Name 
from #A.entities() a 
left join #B.entities() b on a.Population = b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 },
                    new BasicEntity { Name = "A2", Population = 10 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 100 }
                ]
            }
        };


        var vm = CreateAndRunVirtualMachine(query, sources,
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "HashJoin Left Join failed");

        var rows = table.OrderBy(r => r[0]).ToList();
        Assert.AreEqual("A1_X", rows[0][0]);
        Assert.AreEqual("B1", rows[0][1]);

        Assert.AreEqual("A2_X", rows[1][0]);
        Assert.IsNull(rows[1][1]);
    }
}

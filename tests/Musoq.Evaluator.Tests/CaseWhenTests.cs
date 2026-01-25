using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class CaseWhenTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void WhenWhenTrueCaseWhenTest()
    {
        var query = "select (case when 1 = 1 then 1 else 0 end) as Value from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1, table[0].Values[0]);
    }

    [TestMethod]
    public void WhenWhenFalseCaseWhenTest()
    {
        var query = "select (case when 1 = 2 then 1 else 0 end) as Value from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0, table[0].Values[0]);
    }

    [TestMethod]
    public void WhenWhenTrueOnEntityFieldCaseWhenWithElseTest()
    {
        var query = "select (case when e.City = 'WARSAW' then 1 else 0 end) as Value from #A.entities() e";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1, table[0].Values[0]);
    }

    [TestMethod]
    public void WhenWhenTrueOnEntityField_ShouldNotThrowException()
    {
        var query =
            "select (case when e.City <> 'WROCLAW' then 'TEST' else e.ThrowException() end) as Value from #A.entities() e";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("TEST", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenWhenTrueOnEntityFieldWithAndCondition_ShouldNotThrowException()
    {
        var query =
            "select (case when e.City <> 'WROCLAW' AND e.City <> 'KRAKOW' then 'TEST' else e.ThrowException() end) as Value from #A.entities() e";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("TEST", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenWhenTrueOnEntityFieldWithOrCondition_ShouldNotThrowException()
    {
        var query =
            "select (case when e.City = 'WROCLAW' OR e.City = 'KRAKOW' then 'TEST' else e.ThrowException() end) as Value from #A.entities() e";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("KRAKOW", "POLAND", 500)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("TEST", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenWhenFalseOnEntityField_ShouldThrowException()
    {
        var query =
            "select (case when e.City = 'WROCLAW' then 'TEST' else e.ThrowException() end) as Value from #A.entities() e";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        Assert.Throws<MethodCallThrownException>(() => vm.Run(TestContext.CancellationToken));
    }
}

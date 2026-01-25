using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class ImplicitBooleanConversionTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void WhenMatchFunctionUsedWithExplicitTrueComparison_ShouldWork()
    {
        var query = "select Name from #A.entities() where Match('\\d+', Name) = true order by Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Test123"),
                    new BasicEntity("NoNumbers")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Test123", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenMatchFunctionUsedWithImplicitBooleanConversion_ShouldWork()
    {
        var query = "select Name from #A.entities() where Match('\\d+', Name) order by Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Test123"),
                    new BasicEntity("NoNumbers")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Test123", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenCaseWhenUsedWithExplicitTrueComparison_ShouldWork()
    {
        var query =
            "select (case when Match('\\d+', Name) = true then 'HasNumbers' else 'NoNumbers' end) as Result from #A.entities() order by Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Test123"),
                    new BasicEntity("NoNumbers")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("NoNumbers", table[0].Values[0]);
        Assert.AreEqual("HasNumbers", table[1].Values[0]);
    }

    [TestMethod]
    public void WhenCaseWhenUsedWithImplicitBooleanConversion_ShouldWork()
    {
        var query =
            "select (case when Match('\\d+', Name) then 'HasNumbers' else 'NoNumbers' end) as Result from #A.entities() order by Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Test123"),
                    new BasicEntity("NoNumbers")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("NoNumbers", table[0].Values[0]);
        Assert.AreEqual("HasNumbers", table[1].Values[0]);
    }
}

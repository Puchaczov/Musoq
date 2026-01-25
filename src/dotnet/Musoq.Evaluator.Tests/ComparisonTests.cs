using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class ComparisonTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void ArithmeticOpsGreaterTest()
    {
        var query = "select City from #A.entities() where Population > 400d";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("KATOWICE", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("City", table.Columns.ElementAt(0).ColumnName);

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
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("KATOWICE", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("City", table.Columns.ElementAt(0).ColumnName);

        Assert.AreEqual(2, table.Count(), "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "WARSAW"), "First entry should be 'WARSAW'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "CZESTOCHOWA"),
            "Second entry should be 'CZESTOCHOWA'");
    }

    [TestMethod]
    public void ArithmeticOpsEqualsTest()
    {
        var query = "select City from #A.entities() where Population = 250d";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("KATOWICE", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("City", table.Columns.ElementAt(0).ColumnName);

        Assert.AreEqual(2, table.Count());
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "KATOWICE"), "Collection should contain KATOWICE");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "BERLIN"), "Collection should contain BERLIN");
    }

    [TestMethod]
    public void ArithmeticOpsLessTest()
    {
        var query = "select City from #A.entities() where Population < 350d";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("KATOWICE", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("City", table.Columns.ElementAt(0).ColumnName);

        Assert.AreEqual(2, table.Count(), "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "KATOWICE"), "First entry should be 'KATOWICE'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "BERLIN"), "Second entry should be 'BERLIN'");
    }


    [TestMethod]
    public void ArithmeticOpsLessEqualTest()
    {
        var query = "select City from #A.entities() where Population <= 350d";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("KATOWICE", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("City", table.Columns.ElementAt(0).ColumnName);

        Assert.AreEqual(3, table.Count(), "Table should have 3 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "KATOWICE"), "First entry should be 'KATOWICE'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "BERLIN"), "Second entry should be 'BERLIN'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "MUNICH"), "Third entry should be 'MUNICH'");
    }
}

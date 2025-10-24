using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

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
                [
                    new BasicEntity("A", 100),
                    new BasicEntity("AB", 200),
                    new BasicEntity("ABC", 300),
                    new BasicEntity("ABCD", 400)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => (decimal)entry[0] == 100m), "First entry should be 100");
        Assert.IsTrue(table.Any(entry => (decimal)entry[0] == 400m), "Second entry should be 400");
    }

    [TestMethod]
    public void SimpleNotInOperator()
    {
        var query = "select Population from #A.Entities() where Population not in (100, 400)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A", 100),
                    new BasicEntity("AB", 200),
                    new BasicEntity("ABC", 300),
                    new BasicEntity("ABCD", 400)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => (decimal)entry[0] == 200m), "First entry should be 200");
        Assert.IsTrue(table.Any(entry => (decimal)entry[0] == 300m), "Second entry should be 300");
    }

    [TestMethod]
    public void InWithArgumentFromSourceOperator()
    {
        var query = "select Country from #A.Entities() where City in (Country, 'Warsaw')";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Poland", "Warsaw"),
                    new BasicEntity("Berlin", "Germany"),
                    new BasicEntity("Singapore", "Singapore"),
                    new BasicEntity("France", "Paris"),
                    new BasicEntity("Monaco", "Monaco")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(3, table.Count, "Table should have 3 entries");

        Assert.IsTrue(table.Any(entry => (string)entry[0] == "Poland"), "First entry should be Poland");
        Assert.IsTrue(table.Any(entry => (string)entry[0] == "Singapore"), "Second entry should be Singapore");
        Assert.IsTrue(table.Any(entry => (string)entry[0] == "Monaco"), "Third entry should be Monaco");
    }

    [TestMethod]
    public void NotInWithArgumentFromSourceOperator()
    {
        var query = "select Country from #A.Entities() where City not in (Country, 'Warsaw')";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Poland", "Warsaw"),
                    new BasicEntity("Berlin", "Germany"),
                    new BasicEntity("Singapore", "Singapore"),
                    new BasicEntity("France", "Paris"),
                    new BasicEntity("Monaco", "Monaco")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => (string)entry[0] == "Berlin"), "First entry should be Berlin");
        Assert.IsTrue(table.Any(entry => (string)entry[0] == "France"), "Second entry should be France");
    }
}

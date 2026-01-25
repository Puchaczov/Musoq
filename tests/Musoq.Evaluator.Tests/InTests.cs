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
        var table = vm.Run(TestContext.CancellationToken);

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
        var table = vm.Run(TestContext.CancellationToken);

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
        var table = vm.Run(TestContext.CancellationToken);

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
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => (string)entry[0] == "Berlin"), "First entry should be Berlin");
        Assert.IsTrue(table.Any(entry => (string)entry[0] == "France"), "Second entry should be France");
    }

    #region IN Operator Edge Cases

    [TestMethod]
    public void InWithSingleElement_ShouldWork()
    {
        var query = "select Population from #A.Entities() where Population in (100)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A", 100),
                    new BasicEntity("B", 200),
                    new BasicEntity("C", 100)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.All(entry => (decimal)entry[0] == 100m));
    }

    [TestMethod]
    public void InWithManyElements_ShouldWork()
    {
        var query =
            "select Population from #A.Entities() where Population in (100, 200, 300, 400, 500, 600, 700, 800, 900, 1000)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A", 100),
                    new BasicEntity("B", 250),
                    new BasicEntity("C", 500),
                    new BasicEntity("D", 750),
                    new BasicEntity("E", 1000)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.IsTrue(table.Any(entry => (decimal)entry[0] == 100m));
        Assert.IsTrue(table.Any(entry => (decimal)entry[0] == 500m));
        Assert.IsTrue(table.Any(entry => (decimal)entry[0] == 1000m));
    }

    [TestMethod]
    public void InWithStrings_ShouldWork()
    {
        var query = "select Name from #A.Entities() where Name in ('Alice', 'Bob', 'Charlie')";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Alice"),
                    new BasicEntity("David"),
                    new BasicEntity("Bob"),
                    new BasicEntity("Eve"),
                    new BasicEntity("Charlie")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.IsTrue(table.Any(entry => (string)entry[0] == "Alice"));
        Assert.IsTrue(table.Any(entry => (string)entry[0] == "Bob"));
        Assert.IsTrue(table.Any(entry => (string)entry[0] == "Charlie"));
    }

    [TestMethod]
    public void InWithDuplicatesInList_ShouldWork()
    {
        var query = "select Population from #A.Entities() where Population in (100, 100, 200, 200)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A", 100),
                    new BasicEntity("B", 200),
                    new BasicEntity("C", 300)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(entry => (decimal)entry[0] == 100m));
        Assert.IsTrue(table.Any(entry => (decimal)entry[0] == 200m));
    }

    [TestMethod]
    public void InWithNoMatches_ShouldReturnEmpty()
    {
        var query = "select Population from #A.Entities() where Population in (999, 888, 777)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A", 100),
                    new BasicEntity("B", 200),
                    new BasicEntity("C", 300)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void NotInWithNoMatches_ShouldReturnAll()
    {
        var query = "select Population from #A.Entities() where Population not in (999, 888)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A", 100),
                    new BasicEntity("B", 200)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    public TestContext TestContext { get; set; }

    #endregion
}

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class StringComparisonConsistencyTests
{
    [TestMethod]
    public void OrderBy_StringsSortedByOrdinal_AscendingOrder()
    {
        var query = "select Name from #A.entities() order by Name asc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("banana"),
                    new BasicEntity("Apple"),
                    new BasicEntity("cherry"),
                    new BasicEntity("Banana")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);


        Assert.AreEqual("Apple", table[0].Values[0]);
        Assert.AreEqual("Banana", table[1].Values[0]);
        Assert.AreEqual("banana", table[2].Values[0]);
        Assert.AreEqual("cherry", table[3].Values[0]);
    }

    [TestMethod]
    public void OrderBy_StringsSortedByOrdinal_DescendingOrder()
    {
        var query = "select Name from #A.entities() order by Name desc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("banana"),
                    new BasicEntity("Apple"),
                    new BasicEntity("cherry"),
                    new BasicEntity("Banana")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);


        Assert.AreEqual("cherry", table[0].Values[0]);
        Assert.AreEqual("banana", table[1].Values[0]);
        Assert.AreEqual("Banana", table[2].Values[0]);
        Assert.AreEqual("Apple", table[3].Values[0]);
    }

    [TestMethod]
    public void OrderBy_UnicodeStrings_DeterministicSort()
    {
        var query = "select Name from #A.entities() order by Name asc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("zzz"),
                    new BasicEntity("Ąbc"),
                    new BasicEntity("abc"),
                    new BasicEntity("Abc")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);


        Assert.AreEqual("Abc", table[0].Values[0]);
        Assert.AreEqual("abc", table[1].Values[0]);
        Assert.AreEqual("zzz", table[2].Values[0]);
        Assert.AreEqual("Ąbc", table[3].Values[0]);
    }

    [TestMethod]
    public void OrderBy_Integers_NotAffectedByStringComparer()
    {
        var query = "select Population from #A.entities() order by Population asc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("c", 300),
                    new BasicEntity("a", 100),
                    new BasicEntity("b", 200)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual(100m, table[0].Values[0]);
        Assert.AreEqual(200m, table[1].Values[0]);
        Assert.AreEqual(300m, table[2].Values[0]);
    }



    [TestMethod]
    public void Where_ContainsCaseInsensitive_MatchesDifferentCases()
    {
        var query = "select Name from #A.entities() where Contains(Name, 'world')";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Hello World"),
                    new BasicEntity("HELLO WORLD"),
                    new BasicEntity("hello world"),
                    new BasicEntity("goodbye")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count, "Contains should be case-insensitive");
    }

    [TestMethod]
    public void Where_StartsWithCaseInsensitive_MatchesDifferentCases()
    {
        var query = "select Name from #A.entities() where StartsWith(Name, 'hello')";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Hello World"),
                    new BasicEntity("HELLO WORLD"),
                    new BasicEntity("hello world"),
                    new BasicEntity("world hello")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count, "StartsWith should be case-insensitive");
    }

    [TestMethod]
    public void Where_EndsWithCaseInsensitive_MatchesDifferentCases()
    {
        var query = "select Name from #A.entities() where EndsWith(Name, 'world')";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Hello World"),
                    new BasicEntity("HELLO WORLD"),
                    new BasicEntity("hello world"),
                    new BasicEntity("world hello")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count, "EndsWith should be case-insensitive");
    }



    [TestMethod]
    public void Select_ToUpperUsesInvariantCulture()
    {
        var query = "select ToUpper(Name) from #A.entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("hello world"),
                    new BasicEntity("zażółć")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "HELLO WORLD"));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "ZAŻÓŁĆ"));
    }

    [TestMethod]
    public void Select_ToLowerUsesInvariantCulture()
    {
        var query = "select ToLower(Name) from #A.entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("HELLO WORLD"),
                    new BasicEntity("ZAŻÓŁĆ")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "hello world"));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "zażółć"));
    }

    [TestMethod]
    public void Select_ReplaceCaseInsensitive()
    {
        var query = "select Replace(Name, 'hello', 'Hi') from #A.entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Hello World"),
                    new BasicEntity("HELLO World"),
                    new BasicEntity("hello World")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.IsTrue(table.All(row => ((string)row.Values[0]).StartsWith("Hi")),
            "Replace should be case-insensitive and replace all case variants");
    }

    [TestMethod]
    public void Select_IndexOfCaseInsensitive()
    {
        var query = "select IndexOf(Name, 'WORLD') from #A.entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("hello world"),
                    new BasicEntity("Hello World"),
                    new BasicEntity("HELLO WORLD")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.IsTrue(table.All(row => (int?)row.Values[0] == 6),
            "IndexOf should be case-insensitive and return 6 for all rows");
    }



}

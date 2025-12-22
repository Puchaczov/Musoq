using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests;

/// <summary>
/// Tests for hash-optional schema syntax (e.g., "from schema.method()" without the # prefix).
/// The parser normalizes schema names by adding # if not present, so both syntaxes work.
/// </summary>
[TestClass]
public class HashOptionalSchemaTests : BasicEntityTestBase
{
    [TestMethod]
    public void HashOptional_SimpleSelect_ShouldWork()
    {
        // Query uses "A.Entities()" without # - parser normalizes to "#A"
        var query = "select Name from A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("Test1"), new BasicEntity("Test2")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
        var values = new HashSet<string> { (string)table[0][0], (string)table[1][0] };
        Assert.Contains("Test1", values, "Expected Test1 to be in results");
        Assert.Contains("Test2", values, "Expected Test2 to be in results");
    }

    [TestMethod]
    public void HashOptional_SelectWithWhere_ShouldWork()
    {
        var query = "select Name from A.Entities() where Name = 'Test1'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("Test1"), new BasicEntity("Test2")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Test1", table[0][0]);
    }

    [TestMethod]
    public void HashOptional_SelectWithArithmetic_ShouldWork()
    {
        var query = "select 1 + 2 * 3 from A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("Test")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(7, System.Convert.ToInt32(table[0][0]));
    }

    [TestMethod]
    public void HashOptional_SelectWithOrderBy_ShouldWork()
    {
        var query = "select Name from A.Entities() order by Name desc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("A"), new BasicEntity("C"), new BasicEntity("B")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("C", table[0][0]);
        Assert.AreEqual("B", table[1][0]);
        Assert.AreEqual("A", table[2][0]);
    }

    [TestMethod]
    public void HashOptional_ReorderedQuery_ShouldWork()
    {
        var query = "from A.Entities() select Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("Test1"), new BasicEntity("Test2")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void HashOptional_WithGroupBy_ShouldWork()
    {
        var query = "select City, Count(City) from A.Entities() group by City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [
                new BasicEntity { City = "Warsaw" },
                new BasicEntity { City = "Warsaw" },
                new BasicEntity { City = "London" }
            ]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void HashOptional_WithAlias_ShouldWork()
    {
        var query = "select a.Name from A.Entities() a";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("Test1")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Test1", table[0][0]);
    }

    [TestMethod]
    public void HashOptional_WithSkipTake_ShouldWork()
    {
        var query = "select Name from A.Entities() order by Name skip 1 take 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [
                new BasicEntity("A"),
                new BasicEntity("B"),
                new BasicEntity("C"),
                new BasicEntity("D")
            ]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("B", table[0][0]);
        Assert.AreEqual("C", table[1][0]);
    }

    [TestMethod]
    public void HashOptional_InnerJoin_ShouldWork()
    {
        var query = "select a.Name from A.Entities() a inner join B.Entities() b on a.Name = b.Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("Common"), new BasicEntity("OnlyA")]},
            {"#B", [new BasicEntity("Common"), new BasicEntity("OnlyB")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Common", table[0][0]);
    }

    [TestMethod]
    public void HashOptional_MixedSyntax_WithAndWithoutHash_ShouldWork()
    {
        // Mix of hash and non-hash syntax in the same query
        var query = "select a.Name from #A.Entities() a inner join B.Entities() b on a.Name = b.Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("Common"), new BasicEntity("OnlyA")]},
            {"#B", [new BasicEntity("Common"), new BasicEntity("OnlyB")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Common", table[0][0]);
    }

    [TestMethod]
    public void HashSyntax_StillWorks_ShouldWork()
    {
        // Verify existing hash syntax still works
        var query = "select Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("Test1"), new BasicEntity("Test2")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void HashOptional_WithDistinct_ShouldWork()
    {
        var query = "select distinct Name from A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [
                new BasicEntity("Test"),
                new BasicEntity("Test"),
                new BasicEntity("Other")
            ]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void HashOptional_UnionOperator_ShouldWork()
    {
        var query = "select Name from A.Entities() union (Name) select Name from B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("First")]},
            {"#B", [new BasicEntity("Second")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void HashOptional_CteQuery_ShouldWork()
    {
        var query = "with cte as (select Name, City from A.Entities()) select Name from cte";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("Test") { City = "Warsaw" }]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Test", table[0][0]);
    }
}

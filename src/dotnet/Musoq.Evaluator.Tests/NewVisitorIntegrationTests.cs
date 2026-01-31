using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Additional integration tests to ensure comprehensive visitor coverage.
/// </summary>
[TestClass]
public class NewVisitorIntegrationTests : BasicEntityTestBase
{
    [TestMethod]
    public void NewVisitor_SimpleSelect_ShouldWork()
    {
        var query = "select Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test1"), new BasicEntity("Test2")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
        var values = new HashSet<string> { (string)table[0][0], (string)table[1][0] };
        Assert.Contains("Test1", values, "Expected Test1 to be in results");
        Assert.Contains("Test2", values, "Expected Test2 to be in results");
    }

    [TestMethod]
    public void NewVisitor_SelectWithWhere_ShouldWork()
    {
        var query = "select Name from #A.Entities() where Name = 'Test1'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test1"), new BasicEntity("Test2")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Test1", table[0][0]);
    }

    [TestMethod]
    public void NewVisitor_SelectWithArithmetic_ShouldWork()
    {
        var query = "select 1 + 2 * 3 from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(7, Convert.ToInt32(table[0][0]));
    }

    [TestMethod]
    public void NewVisitor_SelectWithOrderBy_ShouldWork()
    {
        var query = "select Name from #A.Entities() order by Name desc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("A"), new BasicEntity("C"), new BasicEntity("B")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("C", table[0][0]);
        Assert.AreEqual("B", table[1][0]);
        Assert.AreEqual("A", table[2][0]);
    }

    [TestMethod]
    public void NewVisitor_SelectWithStringConcat_ShouldWork()
    {
        var query = "select Name + ' - ' + City from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "Test", City = "NYC" }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Test - NYC", table[0][0]);
    }

    [TestMethod]
    public void NewVisitor_SelectWithGroupBy_ShouldWork()
    {
        var query = "select City, Count(City) from #A.Entities() group by City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A", City = "NYC" },
                    new BasicEntity { Name = "B", City = "NYC" },
                    new BasicEntity { Name = "C", City = "LA" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void NewVisitor_SelectWithJoin_ShouldWork()
    {
        var query = @"
            select a.Name, b.Name
            from #A.Entities() a
            inner join #B.Entities() b on a.City = b.City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "PersonA", City = "NYC" }] },
            { "#B", [new BasicEntity { Name = "PersonB", City = "NYC" }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("PersonA", table[0][0]);
        Assert.AreEqual("PersonB", table[0][1]);
    }

    [TestMethod]
    public void NewVisitor_SelectWithUnionAll_ShouldWork()
    {
        var query = @"
            select Name from #A.Entities()
            union all (Name)
            select Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test1")] },
            { "#B", [new BasicEntity("Test2")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
    }
}

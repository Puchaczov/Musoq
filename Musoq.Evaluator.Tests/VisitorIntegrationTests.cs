using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Integration tests for ToCSharpRewriteTreeVisitor functionality.
/// </summary>
[TestClass]
public class VisitorIntegrationTests : BasicEntityTestBase
{
    [TestMethod]
    public void Visitor_SimpleSelect_ShouldWork()
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
    public void Visitor_SelectWithWhere_ShouldWork()
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
    public void Visitor_SelectWithArithmetic_ShouldWork()
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
    public void Visitor_SelectWithOrderBy_ShouldWork()
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
    public void Visitor_SelectWithLike_ShouldWork()
    {
        var query = "select Name from #A.Entities() where Name like 'Test%'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test1"), new BasicEntity("Other"), new BasicEntity("Test2")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Visitor_SelectWithIn_ShouldWork()
    {
        var query = "select Name from #A.Entities() where Name in ('Test1', 'Test2')";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test1"), new BasicEntity("Other"), new BasicEntity("Test2")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Visitor_SelectWithSkipTake_ShouldWork()
    {
        var query = "select Name from #A.Entities() order by Name skip 1 take 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("A"), new BasicEntity("B"), new BasicEntity("C"), new BasicEntity("D")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("B", table[0][0]);
        Assert.AreEqual("C", table[1][0]);
    }

    [TestMethod]
    public void Visitor_SelectWithStringConcat_ShouldWork()
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
    public void Visitor_SelectWithSingleStringConcat_ShouldWork()
    {
        var query = "select Name + City from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "Test", City = "NYC" }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("TestNYC", table[0][0]);
    }

    [TestMethod]
    public void Visitor_SelectWithNot_ShouldWork()
    {
        var query = "select Name from #A.Entities() where Name <> 'Test1'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test1"), new BasicEntity("Test2")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Test2", table[0][0]);
    }

    [TestMethod]
    public void Visitor_SelectWithGreaterThan_ShouldWork()
    {
        var query = "select Population from #A.Entities() where Population > 100 and Population < 300";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("A", 50),
                    new BasicEntity("B", 200),
                    new BasicEntity("C", 400)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(200m, table[0][0]);
    }

    [TestMethod]
    public void Visitor_SelectWithIsNull_ShouldWork()
    {
        var query = "select Name from #A.Entities() where NullableValue is null";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("HasValue") { NullableValue = 5 },
                    new BasicEntity("NoValue") { NullableValue = null }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("NoValue", table[0][0]);
    }

    [TestMethod]
    public void Visitor_SelectWithCoalesce_ShouldWork()
    {
        var query = "select Coalesce(NullableValue, 0) from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Test") { NullableValue = null }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
    }
}

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

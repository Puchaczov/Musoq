using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Schema;

using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class HashJoinCompositeKeysTests : BasicEntityTestBase
{
    [TestMethod]
    public void InnerJoin_WithCompositeKey_ShouldUseHashJoin()
    {
        const string query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
inner join #B.entities() b 
on a.Country = b.Country AND a.City = b.City";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "John", City = "New York", Country = "USA" },
                    new BasicEntity { Name = "Alice", City = "London", Country = "UK" },
                    new BasicEntity { Name = "Bob", City = "Paris", Country = "France" }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "Doe", City = "New York", Country = "USA" }, // Match John
                    new BasicEntity { Name = "Smith", City = "London", Country = "UK" },  // Match Alice
                    new BasicEntity { Name = "Pierre", City = "Lyon", Country = "France" } // No match (different city)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources, new CompilationOptions(useHashJoin: true));
        var table = vm.Run();

        Assert.AreEqual(2, table.Count, "Should have 2 matches");
        
        var rows = table.OrderBy(r => r[0]).ToList();
        
        Assert.AreEqual("Alice", rows[0][0]);
        Assert.AreEqual("Smith", rows[0][1]);
        
        Assert.AreEqual("John", rows[1][0]);
        Assert.AreEqual("Doe", rows[1][1]);
    }

    [TestMethod]
    public void LeftOuterJoin_WithCompositeKey_ShouldUseHashJoin()
    {
        const string query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
left outer join #B.entities() b 
on a.Country = b.Country AND a.City = b.City";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "John", City = "New York", Country = "USA" },
                    new BasicEntity { Name = "Bob", City = "Paris", Country = "France" }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "Doe", City = "New York", Country = "USA" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources, new CompilationOptions(useHashJoin: true));
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
        
        var rows = table.OrderBy(r => r[0]).ToList();
        
        Assert.AreEqual("Bob", rows[0][0]);
        Assert.IsNull(rows[0][1]);
        
        Assert.AreEqual("John", rows[1][0]);
        Assert.AreEqual("Doe", rows[1][1]);
    }

    [TestMethod]
    public void RightOuterJoin_WithCompositeKey_ShouldUseHashJoin()
    {
        const string query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
right outer join #B.entities() b 
on a.Country = b.Country AND a.City = b.City";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "John", City = "New York", Country = "USA" }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "Doe", City = "New York", Country = "USA" }, // Match John
                    new BasicEntity { Name = "Pierre", City = "Paris", Country = "France" } // No match
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources, new CompilationOptions(useHashJoin: true));
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
        
        var rows = table.OrderBy(r => r[1]).ToList();
        
        Assert.AreEqual("John", rows[0][0]);
        Assert.AreEqual("Doe", rows[0][1]);
        
        Assert.IsNull(rows[1][0]);
        Assert.AreEqual("Pierre", rows[1][1]);
    }

    [TestMethod]
    public void InnerJoin_WithNullsInCompositeKey_ShouldNotMatch()
    {
        const string query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
inner join #B.entities() b 
on a.Country = b.Country AND a.City = b.City";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "John", City = null, Country = "USA" }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "Doe", City = null, Country = "USA" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources, new CompilationOptions(useHashJoin: true));
        var table = vm.Run();

        Assert.AreEqual(0, table.Count, "Should have 0 matches because NULL != NULL");
    }

    [TestMethod]
    public void InnerJoin_WithThreeCompositeKeys_ShouldWork()
    {
        const string query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
inner join #B.entities() b 
on a.Country = b.Country AND a.City = b.City AND a.Month = b.Month";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "John", City = "NY", Country = "USA", Month = "Jan" },
                    new BasicEntity { Name = "Alice", City = "NY", Country = "USA", Month = "Feb" }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "Doe", City = "NY", Country = "USA", Month = "Jan" }, // Match John
                    new BasicEntity { Name = "Smith", City = "NY", Country = "USA", Month = "Mar" } // No match
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources, new CompilationOptions(useHashJoin: true));
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("John", table[0][0]);
        Assert.AreEqual("Doe", table[0][1]);
    }

    [TestMethod]
    public void LeftOuterJoin_WithEmptySource_ShouldReturnAllLeft()
    {
        const string query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
left outer join #B.entities() b 
on a.Country = b.Country AND a.City = b.City";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "John", City = "NY", Country = "USA" }
                ]
            },
            {
                "#B", new List<BasicEntity>() // Empty
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources, new CompilationOptions(useHashJoin: true));
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("John", table[0][0]);
        Assert.IsNull(table[0][1]);
    }

    [TestMethod]
    public void InnerJoin_WithAdditionalNonEquiCondition()
    {
        const string query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
inner join #B.entities() b 
on a.Country = b.Country AND a.City = b.City AND a.Population > b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "BigCity", City = "NY", Country = "USA", Population = 1000 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "SmallCity", City = "NY", Country = "USA", Population = 100 }, // Match (1000 > 100)
                    new BasicEntity { Name = "HugeCity", City = "NY", Country = "USA", Population = 2000 }  // No match (1000 < 2000)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources, new CompilationOptions(useHashJoin: true));
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("BigCity", table[0][0]);
        Assert.AreEqual("SmallCity", table[0][1]);
    }

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
}

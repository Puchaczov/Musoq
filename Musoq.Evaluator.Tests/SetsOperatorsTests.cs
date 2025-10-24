using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class SetsOperatorsTests : BasicEntityTestBase
{
    [TestMethod]
    public void UnionWithDifferentColumnsAsAKeyTest()
    {
        var query = @"select Name from #A.Entities() union (Name) select City as Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001"), new BasicEntity("002")]},
            {"#B", [new BasicEntity("003", "", 0), new BasicEntity("004", "", 0)]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.IsTrue(table.Count == 4, "Table should contain 4 rows");

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "001"), "Missing 001");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "002"), "Missing 002");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "003"), "Missing 003");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "004"), "Missing 004");
    }
    
    [TestMethod]
    public void AliasedUnionWithDifferentColumnsAsAKeyTest()
    {
        var query =
            """
select 
    a.Name as a1,
    b.Value
from #A.Entities() a
cross apply a.ToCharArray(a.Name) b
union (Name) 
select 
    a.Name as a1,
    b.Value
from #A.Entities() a
cross apply a.ToCharArray(a.Name) b
""";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001"), new BasicEntity("002")]},
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
    }

    [TestMethod]
    public void UnionWithSkipTest()
    {
        var query = @"select Name from #A.Entities() skip 1 union (Name) select Name from #B.Entities() skip 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001"), new BasicEntity("002")]},
            {"#B", [new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("005")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.IsTrue(table.Count == 2, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "002"), "First entry should be '002'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "005"), "Second entry should be '005'");
    }

    [TestMethod]
    public void UnionAllWithSkipTest()
    {
        var query = @"select Name from #A.Entities() skip 1 union all (Name) select Name from #B.Entities() skip 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001"), new BasicEntity("005")]},
            {"#B", [new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("005")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.IsTrue(table.Count == 2, "Table should have 2 entries");

        Assert.IsTrue(table.All(entry => (string)entry.Values[0] == "005"), "All entries should be '005'");
    }

    [TestMethod]
    public void MultipleUnionAllTest()
    {
        var query = @"
select Name from #A.Entities() union all (Name) 
select Name from #A.Entities() union all (Name) 
select Name from #A.Entities() union all (Name) 
select Name from #A.Entities() union all (Name) 
select Name from #A.Entities()";
            
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("005")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.IsTrue(table.Count == 5, "Table should have 5 entries");

        Assert.IsTrue(table.All(entry => 
                (string)entry.Values[0] == "005"), 
            "All entries should be '005'");
    }

    [TestMethod]
    public void UnionAllWhenMultipleSelectsCombinedWithUnionAllWithinCteExpression_ShouldSucceed()
    {
        var query = @"
with p as (
    select 1 as Id, 'EMPTY' as Name from #A.Entities()
    union all (Name)
    select 2 as Id, 'EMPTY2' as Name from #A.Entities()
    union all (Name)
    select 3 as Id, 'EMPTY3' as Name from #A.Entities()
)
select Id, Name from p
";
            
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("005")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.IsTrue(table.Count == 3, "Table should have 3 entries");

        Assert.IsTrue(table.Any(entry => 
                Convert.ToInt32(entry.Values[0]) == 1 && 
                (string)entry.Values[1] == "EMPTY"), 
            "First entry should match expected values");

        Assert.IsTrue(table.Any(entry => 
                Convert.ToInt32(entry.Values[0]) == 2 && 
                (string)entry.Values[1] == "EMPTY2"), 
            "Second entry should match expected values");

        Assert.IsTrue(table.Any(entry => 
                Convert.ToInt32(entry.Values[0]) == 3 && 
                (string)entry.Values[1] == "EMPTY3"), 
            "Third entry should match expected values");
    }

    [TestMethod]
    public void UnionWhenMultipleSelectsCombinedWithUnionWithinCteExpression_ShouldSucceed()
    {
        var query = @"
with p as (
    select 1 as Id, 'EMPTY' as Name from #A.Entities()
    union (Name)
    select 2 as Id, 'EMPTY2' as Name from #A.Entities()
    union (Name)
    select 3 as Id, 'EMPTY3' as Name from #A.Entities()
)
select Id, Name from p
";
            
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("005")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.IsTrue(table.Count == 3, "Table should have 3 entries");

        Assert.IsTrue(table.Any(entry => 
                Convert.ToInt32(entry.Values[0]) == 1 && 
                (string)entry.Values[1] == "EMPTY"), 
            "First entry should match expected values");

        Assert.IsTrue(table.Any(entry => 
                Convert.ToInt32(entry.Values[0]) == 2 && 
                (string)entry.Values[1] == "EMPTY2"), 
            "Second entry should match expected values");

        Assert.IsTrue(table.Any(entry => 
                Convert.ToInt32(entry.Values[0]) == 3 && 
                (string)entry.Values[1] == "EMPTY3"), 
            "Third entry should match expected values");
    }

    [TestMethod]
    public void MultipleUnionAllWithSkipTest()
    {
        var query = @"
select Name from #A.Entities() skip 1 
union all (Name) 
select Name from #B.Entities() skip 2
union all (Name)
select Name from #C.Entities() skip 3";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001"), new BasicEntity("005")]},
            {"#B", [new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("005")]},
            {
                "#C",
                [
                    new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("004"), new BasicEntity("005")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.IsTrue(table.Count == 3, "Table should have 3 entries");

        Assert.IsTrue(table.All(entry => (string)entry.Values[0] == "005"), "All entries should be '005'");
    }

    [TestMethod]
    public void UnionWithoutDuplicatedKeysTest()
    {
        var query = @"select Name from #A.Entities() union (Name) select Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001"), new BasicEntity("002")]},
            {"#B", [new BasicEntity("003"), new BasicEntity("004")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.IsTrue(table.Count == 4, "Table should contain 4 rows");

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "001") &&
                      table.Any(row => (string)row.Values[0] == "002") &&
                      table.Any(row => (string)row.Values[0] == "003") &&
                      table.Any(row => (string)row.Values[0] == "004"),
            "Expected rows with values 001, 002, 003, and 004");
    }

    [TestMethod]
    public void UnionWithDuplicatedKeysTest()
    {
        var query = @"select Name from #A.Entities() union (Name) select Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001"), new BasicEntity("002")]},
            {"#B", [new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("005")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.IsTrue(table.Count == 3, "Table should have 3 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "001"), "First entry should be '001'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "002"), "Second entry should be '002'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "005"), "Third entry should be '005'");
    }

    [TestMethod]
    public void MultipleUnionsWithDuplicatedKeysTest()
    {
        var query =
            @"select Name from #A.Entities() union (Name) select Name from #B.Entities() union (Name) select Name from #C.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001")]},
            {"#B", [new BasicEntity("001"), new BasicEntity("002")]},
            {"#C", [new BasicEntity("005")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.IsTrue(table.Count == 3, "Table should have 3 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "001"), "First entry should be '001'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "002"), "Second entry should be '002'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "005"), "Third entry should be '005'");
    }

    [TestMethod]
    public void MultipleUnionsWithoutDuplicatedKeysTest()
    {
        var query =
            @"select Name from #A.Entities() union (Name) select Name from #B.Entities() union (Name) select Name from #C.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001")]},
            {"#B", [new BasicEntity("002")]},
            {"#C", [new BasicEntity("005")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.IsTrue(table.Count == 3, "Table should have 3 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "001"), "First entry should be '001'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "002"), "Second entry should be '002'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "005"), "Third entry should be '005'");
    }

    [TestMethod]
    public void MultipleUnionsComplexTest()
    {
        var query =
            @"
select Name from #A.Entities() union (Name) 
select Name from #B.Entities() union (Name) 
select Name from #C.Entities() union (Name) 
select Name from #D.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001")]},
            {"#B", [new BasicEntity("002")]},
            {"#C", [new BasicEntity("005")]},
            {"#D", [new BasicEntity("007"), new BasicEntity("001")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.IsTrue(table.Count == 4, "Table should have 4 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "001"), "First entry should be '001'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "002"), "Second entry should be '002'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "005"), "Third entry should be '005'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "007"), "Fourth entry should be '007'");
    }

    [TestMethod]
    public void UnionAllWithDuplicatedKeysTest()
    {
        var query = @"select Name from #A.Entities() union all (Name) select Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001"), new BasicEntity("002")]},
            {"#B", [new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("005")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.IsTrue(table.Count == 5, "Table should have 5 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "001"), "First entry should be '001'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "002"), "Second entry should be '002'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "001"), "Third entry should be '001'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "002"), "Fourth entry should be '002'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "005"), "Fifth entry should be '005'");
    }

    [TestMethod]
    public void MultipleUnionsAllWithDuplicatedKeysTest()
    {
        var query =
            @"select Name from #A.Entities() union all (Name) select Name from #B.Entities() union all (Name) select Name from #C.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001")]},
            {"#B", [new BasicEntity("001"), new BasicEntity("002")]},
            {"#C", [new BasicEntity("005")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("001", table[0].Values[0]);
        Assert.AreEqual("001", table[1].Values[0]);
        Assert.AreEqual("002", table[2].Values[0]);
        Assert.AreEqual("005", table[3].Values[0]);
    }

    [TestMethod]
    public void MultipleUnionsAllWithoutDuplicatedKeysTest()
    {
        var query =
            @"select Name from #A.Entities() union all (Name) select Name from #B.Entities() union all (Name) select Name from #C.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001")]},
            {"#B", [new BasicEntity("002")]},
            {"#C", [new BasicEntity("005")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("001", table[0].Values[0]);
        Assert.AreEqual("002", table[1].Values[0]);
        Assert.AreEqual("005", table[2].Values[0]);
    }

    [TestMethod]
    public void MultipleUnionsAllComplexTest()
    {
        var query =
            @"
select Name from #A.Entities() union all (Name) 
select Name from #B.Entities() union all (Name) 
select Name from #C.Entities() union all (Name) 
select Name from #D.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001")]},
            {"#B", [new BasicEntity("002")]},
            {"#C", [new BasicEntity("005")]},
            {"#D", [new BasicEntity("007"), new BasicEntity("001")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.IsTrue(table.Count == 5, "Table should have 5 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "001"), "First entry should be '001'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "002"), "Second entry should be '002'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "005"), "Third entry should be '005'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "007"), "Fourth entry should be '007'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "001"), "Fifth entry should be '001'");
    }

    [TestMethod]
    public void UnionAllWithoutDuplicatedKeysTest()
    {
        var query = @"select Name from #A.Entities() union all (Name) select Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001"), new BasicEntity("002")]},
            {"#B", [new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.IsTrue(table.Count == 5, "Table should contain 5 rows");

        Assert.IsTrue(table.Count(row => (string)row.Values[0] == "001") == 2 &&
                      table.Any(row => (string)row.Values[0] == "002") &&
                      table.Any(row => (string)row.Values[0] == "003") &&
                      table.Any(row => (string)row.Values[0] == "004"),
            "Expected two rows with 001 and one row each with 002, 003, and 004");
    }

    [TestMethod]
    public void ExceptDoubleSourceTest()
    {
        var query = @"select Name from #A.Entities() except (Name) select Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001"), new BasicEntity("002")]},
            {"#B", [new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("002", table[0].Values[0]);
    }

    [TestMethod]
    public void ExceptWithSkipDoubleSourceTest()
    {
        var query = @"select Name from #A.Entities() skip 1 except (Name) select Name from #B.Entities() skip 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("010")]},
            {"#B", [new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("002")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("010", table[0].Values[0]);
    }

    [TestMethod]
    public void ExceptTripleSourcesTest()
    {
        var query =
            @"select Name from #A.Entities() except (Name) select Name from #B.Entities() except (Name) select Name from #C.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001"), new BasicEntity("002")]},
            {"#B", [new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")]},
            {"#C", [new BasicEntity("002")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void ExceptWithSkipTripleSourcesTest()
    {
        var query =
            @"select Name from #A.Entities() skip 1 except (Name) 
select Name from #B.Entities() skip 2 except (Name) 
select Name from #C.Entities() skip 3";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001"), new BasicEntity("002")]},
            {"#B", [new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")]},
            {"#C", [new BasicEntity("005")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("002", table[0].Values[0]);
    }

    [TestMethod]
    public void ExceptMultipleSourcesTest()
    {
        var query =
            @"
select Name from #A.Entities() except (Name) 
select Name from #B.Entities() except (Name) 
select Name from #C.Entities() except (Name) 
select Name from #D.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("007"), new BasicEntity("008")
                ]
            },
            {"#B", [new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")]},
            {"#C", [new BasicEntity("005")]},
            {"#D", [new BasicEntity("007")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Count(r => (string)r.Values[0] == "002") == 1, "Expected one row with '002'");
        Assert.IsTrue(table.Count(r => (string)r.Values[0] == "008") == 1, "Expected one row with '008'");
    }

    [TestMethod]
    public void IntersectDoubleSourceTest()
    {
        var query = @"select Name from #A.Entities() intersect (Name) select Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001"), new BasicEntity("002")]},
            {"#B", [new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("001", table[0].Values[0]);
    }

    [TestMethod]
    public void IntersectWithSkipDoubleSourceTest()
    {
        var query = @"select Name from #A.Entities() skip 1 intersect (Name) select Name from #B.Entities() skip 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("005")]},
            {
                "#B",
                [
                    new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001"), new BasicEntity("005")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("005", table[0].Values[0]);
    }

    [TestMethod]
    public void IntersectTripleSourcesTest()
    {
        var query =
            @"select Name from #A.Entities() intersect (Name) select Name from #B.Entities() intersect (Name) select Name from #C.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001"), new BasicEntity("002")]},
            {"#B", [new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")]},
            {"#C", [new BasicEntity("002"), new BasicEntity("001")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("001", table[0].Values[0]);
    }

    [TestMethod]
    public void IntersectWithSkipTripleSourcesTest()
    {
        var query =
            @"
select Name from #A.Entities() skip 1 intersect (Name) 
select Name from #B.Entities() skip 2 intersect (Name) 
select Name from #C.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("005")]},
            {
                "#B",
                [
                    new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001"), new BasicEntity("005")
                ]
            },
            {"#C", [new BasicEntity("002"), new BasicEntity("001"), new BasicEntity("005")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("005", table[0].Values[0]);
    }

    [TestMethod]
    public void IntersectMultipleSourcesTest()
    {
        var query =
            @"
select Name from #A.Entities() intersect (Name) 
select Name from #B.Entities() intersect (Name) 
select Name from #C.Entities() intersect (Name) 
select Name from #D.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("007"), new BasicEntity("008")
                ]
            },
            {"#B", [new BasicEntity("003"), new BasicEntity("007"), new BasicEntity("001")]},
            {"#C", [new BasicEntity("005"), new BasicEntity("007"), new BasicEntity("001")]},
            {"#D", [new BasicEntity("008"), new BasicEntity("007"), new BasicEntity("001")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.IsTrue(table.Count == 2, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "001"), "First entry should be '001'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "007"), "Second entry should be '007'");
    }

    [TestMethod]
    public void MixedSourcesExceptUnionScenarioTest()
    {
        var query =
            @"select Name from #A.Entities()
except (Name)
select Name from #B.Entities()
union (Name)
select Name from #C.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001"), new BasicEntity("002")]},
            {"#B", [new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")]},
            {"#C", [new BasicEntity("002"), new BasicEntity("001")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.IsTrue(table.Count == 2, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "002"), "First entry should be '002'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "001"), "Second entry should be '001'");
    }

    [TestMethod]
    public void MixedSourcesExceptUnionScenario1Test()
    {
        var query =
            @"select Name from #A.Entities()
except (Name)
select Name from #B.Entities()
union (Name)
select Name from #C.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001"), new BasicEntity("002")]},
            {"#B", [new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")]},
            {"#C", [new BasicEntity("002"), new BasicEntity("001")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.IsTrue(table.Count == 2, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "002"), "First entry should be '002'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "001"), "Second entry should be '001'");
    }

    [TestMethod]
    public void MixedSourcesWithSkipExceptUnionWithConditionsScenarioTest()
    {
        var query =
            @"select Name from #A.Entities() skip 1
except (Name)
select Name from #B.Entities() skip 2
union (Name)
select Name from #C.Entities() skip 3";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001"), new BasicEntity("002")]},
            {"#B", [new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")]},
            {"#C", [new BasicEntity("002"), new BasicEntity("001")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("002", table[0].Values[0]);
    }

    [TestMethod]
    public void MixedSourcesWithSkipIntersectUnionScenarioTest()
    {
        var query =
            @"select Name from #A.Entities() skip 1
intersect (Name)
select Name from #B.Entities() skip 2
union (Name)
select Name from #C.Entities() skip 3";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("002"), new BasicEntity("001")]},
            {"#B", [new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")]},
            {
                "#C",
                [
                    new BasicEntity("002"), new BasicEntity("001"), new BasicEntity("003"), new BasicEntity("006")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.IsTrue(table.Count == 2, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "001"), "First entry should be '001'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "006"), "Second entry should be '006'");
    }

    [TestMethod]
    public void MixedSourcesExceptUnionWithMultipleColumnsScenarioTest()
    {
        var query =
            @"select Name, RandomNumber() from #A.Entities()
except (Name)
select Name, RandomNumber() from #B.Entities()
union (Name)
select Name, RandomNumber() from #C.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001"), new BasicEntity("002")]},
            {"#B", [new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")]},
            {"#C", [new BasicEntity("002"), new BasicEntity("001")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.IsTrue(table.Count == 2, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "002"), "First entry should be '002'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "001"), "Second entry should be '001'");
    }

    [TestMethod]
    public void MixedMultipleSourcesTest()
    {
        var query =
            @"
select Name from #A.Entities() union (Name) 
select Name from #B.Entities() except (Name) 
select Name from #C.Entities() intersect (Name) 
select Name from #D.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("007"), new BasicEntity("008")
                ]
            },
            {"#B", [new BasicEntity("003"), new BasicEntity("007"), new BasicEntity("001")]},
            {"#C", [new BasicEntity("005"), new BasicEntity("007")]},
            {"#D", [new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("003")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.IsTrue(table.Count == 3, "Table should have 3 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "001"), "First entry should be '001'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "002"), "Second entry should be '002'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "003"), "Third entry should be '003'");
    }

    [TestMethod]
    public void UnionSourceGroupByTest()
    {
        var query =
            @"select City, Sum(Population) from #A.Entities() group by City
union (City)
select City, Sum(Population) from #B.Entities() group by City
union (City)
select City, Sum(Population) from #C.Entities() group by City";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001", "", 100), new BasicEntity("001", "", 100)]},
            {
                "#B",
                [
                    new BasicEntity("003", "", 13), new BasicEntity("003", "", 13), new BasicEntity("003", "", 13)
                ]
            },
            {"#C", [new BasicEntity("002", "", 14), new BasicEntity("002", "", 14)]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.IsTrue(table.Count == 3, "Table should have 3 entries");

        Assert.IsTrue(table.Any(entry => 
            (string)entry.Values[0] == "001" && 
            (decimal)entry.Values[1] == 200m
        ), "First entry should be '001' with value 200");

        Assert.IsTrue(table.Any(entry => 
            (string)entry.Values[0] == "003" && 
            (decimal)entry.Values[1] == 39m
        ), "Second entry should be '003' with value 39");

        Assert.IsTrue(table.Any(entry => 
            (string)entry.Values[0] == "002" && 
            (decimal)entry.Values[1] == 28m
        ), "Third entry should be '002' with value 28");
    }

    [TestMethod]
    public void UnionAllSourceGroupByTest()
    {
        var query =
            @"select City, Sum(Population) from #A.Entities() group by City
union all (City)
select City, Sum(Population) from #B.Entities() group by City
union all (City)
select City, Sum(Population) from #C.Entities() group by City";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001", "", 100), new BasicEntity("001", "", 100)]},
            {
                "#B",
                [
                    new BasicEntity("003", "", 13), new BasicEntity("003", "", 13), new BasicEntity("003", "", 13)
                ]
            },
            {"#C", [new BasicEntity("002", "", 14), new BasicEntity("002", "", 14)]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.IsTrue(table.Count == 3, "Table should have 3 entries");

        Assert.IsTrue(table.Any(entry => 
            (string)entry.Values[0] == "001" && 
            (decimal)entry.Values[1] == 200m
        ), "First entry should be '001' with value 200");

        Assert.IsTrue(table.Any(entry => 
            (string)entry.Values[0] == "003" && 
            (decimal)entry.Values[1] == 39m
        ), "Second entry should be '003' with value 39");

        Assert.IsTrue(table.Any(entry => 
            (string)entry.Values[0] == "002" && 
            (decimal)entry.Values[1] == 28m
        ), "Third entry should be '002' with value 28");
        
    }

    [TestMethod]
    public void ExceptSourceGroupByTest()
    {
        var query =
            @"select City, Sum(Population) from #A.Entities() group by City
except (City)
select City, Sum(Population) from #B.Entities() group by City
except (City)
select City, Sum(Population) from #C.Entities() group by City";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("001", "", 100), new BasicEntity("001", "", 100),
                    new BasicEntity("002", "", 500)
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("003", "", 13), new BasicEntity("003", "", 13), new BasicEntity("003", "", 13)
                ]
            },
            {"#C", [new BasicEntity("002", "", 14), new BasicEntity("002", "", 14)]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.IsTrue(table.Count == 1, "Table should have 1 entry");

        Assert.IsTrue(table.Any(entry => 
            (string)entry.Values[0] == "001" && 
            (decimal)entry.Values[1] == 200m
        ), "First entry should be '001' with value 200");
    }

    [TestMethod]
    public void IntersectSourceGroupByTest()
    {
        var query =
            @"select City, Sum(Population) from #A.Entities() group by City
except (City)
select City, Sum(Population) from #B.Entities() group by City
except (City)
select City, Sum(Population) from #C.Entities() group by City";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("001", "", 100), new BasicEntity("001", "", 100),
                    new BasicEntity("002", "", 500)
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("003", "", 13), new BasicEntity("003", "", 13), new BasicEntity("003", "", 13)
                ]
            },
            {"#C", [new BasicEntity("002", "", 14), new BasicEntity("002", "", 14)]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("001", table[0].Values[0]);
        Assert.AreEqual(200m, table[0].Values[1]);
    }

    [TestMethod]
    public void UnionSameSourceTest()
    {
        var query =
            @"
select Name from #A.Entities() where Name = '001'
union (Name)
select Name from #A.Entities() where Name = '002'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001"), new BasicEntity("002")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.IsTrue(table.Count == 2, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "001"), "First entry should be '001'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "002"), "Second entry should be '002'");
    }

    [TestMethod]
    public void UnionMultipleTimesSameSourceTest()
    {
        var query =
            @"
select Name from #A.Entities() where Name = '001'
union (Name)
select Name from #A.Entities() where Name = '002'
union (Name)
select Name from #A.Entities() where Name = '003'
union (Name)
select Name from #A.Entities() where Name = '004'
union (Name)
select Name from #A.Entities() where Name = '005'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("003"), new BasicEntity("004"),
                    new BasicEntity("005")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.IsTrue(table.Count == 5, "Table should have 5 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "001"), "First entry should be '001'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "002"), "Second entry should be '002'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "003"), "Third entry should be '003'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "004"), "Fourth entry should be '004'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "005"), "Fifth entry should be '005'");
    }
    
    [TestMethod]
    public void WhenWrongTypeBetweenUnions_ShouldFail()
    {
        var query = @"select Name from #A.Entities() union (Name) select 1 as Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001")]}
        };
        
        Assert.Throws<SetOperatorMustHaveSameTypesOfColumnsException>(() => CreateAndRunVirtualMachine(query, sources));
    }
    
    [TestMethod]
    public void WhenWrongTypeBetweenUnionAll_ShouldFail()
    {
        var query = @"select Name from #A.Entities() union all (Name) select 1 as Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001")]}
        };
        
        Assert.Throws<SetOperatorMustHaveSameTypesOfColumnsException>(() => CreateAndRunVirtualMachine(query, sources));
    }
    
    [TestMethod]
    public void WhenWrongTypeBetweenExcept_ShouldFail()
    {
        var query = @"select Name from #A.Entities() except (Name) select 1 as Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001")]}
        };
        
        Assert.Throws<SetOperatorMustHaveSameTypesOfColumnsException>(() => CreateAndRunVirtualMachine(query, sources));
    }
    
    [TestMethod]
    public void WhenWrongTypeBetweenIntersect_ShouldFail()
    {
        var query = @"select Name from #A.Entities() intersect (Name) select 1 as Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001")]}
        };
        
        Assert.Throws<SetOperatorMustHaveSameTypesOfColumnsException>(() => CreateAndRunVirtualMachine(query, sources));
    }
    
    [TestMethod]
    public void WhenUnionDoesNotHaveAKey_ShouldFail()
    {
        var query = @"select Name from #A.Entities() union () select Name as Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001")]}
        };
        
        Assert.Throws<SetOperatorMustHaveKeyColumnsException>(() => CreateAndRunVirtualMachine(query, sources));
    }
    
    [TestMethod]
    public void WhenUnionAllDoesNotHaveAKey_ShouldFail()
    {
        var query = @"select Name from #A.Entities() union all () select Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001")]}
        };
        
        Assert.Throws<SetOperatorMustHaveKeyColumnsException>(() => CreateAndRunVirtualMachine(query, sources));
    }
    
    [TestMethod]
    public void WhenExceptDoesNotHaveAKey_ShouldFail()
    {
        var query = @"select Name from #A.Entities() except () select Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001")]}
        };
        
        Assert.Throws<SetOperatorMustHaveKeyColumnsException>(() => CreateAndRunVirtualMachine(query, sources));
    }
    
    [TestMethod]
    public void WhenIntersectDoesNotHaveAKey_ShouldFail()
    {
        var query = @"select Name from #A.Entities() intersect () select Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001")]}
        };
        
        Assert.Throws<SetOperatorMustHaveKeyColumnsException>(() => CreateAndRunVirtualMachine(query, sources));
    }
}

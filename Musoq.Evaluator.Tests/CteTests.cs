using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class CteTests : BasicEntityTestBase
{
    [TestMethod]
    public void SimpleCteTest()
    {
        var query = "with p as (select City, Country from #A.entities()) select Country, City from p";

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
        var table = vm.Run();

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("Country", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("City", table.Columns.ElementAt(1).ColumnName);
        
        Assert.IsTrue(table.Count == 5, "Table should contain 5 rows");

        Assert.IsTrue(table.Count(row => 
                          (string)row.Values[0] == "POLAND") == 3 &&
                      table.Any(row => 
                          (string)row.Values[0] == "POLAND" && 
                          (string)row.Values[1] == "WARSAW") &&
                      table.Any(row =>
                          (string)row.Values[0] == "POLAND" && 
                          (string)row.Values[1] == "CZESTOCHOWA") &&
                      table.Any(row =>
                          (string)row.Values[0] == "POLAND" && 
                          (string)row.Values[1] == "KATOWICE"),
            "Expected three rows for POLAND with cities WARSAW, CZESTOCHOWA and KATOWICE");

        Assert.IsTrue(table.Count(row => 
                          (string)row.Values[0] == "GERMANY") == 2 &&
                      table.Any(row => 
                          (string)row.Values[0] == "GERMANY" && 
                          (string)row.Values[1] == "BERLIN") &&
                      table.Any(row =>
                          (string)row.Values[0] == "GERMANY" && 
                          (string)row.Values[1] == "MUNICH"),
            "Expected two rows for GERMANY with cities BERLIN and MUNICH");
    }

    [TestMethod]
    public void SimpleCteWithStarTest()
    {
        var query = "with p as (select City, Country from #A.entities()) select * from p";

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
        var table = vm.Run();

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Country", table.Columns.ElementAt(1).ColumnName);
        
        Assert.IsTrue(table.Count() == 5, "Table should have 5 entries");

        Assert.IsTrue(table.Any(entry => 
            (string)entry.Values[0] == "WARSAW" && 
            (string)entry.Values[1] == "POLAND"
        ), "First entry should be WARSAW, POLAND");

        Assert.IsTrue(table.Any(entry => 
            (string)entry.Values[0] == "CZESTOCHOWA" && 
            (string)entry.Values[1] == "POLAND"
        ), "Second entry should be CZESTOCHOWA, POLAND");

        Assert.IsTrue(table.Any(entry => 
            (string)entry.Values[0] == "KATOWICE" && 
            (string)entry.Values[1] == "POLAND"
        ), "Third entry should be KATOWICE, POLAND");

        Assert.IsTrue(table.Any(entry => 
            (string)entry.Values[0] == "BERLIN" && 
            (string)entry.Values[1] == "GERMANY"
        ), "Fourth entry should be BERLIN, GERMANY");

        Assert.IsTrue(table.Any(entry => 
            (string)entry.Values[0] == "MUNICH" && 
            (string)entry.Values[1] == "GERMANY"
        ), "Fifth entry should be MUNICH, GERMANY");
    }

    [TestMethod]
    public void SimpleCteWithGroupingTest()
    {
        var query =
            "with p as (select Country, Sum(Population) from #A.entities() group by Country) select * from p";

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
        var table = vm.Run();

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("Country", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Sum(Population)", table.Columns.ElementAt(1).ColumnName);
        
        Assert.IsTrue(table.Count == 2, "Table should contain 2 rows");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "POLAND" && 
                (decimal)row.Values[1] == 1150m),
            "Expected row for POLAND with value 1150");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "GERMANY" && 
                (decimal)row.Values[1] == 600m),
            "Expected row for GERMANY with value 600");
    }

    [TestMethod]
    public void WhenSameAliasesUsedWithinCteInnerExpression_ShouldThrow()
    {
        var query =
            "with p as (select 1 from #A.entities() a inner join #A.entities() a on 1 = 1) select * from p";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", []
            }
        };

        Assert.Throws<AliasAlreadyUsedException>(() => CreateAndRunVirtualMachine(query, sources));
    }


    [TestMethod]
    public void SimpleCteWithGrouping2Test()
    {
        var query =
            @"
with p as (
    select 
        Population, 
        Country 
    from #A.entities()
) 
select Country, Sum(Population) from p group by Country";

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
        var table = vm.Run();

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("Country", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Sum(Population)", table.Columns.ElementAt(1).ColumnName);
        
        Assert.IsTrue(table.Count() == 2, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => 
            (string)entry.Values[0] == "POLAND" && 
            (decimal)entry.Values[1] == 1150m
        ), "First entry should be POLAND, 1150");

        Assert.IsTrue(table.Any(entry => 
            (string)entry.Values[0] == "GERMANY" && 
            (decimal)entry.Values[1] == 600m
        ), "Second entry should be GERMANY, 600");
    }

    [TestMethod]
    public void SimpleCteWithUnionTest()
    {
        var query =
            "with p as (select City, Country from #A.entities() union (Country, City) select City, Country from #B.entities()) select City, Country from p";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400)
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("KATOWICE", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Country", table.Columns.ElementAt(1).ColumnName);
        
        Assert.IsTrue(table.Count == 5, "Table should contain 5 rows");

        Assert.IsTrue(table.Count(row => 
                (string)row.Values[1] == "POLAND" && 
                new[] { "WARSAW", "CZESTOCHOWA", "KATOWICE" }.Contains((string)row.Values[0])) == 3, 
            "Expected 3 cities from Poland not found");

        Assert.IsTrue(table.Count(row => 
                (string)row.Values[1] == "GERMANY" && 
                new[] { "BERLIN", "MUNICH" }.Contains((string)row.Values[0])) == 2,
            "Expected 2 cities from Germany not found");
    }

    [TestMethod]
    public void SimpleCteWithUnionAllTest()
    {
        var query =
            "with p as (select City, Country from #A.entities() union all (Country) select City, Country from #B.entities()) select City, Country from p";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400)
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("KATOWICE", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Country", table.Columns.ElementAt(1).ColumnName);

        Assert.IsTrue(table.Count() == 5, "Table should have 5 entries");

        Assert.IsTrue(table.Any(entry => 
                (string)entry.Values[0] == "WARSAW" && 
                (string)entry.Values[1] == "POLAND"), 
            "Entry for WARSAW, POLAND should match");

        Assert.IsTrue(table.Any(entry => 
                (string)entry.Values[0] == "CZESTOCHOWA" && 
                (string)entry.Values[1] == "POLAND"), 
            "Entry for CZESTOCHOWA, POLAND should match");

        Assert.IsTrue(table.Any(entry => 
                (string)entry.Values[0] == "KATOWICE" && 
                (string)entry.Values[1] == "POLAND"), 
            "Entry for KATOWICE, POLAND should match");

        Assert.IsTrue(table.Any(entry => 
                (string)entry.Values[0] == "BERLIN" && 
                (string)entry.Values[1] == "GERMANY"), 
            "Entry for BERLIN, GERMANY should match");

        Assert.IsTrue(table.Any(entry => 
                (string)entry.Values[0] == "MUNICH" && 
                (string)entry.Values[1] == "GERMANY"), 
            "Entry for MUNICH, GERMANY should match");
    }

    [TestMethod]
    public void SimpleCteWithExceptTest()
    {
        var query =
            "with p as (select City, Country from #A.entities() except (Country) select City, Country from #B.entities()) select City, Country from p";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("HELSINKI", "FINLAND", 500),
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400)
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("KATOWICE", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Country", table.Columns.ElementAt(1).ColumnName);
        
        Assert.IsTrue(table.Count() == 1, "Table should have 1 entry");

        Assert.IsTrue(table.Any(entry => 
            (string)entry.Values[0] == "HELSINKI" && 
            (string)entry.Values[1] == "FINLAND"
        ), "First entry should be HELSINKI, FINLAND");
    }

    [TestMethod]
    public void SimpleCteWithIntersectTest()
    {
        var query =
            "with p as (select City, Country from #A.entities() intersect (Country, City) select City, Country from #B.entities()) select City, Country from p";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("HELSINKI", "FINLAND", 500),
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400)
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("WARSAW", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350),
                    new BasicEntity("HELSINKI", "FINLAND", 500)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Country", table.Columns.ElementAt(1).ColumnName);

        Assert.IsTrue(table.Count == 2, "Table should contain 2 rows");

        Assert.IsTrue(table.All(row => 
                new[] { ("HELSINKI", "FINLAND"), ("WARSAW", "POLAND") }.Contains(((string)row.Values[0], (string)row.Values[1]))),
            "Expected rows with values: (HELSINKI,FINLAND), (WARSAW,POLAND)");
    }

    [TestMethod]
    public void CteWithSetOperatorTest()
    {
        var query = @"
with p as (
    select City, Country from #A.entities() intersect (Country, City) 
    select City, Country from #B.entities()
) 
select City, Country from p";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("HELSINKI", "FINLAND", 500),
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400)
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("WARSAW", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350),
                    new BasicEntity("HELSINKI", "FINLAND", 500)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Country", table.Columns.ElementAt(1).ColumnName);
        
        Assert.IsTrue(table.Count == 2, "Table should contain 2 rows");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "HELSINKI" && 
                (string)row.Values[1] == "FINLAND"),
            "Expected row for HELSINKI, FINLAND");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "WARSAW" && 
                (string)row.Values[1] == "POLAND"),
            "Expected row for WARSAW, POLAND");
    }

    [TestMethod]
    public void CteWithTwoOuterExpressionTest()
    {
        var query = @"
with p as (
    select City, Country from #A.entities()
) 
select City, Country from p union (City, Country)
select City, Country from #B.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("HELSINKI", "FINLAND", 500),
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400)
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("WARSAW", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350),
                    new BasicEntity("HELSINKI", "FINLAND", 500)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Country", table.Columns.ElementAt(1).ColumnName);
        
        Assert.IsTrue(table.Count == 5, "Table should contain 5 rows");

        Assert.IsTrue(table.Any(row => 
                (string)row.Values[0] == "HELSINKI" && 
                (string)row.Values[1] == "FINLAND"), 
            "Missing HELSINKI/FINLAND");

        Assert.IsTrue(table.Any(row => 
                (string)row.Values[0] == "WARSAW" && 
                (string)row.Values[1] == "POLAND"),
            "Missing WARSAW/POLAND");

        Assert.IsTrue(table.Any(row => 
                (string)row.Values[0] == "CZESTOCHOWA" && 
                (string)row.Values[1] == "POLAND"),
            "Missing CZESTOCHOWA/POLAND");

        Assert.IsTrue(table.Any(row => 
                (string)row.Values[0] == "BERLIN" && 
                (string)row.Values[1] == "GERMANY"),
            "Missing BERLIN/GERMANY");

        Assert.IsTrue(table.Any(row => 
                (string)row.Values[0] == "MUNICH" && 
                (string)row.Values[1] == "GERMANY"),
            "Missing MUNICH/GERMANY");
    }

    [TestMethod]
    public void SimpleCteWithMultipleOuterExpressionsTest()
    {
        var query = @"
with p as (
    select City, Country from #A.entities() intersect (Country, City) 
    select City, Country from #B.entities()
) select City, Country from p where Country = 'FINLAND' union (Country, City)
  select City, Country from p where Country = 'POLAND'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("HELSINKI", "FINLAND", 500),
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400)
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("WARSAW", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350),
                    new BasicEntity("TOKYO", "JAPAN", 500),
                    new BasicEntity("HELSINKI", "FINLAND", 500)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Country", table.Columns.ElementAt(1).ColumnName);
        
        Assert.IsTrue(table.Count() == 2, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => 
            (string)entry.Values[0] == "HELSINKI" && 
            (string)entry.Values[1] == "FINLAND"
        ), "First entry should be HELSINKI, FINLAND");

        Assert.IsTrue(table.Any(entry => 
            (string)entry.Values[0] == "WARSAW" && 
            (string)entry.Values[1] == "POLAND"
        ), "Second entry should be WARSAW, POLAND");
    }

    [TestMethod]
    public void CteWithSetInInnerOuterExpressionTest()
    {
        var query = @"
with p as (
    select City, Country from #A.entities() intersect (Country, City) 
    select City, Country from #B.entities()
) 
select City, Country from p union (City, Country)
select City, Country from #C.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("HELSINKI", "FINLAND", 500),
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400)
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("WARSAW", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350),
                    new BasicEntity("HELSINKI", "FINLAND", 500)
                ]
            },
            {
                "#C",
                [
                    new BasicEntity("NEW YORK", "USA", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Country", table.Columns.ElementAt(1).ColumnName);

        Assert.AreEqual(3, table.Count());

        Assert.IsTrue(table.Any(entry => 
                (string)entry.Values[0] == "HELSINKI" && 
                (string)entry.Values[1] == "FINLAND"), 
            "First entry should be Helsinki, Finland");

        Assert.IsTrue(table.Any(entry => 
                (string)entry.Values[0] == "WARSAW" && 
                (string)entry.Values[1] == "POLAND"), 
            "Second entry should be Warsaw, Poland");

        Assert.IsTrue(table.Any(entry => 
                (string)entry.Values[0] == "NEW YORK" && 
                (string)entry.Values[1] == "USA"), 
            "Third entry should be New York, USA");
    }

    [TestMethod]
    public void MultipleCteExpressionsTest()
    {
        const string query = @"
with p as (
    select City, Country from #A.entities()
), c as (
    select City, Country from #B.entities()
), d as (
    select City, Country from p where City = 'HELSINKI'
), f as (
    select City, Country from #B.entities() where City = 'WARSAW'
)
select City, Country from p union (City, Country)
select City, Country from c union (City, Country)
select City, Country from d union (City, Country)
select City, Country from f";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("HELSINKI", "FINLAND", 500),
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400)
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("WARSAW", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350),
                    new BasicEntity("HELSINKI", "FINLAND", 500)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Country", table.Columns.ElementAt(1).ColumnName);
        
        Assert.IsTrue(table.Count == 5, "Table should contain 5 rows");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "HELSINKI" && 
                (string)row.Values[1] == "FINLAND"),
            "Expected row for HELSINKI, FINLAND");

        Assert.IsTrue(table.Count(row => 
                          (string)row.Values[1] == "POLAND") == 2 &&
                      table.Any(row => 
                          (string)row.Values[0] == "WARSAW" && 
                          (string)row.Values[1] == "POLAND") &&
                      table.Any(row =>
                          (string)row.Values[0] == "CZESTOCHOWA" && 
                          (string)row.Values[1] == "POLAND"),
            "Expected two rows for POLAND with cities WARSAW and CZESTOCHOWA");

        Assert.IsTrue(table.Count(row => 
                          (string)row.Values[1] == "GERMANY") == 2 &&
                      table.Any(row => 
                          (string)row.Values[0] == "BERLIN" && 
                          (string)row.Values[1] == "GERMANY") &&
                      table.Any(row =>
                          (string)row.Values[0] == "MUNICH" && 
                          (string)row.Values[1] == "GERMANY"),
            "Expected two rows for GERMANY with cities BERLIN and MUNICH");
    }

    [TestMethod]
    public void WhenMixedQueriesGroupingAreUsed_Variant1_ShouldPass()
    {
        var query = @"
with first as (
    select 1 as Test from #A.entities()
), second as (
    select 2 as Test from #B.entities() group by 'fake'
)
select c.GetBytes(c.Name) from first a 
    inner join #A.entities() c on 1 = 1 
    inner join second b on 1 = 1";
            
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("First")
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("Second")
                ]
            }
        };
            
        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.IsTrue(table.Count == 1, "Table should have 1 entry");
        Assert.IsTrue(table.Any(entry => 
            Encoding.UTF8.GetString((byte[])entry.Values[0]) == "First"
        ), "First entry should be 'First'");
    }

    [TestMethod]
    public void WhenMixedQueriesGroupingAreUsed_Variant2_ShouldPass()
    {
        var query = @"
with first as (
    select Name from #A.entities()
), second as (
    select Name from #B.entities() group by Name
)
select 
    c.GetBytes(c.Name) 
from first a 
    inner join #A.entities() c on 1 = 1 
    inner join second b on 1 = 1";
            
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("First")
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("Second")
                ]
            }
        };
            
        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
            
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("First", Encoding.UTF8.GetString((byte[])table[0].Values[0]));
    }

    [TestMethod]
    public void WhenMixedQueriesGroupingAreUsed_Variant3_ShouldPass()
    {
        var query = @"
with first as (
    select Name from #A.entities()
), second as (
    select Name from #B.entities() group by Name
)
select
    a.Name,
    b.Name,
    c.GetBytes(c.Name) 
from first a 
    inner join #A.entities() c on 1 = 1 
    inner join second b on 1 = 1";
            
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("First")
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("Second")
                ]
            }
        };
            
        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
            
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("First", (string)table[0].Values[0]);
        Assert.AreEqual("Second", (string)table[0].Values[1]);
        Assert.AreEqual("First", Encoding.UTF8.GetString((byte[])table[0].Values[2]));
    }

    [TestMethod]
    public void WhenMixedQueriesGroupingAreUsed_Variant4_ShouldPass()
    {
        var query = @"
with first as (
    select a.Name as Name from #A.entities() a group by a.Name having Count(a.Name) > 0
), second as (
    select b.Name() as Name from #B.entities() b inner join first a on 1 = 1
), third as (
    select b.Name as Name from second b group by b.Name having Count(b.Name) > 0
), fourth as (
    select
        c.Name() as Name
    from second b
        inner join #A.entities() c on 1 = 1
        inner join third d on 1 = 1
)
select
    c.Name as Name
from fourth c";
            
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("First")
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("Second")
                ]
            },
            {
                "#C",
                [
                    new BasicEntity("Third")
                ]
            }
        };
            
        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
            
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("First", (string)table[0].Values[0]);
    }
}

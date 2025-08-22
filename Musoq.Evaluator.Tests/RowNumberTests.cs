using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class RowNumberTests : BasicEntityTestBase
{
    [TestMethod]
    public void WhenRowNumberSimpleTest_ShouldPass()
    {
        var query = "select RowNumber() from @A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity {Country = "Poland"},
                    new BasicEntity {Country = "Brazil"}
                ]
            }
        };
        
        var vm = CreateAndRunVirtualMachine(query, sources);
        
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(1, table[0].Values[0]);
        Assert.AreEqual(2, table[1].Values[0]);
    }
    
    // -- create
    // CREATE TABLE Cities (
    //     City TEXT NOT NULL,
    //     Country TEXT NOT NULL,
    //     Population INT NOT NULL
    // );
    //
    // -- insert
    //     INSERT INTO Cities VALUES ('WARSAW', 'POLAND', 500);
    //     INSERT INTO Cities VALUES ('MUNICH', 'GERMANY', 350);
    //
    // -- fetch 
    // SELECT Country, row_number() over (ORDER BY Country) FROM Cities order by Country;
    [TestMethod]
    public void WhenRowNumberWithOrderBySimpleTest_ShouldPass()
    {
        var query = "select Country, RowNumber() from @A.entities() order by Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity {Country = "Poland"},
                    new BasicEntity {Country = "Germany"}
                ]
            }
        };
        
        var vm = CreateAndRunVirtualMachine(query, sources);
        
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(1, table[0].Values[1]);
        Assert.AreEqual(2, table[1].Values[1]);
        
        Assert.AreEqual("Germany", table[0].Values[0]);
        Assert.AreEqual("Poland", table[1].Values[0]);
    }

    [TestMethod]
    public void WhenRowNumberWithWhereTest_ShouldPass()
    {
        var query = "select Country, RowNumber() from @A.entities() where Country = 'Poland'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity {Country = "Poland"},
                    new BasicEntity {Country = "Germany"},
                    new BasicEntity {Country = "Poland"}
                ]
            }
        };
        
        var vm = CreateAndRunVirtualMachine(query, sources);
        
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Poland" && (int)row.Values[1] == 1), "Expected row with Poland, 1 not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Poland" && (int)row.Values[1] == 2), "Expected row with Poland, 2 not found");
    }

    [TestMethod]
    public void WhenRowNumberWithSkipTest_ShouldPass()
    {
        var query = "select Country, RowNumber() from @A.entities() where Country = 'Poland' skip 1";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity {Country = "Poland"},
                    new BasicEntity {Country = "Germany"},
                    new BasicEntity {Country = "Poland"}
                ]
            }
        };
        
        var vm = CreateAndRunVirtualMachine(query, sources);
        
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Count);
        
        Assert.AreEqual(2, table[0].Values[1]);
        Assert.AreEqual("Poland", table[0].Values[0]);
    }
    
    // -- create
    //     CREATE TABLE Cities (
    //     City TEXT NOT NULL,
    //     Country TEXT NOT NULL,
    //     Population INT NOT NULL
    // );
    //
    // -- insert
    // INSERT INTO Cities VALUES ('WARSAW', 'POLAND', 500);
    // INSERT INTO Cities VALUES ('MUNICH', 'GERMANY', 350);
    // INSERT INTO Cities VALUES ('CZESTOCHOWA', 'POLAND', 250);
    //
    // -- fetch 
    // SELECT Country, row_number() over (ORDER BY Country) FROM Cities group by Country order by Country offset 1;
    [TestMethod]
    public void WhenRowNumberWithGroupByAndSkipTest_ShouldPass()
    {
        var query = "select Country, RowNumber() from @A.entities() group by Country order by Country skip 1";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity {Country = "Poland"},
                    new BasicEntity {Country = "Germany"},
                    new BasicEntity {Country = "Poland"}
                ]
            }
        };
        
        var vm = CreateAndRunVirtualMachine(query, sources);
        
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Count);
        
        Assert.AreEqual(2, table[0].Values[1]);
        Assert.AreEqual("Poland", table[0].Values[0]);
    }
    
    // -- create
    // CREATE TABLE Cities (
    //     City TEXT NOT NULL,
    //     Country TEXT NOT NULL,
    //     Population INT NOT NULL
    // );
    //
    // -- insert
    // INSERT INTO Cities VALUES ('WARSAW', 'POLAND', 500);
    // INSERT INTO Cities VALUES ('MUNICH', 'GERMANY', 350);
    // INSERT INTO Cities VALUES ('CZESTOCHOWA', 'POLAND', 250);
    //
    // -- fetch 
    // SELECT Country, row_number() over (ORDER BY Country) FROM Cities group by Country order by Country limit 1;
    [TestMethod]
    public void WhenRowNumberWithGroupByAndTakeTest_ShouldPass()
    {
        var query = "select Country, RowNumber() from @A.entities() group by Country order by Country take 1";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity {Country = "Poland"},
                    new BasicEntity {Country = "Germany"},
                    new BasicEntity {Country = "Poland"}
                ]
            }
        };
        
        var vm = CreateAndRunVirtualMachine(query, sources);
        
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Count);
        
        Assert.AreEqual(1, table[0].Values[1]);
        Assert.AreEqual("Germany", table[0].Values[0]);
    }
}

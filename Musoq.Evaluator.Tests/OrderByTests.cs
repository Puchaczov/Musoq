using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class OrderByTests : BasicEntityTestBase
{
    [TestMethod]
    public void WhenOrderByColumn_ShouldSucceed()
    {
        var query = @"select City from @A.Entities() order by Money";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(-200))
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("cracow", table[0].Values[0]);
        Assert.AreEqual("katowice", table[1].Values[0]);
        Assert.AreEqual("czestochowa", table[2].Values[0]);
    }

    [TestMethod]
    public void WhenOrderByDescColumn_ShouldSucceed()
    {
        var query = @"select City from @A.Entities() order by Money desc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(-200))
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("czestochowa", table[0].Values[0]);
        Assert.AreEqual("katowice", table[1].Values[0]);
        Assert.AreEqual("cracow", table[2].Values[0]);
    }
        
    [TestMethod]
    public void WhenOrderByMultipleColumnFirstDesc_ShouldSucceed()
    {
        var query = @"select City from @A.Entities() order by Money desc, Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(-200))
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("czestochowa", table[0].Values[0]);
        Assert.AreEqual("katowice", table[1].Values[0]);
        Assert.AreEqual("cracow", table[2].Values[0]);
    }
        
    [TestMethod]
    public void WhenOrderByMultipleColumns_ShoulSucceed()
    {
        var query = @"select City from @A.Entities() order by Money, Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(-200))
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("cracow", table[0].Values[0]);
        Assert.AreEqual("katowice", table[1].Values[0]);
        Assert.AreEqual("czestochowa", table[2].Values[0]);
    }
        
    [TestMethod]
    public void WhenOrderByMultipleColumnsBothDesc_ShouldSucceed()
    {
        var query = @"select City from @A.Entities() order by Money desc, Name desc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(-200))
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("czestochowa", table[0].Values[0]);
        Assert.AreEqual("katowice", table[1].Values[0]);
        Assert.AreEqual("cracow", table[2].Values[0]);
    }
        
    [TestMethod]
    public void WhenOrderByMultipleColumnsSecondColumnDesc_ShouldSucceed()
    {
        var query = @"select City + '-' + ToString(Money) from @A.Entities() order by City, Money desc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("katowice", "feb", Convert.ToDecimal(100)),
                    new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(10))
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("cracow-10", table[0].Values[0]);
        Assert.AreEqual("czestochowa-400", table[1].Values[0]);
        Assert.AreEqual("katowice-300", table[2].Values[0]);
        Assert.AreEqual("katowice-100", table[3].Values[0]);
    }
        
    [TestMethod]
    public void WhenOrderByAfterGroupBy_ShouldSuccess()
    {
        var query = @"select City from @A.Entities() group by City order by City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("katowice", "feb", Convert.ToDecimal(100)),
                    new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(10))
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("cracow", table[0].Values[0]);
        Assert.AreEqual("czestochowa", table[1].Values[0]);
        Assert.AreEqual("katowice", table[2].Values[0]);
    }
        
    [TestMethod]
    public void WhenOrderByWithDescAfterGroupBy_ShouldSucceed()
    {
        var query = @"select City from @A.Entities() group by City order by City desc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("katowice", "feb", Convert.ToDecimal(100)),
                    new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(10))
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("katowice", table[0].Values[0]);
        Assert.AreEqual("czestochowa", table[1].Values[0]);
        Assert.AreEqual("cracow", table[2].Values[0]);
    }
        
    [TestMethod]
    public void WhenOrderByWithGroupByMultipleColumnAndFirstDesc_ShouldSucceed()
    {
        var query = @"select City, Money from @A.Entities() group by City, Money order by City desc, Money";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("katowice", "feb", Convert.ToDecimal(100)),
                    new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(10))
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("katowice", table[0].Values[0]);
        Assert.AreEqual(100m, table[0].Values[1]);
        Assert.AreEqual("katowice", table[1].Values[0]);
        Assert.AreEqual(300m, table[1].Values[1]);
        Assert.AreEqual("czestochowa", table[2].Values[0]);
        Assert.AreEqual(400m, table[2].Values[1]);
        Assert.AreEqual("cracow", table[3].Values[0]);
        Assert.AreEqual(10m, table[3].Values[1]);
    }
        
    [TestMethod]
    public void WhenOrderByAfterGroupByMultipleColumnBothDesc_ShouldSucceed()
    {
        var query = @"select City, Money from @A.Entities() group by City, Money order by City desc, Money desc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("katowice", "feb", Convert.ToDecimal(100)),
                    new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(10))
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("katowice", table[0].Values[0]);
        Assert.AreEqual(300m, table[0].Values[1]);
        Assert.AreEqual("katowice", table[1].Values[0]);
        Assert.AreEqual(100m, table[1].Values[1]);
        Assert.AreEqual("czestochowa", table[2].Values[0]);
        Assert.AreEqual(400m, table[2].Values[1]);
        Assert.AreEqual("cracow", table[3].Values[0]);
        Assert.AreEqual(10m, table[3].Values[1]);
    }
        
    [TestMethod]
    public void WhenOrderByAfterGroupByHaving_ShouldSucceed()
    {
        var query = @"select City, Sum(Money) from @A.Entities() group by City having Sum(Money) >= 400 order by City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("katowice", "feb", Convert.ToDecimal(100)),
                    new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(10))
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("czestochowa", table[0].Values[0]);
        Assert.AreEqual(400m, table[0].Values[1]);
        Assert.AreEqual("katowice", table[1].Values[0]);
        Assert.AreEqual(400m, table[1].Values[1]);
    }
        
    [TestMethod]
    public void WhenOrderByDescAfterGroupByHaving_ShouldSucceed()
    {
        var query = @"select City, Sum(Money) from @A.Entities() group by City having Sum(Money) >= 400 order by City desc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("katowice", "feb", Convert.ToDecimal(100)),
                    new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(10))
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("katowice", table[0].Values[0]);
        Assert.AreEqual(400m, table[0].Values[1]);
        Assert.AreEqual("czestochowa", table[1].Values[0]);
        Assert.AreEqual(400m, table[1].Values[1]);
    }
        
    [TestMethod]
    public void WhenOrderByClauseWithOperation_ShouldSucceed()
    {
        const string query = @"select Money from @A.Entities() order by Money * -1";
            
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("katowice", "feb", Convert.ToDecimal(100)),
                    new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(10)),
                    new BasicEntity("glasgow", "feb", Convert.ToDecimal(-10))
                ]
            }
        };
            
        var vm = CreateAndRunVirtualMachine(query, sources);
            
        var table = vm.Run();
            
        Assert.AreEqual(5, table.Count);
        Assert.AreEqual(400m, table[0].Values[0]);
        Assert.AreEqual(300m, table[1].Values[0]);
        Assert.AreEqual(100m, table[2].Values[0]);
        Assert.AreEqual(10m, table[3].Values[0]);
        Assert.AreEqual(-10m, table[4].Values[0]);
    }
        
    [TestMethod]
    public void WhenOrderByClauseWithOperationDesc_ShouldSucceed()
    {
        var query = @"select Money from @A.Entities() order by Money * -1 desc";
            
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("katowice", "feb", Convert.ToDecimal(100)),
                    new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(10)),
                    new BasicEntity("glasgow", "feb", Convert.ToDecimal(-10))
                ]
            }
        };
            
        var vm = CreateAndRunVirtualMachine(query, sources);
            
        var table = vm.Run();
            
        Assert.AreEqual(5, table.Count);
        Assert.AreEqual(-10m, table[0].Values[0]);
        Assert.AreEqual(10m, table[1].Values[0]);
        Assert.AreEqual(100m, table[2].Values[0]);
        Assert.AreEqual(300m, table[3].Values[0]);
        Assert.AreEqual(400m, table[4].Values[0]);
    }

    [TestMethod]
    public void WhenOrderByWithinCteExpression_ShouldSucceed()
    {
        const string query = @"with cte as ( select City, Money from @A.Entities() order by Money ) select City from cte";
            
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("katowice", "feb", Convert.ToDecimal(100)),
                    new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(10)),
                    new BasicEntity("glasgow", "feb", Convert.ToDecimal(-10))
                ]
            }
        };
            
        var vm = CreateAndRunVirtualMachine(query, sources);
            
        var table = vm.Run();Assert.IsTrue(table.Count == 5, "Table should have 5 entries");

        Assert.IsTrue(table.Any(entry => 
                (string)entry.Values[0] == "glasgow"), 
            "First entry should be 'glasgow'");

        Assert.IsTrue(table.Any(entry => 
                (string)entry.Values[0] == "cracow"), 
            "Second entry should be 'cracow'");

        Assert.IsTrue(table.Any(entry => 
                (string)entry.Values[0] == "katowice" && table.Count(e => (string)e.Values[0] == "katowice") == 2), 
            "Two entries should be 'katowice'");

        Assert.IsTrue(table.Any(entry => 
                (string)entry.Values[0] == "czestochowa"), 
            "Last entry should be 'czestochowa'");
    }
        
    [TestMethod]
    public void WhenOrderByDescWithinCteExpression_ShouldSucceed()
    {
        const string query = @"with cte as ( select City, Money from @A.Entities() order by Money desc ) select City from cte";
            
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("katowice", "feb", Convert.ToDecimal(100)),
                    new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(10)),
                    new BasicEntity("glasgow", "feb", Convert.ToDecimal(-10))
                ]
            }
        };
            
        var vm = CreateAndRunVirtualMachine(query, sources);
            
        var table = vm.Run();
        
        Assert.IsTrue(table.Count == 5, "Table should contain 5 rows");

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "czestochowa"), "Missing czestochowa");
        Assert.IsTrue(table.Count(row => (string)row.Values[0] == "katowice") == 2, "Should have exactly 2 rows with katowice");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "cracow"), "Missing cracow");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "glasgow"), "Missing glasgow");
    }
        
    [TestMethod]
    public void WhenOrderByWithMultipleColumnsFirstDescWithinCteExpression_ShouldSucceed()
    {
        const string query = @"with cte as ( select City, Money from @A.Entities() order by Money desc, City ) select City from cte";
            
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("katowice", "feb", Convert.ToDecimal(100)),
                    new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(10)),
                    new BasicEntity("glasgow", "feb", Convert.ToDecimal(-10))
                ]
            }
        };
            
        var vm = CreateAndRunVirtualMachine(query, sources);
            
        var table = vm.Run();
        
        Assert.IsTrue(table.Count == 5, "Table should contain 5 rows");

        Assert.IsTrue(table.Count(row => (string)row.Values[0] == "katowice") == 2 &&
                      table.Any(row => (string)row.Values[0] == "czestochowa") &&
                      table.Any(row => (string)row.Values[0] == "cracow") &&
                      table.Any(row => (string)row.Values[0] == "glasgow"),
            "Expected two rows with katowice and one row each with czestochowa, cracow, and glasgow");
    }
        
    [TestMethod]
    public void WhenOrderByWithMultipleColumnsBothDescWithinCteExpression_ShouldSucceed()
    {
        const string query = @"with cte as ( select City, Money from @A.Entities() order by Money desc, City desc ) select City from cte";
            
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("katowice", "feb", Convert.ToDecimal(100)),
                    new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(10)),
                    new BasicEntity("glasgow", "feb", Convert.ToDecimal(-10))
                ]
            }
        };
            
        var vm = CreateAndRunVirtualMachine(query, sources);
            
        var table = vm.Run();
        
        Assert.IsTrue(table.Count == 5, "Table should contain 5 rows");

        var expectedCities = new[] { "czestochowa", "katowice", "cracow", "glasgow" };
        Assert.IsTrue(expectedCities.All(city => 
                table.Any(row => (string)row.Values[0] == city)),
            "Not all expected cities found in table");

        Assert.IsTrue(table.Count(row => 
                (string)row.Values[0] == "katowice") == 2,
            "Expected 2 rows with Katowice");
    }
        
    [TestMethod]
    public void WhenOrderByWithMultipleColumnsBothDescWithinCteExpression_BothRetrieved_ShouldSucceed()
    {
        const string query = @"with cte as ( select City, Money from @A.Entities() order by Money desc, City desc ) select City, Money from cte";
            
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("katowice", "feb", Convert.ToDecimal(100)),
                    new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(10)),
                    new BasicEntity("glasgow", "feb", Convert.ToDecimal(-10))
                ]
            }
        };
            
        var vm = CreateAndRunVirtualMachine(query, sources);
            
        var table = vm.Run();Assert.IsTrue(table.Count == 5, "Table should have 5 entries");

        Assert.IsTrue(table.Any(entry => 
                (string)entry.Values[0] == "czestochowa" && 
                (decimal)entry.Values[1] == 400m), 
            "First entry should be czestochowa with 400m");

        Assert.IsTrue(table.Any(entry => 
                (string)entry.Values[0] == "katowice" && 
                (decimal)entry.Values[1] == 300m), 
            "Second entry should be katowice with 300m");

        Assert.IsTrue(table.Any(entry => 
                (string)entry.Values[0] == "katowice" && 
                (decimal)entry.Values[1] == 100m), 
            "Third entry should be katowice with 100m");

        Assert.IsTrue(table.Any(entry => 
                (string)entry.Values[0] == "cracow" && 
                (decimal)entry.Values[1] == 10m), 
            "Fourth entry should be cracow with 10m");

        Assert.IsTrue(table.Any(entry => 
                (string)entry.Values[0] == "glasgow" && 
                (decimal)entry.Values[1] == -10m), 
            "Fifth entry should be glasgow with -10m");
    }

    [TestMethod]
    public void WhenOrderByCaseWhenExpression_ShouldSucceed()
    {
        var query = @"select City from @A.Entities() order by case when Money > 0 then Money else 0d end";
            
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("katowice", "feb", Convert.ToDecimal(100)),
                    new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(10)),
                    new BasicEntity("glasgow", "feb", Convert.ToDecimal(-10))
                ]
            }
        };
            
        var vm = CreateAndRunVirtualMachine(query, sources);
            
        var table = vm.Run();
            
        Assert.AreEqual(5, table.Count);
        Assert.AreEqual("glasgow", table[0].Values[0]);
        Assert.AreEqual("cracow", table[1].Values[0]);
        Assert.AreEqual("katowice", table[2].Values[0]);
        Assert.AreEqual("katowice", table[3].Values[0]);
        Assert.AreEqual("czestochowa", table[4].Values[0]);
    }
        
    [TestMethod]
    public void WhenOrderByCaseWhenDescExpression_ShouldSucceed()
    {
        var query = @"select City from @A.Entities() order by case when Money > 0 then Money else 0d end desc";
            
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("katowice", "feb", Convert.ToDecimal(100)),
                    new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(10)),
                    new BasicEntity("glasgow", "feb", Convert.ToDecimal(-10))
                ]
            }
        };
            
        var vm = CreateAndRunVirtualMachine(query, sources);
            
        var table = vm.Run();
            
        Assert.AreEqual(5, table.Count);
        Assert.AreEqual("czestochowa", table[0].Values[0]);
        Assert.AreEqual("katowice", table[1].Values[0]);
        Assert.AreEqual("katowice", table[2].Values[0]);
        Assert.AreEqual("cracow", table[3].Values[0]);
        Assert.AreEqual("glasgow", table[4].Values[0]);
    }
        
    [TestMethod]
    public void WhenOrderByMultipleColumnsFirstOneIsCaseWhenExpression_ShouldSucceed()
    {
        var query = @"select City from @A.Entities() order by case when Money > 0 then Money else 0d end, City";
            
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("katowice", "feb", Convert.ToDecimal(100)),
                    new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(10)),
                    new BasicEntity("glasgow", "feb", Convert.ToDecimal(-10))
                ]
            }
        };
            
        var vm = CreateAndRunVirtualMachine(query, sources);
            
        var table = vm.Run();
            
        Assert.AreEqual(5, table.Count);
        Assert.AreEqual("glasgow", table[0].Values[0]);
        Assert.AreEqual("cracow", table[1].Values[0]);
        Assert.AreEqual("katowice", table[2].Values[0]);
        Assert.AreEqual("katowice", table[3].Values[0]);
        Assert.AreEqual("czestochowa", table[4].Values[0]);
    }
        
    [TestMethod]
    public void WhenOrderByMultipleColumnsFirstOneIsCaseWhenDescExpression_ShouldSucceed()
    {
        var query = @"select City from @A.Entities() order by case when Money > 0 then Money else 0d end desc, City desc";
            
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("katowice", "feb", Convert.ToDecimal(100)),
                    new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(10)),
                    new BasicEntity("glasgow", "feb", Convert.ToDecimal(-10))
                ]
            }
        };
            
        var vm = CreateAndRunVirtualMachine(query, sources);
            
        var table = vm.Run();
            
        Assert.AreEqual(5, table.Count);
        Assert.AreEqual("czestochowa", table[0].Values[0]);
        Assert.AreEqual("katowice", table[1].Values[0]);
        Assert.AreEqual("katowice", table[2].Values[0]);
        Assert.AreEqual("cracow", table[3].Values[0]);
        Assert.AreEqual("glasgow", table[4].Values[0]);
    }
        
    [TestMethod]
    public void WhenOrderByWithInnerJoin_ShouldSucceed()
    {
        var query = @"select a.City from @A.Entities() a inner join @A.Entities() b on a.City = b.City order by a.Money";
            
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(10)),
                    new BasicEntity("glasgow", "feb", Convert.ToDecimal(-10))
                ]
            }
        };
            
        var vm = CreateAndRunVirtualMachine(query, sources);
            
        var table = vm.Run();
            
        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("glasgow", table[0].Values[0]);
        Assert.AreEqual("cracow", table[1].Values[0]);
        Assert.AreEqual("katowice", table[2].Values[0]);
        Assert.AreEqual("czestochowa", table[3].Values[0]);
    }
        
    [TestMethod]
    public void WhenOrderByDescendingWithInnerJoin_ShouldSucceed()
    {
        var query = @"select a.City from @A.Entities() a inner join @A.Entities() b on a.City = b.City order by a.Money desc";
            
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(10)),
                    new BasicEntity("glasgow", "feb", Convert.ToDecimal(-10))
                ]
            }
        };
            
        var vm = CreateAndRunVirtualMachine(query, sources);
            
        var table = vm.Run();
            
        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("czestochowa", table[0].Values[0]);
        Assert.AreEqual("katowice", table[1].Values[0]);
        Assert.AreEqual("cracow", table[2].Values[0]);
        Assert.AreEqual("glasgow", table[3].Values[0]);
    }
        
    [TestMethod]
    public void WhenOrderByWithInnerJoinAndGroupBy_ShouldSucceed()
    {
        var query = @"select a.City from @A.Entities() a inner join @A.Entities() b on a.City = b.City group by a.City order by a.City";
            
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(10)),
                    new BasicEntity("glasgow", "feb", Convert.ToDecimal(-10))
                ]
            }
        };
            
        var vm = CreateAndRunVirtualMachine(query, sources);
            
        var table = vm.Run();
            
        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("cracow", table[0].Values[0]);
        Assert.AreEqual("czestochowa", table[1].Values[0]);
        Assert.AreEqual("glasgow", table[2].Values[0]);
        Assert.AreEqual("katowice", table[3].Values[0]);
    }
        
    [TestMethod]
    public void WhenOrderByDescendingWithInnerJoinAndGroupBy_ShouldSucceed()
    {
        var query = @"select a.City from @A.Entities() a inner join @A.Entities() b on a.City = b.City group by a.City order by a.City desc";
            
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                    new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                    new BasicEntity("cracow", "jan", Convert.ToDecimal(10)),
                    new BasicEntity("glasgow", "feb", Convert.ToDecimal(-10))
                ]
            }
        };
            
        var vm = CreateAndRunVirtualMachine(query, sources);
            
        var table = vm.Run();
            
        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("katowice", table[0].Values[0]);
        Assert.AreEqual("glasgow", table[1].Values[0]);
        Assert.AreEqual("czestochowa", table[2].Values[0]);
        Assert.AreEqual("cracow", table[3].Values[0]);
    }

    [TestMethod]
    public void WhenOrderByWithGroupBy_ShouldSucceed()
    {
        const string query = """
                             select 
                                a.GetTypeName(a.Name),
                                a.Count(a.Name)
                             from @A.Entities() a
                             group by a.GetTypeName(a.Name)
                             order by a.GetTypeName(a.Name)
                             """;
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [new BasicEntity("a"), new BasicEntity("b"), new BasicEntity("c")]
            }
        };
        
        var vm = CreateAndRunVirtualMachine(query, sources);
        
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Count);
        
        Assert.AreEqual("System.String", table[0].Values[0]);
        Assert.AreEqual(3, table[0].Values[1]);
    }
}
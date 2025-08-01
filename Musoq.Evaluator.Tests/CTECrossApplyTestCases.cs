using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class CTECrossApplyTestCases : GenericEntityTestBase
{
    [TestMethod]
    public void CTE_WithCrossApply_ShouldNotThrowKeyNotFoundException()
    {
        // This reproduces the exact query from the issue reported by @Puchaczov
        // Modified to use #schema.first() since the test infrastructure doesn't have #system.dual()
        const string query = "with testX as ( select 'to jest test' as Text from #schema.first() a ) select t.Text as Text, t2.Value as Value from testX t cross apply t.Split(t.Text, ' ') t2";
        
        // Provide an empty array since we're just generating a constant text value
        var firstSource = new object[1] { new object() };
        var vm = CreateAndRunVirtualMachine(query, firstSource);
        var table = vm.Run();
        
        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("Text", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Value", table.Columns.ElementAt(1).ColumnName);
        
        // Should have 3 rows for 'to', 'jest', 'test'
        Assert.AreEqual(3, table.Count);
        
        Assert.IsTrue(table.Any(row => 
            (string)row[0] == "to jest test" && 
            (string)row[1] == "to"
        ), "First row should match 'to jest test', 'to'");
        
        Assert.IsTrue(table.Any(row => 
            (string)row[0] == "to jest test" && 
            (string)row[1] == "jest"
        ), "Second row should match 'to jest test', 'jest'");
        
        Assert.IsTrue(table.Any(row => 
            (string)row[0] == "to jest test" && 
            (string)row[1] == "test"
        ), "Third row should match 'to jest test', 'test'");
    }
    
    [TestMethod]
    public void CTE_WithCrossApply_UsingSystemDual_ShouldNotThrowKeyNotFoundException()
    {
        // Test using a simpler version that's more compatible with the test environment
        const string query = "with testX as ( select 'Hello World' as Text from #schema.first() ) select t.Text as Text, t2.Value as Value from testX t cross apply t.Split(t.Text, ' ') t2";
        
        var firstSource = new object[1] { new { } };
        var vm = CreateAndRunVirtualMachine(query, firstSource);
        var table = vm.Run();
        
        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("Text", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Value", table.Columns.ElementAt(1).ColumnName);
        
        // Should have 2 rows for 'Hello', 'World'
        Assert.AreEqual(2, table.Count);
        
        Assert.IsTrue(table.Any(row => 
            (string)row[0] == "Hello World" && 
            (string)row[1] == "Hello"
        ), "First row should match 'Hello World', 'Hello'");
        
        Assert.IsTrue(table.Any(row => 
            (string)row[0] == "Hello World" && 
            (string)row[1] == "World"
        ), "Second row should match 'Hello World', 'World'");
    }
}
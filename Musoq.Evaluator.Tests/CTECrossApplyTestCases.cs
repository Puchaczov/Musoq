using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class CteCrossApplyTestCases : GenericEntityTestBase
{
    [TestMethod]
    public void CTE_WithCrossApply_ShouldNotThrowKeyNotFoundException()
    {
        const string query = @"
            with testX as ( 
                select 'to jest test' as Text 
                from #schema.first() a 
            ) 
            select t.Text as Text, t2.Value as Value 
            from testX t 
            cross apply t.Split(t.Text, ' ') t2";
        
        var firstSource = new object[1] { new object() };
        var vm = CreateAndRunVirtualMachine(query, firstSource);
        var table = vm.Run();
        
        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("Text", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Value", table.Columns.ElementAt(1).ColumnName);
        
        Assert.AreEqual(3, table.Count);
        
        Assert.IsTrue(table.Any(row => 
            (string)row[0] == "to jest test" && 
            (string)row[1] == "to"));
        
        Assert.IsTrue(table.Any(row => 
            (string)row[0] == "to jest test" && 
            (string)row[1] == "jest"));
        
        Assert.IsTrue(table.Any(row => 
            (string)row[0] == "to jest test" && 
            (string)row[1] == "test"));
    }
    
    [TestMethod]
    public void CTE_WithCrossApply_UsingSystemDual_ShouldNotThrowKeyNotFoundException()
    {
        const string query = @"
            with testX as ( 
                select 'Hello World' as Text 
                from #schema.first() 
            ) 
            select t.Text as Text, t2.Value as Value 
            from testX t 
            cross apply t.Split(t.Text, ' ') t2";
        
        var firstSource = new object[1] { new { } };
        var vm = CreateAndRunVirtualMachine(query, firstSource);
        var table = vm.Run();
        
        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("Text", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Value", table.Columns.ElementAt(1).ColumnName);
        
        Assert.AreEqual(2, table.Count);
        
        Assert.IsTrue(table.Any(row => 
            (string)row[0] == "Hello World" && 
            (string)row[1] == "Hello"));
        
        Assert.IsTrue(table.Any(row => 
            (string)row[0] == "Hello World" && 
            (string)row[1] == "World"));
    }
}

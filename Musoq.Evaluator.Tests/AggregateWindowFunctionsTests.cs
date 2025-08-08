using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class AggregateWindowFunctionsTests : BasicEntityTestBase
{
    [TestMethod]
    public void SumOver_WithWindow_ShouldWork()
    {
        var query = @"
            select 
                Value,
                SUM(Value) OVER (ORDER BY Value) as RunningSum
            from #A.entities() 
            order by Value";

        var vm = CreateAndRunVirtualMachine(query);
        var table = vm.Run();

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("Value", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("RunningSum", table.Columns.ElementAt(1).ColumnName);
        
        // Basic validation that the function is callable
        Assert.IsTrue(table.Count > 0);
    }

    [TestMethod]
    public void CountOver_WithWindow_ShouldWork()
    {
        var query = @"
            select 
                Value,
                COUNT(Value) OVER (PARTITION BY Country) as CountByCountry
            from #A.entities() 
            order by Value";

        var vm = CreateAndRunVirtualMachine(query);
        var table = vm.Run();

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("Value", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("CountByCountry", table.Columns.ElementAt(1).ColumnName);
        
        // Basic validation that the function is callable
        Assert.IsTrue(table.Count > 0);
    }

    [TestMethod]
    public void AvgOver_WithWindow_ShouldWork()
    {
        var query = @"
            select 
                Value,
                AVG(Value) OVER (PARTITION BY Country ORDER BY Value) as AvgByCountry
            from #A.entities() 
            order by Value";

        var vm = CreateAndRunVirtualMachine(query);
        var table = vm.Run();

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("Value", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("AvgByCountry", table.Columns.ElementAt(1).ColumnName);
        
        // Basic validation that the function is callable
        Assert.IsTrue(table.Count > 0);
    }

    [TestMethod]
    public void MixedAggregateWindowFunctions_ShouldWork()
    {
        var query = @"
            select 
                Value,
                Country,
                SUM(Value) OVER (ORDER BY Value) as RunningSum,
                COUNT(Value) OVER (PARTITION BY Country) as CountByCountry,
                AVG(Value) OVER (PARTITION BY Country ORDER BY Value) as AvgByCountry,
                RANK() OVER (ORDER BY Value DESC) as Ranking
            from #A.entities() 
            order by Value";

        var vm = CreateAndRunVirtualMachine(query);
        var table = vm.Run();

        Assert.AreEqual(6, table.Columns.Count());
        Assert.AreEqual("Value", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Country", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual("RunningSum", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual("CountByCountry", table.Columns.ElementAt(3).ColumnName);
        Assert.AreEqual("AvgByCountry", table.Columns.ElementAt(4).ColumnName);
        Assert.AreEqual("Ranking", table.Columns.ElementAt(5).ColumnName);
        
        // Basic validation that the functions are callable together
        Assert.IsTrue(table.Count > 0);
    }
}
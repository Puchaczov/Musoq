using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class CrossApplyBugTestCases : GenericEntityTestBase
{
    private class TestClass1
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    private class TestClass2
    {
        public string FilterKey { get; set; }
        public decimal Amount { get; set; }
    }

    [TestMethod]
    public void CrossApply_WithNullParameter_ShouldHandleGracefully()
    {
        
        const string query = "select a.Key, a.Value, b.FilterKey, b.Amount from #schema.first() a cross apply #schema.second(a.Key) b";
        
        var firstSource = new List<TestClass1>
        {
            new() { Key = "Valid", Value = "Test1" },
            new() { Key = null, Value = "Test2" },  
            new() { Key = "Another", Value = "Test3" }
        }.ToArray();
        
        var secondSource = new List<TestClass2>
        {
            new() { FilterKey = "Valid", Amount = 100m },
            new() { FilterKey = "Another", Amount = 200m },
            new() { FilterKey = null, Amount = 300m }  
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query, 
            firstSource, 
            secondSource, 
            null, 
            null,
            null,
            (parameters, source) => 
            {
                var filterKey = parameters[0]; 
                return new ObjectRowsSource(source.Rows.Where(f => 
                    Equals(f["FilterKey"], filterKey)).ToArray());
            });
        
        var table = vm.Run(TestContext.CancellationToken);
        
        
        Assert.IsNotNull(table);
    }

    [TestMethod]
    public void CrossApply_WithEmptySecondSource_ShouldReturnEmpty()
    {
        
        const string query = "select a.Key, a.Value, b.FilterKey, b.Amount from #schema.first() a cross apply #schema.second(a.Key) b";
        
        var firstSource = new List<TestClass1>
        {
            new() { Key = "Valid", Value = "Test1" },
            new() { Key = "NoMatch", Value = "Test2" }  
        }.ToArray();
        
        var secondSource = new List<TestClass2>
        {
            new() { FilterKey = "Valid", Amount = 100m }
            
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query, 
            firstSource, 
            secondSource, 
            null, 
            null,
            null,
            (parameters, source) => 
            {
                var filterKey = (string)parameters[0];
                return new ObjectRowsSource(source.Rows.Where(f => 
                    (string)f["FilterKey"] == filterKey).ToArray());
            });
        
        var table = vm.Run(TestContext.CancellationToken);
        
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Valid", table[0].Values[0]);
    }

    [TestMethod]
    public void CrossApply_WithComplexParameterTypes_ShouldHandleGracefully()
    {
        
        const string query = "select a.Key, a.Value, b.FilterKey, b.Amount from #schema.first() a cross apply #schema.second(a.Key, a.Value) b";
        
        var firstSource = new List<TestClass1>
        {
            new() { Key = "Test", Value = "Value1" },
            new() { Key = null, Value = null }  
        }.ToArray();
        
        var secondSource = new List<TestClass2>
        {
            new() { FilterKey = "Test", Amount = 100m }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query, 
            firstSource, 
            secondSource, 
            null, 
            null,
            null,
            (parameters, source) => 
            {
                
                var filterKey = parameters[0];
                var filterValue = parameters[1];
                return new ObjectRowsSource(source.Rows.Where(f => 
                    Equals(f["FilterKey"], filterKey)).ToArray());
            });
        
        var table = vm.Run(TestContext.CancellationToken);
        
        
        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count); 
    }

    [TestMethod]
    public void CrossApply_WithNullRowsFromFunction_ShouldHandleNullGracefully()
    {
        
        
        const string query = "select a.Key, a.Value, b.FilterKey, b.Amount from #schema.first() a cross apply #schema.second(a.Key) b";
        
        var firstSource = new List<TestClass1>
        {
            new() { Key = "Test", Value = "Test1" }
        }.ToArray();
        
        var secondSource = new List<TestClass2>().ToArray(); 

        var vm = CreateAndRunVirtualMachine(
            query, 
            firstSource, 
            secondSource, 
            null, 
            null,
            null,
            (parameters, source) => new ObjectRowsSource(null)); 
        
        var table = vm.Run(TestContext.CancellationToken);
        
        
        Assert.IsNotNull(table);
        Assert.AreEqual(0, table.Count); 
    }

    [TestMethod]
    public void CrossApply_WithThrowingFunction_ShouldPropagateException()
    {
        
        const string query = "select a.Key, a.Value, b.FilterKey, b.Amount from #schema.first() a cross apply #schema.second(a.Key) b";
        
        var firstSource = new List<TestClass1>
        {
            new() { Key = "Test", Value = "Test1" }
        }.ToArray();
        
        var secondSource = new List<TestClass2>().ToArray(); 

        var vm = CreateAndRunVirtualMachine(
            query, 
            firstSource, 
            secondSource, 
            null, 
            null,
            null,
            (parameters, source) => throw new InvalidOperationException("Function failed")); 
        
        
        Assert.Throws<InvalidOperationException>(() => vm.Run(TestContext.CancellationToken));
    }

    [TestMethod]
    public void CrossApply_WithParameterTypeMismatch_ShouldHandleGracefully()
    {
        
        const string query = "select a.Key, a.Value, b.FilterKey, b.Amount from #schema.first() a cross apply #schema.second(a.Key) b";
        
        var firstSource = new List<TestClass1>
        {
            new() { Key = "123", Value = "Test1" }  
        }.ToArray();
        
        var secondSource = new List<TestClass2>
        {
            new() { FilterKey = "123", Amount = 100m }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query, 
            firstSource, 
            secondSource, 
            null, 
            null,
            null,
            (parameters, source) => 
            {
                
                var param = parameters[0];
                if (param != null && int.TryParse(param.ToString(), out var intParam))
                {
                    
                    return new ObjectRowsSource(source.Rows.Where(f => 
                        f["FilterKey"]?.ToString() == intParam.ToString()).ToArray());
                }
                return new ObjectRowsSource(source.Rows.Where(f => 
                    Equals(f["FilterKey"], param)).ToArray());
            });
        
        var table = vm.Run(TestContext.CancellationToken);
        
        
        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
    }

    public TestContext TestContext { get; set; }
}

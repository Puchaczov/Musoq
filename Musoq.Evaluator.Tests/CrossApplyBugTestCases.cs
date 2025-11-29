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
        // Test case where the parameter passed to cross apply is null
        const string query = "select a.Key, a.Value, b.FilterKey, b.Amount from #schema.first() a cross apply #schema.second(a.Key) b";
        
        var firstSource = new List<TestClass1>
        {
            new() { Key = "Valid", Value = "Test1" },
            new() { Key = null, Value = "Test2" },  // This null key should be passed to second source
            new() { Key = "Another", Value = "Test3" }
        }.ToArray();
        
        var secondSource = new List<TestClass2>
        {
            new() { FilterKey = "Valid", Amount = 100m },
            new() { FilterKey = "Another", Amount = 200m },
            new() { FilterKey = null, Amount = 300m }  // This matches null key
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
                var filterKey = parameters[0]; // This could be null!
                return new ObjectRowsSource(source.Rows.Where(f => 
                    Equals(f["FilterKey"], filterKey)).ToArray());
            });
        
        var table = vm.Run(TestContext.CancellationToken);
        
        // Should handle null parameters gracefully
        Assert.IsNotNull(table);
    }

    [TestMethod]
    public void CrossApply_WithEmptySecondSource_ShouldReturnEmpty()
    {
        // Test case where the second source returns no rows for some parameters
        const string query = "select a.Key, a.Value, b.FilterKey, b.Amount from #schema.first() a cross apply #schema.second(a.Key) b";
        
        var firstSource = new List<TestClass1>
        {
            new() { Key = "Valid", Value = "Test1" },
            new() { Key = "NoMatch", Value = "Test2" }  // This should return empty from second source
        }.ToArray();
        
        var secondSource = new List<TestClass2>
        {
            new() { FilterKey = "Valid", Amount = 100m }
            // No row with FilterKey = "NoMatch"
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
        
        // Should only return 1 row (where Key = "Valid")
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Valid", table[0].Values[0]);
    }

    [TestMethod]
    public void CrossApply_WithComplexParameterTypes_ShouldHandleGracefully()
    {
        // Test more complex parameter scenarios that might cause issues
        const string query = "select a.Key, a.Value, b.FilterKey, b.Amount from #schema.first() a cross apply #schema.second(a.Key, a.Value) b";
        
        var firstSource = new List<TestClass1>
        {
            new() { Key = "Test", Value = "Value1" },
            new() { Key = null, Value = null }  // Both parameters null
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
                // This lambda should handle multiple null parameters
                var filterKey = parameters[0];
                var filterValue = parameters[1];
                return new ObjectRowsSource(source.Rows.Where(f => 
                    Equals(f["FilterKey"], filterKey)).ToArray());
            });
        
        var table = vm.Run(TestContext.CancellationToken);
        
        // Should handle multiple parameters gracefully
        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count); // Only one match for "Test"
    }

    [TestMethod]
    public void CrossApply_WithNullRowsFromFunction_ShouldHandleNullGracefully()
    {
        // This test demonstrates the bug fix: when the cross apply function returns a source with null Rows
        // The fix ensures null Rows are converted to empty enumerable instead of causing NullReferenceException
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
            (parameters, source) => new ObjectRowsSource(null)); // This creates a source with null Rows!
        
        var table = vm.Run(TestContext.CancellationToken);
        
        // After the fix, this should handle null gracefully and return empty result
        Assert.IsNotNull(table);
        Assert.AreEqual(0, table.Count); // Should return empty result gracefully
    }

    [TestMethod]
    public void CrossApply_WithThrowingFunction_ShouldPropagateException()
    {
        // Test what happens when the cross apply function throws an exception
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
            (parameters, source) => throw new InvalidOperationException("Function failed")); // Function throws!
        
        // This should propagate the exception
        Assert.Throws<InvalidOperationException>(() => vm.Run(TestContext.CancellationToken));
    }

    [TestMethod]
    public void CrossApply_WithParameterTypeMismatch_ShouldHandleGracefully()
    {
        // Test what happens when parameter types don't match expected types
        const string query = "select a.Key, a.Value, b.FilterKey, b.Amount from #schema.first() a cross apply #schema.second(a.Key) b";
        
        var firstSource = new List<TestClass1>
        {
            new() { Key = "123", Value = "Test1" }  // String that could be confused as int
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
                // Try to cause type confusion
                var param = parameters[0];
                if (param != null && int.TryParse(param.ToString(), out var intParam))
                {
                    // Look for integer match when parameter was string
                    return new ObjectRowsSource(source.Rows.Where(f => 
                        f["FilterKey"]?.ToString() == intParam.ToString()).ToArray());
                }
                return new ObjectRowsSource(source.Rows.Where(f => 
                    Equals(f["FilterKey"], param)).ToArray());
            });
        
        var table = vm.Run(TestContext.CancellationToken);
        
        // Should handle type conversion gracefully
        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
    }

    public TestContext TestContext { get; set; }
}

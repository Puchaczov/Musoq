using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class OuterApplySelfPropertyTests : GenericEntityTestBase
{
    private class OuterApplyClass1
    {
        public string City { get; set; }
        
        public double[] Values { get; set; }
    }
    
    private class OuterApplyClass2
    {
        public string City { get; set; }
        
        public List<double> Values { get; set; }
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public class ComplexType1
    {
        public string Value1 { get; set; }
        
        public int Value2 { get; set; }
    }

    private class OuterApplyClass3
    {
        public string City { get; set; }
        
        [BindablePropertyAsTable]
        public ComplexType1[] Values { get; set; } 
    }
    
    private class OuterApplyClass4
    {
        public string City { get; set; }
        
        [BindablePropertyAsTable]
        public List<ComplexType1> Values { get; set; } 
    }
    
    private class OuterApplyClass5
    {
        public string City { get; set; }
        
        public double[] Values1 { get; set; }
        
        public double[] Values2 { get; set; }
    }
    
    private class OuterApplyClass7
    {
        public ComplexType5 ComplexType { get; set; }
    }
    
    public class ComplexType5
    {
        [BindablePropertyAsTable]
        public List<int> PrimitiveValues { get; set; }
        
        [BindablePropertyAsTable]
        public List<ComplexType6> ComplexValues { get; set; }
    }

    public class ComplexType6
    {
        public int Value { get; set; }
    }
    
    [TestMethod]
    public void OuterApplyProperty_NoMatch_ShouldPass()
    {
        const string query = "select a.City, b.Value from #schema.first() a outer apply a.Values as b";
        
        var firstSource = new List<OuterApplyClass1>
        {
            new() {City = "City1", Values = []},
        }.ToArray();
        
        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );
        
        var table = vm.Run(TestContext.CancellationToken);
        
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("a.City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        
        Assert.AreEqual(1, table.Count);
        
        Assert.AreEqual("City1", table[0].Values[0]);
        Assert.IsNull(table[0].Values[1]);
    }
    
    [TestMethod]
    public void OuterApplyProperty_WithPrimitiveArray_ShouldPass()
    {
        const string query = "select a.City, b.Value from #schema.first() a outer apply a.Values as b";
        
        var firstSource = new List<OuterApplyClass1>
        {
            new() {City = "City1", Values = [1]},
            new() {City = "City2", Values = [2, 3]},
            new() {City = "City3", Values = [4, 5, 6]}
        }.ToArray();
        
        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );
        
        var table = vm.Run(TestContext.CancellationToken);
        
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("a.City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        
        Assert.AreEqual(6, table.Count);
        
        Assert.AreEqual(6, table.Count, "Table should contain 6 rows");

        Assert.AreEqual(1,
table.Count(row =>
                (string)row.Values[0] == "City1" &&
                new[] { 1d }.Contains((double)row.Values[1])), "Expected 1 row for City1 with value 1");

        Assert.AreEqual(2,
table.Count(row =>
                (string)row.Values[0] == "City2" &&
                new[] { 2d, 3d }.Contains((double)row.Values[1])), "Expected 2 rows for City2 with values 2 and 3");

        Assert.AreEqual(3,
table.Count(row =>
                (string)row.Values[0] == "City3" &&
                new[] { 4d, 5d, 6d }.Contains((double)row.Values[1])), "Expected 3 rows for City3 with values 4, 5 and 6");
    }
    
    [TestMethod]
    public void OuterApplyProperty_WithWhere_ShouldPass()
    {
        const string query = "select a.City, b.Value from #schema.first() a outer apply a.Values as b where b.Value >= 2";
        
        var firstSource = new List<OuterApplyClass1>
        {
            new() {City = "City1", Values = [1]},
            new() {City = "City2", Values = [2, 3]},
            new() {City = "City3", Values = [4, 5, 6]}
        }.ToArray();
        
        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );
        
        var table = vm.Run(TestContext.CancellationToken);
        
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("a.City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        
        Assert.AreEqual(5, table.Count, "Result should contain exactly 5 city-value pairs");

        Assert.IsTrue(table.Any(row => 
            (string)row.Values[0] == "City2" && 
            Math.Abs((double)row.Values[1] - 2.0) < 0.0001
        ), "Expected pair (City2, 2.0) not found");

        Assert.IsTrue(table.Any(row => 
            (string)row.Values[0] == "City2" && 
            Math.Abs((double)row.Values[1] - 3.0) < 0.0001
        ), "Expected pair (City2, 3.0) not found");

        Assert.IsTrue(table.Any(row => 
            (string)row.Values[0] == "City3" && 
            Math.Abs((double)row.Values[1] - 4.0) < 0.0001
        ), "Expected pair (City3, 4.0) not found");

        Assert.IsTrue(table.Any(row => 
            (string)row.Values[0] == "City3" && 
            Math.Abs((double)row.Values[1] - 5.0) < 0.0001
        ), "Expected pair (City3, 5.0) not found");

        Assert.IsTrue(table.Any(row => 
            (string)row.Values[0] == "City3" && 
            Math.Abs((double)row.Values[1] - 6.0) < 0.0001
        ), "Expected pair (City3, 6.0) not found");
    }
    
    [TestMethod]
    public void OuterApplyProperty_WithGroupBy_ShouldPass()
    {
        const string query = "select a.City, a.Sum(b.Value) from #schema.first() a outer apply a.Values as b group by a.City";
        
        var firstSource = new List<OuterApplyClass1>
        {
            new() {City = "City1", Values = [1]},
            new() {City = "City2", Values = [2, 3]},
            new() {City = "City3", Values = [4, 5, 6]}
        }.ToArray();
        
        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );
        
        var table = vm.Run(TestContext.CancellationToken);
        
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("a.City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("a.Sum(b.Value)", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(1).ColumnType);
        
        Assert.AreEqual(3, table.Count, "Table should have 3 entries");

        Assert.IsTrue(table.Any(entry => 
            (string)entry.Values[0] == "City1" && 
            (decimal)entry.Values[1] == 1m), "First entry should be City1 with 1m");

        Assert.IsTrue(table.Any(entry => 
            (string)entry.Values[0] == "City2" && 
            (decimal)entry.Values[1] == 5m), "Second entry should be City2 with 5m");

        Assert.IsTrue(table.Any(entry => 
            (string)entry.Values[0] == "City3" && 
            (decimal)entry.Values[1] == 15m), "Third entry should be City3 with 15m");
    }
    
    [TestMethod]
    public void OuterApplyProperty_WithPrimitiveList_ShouldPass()
    {
        const string query = "select a.City, b.Value from #schema.first() a outer apply a.Values as b";
        
        var firstSource = new List<OuterApplyClass2>
        {
            new() {City = "City1", Values = [1]},
            new() {City = "City2", Values = [2, 3]},
            new() {City = "City3", Values = [4, 5, 6]}
        }.ToArray();
        
        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );
        
        var table = vm.Run(TestContext.CancellationToken);
        
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("a.City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        
        Assert.AreEqual(6, table.Count, "Table should contain 6 rows");

        Assert.IsTrue(table.Count(row => 
                          (string)row.Values[0] == "City1") == 1 &&
                      table.Any(row => 
                          (string)row.Values[0] == "City1" && 
                          (double)row.Values[1] == 1d),
            "Expected one row for City1 with value 1");

        Assert.IsTrue(table.Count(row => 
                          (string)row.Values[0] == "City2") == 2 &&
                      table.Any(row => 
                          (string)row.Values[0] == "City2" && 
                          (double)row.Values[1] == 2d) &&
                      table.Any(row =>
                          (string)row.Values[0] == "City2" && 
                          (double)row.Values[1] == 3d),
            "Expected two rows for City2 with values 2 and 3");

        Assert.IsTrue(table.Count(row => 
                          (string)row.Values[0] == "City3") == 3 &&
                      table.Any(row => 
                          (string)row.Values[0] == "City3" && 
                          (double)row.Values[1] == 4d) &&
                      table.Any(row =>
                          (string)row.Values[0] == "City3" && 
                          (double)row.Values[1] == 5d) &&
                      table.Any(row =>
                          (string)row.Values[0] == "City3" && 
                          (double)row.Values[1] == 6d),
            "Expected three rows for City3 with values 4, 5 and 6");
    }
    
    [TestMethod]
    public void OuterApplyProperty_WithComplexArray_ShouldPass()
    {
        const string query = "select a.City, b.Value1, b.Value2 from #schema.first() a outer apply a.Values as b";
        
        var firstSource = new List<OuterApplyClass3>
        {
            new() {City = "City1", Values = [new ComplexType1 {Value1 = "Value1", Value2 = 1}]},
            new() {City = "City2", Values = [new ComplexType1 {Value1 = "Value2", Value2 = 2}, new ComplexType1 {Value1 = "Value3", Value2 = 3}]},
            new() {City = "City3", Values = [new ComplexType1 {Value1 = "Value4", Value2 = 4}, new ComplexType1 {Value1 = "Value5", Value2 = 5}, new ComplexType1 {Value1 = "Value6", Value2 = 6}]}
        }.ToArray();
        
        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );
        
        var table = vm.Run(TestContext.CancellationToken);
        
        Assert.AreEqual(3, table.Columns.Count());
        Assert.AreEqual("a.City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("b.Value1", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual("b.Value2", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(int?), table.Columns.ElementAt(2).ColumnType);
        
        Assert.AreEqual(6, table.Count, "Table should contain 6 rows");

        Assert.IsTrue(table.Count(row => 
                          (string)row.Values[0] == "City1") == 1 &&
                      table.Any(row => 
                          (string)row.Values[0] == "City1" && 
                          (string)row.Values[1] == "Value1" && 
                          (int)row.Values[2] == 1),
            "Expected one row for City1 with Value1 and number 1");

        Assert.IsTrue(table.Count(row => 
                          (string)row.Values[0] == "City2") == 2 &&
                      table.Any(row => 
                          (string)row.Values[0] == "City2" && 
                          (string)row.Values[1] == "Value2" && 
                          (int)row.Values[2] == 2) &&
                      table.Any(row =>
                          (string)row.Values[0] == "City2" && 
                          (string)row.Values[1] == "Value3" && 
                          (int)row.Values[2] == 3),
            "Expected two rows for City2 with Value2/3 and numbers 2/3");

        Assert.IsTrue(table.Count(row => 
                          (string)row.Values[0] == "City3") == 3 &&
                      table.Any(row => 
                          (string)row.Values[0] == "City3" && 
                          (string)row.Values[1] == "Value4" && 
                          (int)row.Values[2] == 4) &&
                      table.Any(row =>
                          (string)row.Values[0] == "City3" && 
                          (string)row.Values[1] == "Value5" && 
                          (int)row.Values[2] == 5) &&
                      table.Any(row =>
                          (string)row.Values[0] == "City3" && 
                          (string)row.Values[1] == "Value6" && 
                          (int)row.Values[2] == 6),
            "Expected three rows for City3 with Value4/5/6 and numbers 4/5/6");
    }
    
    [TestMethod]
    public void OuterApplyProperty_WithComplexList_ShouldPass()
    {
        const string query = "select a.City, b.Value1, b.Value2 from #schema.first() a outer apply a.Values as b";
        
        var firstSource = new List<OuterApplyClass4>
        {
            new() {City = "City1", Values = [new ComplexType1 {Value1 = "Value1", Value2 = 1}]},
            new() {City = "City2", Values = [new ComplexType1 {Value1 = "Value2", Value2 = 2}, new ComplexType1 {Value1 = "Value3", Value2 = 3}]},
            new() {City = "City3", Values = [new ComplexType1 {Value1 = "Value4", Value2 = 4}, new ComplexType1 {Value1 = "Value5", Value2 = 5}, new ComplexType1 {Value1 = "Value6", Value2 = 6}]}
        }.ToArray();
        
        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );
        
        var table = vm.Run(TestContext.CancellationToken);
        
        Assert.AreEqual(3, table.Columns.Count());
        Assert.AreEqual("a.City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("b.Value1", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual("b.Value2", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(int?), table.Columns.ElementAt(2).ColumnType);
        
        Assert.AreEqual(6, table.Count, "Table should have 6 entries");

        Assert.IsTrue(table.Any(entry => 
                (string)entry.Values[0] == "City1" && 
                (string)entry.Values[1] == "Value1" && 
                Convert.ToInt32(entry.Values[2]) == 1), 
            "First entry should match expected values");

        Assert.IsTrue(table.Any(entry => 
                (string)entry.Values[0] == "City2" && 
                (string)entry.Values[1] == "Value2" && 
                Convert.ToInt32(entry.Values[2]) == 2), 
            "Second entry should match expected values");

        Assert.IsTrue(table.Any(entry => 
                (string)entry.Values[0] == "City2" && 
                (string)entry.Values[1] == "Value3" && 
                Convert.ToInt32(entry.Values[2]) == 3), 
            "Third entry should match expected values");

        Assert.IsTrue(table.Any(entry => 
                (string)entry.Values[0] == "City3" && 
                (string)entry.Values[1] == "Value4" && 
                Convert.ToInt32(entry.Values[2]) == 4), 
            "Fourth entry should match expected values");

        Assert.IsTrue(table.Any(entry => 
                (string)entry.Values[0] == "City3" && 
                (string)entry.Values[1] == "Value5" && 
                Convert.ToInt32(entry.Values[2]) == 5), 
            "Fifth entry should match expected values");

        Assert.IsTrue(table.Any(entry => 
                (string)entry.Values[0] == "City3" && 
                (string)entry.Values[1] == "Value6" && 
                Convert.ToInt32(entry.Values[2]) == 6), 
            "Sixth entry should match expected values");
    }
    
    [TestMethod]
    public void OuterApplyProperty_MultiplePrimitiveArrays_ShouldPass()
    {
        const string query = "select b.Value, c.Value from #schema.first() a outer apply a.Values1 as b outer apply a.Values2 as c";
        
        var firstSource = new List<OuterApplyClass5>
        {
            new() {City = "City1", Values1 = [1], Values2=[1.1]},
            new() {City = "City2", Values1 = [2, 3], Values2 = [2.1, 2.2, 3.3]},
            new() {City = "City3", Values1 = [4, 5, 6], Values2 = [4.1, 5.1, 6.1]}
        }.ToArray();
        
        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );
        
        var table = vm.Run(TestContext.CancellationToken);
        
        
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("b.Value", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(double?), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("c.Value", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(double?), table.Columns.ElementAt(1).ColumnType);
        
        Assert.AreEqual(16, table.Count);

        var expectedPairs = new List<(double First, double Second)>
        {
            (1.0, 1.1),
            
            (2.0, 2.1), (2.0, 2.2), (2.0, 3.3),
            (3.0, 2.1), (3.0, 2.2), (3.0, 3.3),
            
            (4.0, 4.1), (4.0, 5.1), (4.0, 6.1),
            (5.0, 4.1), (5.0, 5.1), (5.0, 6.1),
            (6.0, 4.1), (6.0, 5.1), (6.0, 6.1)
        };

        foreach (var expected in expectedPairs)
        {
            Assert.IsTrue(
                table.Any(row => 
                    Math.Abs((double)row.Values[0] - expected.First) < 0.0001 && 
                    Math.Abs((double)row.Values[1] - expected.Second) < 0.0001
                ),
                $"Expected combination ({expected.First}, {expected.Second}) not found"
            );
        }

        var firstColumnFrequencies = new Dictionary<double, int>
        {
            {1.0, 1},  
            {2.0, 3},  
            {3.0, 3},  
            {4.0, 3},  
            {5.0, 3},
            {6.0, 3}
        };

        foreach (var pair in firstColumnFrequencies)
        {
            var actualCount = table.Count(row => 
                Math.Abs((double)row.Values[0] - pair.Key) < 0.0001
            );
            Assert.AreEqual(pair.Value, actualCount,
                $"Value {pair.Key} in first column should appear {pair.Value} times");
        }
    }
    
    [TestMethod]
    public void WhenApplyChainedProperty_WithPrimitiveList_ShouldPass()
    {
        const string query = """
                             select 
                                b.Value 
                             from #schema.first() a 
                             outer apply a.ComplexType.PrimitiveValues as b
                             """;
        
        var firstSource = new List<OuterApplyClass7>
        {
            new() {
                ComplexType = new ComplexType5
                {
                    PrimitiveValues = [1, 2]
                }
            }
        }.ToArray();
        
        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );
        
        var table = vm.Run(TestContext.CancellationToken);
        
        Assert.AreEqual(1, table.Columns.Count());
        
        Assert.AreEqual(2, table.Count, "Table should contain 2 rows");

        Assert.IsTrue(table.Any(row => 
                (int)row.Values[0] == 1), 
            "Expected row with value 1");

        Assert.IsTrue(table.Any(row => 
                (int)row.Values[0] == 2),
            "Expected row with value 2");
    }
    
    [TestMethod]
    public void WhenApplyChainedProperty_WithComplexList_ShouldPass()
    {
        const string query = """
                             select 
                                b.Value 
                             from #schema.first() a 
                             outer apply a.ComplexType.ComplexValues as b
                             """;
        
        var firstSource = new List<OuterApplyClass7>
        {
            new() {
                ComplexType = new ComplexType5
                {
                    ComplexValues = [new ComplexType6 { Value = 1}, new ComplexType6 { Value = 2}]
                }
            }
        }.ToArray();
        
        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );
        
        var table = vm.Run(TestContext.CancellationToken);
        
        Assert.AreEqual(1, table.Columns.Count());
        
        Assert.AreEqual(2, table.Count);
        
        Assert.AreEqual(1,
table.Count(row =>
                (int)row.Values[0] == 1), "Expected row with value 1");
        Assert.AreEqual(1,
table.Count(row =>
                (int)row.Values[0] == 2), "Expected row with value 2");
    }

    public TestContext TestContext { get; set; }
}

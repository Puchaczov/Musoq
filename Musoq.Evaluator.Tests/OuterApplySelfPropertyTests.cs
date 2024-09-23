using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
        
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("a.City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        
        Assert.AreEqual(1, table.Count);
        
        Assert.AreEqual("City1", table[0].Values[0]);
        Assert.AreEqual(null, table[0].Values[1]);
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
        
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("a.City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        
        Assert.AreEqual(6, table.Count);
        
        Assert.AreEqual("City1", table[0].Values[0]);
        Assert.AreEqual(1d, table[0].Values[1]);
        
        Assert.AreEqual("City2", table[1].Values[0]);
        Assert.AreEqual(2d, table[1].Values[1]);
        
        Assert.AreEqual("City2", table[2].Values[0]);
        Assert.AreEqual(3d, table[2].Values[1]);
        
        Assert.AreEqual("City3", table[3].Values[0]);
        Assert.AreEqual(4d, table[3].Values[1]);
        
        Assert.AreEqual("City3", table[4].Values[0]);
        Assert.AreEqual(5d, table[4].Values[1]);
        
        Assert.AreEqual("City3", table[5].Values[0]);
        Assert.AreEqual(6d, table[5].Values[1]);
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
        
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("a.City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        
        Assert.AreEqual(5, table.Count);
        
        Assert.AreEqual("City2", table[0].Values[0]);
        Assert.AreEqual(2d, table[0].Values[1]);
        
        Assert.AreEqual("City2", table[1].Values[0]);
        Assert.AreEqual(3d, table[1].Values[1]);
        
        Assert.AreEqual("City3", table[2].Values[0]);
        Assert.AreEqual(4d, table[2].Values[1]);
        
        Assert.AreEqual("City3", table[3].Values[0]);
        Assert.AreEqual(5d, table[3].Values[1]);
        
        Assert.AreEqual("City3", table[4].Values[0]);
        Assert.AreEqual(6d, table[4].Values[1]);
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
        
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("a.City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("Sum(b.Value)", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(1).ColumnType);
        
        Assert.AreEqual(3, table.Count);
        
        Assert.AreEqual("City1", table[0].Values[0]);
        Assert.AreEqual(1m, table[0].Values[1]);
        
        Assert.AreEqual("City2", table[1].Values[0]);
        Assert.AreEqual(5m, table[1].Values[1]);
        
        Assert.AreEqual("City3", table[2].Values[0]);
        Assert.AreEqual(15m, table[2].Values[1]);
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
        
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("a.City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        
        Assert.AreEqual(6, table.Count);
        
        Assert.AreEqual("City1", table[0].Values[0]);
        Assert.AreEqual(1d, table[0].Values[1]);
        
        Assert.AreEqual("City2", table[1].Values[0]);
        Assert.AreEqual(2d, table[1].Values[1]);
        
        Assert.AreEqual("City2", table[2].Values[0]);
        Assert.AreEqual(3d, table[2].Values[1]);
        
        Assert.AreEqual("City3", table[3].Values[0]);
        Assert.AreEqual(4d, table[3].Values[1]);
        
        Assert.AreEqual("City3", table[4].Values[0]);
        Assert.AreEqual(5d, table[4].Values[1]);
        
        Assert.AreEqual("City3", table[5].Values[0]);
        Assert.AreEqual(6d, table[5].Values[1]);
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
        
        var table = vm.Run();
        
        Assert.AreEqual(3, table.Columns.Count());
        Assert.AreEqual("a.City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("b.Value1", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual("b.Value2", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(int?), table.Columns.ElementAt(2).ColumnType);
        
        Assert.AreEqual(6, table.Count);
        
        Assert.AreEqual("City1", table[0].Values[0]);
        Assert.AreEqual("Value1", table[0].Values[1]);
        Assert.AreEqual(1, table[0].Values[2]);
        
        Assert.AreEqual("City2", table[1].Values[0]);
        Assert.AreEqual("Value2", table[1].Values[1]);
        Assert.AreEqual(2, table[1].Values[2]);
        
        Assert.AreEqual("City2", table[2].Values[0]);
        Assert.AreEqual("Value3", table[2].Values[1]);
        Assert.AreEqual(3, table[2].Values[2]);
        
        Assert.AreEqual("City3", table[3].Values[0]);
        Assert.AreEqual("Value4", table[3].Values[1]);
        Assert.AreEqual(4, table[3].Values[2]);
        
        Assert.AreEqual("City3", table[4].Values[0]);
        Assert.AreEqual("Value5", table[4].Values[1]);
        Assert.AreEqual(5, table[4].Values[2]);
        
        Assert.AreEqual("City3", table[5].Values[0]);
        Assert.AreEqual("Value6", table[5].Values[1]);
        Assert.AreEqual(6, table[5].Values[2]);
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
        
        var table = vm.Run();
        
        Assert.AreEqual(3, table.Columns.Count());
        Assert.AreEqual("a.City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("b.Value1", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual("b.Value2", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(int?), table.Columns.ElementAt(2).ColumnType);
        
        Assert.AreEqual(6, table.Count);
        
        Assert.AreEqual("City1", table[0].Values[0]);
        Assert.AreEqual("Value1", table[0].Values[1]);
        Assert.AreEqual(1, table[0].Values[2]);
        
        Assert.AreEqual("City2", table[1].Values[0]);
        Assert.AreEqual("Value2", table[1].Values[1]);
        Assert.AreEqual(2, table[1].Values[2]);
        
        Assert.AreEqual("City2", table[2].Values[0]);
        Assert.AreEqual("Value3", table[2].Values[1]);
        Assert.AreEqual(3, table[2].Values[2]);
        
        Assert.AreEqual("City3", table[3].Values[0]);
        Assert.AreEqual("Value4", table[3].Values[1]);
        Assert.AreEqual(4, table[3].Values[2]);
        
        Assert.AreEqual("City3", table[4].Values[0]);
        Assert.AreEqual("Value5", table[4].Values[1]);
        Assert.AreEqual(5, table[4].Values[2]);
        
        Assert.AreEqual("City3", table[5].Values[0]);
        Assert.AreEqual("Value6", table[5].Values[1]);
        Assert.AreEqual(6, table[5].Values[2]);
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
        
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("b.Value", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(double?), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("c.Value", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(double?), table.Columns.ElementAt(1).ColumnType);
        
        Assert.AreEqual(16, table.Count);
        
        Assert.AreEqual(1d, table[0].Values[0]);
        Assert.AreEqual(1.1d, table[0].Values[1]);
        
        Assert.AreEqual(2d, table[1].Values[0]);
        Assert.AreEqual(2.1d, table[1].Values[1]);
        
        Assert.AreEqual(2d, table[2].Values[0]);
        Assert.AreEqual(2.2d, table[2].Values[1]);
        
        Assert.AreEqual(2d, table[3].Values[0]);
        Assert.AreEqual(3.3d, table[3].Values[1]);
        
        Assert.AreEqual(3d, table[4].Values[0]);
        Assert.AreEqual(2.1d, table[4].Values[1]);
        
        Assert.AreEqual(3d, table[5].Values[0]);
        Assert.AreEqual(2.2d, table[5].Values[1]);
        
        Assert.AreEqual(3d, table[6].Values[0]);
        Assert.AreEqual(3.3d, table[6].Values[1]);
        
        Assert.AreEqual(4d, table[7].Values[0]);
        Assert.AreEqual(4.1d, table[7].Values[1]);
        
        Assert.AreEqual(4d, table[8].Values[0]);
        Assert.AreEqual(5.1d, table[8].Values[1]);
        
        Assert.AreEqual(4d, table[9].Values[0]);
        Assert.AreEqual(6.1d, table[9].Values[1]);
        
        Assert.AreEqual(5d, table[10].Values[0]);
        Assert.AreEqual(4.1d, table[10].Values[1]);
            
        Assert.AreEqual(5d, table[11].Values[0]);
        Assert.AreEqual(5.1d, table[11].Values[1]);
        
        Assert.AreEqual(5d, table[12].Values[0]);
        Assert.AreEqual(6.1d, table[12].Values[1]);
        
        Assert.AreEqual(6d, table[13].Values[0]);
        Assert.AreEqual(4.1d, table[13].Values[1]);
        
        Assert.AreEqual(6d, table[14].Values[0]);
        Assert.AreEqual(5.1d, table[14].Values[1]);
        
        Assert.AreEqual(6d, table[15].Values[0]);
        Assert.AreEqual(6.1d, table[15].Values[1]);
    }
}
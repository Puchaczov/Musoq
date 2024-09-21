using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class CrossApplyMethodCallTests : GenericEntityTestBase
{
    private class CrossApplyClass1
    {
        public int Value1 { get; set; }
        
        public string Value2 { get; set; }
    }
    
    private class CrossApplyClass2
    {
        public string Text { get; set; }
    }
    
    [TestMethod]
    public void CrossApplyProperty_NoMatch_ShouldPass()
    {
        const string query = "select b.Value from #schema.first() a cross apply a.Split(a.Value2, ' ') as b";
        
        var firstSource = new List<CrossApplyClass1>
        {
            new() {Value1 = 1, Value2 = string.Empty}
        }.ToArray();
        
        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );
        
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("b.Value", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        
        Assert.AreEqual(0, table.Count);
    }
    
    [TestMethod]
    public void CrossApplyProperty_SplitStringToWords_ShouldPass()
    {
        const string query = "select b.Value from #schema.first() a cross apply a.Split(a.Text, ' ') as b";
        
        var firstSource = new List<CrossApplyClass2>
        {
            new() {Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit."},
        }.ToArray();
        
        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );
        
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("b.Value", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        
        Assert.AreEqual(8, table.Count);
        
        Assert.AreEqual("Lorem", table[0][0]);
        Assert.AreEqual("ipsum", table[1][0]);
        Assert.AreEqual("dolor", table[2][0]);
        Assert.AreEqual("sit", table[3][0]);
        Assert.AreEqual("amet,", table[4][0]);
        Assert.AreEqual("consectetur", table[5][0]);
        Assert.AreEqual("adipiscing", table[6][0]);
        Assert.AreEqual("elit.", table[7][0]);
    }
    
    [TestMethod]
    public void CrossApplyProperty_SkipAfterSplit_ShouldPass()
    {
        const string query = "select b.Value from #schema.first() a cross apply a.Skip(a.Split(a.Text, ' '), 1) as b";
        
        var firstSource = new List<CrossApplyClass2>
        {
            new() {Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit."},
        }.ToArray();
        
        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );
        
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("b.Value", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        
        Assert.AreEqual(7, table.Count);
        
        Assert.AreEqual("ipsum", table[0][0]);
        Assert.AreEqual("dolor", table[1][0]);
        Assert.AreEqual("sit", table[2][0]);
        Assert.AreEqual("amet,", table[3][0]);
        Assert.AreEqual("consectetur", table[4][0]);
        Assert.AreEqual("adipiscing", table[5][0]);
        Assert.AreEqual("elit.", table[6][0]);
    }
    
    [TestMethod]
    public void CrossApplyProperty_TakeSkipAfterSplit_ShouldPass()
    {
        const string query = "select b.Value from #schema.first() a cross apply a.Take(a.Skip(a.Split(a.Text, ' '), 1), 6) as b";
        
        var firstSource = new List<CrossApplyClass2>
        {
            new() {Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit."},
        }.ToArray();
        
        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );
        
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("b.Value", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        
        Assert.AreEqual(6, table.Count);
        
        Assert.AreEqual("ipsum", table[0][0]);
        Assert.AreEqual("dolor", table[1][0]);
        Assert.AreEqual("sit", table[2][0]);
        Assert.AreEqual("amet,", table[3][0]);
        Assert.AreEqual("consectetur", table[4][0]);
        Assert.AreEqual("adipiscing", table[5][0]);
    }
}
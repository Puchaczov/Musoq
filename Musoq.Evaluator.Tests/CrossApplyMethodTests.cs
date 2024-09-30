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
    
    private class CrossApplyClass3
    {
        public string Numbers { get; set; }
        
        public string Words { get; set; }
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
    public void CrossApplyProperty_MultipleSplitWords_ShouldPass()
    {
        const string query = "select b.Value, c.Value from #schema.first() a cross apply a.Split(a.Text, ' ') as b cross apply a.Split(a.Text, ' ') as c";
        
        string[] words = ["Lorem", "ipsum", "dolor", "sit", "amet,", "consectetur", "adipiscing", "elit."];
        
        var firstSource = new List<CrossApplyClass2>
        {
            new() {Text = string.Join(" ", words)},
        }.ToArray();
        
        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );
        
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("b.Value", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("c.Value", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
        
        Assert.AreEqual(64, table.Count);
    
        for (var i = 0; i < words.Length; i++)
        {
            for (var j = 0; j < words.Length; j++)
            {
                var index = i * words.Length + j;
                Assert.AreEqual(words[i], table[index][0], $"Mismatch at index {index}, column 0");
                Assert.AreEqual(words[j], table[index][1], $"Mismatch at index {index}, column 1");
            }
        }
    }
    
    [TestMethod]
    public void CrossApplyProperty_SplitWithMultipleProperties_ShouldPass()
    {
        const string query = "select b.Value, c.Value from #schema.first() a cross apply a.Split(a.Numbers, ',') as b cross apply a.Split(a.Words, ' ') as c";
        
        string[] words = ["Lorem", "ipsum", "dolor", "sit", "amet,", "consectetur", "adipiscing", "elit."];
        
        var firstSource = new List<CrossApplyClass3>
        {
            new()
            {
                Words = string.Join(" ", words),
                Numbers = "1,2"
            }
        }.ToArray();
        
        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );
        
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("b.Value", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("c.Value", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
        
        Assert.AreEqual(16, table.Count);
        
        for (var i = 0; i < 2; i++)
        {
            for (var j = 0; j < words.Length; j++)
            {
                var index = i * words.Length + j;
                Assert.AreEqual(i == 0 ? "1" : "2", table[index][0], $"Mismatch at index {index}, column 0");
                Assert.AreEqual(words[j], table[index][1], $"Mismatch at index {index}, column 1");
            }
        }
    }
    
    [TestMethod]
    public void CrossApplyProperty_SplitWithMultipleProperties_ShouldPass2()
    {
        const string query = "select b.Value, c.Value from #schema.first() a cross apply a.Split(a.Words, ' ') as b cross apply b.ToCharArray(b.Value) as c";
        
        string[] words = ["Lorem", "ipsum", "dolor", "sit", "amet,", "consectetur", "adipiscing", "elit."];
        
        var firstSource = new List<CrossApplyClass3>
        {
            new()
            {
                Words = string.Join(" ", words)
            }
        }.ToArray();
        
        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );
        
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("b.Value", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("c.Value", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(char), table.Columns.ElementAt(1).ColumnType);
        
        Assert.AreEqual(49, table.Count);
        
        var index = 0;
        foreach (var word in words)
        {
            foreach (var character in word)
            {
                Assert.AreEqual(word, table[index][0], $"Mismatch at index {index}, column 0");
                Assert.AreEqual(character, table[index][1], $"Mismatch at index {index}, column 1");
                index++;
            }
        }
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
    
    [TestMethod]
    public void CrossApplyProperty_WhereCondition_ShouldPass()
    {
        const string query = "select b.Value from #schema.first() a cross apply a.Split(a.Text, ' ') as b where b.Value.Length > 5";
        
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
        
        Assert.AreEqual(2, table.Count);
        
        Assert.AreEqual("consectetur", table[0][0]);
        Assert.AreEqual("adipiscing", table[1][0]);
    }
    
    [TestMethod]
    public void CrossApplyProperty_GroupBy_ShouldPass()
    {
        const string query = "select Length(b.Value), Count(Length(b.Value)) from #schema.first() a cross apply a.Split(a.Text, ' ') as b group by Length(b.Value)";
        
        var firstSource = new List<CrossApplyClass2>
        {
            new() {Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit."},
        }.ToArray();
        
        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );
        
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("Length(b.Value)", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(int?), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("Count(Length(b.Value))", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(1).ColumnType);
        
        Assert.AreEqual(4, table.Count);
        
        Assert.AreEqual(5, table[0][0]);
        Assert.AreEqual(5, table[0][1]);
        
        Assert.AreEqual(3, table[1][0]);
        Assert.AreEqual(1, table[1][1]);
        
        Assert.AreEqual(11, table[2][0]);
        Assert.AreEqual(1, table[2][1]);
        
        Assert.AreEqual(10, table[3][0]);
        Assert.AreEqual(1, table[3][1]);
    }
}
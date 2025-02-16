using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class OuterApplyMethodCallTests : GenericEntityTestBase
{
    private class OuterApplyClass1
    {
        public int Value1 { get; set; }
        
        public string Value2 { get; set; }
    }
    
    private class OuterApplyClass2
    {
        public string Text { get; set; }
    }
    
    [TestMethod]
    public void OuterApplyProperty_NoMatch_ShouldPass()
    {
        const string query = "select b.Value from #schema.first() a outer apply a.Split(a.Value2, ' ') as b";
        
        var firstSource = new List<OuterApplyClass1>
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
        
        Assert.AreEqual(1, table.Count);
        
        Assert.AreEqual(null, table[0][0]);
    }
    
    [TestMethod]
    public void OuterApplyProperty_SplitStringToWords_ShouldPass()
    {
        const string query = "select b.Value from #schema.first() a outer apply a.Split(a.Text, ' ') as b";
        
        var firstSource = new List<OuterApplyClass2>
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
        
        Assert.IsTrue(table.Count == 8, "Table should contain 8 rows");

        var expectedStrings = new[] {"Lorem", "ipsum", "dolor", "sit", "amet,", "consectetur", "adipiscing", "elit."};
        foreach (var expected in expectedStrings) {
            Assert.IsTrue(table.Any(row => (string)row[0] == expected), $"Row with value {expected} not found");
        }
    }
    
    [TestMethod]
    public void OuterApplyProperty_SkipAfterSplit_ShouldPass()
    {
        const string query = "select b.Value from #schema.first() a outer apply a.Skip(a.Split(a.Text, ' '), 1) as b";
        
        var firstSource = new List<OuterApplyClass2>
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
        
        Assert.IsTrue(table.Count == 7, "Table should contain 7 rows");

        Assert.IsTrue(table.Any(row => (string)row[0] == "ipsum"), "Missing ipsum");
        Assert.IsTrue(table.Any(row => (string)row[0] == "dolor"), "Missing dolor");
        Assert.IsTrue(table.Any(row => (string)row[0] == "sit"), "Missing sit");
        Assert.IsTrue(table.Any(row => (string)row[0] == "amet,"), "Missing amet,");
        Assert.IsTrue(table.Any(row => (string)row[0] == "consectetur"), "Missing consectetur");
        Assert.IsTrue(table.Any(row => (string)row[0] == "adipiscing"), "Missing adipiscing");
        Assert.IsTrue(table.Any(row => (string)row[0] == "elit."), "Missing elit.");
    }
    
    [TestMethod]
    public void OuterApplyProperty_TakeSkipAfterSplit_ShouldPass()
    {
        const string query = "select b.Value from #schema.first() a outer apply a.Take(a.Skip(a.Split(a.Text, ' '), 1), 6) as b";
        
        var firstSource = new List<OuterApplyClass2>
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
        
        Assert.IsTrue(table.Count == 6, "Table should contain 6 rows");

        Assert.IsTrue(table.Any(row => (string)row[0] == "ipsum"), "Missing ipsum");
        Assert.IsTrue(table.Any(row => (string)row[0] == "dolor"), "Missing dolor");
        Assert.IsTrue(table.Any(row => (string)row[0] == "sit"), "Missing sit");
        Assert.IsTrue(table.Any(row => (string)row[0] == "amet,"), "Missing amet,");
        Assert.IsTrue(table.Any(row => (string)row[0] == "consectetur"), "Missing consectetur");
        Assert.IsTrue(table.Any(row => (string)row[0] == "adipiscing"), "Missing adipiscing");
    }
    
    [TestMethod]
    public void OuterApplyProperty_WhereCondition_ShouldPass()
    {
        const string query = "select b.Value from #schema.first() a outer apply a.Split(a.Text, ' ') as b where b.Value.Length > 5";
        
        var firstSource = new List<OuterApplyClass2>
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
        
        Assert.IsTrue(table.Count == 2, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => (string)entry[0] == "consectetur"), "First entry should be 'consectetur'");
        Assert.IsTrue(table.Any(entry => (string)entry[0] == "adipiscing"), "Second entry should be 'adipiscing'");
    }
    
    [TestMethod]
    public void OuterApplyProperty_GroupBy_ShouldPass()
    {
        const string query = "select Length(b.Value), Count(Length(b.Value)) from #schema.first() a outer apply a.Split(a.Text, ' ') as b group by Length(b.Value)";
        
        var firstSource = new List<OuterApplyClass2>
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
        
        Assert.IsTrue(table.Count == 4, "Table should have 4 entries");

        Assert.IsTrue(table.Any(row => 
            (int)row[0] == 5 && 
            (int)row[1] == 5
        ), "First row should be 5, 5");

        Assert.IsTrue(table.Any(row => 
            (int)row[0] == 3 && 
            (int)row[1] == 1
        ), "Second row should be 3, 1");

        Assert.IsTrue(table.Any(row => 
            (int)row[0] == 11 && 
            (int)row[1] == 1
        ), "Third row should be 11, 1");

        Assert.IsTrue(table.Any(row => 
            (int)row[0] == 10 && 
            (int)row[1] == 1
        ), "Fourth row should be 10, 1");
    }
    
    [TestMethod]
    public void OuterApplyProperty_MultipleSplitWords_ShouldPass()
    {
        const string query = "select b.Value, c.Value from #schema.first() a outer apply a.Split(a.Text, ' ') as b outer apply a.Split(a.Text, ' ') as c";
        
        string[] words = ["Lorem", "ipsum", "dolor", "sit", "amet,", "consectetur", "adipiscing", "elit."];
        
        var firstSource = new List<OuterApplyClass2>
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
        
        var expectedCount = words.Length * words.Length;
        Assert.AreEqual(expectedCount, table.Count,
            $"Should have {expectedCount} rows (each word combining with every word)");

        foreach (var firstWord in words)
        {
            foreach (var secondWord in words)
            {
                Assert.IsTrue(
                    table.Any(row => 
                        (string)row[0] == firstWord && 
                        (string)row[1] == secondWord),
                    $"Combination of '{firstWord}' with '{secondWord}' not found"
                );
            }
        }

        foreach (var word in words)
        {
            var firstColumnCount = table.Count(row => (string)row[0] == word);
            Assert.AreEqual(words.Length, firstColumnCount,
                $"Word '{word}' should appear {words.Length} times in first column");

            var secondColumnCount = table.Count(row => (string)row[1] == word);
            Assert.AreEqual(words.Length, secondColumnCount,
                $"Word '{word}' should appear {words.Length} times in second column");
        }
    }
}
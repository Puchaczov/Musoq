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
        
        Assert.IsTrue(table.Count == 8, "Table should contain 8 rows");

        Assert.IsTrue(table.Any(row => (string)row[0] == "Lorem"), "Missing Lorem row");
        Assert.IsTrue(table.Any(row => (string)row[0] == "ipsum"), "Missing ipsum row");
        Assert.IsTrue(table.Any(row => (string)row[0] == "dolor"), "Missing dolor row");
        Assert.IsTrue(table.Any(row => (string)row[0] == "sit"), "Missing sit row");
        Assert.IsTrue(table.Any(row => (string)row[0] == "amet,"), "Missing amet, row");
        Assert.IsTrue(table.Any(row => (string)row[0] == "consectetur"), "Missing consectetur row");
        Assert.IsTrue(table.Any(row => (string)row[0] == "adipiscing"), "Missing adipiscing row");
        Assert.IsTrue(table.Any(row => (string)row[0] == "elit."), "Missing elit. row");
    }
    
    [TestMethod]
    public void CrossApplyProperty_MultipleSplitWords_ShouldPass()
    {
        const string query = @"
            select 
                b.Value, 
                c.Value 
            from #schema.first() a cross apply a.Split(a.Text, ' ') as b cross apply a.Split(a.Text, ' ') as c";
        
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
        
        var expectedTotalCount = words.Length * words.Length;
        Assert.AreEqual(expectedTotalCount, table.Count, 
            "Total count should be square of number of words");
        
        var expectedPairs = from firstWord in words
                           from secondWord in words
                           select (First: firstWord, Second: secondWord);
        
        var actualPairs = table
            .Select(row => (First: (string)row[0], Second: (string)row[1]))
            .ToList();
        
        foreach (var expected in expectedPairs)
        {
            Assert.IsTrue(
                actualPairs.Any(actual =>
                    actual.First == expected.First &&
                    actual.Second == expected.Second),
                $"Missing combination: First='{expected.First}', Second='{expected.Second}'"
            );
        }
        
        foreach (var word in words)
        {
            var expectedFrequency = words.Length;
            var actualFrequency = actualPairs.Count(p => p.First == word);
            Assert.AreEqual(expectedFrequency, actualFrequency,
                $"Word '{word}' should appear {expectedFrequency} times in first column");
        }
        
        foreach (var word in words)
        {
            var expectedFrequency = words.Length;
            var actualFrequency = actualPairs.Count(p => p.Second == word);
            Assert.AreEqual(expectedFrequency, actualFrequency,
                $"Word '{word}' should appear {expectedFrequency} times in second column");
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
        
        // Verify column structure - this part remains the same as order matters for columns
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("b.Value", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("c.Value", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
        
        Assert.AreEqual(16, table.Count);
        
        var expectedPairs = new List<(string Number, string Word)>();
        
        foreach (var number in new[] { "1", "2" })
        {
            foreach (var word in words)
            {
                expectedPairs.Add((number, word));
            }
        }
        
        var actualPairs = table
            .Select(row => (Number: (string)row[0], Word: (string)row[1]))
            .ToList();
        
        foreach (var expected in expectedPairs)
        {
            Assert.IsTrue(
                actualPairs.Any(actual => 
                    actual.Number == expected.Number && 
                    actual.Word == expected.Word),
                $"Missing combination: Number={expected.Number}, Word={expected.Word}"
            );
        }
        
        foreach (var number in new[] { "1", "2" })
        {
            Assert.AreEqual(
                words.Length,
                actualPairs.Count(p => p.Number == number),
                $"Number {number} should appear exactly {words.Length} times"
            );
        }
        
        foreach (var word in words)
        {
            Assert.AreEqual(
                2,
                actualPairs.Count(p => p.Word == word),
                $"Word '{word}' should appear exactly 2 times"
            );
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
        
        var expectedTotalCount = words.Sum(word => word.Length);
        Assert.AreEqual(expectedTotalCount, table.Count, 
            "Total count should match sum of all word lengths");
        
        var expectedPairs = words
            .SelectMany(word => word.Select(ch => (Word: word, Character: ch)))
            .ToList();
        
        var actualPairs = table
            .Select(row => (Word: (string)row[0], Character: (char)row[1]))
            .ToList();
        
        foreach (var expected in expectedPairs)
        {
            Assert.IsTrue(
                actualPairs.Any(actual =>
                    actual.Word == expected.Word &&
                    actual.Character == expected.Character),
                $"Missing combination: Word='{expected.Word}', Character='{expected.Character}'"
            );
        }
        
        foreach (var word in words)
        {
            var expectedFrequency = word.Length;
            var actualFrequency = actualPairs.Count(p => p.Word == word);
            Assert.AreEqual(expectedFrequency, actualFrequency,
                $"Word '{word}' should appear {expectedFrequency} times (once for each of its characters)");
        }
        
        foreach (var actual in actualPairs)
        {
            Assert.IsTrue(
                actual.Word.Contains(actual.Character),
                $"Character '{actual.Character}' should not be paired with word '{actual.Word}' as it's not part of that word"
            );
        }
    }
    
    [TestMethod]
    public void CrossApplyProperty_SkipAfterSplit_ShouldPass()
    {
        // The query splits text by spaces and skips the first word
        const string query = "select b.Value from #schema.first() a cross apply a.Skip(a.Split(a.Text, ' '), 1) as b";
        
        // Our input text contains words that will be split
        var inputText = "Lorem ipsum dolor sit amet, consectetur adipiscing elit.";
        var firstSource = new List<CrossApplyClass2>
        {
            new() { Text = inputText }
        }.ToArray();
        
        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );
        
        var table = vm.Run();
        
        // Verify column structure - this remains ordered as SQL column order matters
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("b.Value", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        
        // Create our expected result set: all words except the first one
        var expectedWords = inputText.Split(' ')
            .Skip(1)  // Skip first word to match our query's behavior
            .ToList();
        
        // Verify the total count matches what we expect
        Assert.AreEqual(expectedWords.Count, table.Count,
            "Result should contain all words except the first one");
        
        // Convert actual results to a comparable format
        var actualWords = table
            .Select(row => (string)row[0])
            .ToList();
        
        // Verify each expected word appears exactly once
        foreach (var expectedWord in expectedWords)
        {
            Assert.AreEqual(
                1,
                actualWords.Count(word => word == expectedWord),
                $"Word '{expectedWord}' should appear exactly once in the results"
            );
        }
        
        // Verify no unexpected words appear
        foreach (var actualWord in actualWords)
        {
            Assert.IsTrue(
                expectedWords.Contains(actualWord),
                $"Found unexpected word '{actualWord}' in results"
            );
        }
        
        // Additional verification: make sure first word is not present
        var firstWord = inputText.Split(' ')[0];
        Assert.IsFalse(
            actualWords.Contains(firstWord),
            $"First word '{firstWord}' should not appear in results due to Skip(1)"
        );
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
        
        Assert.IsTrue(table.Count == 6, "Table should contain 6 rows");

        var expectedWords = new[] { "ipsum", "dolor", "sit", "amet,", "consectetur", "adipiscing" };
        Assert.IsTrue(expectedWords.All(word => 
                table.Any(row => (string)row[0] == word)),
            "Not all expected words found in table");
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

        var actualWords = table
            .Select(row => (string)row[0])
            .ToList();

        var expectedWords = new[] { "consectetur", "adipiscing" };
        foreach (var expectedWord in expectedWords)
        {
            Assert.AreEqual(
                1, 
                actualWords.Count(word => word == expectedWord),
                $"Word '{expectedWord}' should appear exactly once in the results"
            );
        }
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

        Assert.IsTrue(table.Any(row => 
            (int)row[0] == 5 && 
            (int)row[1] == 5));

        Assert.IsTrue(table.Any(row => 
            (int)row[0] == 3 && 
            (int)row[1] == 1));

        Assert.IsTrue(table.Any(row => 
            (int)row[0] == 11 && 
            (int)row[1] == 1));

        Assert.IsTrue(table.Any(row => 
            (int)row[0] == 10 && 
            (int)row[1] == 1));
    }
}
// ReSharper disable UnusedAutoPropertyAccessor.Local
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class CrossApplyMethodCallTests : GenericEntityTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void CrossApplyProperty_NoMatch_ShouldPass()
    {
        const string query = "select b.Value from #schema.first() a cross apply a.Split(a.Value2, ' ') as b";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { Value1 = 1, Value2 = string.Empty }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("b.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table);
    }

    [TestMethod]
    public void CrossApplyProperty_SplitStringToWords_ShouldPass()
    {
        const string query = "select b.Value from #schema.first() a cross apply a.Split(a.Text, ' ') as b";

        var firstSource = new List<CrossApplyClass2>
        {
            new() { Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit." }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("b.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Lorem"], ["ipsum"], ["dolor"], ["sit"],
            ["amet,"], ["consectetur"], ["adipiscing"], ["elit."]);
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
            new() { Text = string.Join(" ", words) }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table, ("b.Value", typeof(string)), ("c.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            words.SelectMany(first => words.Select(second => new object?[] { first, second })).ToArray());
    }

    [TestMethod]
    public void CrossApplyProperty_SplitWithMultipleProperties_ShouldPass()
    {
        const string query =
            "select b.Value, c.Value from #schema.first() a cross apply a.Split(a.Numbers, ',') as b cross apply a.Split(a.Words, ' ') as c";

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

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table, ("b.Value", typeof(string)), ("c.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            new[] { "1", "2" }.SelectMany(number => words.Select(word => new object?[] { number, word })).ToArray());
    }

    [TestMethod]
    public void CrossApplyProperty_SplitWithMultipleProperties_ShouldPass2()
    {
        const string query =
            "select b.Value, c.Value from #schema.first() a cross apply a.Split(a.Words, ' ') as b cross apply b.ToCharArray(b.Value) as c";

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

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table, ("b.Value", typeof(string)), ("c.Value", typeof(char)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            words.SelectMany(word => word.Select(character => new object?[] { word, character })).ToArray());
    }

    [TestMethod]
    public void CrossApplyProperty_SkipAfterSplit_ShouldPass()
    {
        const string query = "select b.Value from #schema.first() a cross apply a.Skip(a.Split(a.Text, ' '), 1) as b";

        var inputText = "Lorem ipsum dolor sit amet, consectetur adipiscing elit.";
        var firstSource = new List<CrossApplyClass2>
        {
            new() { Text = inputText }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        var expectedWords = inputText.Split(' ')
            .Skip(1)
            .ToList();
        TableMaterializationTestHelper.AssertColumns(table, ("b.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table, expectedWords.Select(word => new object?[] { word }).ToArray());
    }

    [TestMethod]
    public void CrossApplyProperty_TakeSkipAfterSplit_ShouldPass()
    {
        const string query =
            "select b.Value from #schema.first() a cross apply a.Take(a.Skip(a.Split(a.Text, ' '), 1), 6) as b";

        var firstSource = new List<CrossApplyClass2>
        {
            new() { Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit." }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        var expectedWords = new[] { "ipsum", "dolor", "sit", "amet,", "consectetur", "adipiscing" };
        TableMaterializationTestHelper.AssertColumns(table, ("b.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table, expectedWords.Select(word => new object?[] { word }).ToArray());
    }

    [TestMethod]
    public void CrossApplyProperty_WhereCondition_ShouldPass()
    {
        const string query =
            "select b.Value from #schema.first() a cross apply a.Split(a.Text, ' ') as b where b.Value.Length > 5";

        var firstSource = new List<CrossApplyClass2>
        {
            new() { Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit." }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        var expectedWords = new[] { "consectetur", "adipiscing" };
        TableMaterializationTestHelper.AssertColumns(table, ("b.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table, expectedWords.Select(word => new object?[] { word }).ToArray());
    }

    [TestMethod]
    public void CrossApplyProperty_GroupBy_ShouldPass()
    {
        const string query =
            "select b.Length(b.Value), b.Count(Length(b.Value)) from #schema.first() a cross apply a.Split(a.Text, ' ') as b group by b.Length(b.Value)";

        var firstSource = new List<CrossApplyClass2>
        {
            new() { Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit." }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("b.Length(b.Value)", typeof(int?)),
            ("b.Count(Length(b.Value))", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            [5, 5L], [3, 1L], [11, 1L], [10, 1L]);
    }

    private sealed class CrossApplyClass1
    {
        public int Value1 { get; set; }

        public string Value2 { get; set; } = string.Empty;
    }

    private sealed class CrossApplyClass2
    {
        public string Text { get; set; } = string.Empty;
    }

    private sealed class CrossApplyClass3
    {
        public string Numbers { get; set; } = string.Empty;

        public string Words { get; set; } = string.Empty;
    }
}

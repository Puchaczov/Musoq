// ReSharper disable UnusedAutoPropertyAccessor.Local
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class OuterApplyMethodCallTests : GenericEntityTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void OuterApplyProperty_NoMatch_ShouldPass()
    {
        const string query = "select b.Value from #schema.first() a outer apply a.Split(a.Value2, ' ') as b";

        var firstSource = new List<OuterApplyClass1>
        {
            new() { Value1 = 1, Value2 = string.Empty }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("b.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, new object?[] { null });
    }

    [TestMethod]
    public void OuterApplyProperty_SplitStringToWords_ShouldPass()
    {
        const string query = "select b.Value from #schema.first() a outer apply a.Split(a.Text, ' ') as b";

        var firstSource = new List<OuterApplyClass2>
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
    public void OuterApplyProperty_SkipAfterSplit_ShouldPass()
    {
        const string query = "select b.Value from #schema.first() a outer apply a.Skip(a.Split(a.Text, ' '), 1) as b";

        var firstSource = new List<OuterApplyClass2>
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
            ["ipsum"], ["dolor"], ["sit"], ["amet,"],
            ["consectetur"], ["adipiscing"], ["elit."]);
    }

    [TestMethod]
    public void OuterApplyProperty_TakeSkipAfterSplit_ShouldPass()
    {
        const string query =
            "select b.Value from #schema.first() a outer apply a.Take(a.Skip(a.Split(a.Text, ' '), 1), 6) as b";

        var firstSource = new List<OuterApplyClass2>
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
            ["ipsum"], ["dolor"], ["sit"], ["amet,"], ["consectetur"], ["adipiscing"]);
    }

    [TestMethod]
    public void OuterApplyProperty_WhereCondition_ShouldPass()
    {
        const string query =
            "select b.Value from #schema.first() a outer apply a.Split(a.Text, ' ') as b where b.Value.Length > 5";

        var firstSource = new List<OuterApplyClass2>
        {
            new() { Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit." }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("b.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["consectetur"], ["adipiscing"]);
    }

    [TestMethod]
    public void OuterApplyProperty_GroupBy_ShouldPass()
    {
        const string query =
            "select b.Length(b.Value), b.Count(b.Length(b.Value)) from #schema.first() a outer apply a.Split(a.Text, ' ') as b group by b.Length(b.Value)";

        var firstSource = new List<OuterApplyClass2>
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
            ("b.Count(b.Length(b.Value))", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            [5, 5L], [3, 1L], [11, 1L], [10, 1L]);
    }

    [TestMethod]
    public void OuterApplyProperty_MultipleSplitWords_ShouldPass()
    {
        const string query =
            "select b.Value, c.Value from #schema.first() a outer apply a.Split(a.Text, ' ') as b outer apply a.Split(a.Text, ' ') as c";

        string[] words = ["Lorem", "ipsum", "dolor", "sit", "amet,", "consectetur", "adipiscing", "elit."];

        var firstSource = new List<OuterApplyClass2>
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

    private sealed class OuterApplyClass1
    {
        public int Value1 { get; set; }

        public string Value2 { get; set; } = string.Empty;
    }

    private sealed class OuterApplyClass2
    {
        public string Text { get; set; } = string.Empty;
    }
}

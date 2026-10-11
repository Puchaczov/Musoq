using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionProviderQueryTests
{
    [TestMethod]
    public void TypedTextSwitch_WhenBranchesHaveDifferentFields_ShouldPreserveNativeStringsAndMissingValues()
    {
        const string query = "text Comment { _: literal '#', Text: rest }; text KeyValue { Key: until '=', Value: rest }; text ConfigLine { Content: switch { pattern '#' => Comment, _ => KeyValue } }; select s.Position as Position, c.Content.Key as Key, c.Content.Text as Text, c.Content.Value as Value from #schema.first() s cross apply Parse<ConfigLine>(s.Line) c order by s.Position";
        SwitchLine[] input = [new(2, "x=42"), new(1, "#hello")];
        using var compiled = CreateAndRunVirtualMachine<SwitchLine, LibraryBase>(query, input);
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)), ("Key", typeof(string)), ("Text", typeof(string)), ("Value", typeof(string)));
        Assert.HasCount(2, rows);
        CollectionAssert.AreEqual(new[] { 1, 2 }, rows.Select(row => row.GetValue<int>(0)).ToArray());
        CollectionAssert.AreEqual(new string?[] { null, "x" }, rows.Select(row => row.GetValue<string?>(1)).ToArray());
        CollectionAssert.AreEqual(new string?[] { "hello", null }, rows.Select(row => row.GetValue<string?>(2)).ToArray());
        CollectionAssert.AreEqual(new string?[] { null, "42" }, rows.Select(row => row.GetValue<string?>(3)).ToArray());
    }

    public sealed record SwitchLine(int Position, string Line);
}

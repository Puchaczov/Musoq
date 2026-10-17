using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class LibraryExecutionAsOfQueryTests : GenericEntityTestBase
{
    public const string CompositeQuery = "select a.Position as LeftPosition, b.Position as RightPosition " +
        "from #schema.first() a asof left join #schema.first() b " +
        "on a.Partition = b.Partition and a.Label = b.Label and a.Key >= b.Key " +
        "tie break by b.Position desc order by LeftPosition";

    [TestMethod]
    public void CompositeAsOfKeys_WhenNullableAndOrdinal_ShouldPreserveExpectedMatches()
    {
        using var compiled = CreateAndRunVirtualMachine<AsOfInput>(CompositeQuery, CreateRows());
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("LeftPosition", typeof(int)), ("RightPosition", typeof(int?)));
        int?[] expected = [1, 2, null, 4, 5, 6, null];
        Assert.HasCount(expected.Length, rows);
        for (var index = 0; index < rows.Length; index++)
        {
            Assert.AreEqual(index + 1, rows[index].GetValue<int>(0));
            Assert.AreEqual(expected[index], rows[index].GetValue<int?>(1));
        }
    }

    public static AsOfInput[] CreateRows() =>
        [new(1, 10, "A", 10m), new(2, 10, "A", 5m), new(3, null, "A", 7m),
         new(4, 10, "B", 8m), new(5, 10, "a", 10m), new(6, 10, "A", 4m), new(7, 10, "A", null)];
    public sealed record AsOfInput(int Position, int? Partition, string Label, decimal? Key);
}

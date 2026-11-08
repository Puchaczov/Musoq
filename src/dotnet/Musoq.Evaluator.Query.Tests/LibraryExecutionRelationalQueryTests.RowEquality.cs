using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class LibraryExecutionRowEqualityQueryTests : GenericEntityTestBase
{
    [TestMethod]
    public void NativeResultRows_WhenCompared_ShouldPreserveNullAndNumericEquality()
    {
        const string query = "select Key, Amount from #schema.first() order by Position";
        RowEqualityInput[] input = [new(1, "same", 42m), new(2, "same", 42m), new(3, "same", null), new(4, "same", null)];
        using var compiled = CreateAndRunVirtualMachine<RowEqualityInput, LibraryBase>(query, input);
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Key", typeof(string)), ("Amount", typeof(decimal?)));
        Assert.HasCount(4, rows);
        Assert.AreEqual(42m, rows[0].GetValue<decimal?>(1));
        Assert.IsNull(rows[2].GetValue<decimal?>(1));
        Assert.IsTrue(rows[0].Equals(rows[1]));
        Assert.IsTrue(rows[2].Equals(rows[3]));
        Assert.IsFalse(rows[0].Equals(rows[2]));
        Assert.AreEqual(rows[0].GetHashCode(), rows[1].GetHashCode());
        Assert.AreEqual(rows[2].GetHashCode(), rows[3].GetHashCode());
    }

    [TestMethod]
    public void NumericResultRowEqualityAndHashing_ShouldMatchEquivalentValues()
    {
        const string query = "select Amount from #schema.first() order by Position";
        using var compiled = CreateAndRunVirtualMachine<RowEqualityInput, LibraryBase>(query,
            [new(1, "same", 42m), new(2, "same", 42m)]);
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Amount", typeof(decimal?)));
        Assert.HasCount(2, rows);
        Assert.AreEqual(42m, rows[0].GetValue<decimal?>(0));
        Assert.IsTrue(rows[0].Equals(rows[1]));
        Assert.AreEqual(rows[0].GetHashCode(), rows[1].GetHashCode());
    }

    public sealed record RowEqualityInput(int Position, string Key, decimal? Amount);
}

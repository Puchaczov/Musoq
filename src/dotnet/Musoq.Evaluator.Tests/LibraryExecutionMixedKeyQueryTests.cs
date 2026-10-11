using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class LibraryExecutionMixedKeyQueryTests : GenericEntityTestBase
{
    public const string MixedKeyQuery = "select a.Position as LeftPosition, b.Position as RightPosition " +
        "from #schema.first() a inner join #schema.first() b " +
        "on a.IntKey = b.LongKey and a.Label = b.Label order by LeftPosition, RightPosition";

    [TestMethod]
    public void CompositeHashKeys_WhenIntegralWidthsDiffer_ShouldPreserveNumericEquality()
        => AssertMatches(MixedKeyQuery);

    [TestMethod]
    public void CompositeHashKeys_WhenMaterializedInACte_ShouldPreserveNumericEquality()
        => AssertMatches("with Data as (select Position, IntKey, LongKey, Label from #schema.first()) " +
            "select a.Position as LeftPosition, b.Position as RightPosition from Data a inner join Data b " +
            "on a.IntKey = b.LongKey and a.Label = b.Label order by LeftPosition, RightPosition");

    [TestMethod]
    public void CompositeHashKeys_WhenIntegralSignsDiffer_ShouldKeepNegativeAndLargeKeysDistinct()
    {
        const string query = "select a.Position as LeftPosition, b.Position as RightPosition " +
            "from #schema.first() a inner join #schema.first() b " +
            "on a.SignedKey = b.UnsignedKey and a.Label = b.Label order by LeftPosition";
        using var compiled = CreateAndRunVirtualMachine<SignedInput>(query,
            [new(1, -1, ulong.MaxValue, "A"), new(2, 42, 42, "A")]);
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("LeftPosition", typeof(int)), ("RightPosition", typeof(int)));
        var row = table.Single();
        Assert.AreEqual(2, row.GetValue<int>(0));
        Assert.AreEqual(2, row.GetValue<int>(1));
    }

    private void AssertMatches(string query)
    {
        using var compiled = CreateAndRunVirtualMachine<MixedInput>(query, CreateRows());
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("LeftPosition", typeof(int)), ("RightPosition", typeof(int)));
        (int Left, int Right)[] expected = [(1, 1), (1, 2), (2, 1), (2, 2), (4, 4), (5, 5)];
        Assert.HasCount(expected.Length, rows);
        for (var index = 0; index < rows.Length; index++)
        {
            Assert.AreEqual(expected[index].Left, rows[index].GetValue<int>(0));
            Assert.AreEqual(expected[index].Right, rows[index].GetValue<int>(1));
        }
    }

    public static MixedInput[] CreateRows() =>
        [new(1, 10, 10, "A"), new(2, 10, 10, "A"), new(3, null, null, "A"),
         new(4, -7, -7, "B"), new(5, 10, 10, "a"), new(6, 20, 30, "A")];
    public sealed record MixedInput(int Position, int? IntKey, long? LongKey, string Label);
    public sealed record SignedInput(int Position, long SignedKey, ulong UnsignedKey, string Label);
}

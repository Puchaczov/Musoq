using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionProviderQueryTests
{
    [TestMethod]
    [DataRow("Value * 2 > 100", 100L)]
    [DataRow("Value % 3 = 1", 7L)]
    public void NullableArithmetic_WhenUsedInAPredicate_ShouldExcludeNullWithoutUnwrapping(string predicate, long value)
    {
        var query = $"select Position, Value from #schema.first() where {predicate}";
        NullableArithmeticInput[] input = [new(1, null), new(2, value)];
        using var compiled = CreateAndRunVirtualMachine<NullableArithmeticInput, LibraryBase>(query, input);
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)), ("Value", typeof(long?)));
        Assert.HasCount(1, rows);
        Assert.AreEqual(2, rows[0].GetValue<int>(0));
        Assert.AreEqual(value, rows[0].GetValue<long?>(1));
    }

    [TestMethod]
    public void NullableCase_WhenOneBranchReadsANullValue_ShouldKeepItsNullableResult()
    {
        const string query = "select Position, case when Position = 1 then 35 else Value end as Result " +
            "from #schema.first() order by Position";
        NullableArithmeticInput[] input = [new(1, null), new(2, null), new(3, 42)];
        using var compiled = CreateAndRunVirtualMachine<NullableArithmeticInput, LibraryBase>(query, input);
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)), ("Result", typeof(long?)));
        Assert.HasCount(3, rows);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, rows.Select(row => row.GetValue<int>(0)).ToArray());
        CollectionAssert.AreEqual(new long?[] { 35, null, 42 }, rows.Select(row => row.GetValue<long?>(1)).ToArray());
    }

    [TestMethod]
    public void NullableConversions_WhenUsedInArithmetic_ShouldPreserveNullAndConcreteResultTypes()
    {
        const string query = "select Position, FromHex(Text) + 1 as Integral, ToFloat(Text) * 2 as Floating, " +
            "ToDouble(Text) + 1 as DoubleValue from #schema.first() order by Position";
        NullableConversionInput[] input = [new(1, "10"), new(2, null)];
        using var compiled = CreateAndRunVirtualMachine<NullableConversionInput, LibraryBase>(query, input);
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)), ("Integral", typeof(long?)),
            ("Floating", typeof(float?)), ("DoubleValue", typeof(double?)));
        Assert.HasCount(2, rows);
        CollectionAssert.AreEqual(new[] { 1, 2 }, rows.Select(row => row.GetValue<int>(0)).ToArray());
        CollectionAssert.AreEqual(new long?[] { 17, null }, rows.Select(row => row.GetValue<long?>(1)).ToArray());
        CollectionAssert.AreEqual(new float?[] { 20f, null }, rows.Select(row => row.GetValue<float?>(2)).ToArray());
        CollectionAssert.AreEqual(new double?[] { 11d, null }, rows.Select(row => row.GetValue<double?>(3)).ToArray());
    }

    public sealed record NullableConversionInput(int Position, string? Text);
    public sealed record NullableArithmeticInput(int Position, long? Value);
}

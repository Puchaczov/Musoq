using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class NullCoalescingOperatorTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("select case when true then 1 else MissingColumn end from #A.Entities()", DiagnosticCode.MQ3001_UnknownColumn)]
    [DataRow("select case when false then MissingMethod() else 1 end from #A.Entities()", DiagnosticCode.MQ3086_UnknownCallable)]
    public void UnreachableCaseResults_ShouldStillBind(string query, DiagnosticCode expected)
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, CreateSingleSource(new BasicEntity())));

        Assert.AreEqual(expected, exception.PrimaryEnvelope.Code);
    }

    [TestMethod]
    public void WhenLeftIsNonNullableValueType_ShouldRejectIncompatibleFallback()
    {
        const string query = "select Population ?? 'unused' from #A.Entities()";
        var sources = CreateSingleSource(new BasicEntity { Population = 42m });

        var exception = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));
        Assert.AreEqual(DiagnosticCode.MQ3005_TypeMismatch, exception.PrimaryEnvelope.Code);
    }

    [TestMethod]
    public void WhenLiteralLeftIsNonNullableValueType_ShouldRejectIncompatibleFallback()
    {
        const string query = "select 1 ?? 'unused' as Value from #A.Entities()";
        var sources = CreateSingleSource(new BasicEntity());

        var exception = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));
        Assert.AreEqual(DiagnosticCode.MQ3005_TypeMismatch, exception.PrimaryEnvelope.Code);
    }

    [TestMethod]
    public void WhenLeftColumnIsNonNullableValueType_ShouldRejectMissingFallbackColumn()
    {
        const string query = "select Population ?? MissingColumn as Value from #A.Entities()";
        var sources = CreateSingleSource(new BasicEntity { Population = 42m });

        var exception = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));
        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, exception.PrimaryEnvelope.Code);
    }

    [TestMethod]
    public void WhenLeftColumnIsNonNullableValueType_ShouldRejectMissingFallbackMethod()
    {
        const string query = "select Population ?? MissingMethod() as Value from #A.Entities()";
        var sources = CreateSingleSource(new BasicEntity { Population = 42m });

        var exception = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));
        Assert.AreEqual(DiagnosticCode.MQ3086_UnknownCallable, exception.PrimaryEnvelope.Code);
    }

    [TestMethod]
    [DataRow(null, 7L)]
    [DataRow(5, 5L)]
    public void MixedIntegralFallback_ShouldWidenBothValuesAndMetadata(int? value, long expected)
    {
        const string query = "select NullableValue ?? 7L as Value from #A.Entities()";
        using var compiled = CreateAndRunVirtualMachine(query, CreateSingleSource(new BasicEntity { NullableValue = value }));
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [expected]);
    }

    [TestMethod]
    public void NonNullableLeft_ShouldRetainTheCommonWiderType()
    {
        const string query = "select 1 ?? 2L as Value from #A.Entities()";
        using var compiled = CreateAndRunVirtualMachine(query, CreateSingleSource(new BasicEntity()));
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [1L]);
    }

    [TestMethod]
    public void MixedNullableFallbacks_ShouldPreserveNullAndWidenMetadata()
    {
        const string query = "select (NullableValue::long) ?? NullableValue as Value from #A.Entities()";
        using var compiled = CreateAndRunVirtualMachine(query, CreateSingleSource(new BasicEntity { NullableValue = null }));
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(long?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, new object?[] { null });
    }

    [TestMethod]
    public void PotentiallyLossyLongDoubleFallback_ShouldRequireAnExplicitConversion()
    {
        const string query = "select (NullableValue::long) ?? (1::double) as Value from #A.Entities()";
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, CreateSingleSource(new BasicEntity())));

        Assert.AreEqual(DiagnosticCode.MQ3005_TypeMismatch, exception.PrimaryEnvelope.Code);
    }

    [TestMethod]
    public void WhenLeftIsNullableValueTypeAndNull_ShouldUseFallback()
    {
        const string query = "select NullableValue ?? 0 from #A.Entities()";
        var sources = CreateSingleSource(new BasicEntity { NullableValue = null });

        var table = CreateAndRunVirtualMachine(query, sources).Run();

        Assert.AreEqual(0, table[0].ReadValue(0));
    }

    [TestMethod]
    public void WhenLeftIsNullableValueTypeAndPresent_ShouldKeepLeft()
    {
        const string query = "select NullableValue ?? 0 from #A.Entities()";
        var sources = CreateSingleSource(new BasicEntity { NullableValue = 5 });

        var table = CreateAndRunVirtualMachine(query, sources).Run();

        Assert.AreEqual(5, table[0].ReadValue(0));
    }

    [TestMethod]
    public void WhenLeftIsReferenceTypeAndNull_ShouldUseFallback()
    {
        const string query = "select Name ?? 'fallback' from #A.Entities()";
        var sources = CreateSingleSource(new BasicEntity { Name = null });

        var table = CreateAndRunVirtualMachine(query, sources).Run();

        Assert.AreEqual("fallback", table[0].ReadValue(0));
    }

    [TestMethod]
    public void WhenNullCoalescingChainsReferenceValues_ShouldEvaluateRightAssociatively()
    {
        const string query = "select Name ?? City ?? 'fallback' as Label from #A.Entities() order by Id";
        var sources = CreateSingleSource(
            new BasicEntity { Id = 1, Name = null, City = null },
            new BasicEntity { Id = 2, Name = null, City = "Paris" },
            new BasicEntity { Id = 3, Name = "Alice", City = "Warsaw" });

        var table = CreateAndRunVirtualMachine(query, sources).Run();

        TableMaterializationTestHelper.AssertColumns(table, ("Label", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["fallback"], ["Paris"], ["Alice"]);
    }

    [TestMethod]
    public void WhenLiteralNullIsLeft_ShouldUseRightExpression()
    {
        const string query = "select null ?? Name from #A.Entities()";
        var sources = CreateSingleSource(new BasicEntity { Name = "live" });

        var table = CreateAndRunVirtualMachine(query, sources).Run();

        Assert.AreEqual("live", table[0].ReadValue(0));
    }

    [TestMethod]
    public void WhenFallbackTypeIsIncompatibleAndLeftCanBeNull_ShouldFail()
    {
        const string query = "select NullableValue ?? 'fallback' from #A.Entities()";
        var sources = CreateSingleSource(new BasicEntity { NullableValue = null });

        var exception = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        StringAssert.Contains(exception.ToString(), "Operator ?? requires compatible fallback types");
    }
}

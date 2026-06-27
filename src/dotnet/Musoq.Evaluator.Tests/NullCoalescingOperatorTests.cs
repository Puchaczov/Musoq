using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class NullCoalescingOperatorTests : BasicEntityTestBase
{
    [TestMethod]
    public void WhenLeftIsNonNullableValueType_ShouldIgnoreFallback()
    {
        const string query = "select Population ?? 'unused' from #A.Entities()";
        var sources = CreateSingleSource(new BasicEntity { Population = 42m });

        var table = CreateAndRunVirtualMachine(query, sources).Run();

        Assert.AreEqual(42m, table[0][0]);
    }

    [TestMethod]
    public void WhenLiteralLeftIsNonNullableValueType_ShouldIgnoreIncompatibleFallback()
    {
        const string query = "select 1 ?? 'unused' as Value from #A.Entities()";
        var sources = CreateSingleSource(new BasicEntity());

        var table = CreateAndRunVirtualMachine(query, sources).Run();

        Assert.AreEqual(1, table[0][0]);
    }

    [TestMethod]
    public void WhenLeftColumnIsNonNullableValueType_ShouldNotBindMissingFallbackColumn()
    {
        const string query = "select Population ?? MissingColumn as Value from #A.Entities()";
        var sources = CreateSingleSource(new BasicEntity { Population = 42m });

        var table = CreateAndRunVirtualMachine(query, sources).Run();

        Assert.AreEqual(42m, table[0][0]);
    }

    [TestMethod]
    public void WhenLeftColumnIsNonNullableValueType_ShouldNotResolveMissingFallbackMethod()
    {
        const string query = "select Population ?? MissingMethod() as Value from #A.Entities()";
        var sources = CreateSingleSource(new BasicEntity { Population = 42m });

        var table = CreateAndRunVirtualMachine(query, sources).Run();

        Assert.AreEqual(42m, table[0][0]);
    }

    [TestMethod]
    public void WhenLeftIsNullableValueTypeAndNull_ShouldUseFallback()
    {
        const string query = "select NullableValue ?? 0 from #A.Entities()";
        var sources = CreateSingleSource(new BasicEntity { NullableValue = null });

        var table = CreateAndRunVirtualMachine(query, sources).Run();

        Assert.AreEqual(0, table[0][0]);
    }

    [TestMethod]
    public void WhenLeftIsNullableValueTypeAndPresent_ShouldKeepLeft()
    {
        const string query = "select NullableValue ?? 0 from #A.Entities()";
        var sources = CreateSingleSource(new BasicEntity { NullableValue = 5 });

        var table = CreateAndRunVirtualMachine(query, sources).Run();

        Assert.AreEqual(5, table[0][0]);
    }

    [TestMethod]
    public void WhenLeftIsReferenceTypeAndNull_ShouldUseFallback()
    {
        const string query = "select Name ?? 'fallback' from #A.Entities()";
        var sources = CreateSingleSource(new BasicEntity { Name = null });

        var table = CreateAndRunVirtualMachine(query, sources).Run();

        Assert.AreEqual("fallback", table[0][0]);
    }

    [TestMethod]
    public void WhenLiteralNullIsLeft_ShouldUseRightExpression()
    {
        const string query = "select null ?? Name from #A.Entities()";
        var sources = CreateSingleSource(new BasicEntity { Name = "live" });

        var table = CreateAndRunVirtualMachine(query, sources).Run();

        Assert.AreEqual("live", table[0][0]);
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
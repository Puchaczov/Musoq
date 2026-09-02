using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Tests.Schema.Generated;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ScalarReuseEndToEndTests
{
    private const string StableProjection =
        "select a.Value, b.Value, c.Value from #licm.outers() a cross apply a.Middles b cross apply b.Leaves c";

    [TestMethod]
    public void StableNestedApply_UsesOwningRowCountsWhenLicmIsEnabled()
    {
        LoopInvariantSampleCounters.Reset();

        using var query = Compile(StableProjection, new CompilationOptions());
        using var table = query.Run();

        Assert.AreEqual(24, table.Count);
        Assert.AreEqual(2, LoopInvariantSampleCounters.OuterStableValueReads);
        Assert.AreEqual(6, LoopInvariantSampleCounters.MiddleStableValueReads);
        Assert.AreEqual(24, LoopInvariantSampleCounters.LeafStableValueReads);
    }

    [TestMethod]
    public void StableNestedApply_RecomputesAtTheLeafWhenLicmIsDisabled()
    {
        LoopInvariantSampleCounters.Reset();

        using var query = Compile(StableProjection, new CompilationOptions().WithLoopInvariantCodeMotion(false));
        using var table = query.Run();

        Assert.AreEqual(24, table.Count);
        Assert.AreEqual(6, LoopInvariantSampleCounters.OuterStableValueReads);
        Assert.AreEqual(6, LoopInvariantSampleCounters.MiddleStableValueReads);
        Assert.AreEqual(24, LoopInvariantSampleCounters.LeafStableValueReads);
    }

    [TestMethod]
    public void VolatileOuterReference_IsNeverHoistedOrCseCollapsed()
    {
        LoopInvariantSampleCounters.Reset();

        const string queryText =
            "select a.VolatileValue, a.VolatileValue, a.VolatileOf(c.Value) as first, a.VolatileOf(c.Value) as second from #licm.outers() a cross apply a.Middles b cross apply b.Leaves c";
        using var query = Compile(queryText, new CompilationOptions());
        using var table = query.Run();

        Assert.AreEqual(24, table.Count);
        Assert.AreEqual(6, LoopInvariantSampleCounters.OuterVolatileValueReads);
        Assert.AreEqual(48, LoopInvariantSampleCounters.VolatileOfCalls);
    }

    [TestMethod]
    public void StableAndVolatileFunctions_PreserveOwnerAndLeafCounts()
    {
        LoopInvariantSampleCounters.Reset();

        var sample = GeneratedCodeSamplesCatalog.GetByFileName("Q250_LoopInvariantStableAndVolatileFunctions.cs");
        using var query = Compile(sample.Query, sample.CompilationOptions);
        using var table = query.Run();

        Assert.AreEqual(6, table.Count);
        Assert.AreEqual(2, LoopInvariantSampleCounters.StableOfCalls);
        Assert.AreEqual(6, LoopInvariantSampleCounters.StablePairCalls);
        Assert.AreEqual(6, LoopInvariantSampleCounters.VolatileOfCalls);
    }

    [TestMethod]
    public void EmptyApply_EagerlyReadsStableOuterButDoesNotReadVolatileLeaf()
    {
        LoopInvariantSampleCounters.Reset();

        var sample = GeneratedCodeSamplesCatalog.GetByFileName("Q251_LoopInvariantEmptyApply.cs");
        using var query = Compile(sample.Query, sample.CompilationOptions);
        using var table = query.Run();

        Assert.AreEqual(0, table.Count);
        Assert.AreEqual(2, LoopInvariantSampleCounters.OuterStableValueReads);
        Assert.AreEqual(0, LoopInvariantSampleCounters.MiddleVolatileValueReads);
    }

    [TestMethod]
    public void GeneratedHotPath_HasDirectLocalsAndNoLazyReuseBranch()
    {
        var generated = InstanceCreator.CompileForInspection(
            StableProjection,
            "ScalarReuseShape",
            new LoopInvariantSampleSchemaProviderAccessor(),
            new Components.TestsLoggerResolver(),
            new CompilationOptions()).GeneratedCSharpCode;

        StringAssert.Contains(generated, "aValue");
        StringAssert.Contains(generated, "bValue");
        StringAssert.Contains(generated, "c.Value");
        Assert.IsFalse(generated.Contains("initialized", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(generated.Contains("cache", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(generated.Contains("??=", StringComparison.Ordinal));
    }

    private static CompiledQuery Compile(string query, CompilationOptions options) =>
        InstanceCreator.CompileForExecution(
            query,
            "ScalarReuse_" + Guid.NewGuid().ToString("N"),
            new LoopInvariantSampleSchemaProviderAccessor(),
            new Components.TestsLoggerResolver(),
            options);

    private sealed class LoopInvariantSampleSchemaProviderAccessor :
        ISchemaProvider
    {
        private readonly ISchemaProvider _inner = CreateProvider();

        public ISchema GetSchema(string schema) => _inner.GetSchema(schema);

        private static ISchemaProvider CreateProvider() =>
            (ISchemaProvider)typeof(GeneratedCodeSamplesCatalog)
                .GetMethod("CreateLoopInvariantSampleSchemaProvider", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(null, null)!;
    }
}

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Cache;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Tests.Common;

namespace Musoq.Converter.Tests;

[TestClass]
public class CompiledQueryCacheTests
{
    static CompiledQueryCacheTests()
    {
        Culture.ApplyWithDefaultCulture();
    }

    [TestMethod]
    public void SameQueryCompiledTwice_SecondCompilationShouldNotAddNewCacheEntry()
    {
        var query = "select 1 from #system.dual()";

        // Use CreateForAnalyze to get the BuildItems and inspect the compilation
        var items1 = InstanceCreator.CreateForAnalyze(
            query, Guid.NewGuid().ToString(),
            new SystemSchemaProvider(), new TestsLoggerResolver());

        var items2 = InstanceCreator.CreateForAnalyze(
            query, Guid.NewGuid().ToString(),
            new SystemSchemaProvider(), new TestsLoggerResolver());

        var normalizedText1 = CompiledQueryCache.GetNormalizedText(items1.Compilation);
        var normalizedText2 = CompiledQueryCache.GetNormalizedText(items2.Compilation);

        Assert.AreEqual(normalizedText1, normalizedText2,
            $"Normalized text should match for same query.\n" +
            $"Text1 length: {normalizedText1.Length}, Text2 length: {normalizedText2.Length}\n" +
            $"First difference at index: {FindFirstDifference(normalizedText1, normalizedText2)}");
    }

    private static int FindFirstDifference(string a, string b)
    {
        var minLen = Math.Min(a.Length, b.Length);
        for (var i = 0; i < minLen; i++)
            if (a[i] != b[i])
                return i;
        return minLen < Math.Max(a.Length, b.Length) ? minLen : -1;
    }

    [TestMethod]
    public void DifferentQueries_ShouldProduceDifferentCacheEntries()
    {
        var query1 = "select 100 from #system.dual()";
        var query2 = "select 200 from #system.dual()";

        var countBefore = CompiledQueryCache.Count;

        InstanceCreator.CompileForStore(
            query1, Guid.NewGuid().ToString(),
            new SystemSchemaProvider(), new TestsLoggerResolver());

        InstanceCreator.CompileForStore(
            query2, Guid.NewGuid().ToString(),
            new SystemSchemaProvider(), new TestsLoggerResolver());

        var countAfter = CompiledQueryCache.Count;

        Assert.IsTrue(countAfter - countBefore >= 2,
            "Two different queries should produce at least two distinct cache entries");
    }

    [TestMethod]
    public void CachedCompilation_ShouldProduceWorkingCompiledQuery()
    {
        var query = "select 1 from #system.dual()";

        // First compilation — cold
        var cold = InstanceCreator.CompileForExecution(
            query, Guid.NewGuid().ToString(),
            new SystemSchemaProvider(), new TestsLoggerResolver());
        var coldResult = cold.Run();

        // Second compilation — should hit cache
        var warm = InstanceCreator.CompileForExecution(
            query, Guid.NewGuid().ToString(),
            new SystemSchemaProvider(), new TestsLoggerResolver());
        var warmResult = warm.Run();

        Assert.AreEqual(1, coldResult.Count);
        Assert.AreEqual(1, warmResult.Count);
        Assert.AreEqual(coldResult[0].Values[0], warmResult[0].Values[0]);
    }

    [TestMethod]
    public void ClearCache_ShouldResetCountToZero()
    {
        var query = "select 1 from #system.dual()";

        InstanceCreator.CompileForStore(
            query, Guid.NewGuid().ToString(),
            new SystemSchemaProvider(), new TestsLoggerResolver());

        Assert.IsTrue(CompiledQueryCache.Count > 0, "Cache should have entries after compilation");

        CompiledQueryCache.Clear();

        Assert.AreEqual(0, CompiledQueryCache.Count, "Cache should be empty after Clear()");
    }

    [TestMethod]
    public void CachedCompilation_WithArithmeticQuery_ShouldWork()
    {
        var query = "select 1 + 2 * 3 from #system.dual()";

        // Cold
        var cold = InstanceCreator.CompileForExecution(
            query, Guid.NewGuid().ToString(),
            new SystemSchemaProvider(), new TestsLoggerResolver());
        var coldResult = cold.Run();

        // Warm (cache hit)
        var warm = InstanceCreator.CompileForExecution(
            query, Guid.NewGuid().ToString(),
            new SystemSchemaProvider(), new TestsLoggerResolver());
        var warmResult = warm.Run();

        Assert.AreEqual(1, coldResult.Count);
        Assert.AreEqual(1, warmResult.Count);
        Assert.AreEqual(7, coldResult[0].Values[0]);
        Assert.AreEqual(7, warmResult[0].Values[0]);
    }
}

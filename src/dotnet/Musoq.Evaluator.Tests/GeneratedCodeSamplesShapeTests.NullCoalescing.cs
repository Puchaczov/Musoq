using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void NullCoalescingNonNullableValueType_WhenCompiledForInspection_ShouldPruneFallback()
    {
        var result = CompileBasicQueryForInspection(
            "select Population ?? 'unused' as Value from #A.entities()");

        Assert.IsFalse(result.GeneratedCSharpCode.Contains("??", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("unused", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.ExecutionPlanText.Contains("unused", StringComparison.Ordinal), result.ExecutionPlanText);
    }

    [TestMethod]
    public void NullCoalescingLiteralNonNullableValueType_WhenCompiledForInspection_ShouldPruneFallback()
    {
        var result = CompileBasicQueryForInspection(
            "select 1 ?? 'unused' as Value from #A.entities()");

        Assert.IsFalse(result.GeneratedCSharpCode.Contains("??", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("unused", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.ExecutionPlanText.Contains("unused", StringComparison.Ordinal), result.ExecutionPlanText);
    }

    [TestMethod]
    public void NullCoalescingNonNullableColumn_WhenCompiledForInspection_ShouldPruneMissingFallbackColumn()
    {
        var result = CompileBasicQueryForInspection(
            "select Population ?? MissingColumn as Value from #A.entities()");

        Assert.IsFalse(result.GeneratedCSharpCode.Contains("??", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("MissingColumn", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.ExecutionPlanText.Contains("MissingColumn", StringComparison.Ordinal), result.ExecutionPlanText);
    }

    [TestMethod]
    public void NullCoalescingNullableValueType_WhenCompiledForInspection_ShouldRenderOperator()
    {
        var result = CompileBasicQueryForInspection(
            "select NullableValue ?? 0 as Value from #A.entities()");

        Assert.Contains("??", result.GeneratedCSharpCode);
        AssertDoesNotUsePluginCoalesce(result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void NullCoalescingReferenceType_WhenCompiledForInspection_ShouldRenderOperator()
    {
        var result = CompileBasicQueryForInspection(
            "select Name ?? 'fallback' as Value from #A.entities()");

        Assert.Contains("??", result.GeneratedCSharpCode);
        Assert.Contains("fallback", result.GeneratedCSharpCode);
        AssertDoesNotUsePluginCoalesce(result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void NullCoalescingLiteralNullLeft_WhenCompiledForInspection_ShouldPruneCoalesce()
    {
        var result = CompileBasicQueryForInspection(
            "select null ?? Name as Value from #A.entities()");

        Assert.IsFalse(result.GeneratedCSharpCode.Contains("??", StringComparison.Ordinal), result.GeneratedCSharpCode);
        AssertDoesNotUsePluginCoalesce(result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void NonCoalescingSample_WhenLocalSnapshotExists_ShouldMatchCatalogOutput()
    {
        var sample = GeneratedCodeSamplesCatalog.GetByFileName("Q01_SimpleSelectWhere.cs");
        var samplePath = GeneratedCodeSampleArtifacts.GetSamplePath(sample);
        if (!File.Exists(samplePath))
            return;

        var expected = File.ReadAllText(samplePath);
        var actual = GeneratedCodeSampleArtifacts.Generate(sample, new TestsLoggerResolver());

        Assert.AreEqual(
            GeneratedCodeSampleArtifacts.NormalizeForComparison(expected),
            GeneratedCodeSampleArtifacts.NormalizeForComparison(actual));
    }

    private static void AssertDoesNotUsePluginCoalesce(string generatedCode)
    {
        Assert.IsFalse(generatedCode.Contains(".Coalesce", StringComparison.Ordinal), generatedCode);
        Assert.IsFalse(generatedCode.Contains("new Musoq.Plugins.LibraryBase().Coalesce", StringComparison.Ordinal), generatedCode);
    }
}

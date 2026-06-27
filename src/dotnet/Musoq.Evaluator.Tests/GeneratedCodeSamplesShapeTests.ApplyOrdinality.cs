using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void ApplyWithOrdinalitySample_WhenCompiledForInspection_ShouldUseIndexedLoopWithoutLinqProjection()
    {
        var result = CompileSampleForInspection(ApplyWithOrdinalitySampleFileName);

        AssertUsesExecutionBackendWithoutRetiredHelperPatterns(result);
        Assert.Contains("ForEachWithOrdinality [nOrdinal, n in", result.ExecutionPlanText);
        Assert.Contains("EvaluationHelper.ConvertScalarEnumerableToTypedChunks<int>", result.GeneratedCSharpCode);
        Assert.Contains("foreach (var nChunk in statement0_nRows)", result.GeneratedCSharpCode);
        Assert.Contains("for (int nIndex = 0, nIndexCount = nChunk.Count; nIndex < nIndexCount; ++nIndex)", result.GeneratedCSharpCode);
        Assert.Contains("++nOrdinal;", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".Select((", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("Select((n, nOrdinal)", StringComparison.Ordinal), result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void ApplyWithOrdinalitySample_WhenCompiledForExecution_ShouldRunExecutableQuery()
    {
        var table = CompileSampleForExecution(ApplyWithOrdinalitySampleFileName).Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("left", table[0][0]);
        Assert.AreEqual(1, table[0][1]);
        Assert.AreEqual(0, table[0][2]);
        Assert.AreEqual("left", table[1][0]);
        Assert.AreEqual(2, table[1][1]);
        Assert.AreEqual(1, table[1][2]);
        Assert.AreEqual("right", table[2][0]);
        Assert.AreEqual(3, table[2][1]);
        Assert.AreEqual(0, table[2][2]);
    }

    [TestMethod]
    public void ApplyWithOrdinalitySample_WhenCheckedIn_ShouldUseIndexedLoopWithoutLinqProjection()
    {
        var sample = ReadSamples().Single(static sample => sample.FileName == ApplyWithOrdinalitySampleFileName);

        Assert.Contains("EvaluationHelper.ConvertScalarEnumerableToTypedChunks<int>", sample.Content);
        Assert.Contains("foreach (var nChunk in statement0_nRows)", sample.Content);
        Assert.Contains("for (int nIndex = 0, nIndexCount = nChunk.Count; nIndex < nIndexCount; ++nIndex)", sample.Content);
        Assert.Contains("++nOrdinal;", sample.Content);
        Assert.IsFalse(sample.Content.Contains(".Select((", StringComparison.Ordinal), sample.Content);
        Assert.IsFalse(sample.Content.Contains("EvaluationHelper.SmartForEach(", StringComparison.Ordinal), sample.Content);
    }
}

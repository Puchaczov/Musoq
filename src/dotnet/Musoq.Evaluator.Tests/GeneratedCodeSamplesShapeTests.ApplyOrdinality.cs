using System;
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("i.Name", typeof(string)),
            ("Number", typeof(int)),
            ("NumberOrdinal", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["left", 1, 0],
            ["left", 2, 1],
            ["right", 3, 0]);
    }

    [TestMethod]
    public void ApplyWithOrdinalitySample_WhenCheckedIn_ShouldUseIndexedLoopWithoutLinqProjection()
    {
        var sample = ReadSample(ApplyWithOrdinalitySampleFileName);

        Assert.Contains("EvaluationHelper.ConvertScalarEnumerableToTypedChunks<int>", sample.Content);
        Assert.Contains("foreach (var nChunk in statement0_nRows)", sample.Content);
        Assert.Contains("for (int nIndex = 0, nIndexCount = nChunk.Count; nIndex < nIndexCount; ++nIndex)", sample.Content);
        Assert.Contains("++nOrdinal;", sample.Content);
        Assert.IsFalse(sample.Content.Contains(".Select((", StringComparison.Ordinal), sample.Content);
        Assert.IsFalse(sample.Content.Contains("EvaluationHelper.SmartForEach(", StringComparison.Ordinal), sample.Content);
    }
}
